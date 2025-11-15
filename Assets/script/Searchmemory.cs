using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // 需确保项目已导入Newtonsoft.Json

public class LabelSearchHandler : MonoBehaviour
{
    [Header("基础配置")]
    public DataManager dataManager;
    public Toggle ButtonToggle;
    public string LabelBySpeak;
    // 存储所有匹配label且重排得分达标的记录
    private List<BallMemory.MemoryData?> matchedLabelRecords = new List<BallMemory.MemoryData?>();
    private BallOperation ballop;
    public GameObject BallList;

    [Header("SiliconFlow 重排模型配置")]
    public string siliconFlowToken = "sk-hrxhrcovururgimnfqxwoggwysrzgahwaplycsyowitwgxnf"; // 替换为你的SiliconFlow Token
    public float rerankThreshold = 0.5f; // 相关性阈值（得分>此值才视为相关）
    private readonly string rerankApiUrl = "https://api.siliconflow.cn/v1/rerank";
    private readonly string rerankModel = "BAAI/bge-reranker-v2-m3";
    private HttpClient httpClient; // 复用HTTP客户端提升效率

    private void Start()
    {
        ballop = GameObject.FindWithTag("BallManager").GetComponent<BallOperation>();
        
        // 初始化HTTP客户端（设置超时和默认请求头）
        httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10); // 10秒超时防止阻塞
        httpClient.DefaultRequestHeaders.Add(
            "Authorization", 
            $"Bearer {siliconFlowToken}"
        );
    }

    // 改为异步方法，避免调用API时阻塞主线程
    public async void IfWhenChanged()
    {
        Debug.Log("Start Search");
        bool ifsuccess = await SearchByLabelWithRerankAndThreshold(); // 调用带阈值筛选的搜索方法
        if (!ifsuccess)
        {
            Debug.Log("not found");
            return;
        }

        // 原有逻辑：检查匹配记录对应的球是否已在场景中存在（保持不变）
        bool hasExistingBalls = false;
        foreach (var data in matchedLabelRecords)
        {
            if (data.HasValue)
            {
                // 构建球的名称："Ball"+memoryId.ToString()
                string ballName = "Ball" + data.Value.memoryId.ToString();
                // 查找场景中是否存在该名称的球（从BallList子物体中找）
                Transform existingBallTrans = BallList.transform.Find(ballName);
                if (existingBallTrans != null)
                {
                    hasExistingBalls = true;
                    // 调用FallDown方法
                    FallDown(existingBallTrans.gameObject);
                }
            }
        }

        // 若没有任何已存在的球，则生成新球（原有逻辑不变）
        if (!hasExistingBalls)
        {
            ballop.GenerateMultipleBalls(matchedLabelRecords);
        }

        // ButtonToggle.isOn=false;
    }

    /// <summary>
    /// 带重排模型+阈值筛选的标签搜索（异步方法）
    /// </summary>
    /// <returns>是否找到匹配且达标的记录</returns>
    private async Task<bool> SearchByLabelWithRerankAndThreshold()
    {
        // 1. 清空之前的搜索结果
        matchedLabelRecords.Clear();
        if (string.IsNullOrEmpty(LabelBySpeak))
        {
            Debug.LogWarning("Search label is empty!");
            return false;
        }

        // 2. 从DataManager获取所有数据并筛选标签匹配的记录（原有逻辑不变）
        List<BallMemory.MemoryData?> labelMatchedData = dataManager.LoadAllDataFromFile();
        // List<BallMemory.MemoryData?> labelMatchedData = new List<BallMemory.MemoryData?>();
        
        // Debug.Log($"label:{LabelBySpeak}");
        // foreach (var data in allData)
        // {
        //     if (data.HasValue) // 空值判断
        //     {
        //         Debug.Log($"label in data:{data.Value.label}");
        //         // 不区分大小写匹配标签
        //         if (string.Equals(data.Value.label, LabelBySpeak, StringComparison.OrdinalIgnoreCase))
        //         {
        //             labelMatchedData.Add(data);
        //         }
        //     }
        // }

        // 若没有匹配的标签记录，直接返回
        if (labelMatchedData.Count == 0)
        {
            Debug.Log("0 memories fetched");
            return false;
        }

        // 3. 调用重排模型，获取排序结果并按阈值筛选
        List<BallMemory.MemoryData?> qualifiedData = await CallRerankModelAndFilterByThreshold(LabelBySpeak, labelMatchedData);
        // 用达标后的结果更新最终列表
        matchedLabelRecords = qualifiedData;

        Debug.Log($"{matchedLabelRecords.Count} qualified memories found (score > {rerankThreshold})");
        return matchedLabelRecords.Count > 0;
    }

    /// <summary>
    /// 调用SiliconFlow重排模型+阈值筛选（核心新增逻辑）
    /// </summary>
    /// <param name="query">搜索关键词（LabelBySpeak）</param>
    /// <param name="candidateData">标签匹配的候选记录</param>
    /// <returns>得分达标（>阈值）的记录列表（已排序）</returns>
    private async Task<List<BallMemory.MemoryData?>> CallRerankModelAndFilterByThreshold(string query, List<BallMemory.MemoryData?> candidateData)
    {
        try
        {
            // 3.1 构建模型需要的documents列表（将记录转为文本内容）
            List<string> documents = new List<string>();
            foreach (var data in candidateData)
            {
                if (data.HasValue)
                {
                    // 拼接记录的核心信息（供模型判断相关性）
                    string docContent = data.Value.label;
                    documents.Add(docContent);
                }
            }

            // 3.2 构造API请求体（符合SiliconFlow接口格式）
            // Debug.Log(query);
            Debug.Log(documents[0]);
            var rerankRequest = new
            {
                model = rerankModel,
                query = query,
                documents = documents,
                top_n = candidateData.Count // 返回所有候选记录的排序结果
            };
            string requestJson = JsonConvert.SerializeObject(rerankRequest);
            var httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // 3.3 发送POST请求并获取响应
            HttpResponseMessage response = await httpClient.PostAsync(rerankApiUrl, httpContent);
            response.EnsureSuccessStatusCode(); // 状态码>=400时抛出异常
            string responseJson = await response.Content.ReadAsStringAsync();
            Debug.Log(responseJson);
            // 3.4 解析API响应结果
            RerankApiResponse rerankResponse = JsonConvert.DeserializeObject<RerankApiResponse>(responseJson);
            if (rerankResponse?.results == null || rerankResponse.results.Count == 0)
            {
                Debug.LogWarning("Rerank model returned empty results, use original data");
                return FilterByThreshold(candidateData, documents, null); // 重排失败时直接按默认得分筛选
            }
            Debug.Log(candidateData.Count);
            // 3.5 按重排结果+阈值筛选达标记录
            return FilterByThreshold(candidateData, documents, rerankResponse.results);
        }
        catch (HttpRequestException ex)
        {
            Debug.LogError($"Rerank API request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Rerank response parse failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unknown error in rerank: {ex.Message}");
        }

        // 任何异常都返回原始标签匹配数据（按默认得分筛选）
        return FilterByThreshold(candidateData, null, null);
    }

    /// <summary>
    /// 按阈值筛选记录（统一处理逻辑：重排成功/失败都走这里）
    /// </summary>
    /// <param name="candidateData">候选记录</param>
    /// <param name="documents">记录对应的文本内容</param>
    /// <param name="rerankResults">重排结果（为null时表示重排失败）</param>
    /// <returns>达标记录列表</returns>
    private List<BallMemory.MemoryData?> FilterByThreshold(List<BallMemory.MemoryData?> candidateData, List<string> documents, List<RerankResult> rerankResults)
    {
        List<BallMemory.MemoryData?> qualifiedData = new List<BallMemory.MemoryData?>();

        if (rerankResults != null)
        {
            // 重排成功：按模型返回的得分筛选
            foreach (var result in rerankResults)
            {

                Debug.Log(result.index);
                int originalIndex = result.index;
                if (originalIndex >= 0 && originalIndex < candidateData.Count)
                {
                    // 只保留得分>阈值的记录
                    if (result.relevance_score > rerankThreshold)
                    {
                        qualifiedData.Add(candidateData[originalIndex]);
                        Debug.Log($"Qualified - Index:{originalIndex}, Score:{result.relevance_score:F4} (>{rerankThreshold}), Content:{documents[originalIndex]}");
                    }
                    else
                    {
                        Debug.Log($"Rejected - Index:{originalIndex}, Score:{result.relevance_score:F4} (<={rerankThreshold}), Content:{documents[originalIndex]}");
                    }
                }
            }
        }
        else
        {
            // 重排失败：默认所有记录得分视为1.0（全部达标，兼容原有逻辑）
            foreach (var data in candidateData)
            {
                qualifiedData.Add(data);
                Debug.Log($"Rerank failed, add all data (default score = 1.0 > {rerankThreshold})");
            }
        }

        return qualifiedData;
    }

    /// <summary>
    /// 球下落方法（原有逻辑不变）
    /// </summary>
    /// <param name="targetBall">要下落的球对象</param>
    private void FallDown(GameObject targetBall)
    {
        Debug.Log($"调用FallDown方法，目标球：{targetBall.name}");
        targetBall.GetComponent<BallMovementController>().SwitchToFallMode();
    }

    /// <summary>
    /// 获取搜索到的所有匹配记录（原有逻辑不变）
    /// </summary>
    /// <returns>匹配的记录列表（返回副本）</returns>
    public List<BallMemory.MemoryData?> GetMatchedRecords()
    {
        return new List<BallMemory.MemoryData?>(matchedLabelRecords);
    }

    /// <summary>
    /// 获取匹配记录的数量（原有逻辑不变）
    /// </summary>
    public int GetMatchedCount()
    {
        return matchedLabelRecords.Count;
    }

    // 原有方法不变
    public void DeleteLabel()
    {
        matchedLabelRecords = new List<BallMemory.MemoryData?>();
        LabelBySpeak = "";
    }

    // 销毁时释放HTTP客户端资源（新增，避免内存泄漏）
    private void OnDestroy()
    {
        httpClient?.Dispose();
    }
}

// ---------------------- 重排模型响应解析类（新增） ----------------------
/// <summary>
/// 匹配SiliconFlow重排API的响应格式
/// </summary>
[Serializable]
public class RerankApiResponse
{
    public string id; // 接口响应ID（可忽略）
    public List<RerankResult> results; // 重排结果列表
}

[Serializable]
public class RerankResult
{
    public int index; // 对应请求documents中的原始索引
    public float relevance_score; // 相关性得分（越高越相关）
    public string document; // 对应的文档内容（可忽略）
}