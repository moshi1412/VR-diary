using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AudioAnalyzer : MonoBehaviour
{
    // 硅基流动API配置
    [Header("硅基流动API配置")]
    private string siliconFlowApiKey = "sk-hrxhrcovururgimnfqxwoggwysrzgahwaplycsyowitwgxnf";

    // 百度智能云API配置
    [Header("百度智能云API配置")]
    private string baiduApiKey = "UbjdHrXLaZWF3B0jiuBCOvfx";
    private string baiduSecretKey = "qXm1FPw9RtLzyq0ryqJ9tf4bnH47uMEC";

    // 音频文件路径（外部传入）
    [Header("文件路径")]
    public string audioFilePath = "";

    // 分析结果回调（新增bool参数表示是否处理成功）
    public Action<string, string, string, bool> onAnalysisCompleted; // 参数：情绪结果、关键词、合并标签、是否处理成功

    /// <summary>
    /// 外部调用此方法开始处理音频并获取结果
    /// </summary>
    /// <param name="onCompleted">回调函数：(情绪结果, 关键词字符串, 合并标签字符串, 是否处理成功)</param>
    public void ProcessAudioAndGetResults(Action<string, string, string, bool> onCompleted)
    {
        onAnalysisCompleted = onCompleted;

        // 参数校验（失败场景均返回处理失败）
        if (string.IsNullOrEmpty(siliconFlowApiKey))
        {
            InvokeCallback("错误：请设置硅基流动API密钥", "", "", false);
            return;
        }

        if (string.IsNullOrEmpty(baiduApiKey) || string.IsNullOrEmpty(baiduSecretKey))
        {
            InvokeCallback("错误：请设置百度智能云API密钥和Secret Key", "", "", false);
            return;
        }
        if (audioFilePath is "")
        {
            DataManager dm = GameObject.FindWithTag("DataManager").GetComponent<DataManager>();
            BallMemory.MemoryData? md = dm.BallOnProcess.GetComponent<BallMemory>().BallData;
            if (!md.HasValue)
            {
                InvokeCallback($"错误：球中不存在记录", "", "", false);
                return;
            }
            audioFilePath = md.Value.recordingpath;
        }
        if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
        {
            InvokeCallback($"错误：音频文件不存在 → {audioFilePath}", "", "", false);
            return;
        }

        StartCoroutine(ProcessAudioCoroutine());
    }

    /// <summary>
    /// 处理音频的协程
    /// </summary>
    private IEnumerator ProcessAudioCoroutine()
    {
        // 1. 音频转文字
        string recognizedText = null;
        yield return StartCoroutine(AudioToText(audioFilePath, (result) => recognizedText = result));

        if (string.IsNullOrEmpty(recognizedText))
        {
            InvokeCallback("识别失败", "", "[UNITY_RESULT]|识别失败|无", false);
            yield break;
        }

        Debug.Log($"[UNITY_RESULT]|{recognizedText}|无");

        // 2. 文本分析
        AnalysisResult analysisResult = null;
        yield return StartCoroutine(AnalyzeText(recognizedText, baiduApiKey, baiduSecretKey, (result) => analysisResult = result));

        // 3. 生成结果并返回
        if (analysisResult != null)
        {
            string sentimentResult = string.Join("、", analysisResult.sentimentTags);
            string keywords = string.Join("、", analysisResult.keywordTags);
            string combinedTags = GenerateCombinedTags(sentimentResult, keywords);
            
            // 完整流程成功，返回处理成功
            InvokeCallback(sentimentResult, keywords, combinedTags, true);
        }
        else
        {
            InvokeCallback("文本分析失败", "", "文本分析失败，无法生成标签", false);
        }
    }

    /// <summary>
    /// 生成合并的标签字符串
    /// </summary>
    private string GenerateCombinedTags(string sentiment, string keywords)
    {
        return $"{sentiment}  {keywords}";
    }

    /// <summary>
    /// 调用回调函数（带处理成功标识）
    /// </summary>
    private void InvokeCallback(string sentiment, string keywords, string combined, bool isSuccess)
    {
        onAnalysisCompleted?.Invoke(sentiment, keywords, combined, isSuccess);
        onAnalysisCompleted = null; // 清空回调避免重复调用
    }

    /// <summary>
    /// 音频转文字（硅基流动API）
    /// </summary>
    public IEnumerator AudioToText(string audioPath, Action<string> onComplete)
    {
        string url = "https://api.siliconflow.cn/v1/audio/transcriptions";

        WWWForm form = new WWWForm();
        form.AddField("model", "FunAudioLLM/SenseVoiceSmall");

        byte[] audioData;
        try
        {
            audioData = File.ReadAllBytes(audioPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"读取音频文件失败：{e.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        form.AddBinaryData("file", audioData, Path.GetFileName(audioPath));

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.SetRequestHeader("Authorization", $"Bearer {siliconFlowApiKey}");

        Debug.Log($"开始识别音频：{audioPath}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var responseJson = JsonUtility.FromJson<SiliconFlowResponse>(request.downloadHandler.text);
                onComplete?.Invoke(responseJson.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"解析响应失败：{e.Message}");
                onComplete?.Invoke(null);
            }
        }
        else
        {
            Debug.LogError($"识别失败：{request.error}");
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }

    /// <summary>
    /// 文本分析（百度API）
    /// </summary>
    private IEnumerator AnalyzeText(string text, string apiKey, string secretKey, Action<AnalysisResult> onComplete)
    {
        if (string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // 获取访问令牌
        string accessToken = null;
        yield return StartCoroutine(GetBaiduAccessToken(apiKey, secretKey, (token) => accessToken = token));

        if (string.IsNullOrEmpty(accessToken))
        {
            onComplete?.Invoke(null);
            yield break;
        }

        // 情绪分析
        SentimentResult sentimentResult = null;
        yield return StartCoroutine(AnalyzeSentiment(text, accessToken, (result) => sentimentResult = result));

        // 关键词提取
        KeywordResult keywordResult = null;
        yield return StartCoroutine(ExtractKeywords(text, accessToken, (result) => keywordResult = result));

        // 整理结果
        AnalysisResult result = new AnalysisResult();
        
        if (sentimentResult != null && sentimentResult.items != null && sentimentResult.items.Length > 0)
        {
            int sentiment = sentimentResult.items[0].sentiment;
            string[] sentimentLabels = { "负面", "中性", "正面" };
            if (sentiment >= 0 && sentiment < sentimentLabels.Length)
            {
                result.sentimentTags.Add(sentimentLabels[sentiment]);
            }
        }

        if (keywordResult != null && keywordResult.items != null)
        {
            foreach (var item in keywordResult.items)
            {
                result.keywordTags.Add(item.tag);
            }
        }

        onComplete?.Invoke(result);
    }

    /// <summary>
    /// 获取百度API访问令牌
    /// </summary>
    private IEnumerator GetBaiduAccessToken(string apiKey, string secretKey, Action<string> onComplete)
    {
        string url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonUtility.FromJson<BaiduTokenResponse>(request.downloadHandler.text);
                onComplete?.Invoke(response.access_token);
            }
            catch
            {
                onComplete?.Invoke(null);
            }
        }
        else
        {
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }

    /// <summary>
    /// 情绪分析
    /// </summary>
    private IEnumerator AnalyzeSentiment(string text, string accessToken, Action<SentimentResult> onComplete)
    {
        string url = $"https://aip.baidubce.com/rpc/2.0/nlp/v1/sentiment_classify?access_token={accessToken}";

        string limitedText = text.Length > 2000 ? text.Substring(0, 2000) : text;
        var data = new SentimentRequestData { text = limitedText };
        string jsonData = JsonUtility.ToJson(data);

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var result = JsonUtility.FromJson<SentimentResult>(request.downloadHandler.text);
                onComplete?.Invoke(result);
            }
            catch
            {
                onComplete?.Invoke(null);
            }
        }
        else
        {
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }

    /// <summary>
    /// 提取关键词（严格适配百度接口规范）
    /// </summary>
    private IEnumerator ExtractKeywords(string text, string accessToken, Action<KeywordResult> onComplete, int num = 3)
    {
        // 1. 接口URL（拼接access_token和编码参数）
        string url = $"https://aip.baidubce.com/rpc/2.0/nlp/v1/txt_keywords_extraction?access_token={accessToken}&charset=UTF-8";
        Debug.Log("待提取关键词文本:" + text);

        // 2. 构建请求体（严格匹配接口的text数组和num字段）
        var requestData = new BaiduKeywordRequestData
        {
            text = new string[] { text }, // 接口要求text为数组类型
            num = num                     // 可选：关键词数量，默认5个
        };
        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log("请求体JSON:" + jsonData);

        // 3. 构建POST请求
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 4. 设置请求头
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        // 5. 发送请求并等待响应
        yield return request.SendWebRequest();

        // 6. 处理响应结果
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("关键词提取接口调用成功，响应内容:" + request.downloadHandler.text);
            try
            {
                // 解析响应（完全匹配百度返回格式）
                var result = JsonUtility.FromJson<BaiduKeywordResult>(request.downloadHandler.text);
                if (result != null && result.results != null && result.results.Length > 0)
                {
                    onComplete?.Invoke(new KeywordResult
                    {
                        log_id = result.log_id,
                        items = System.Array.ConvertAll(result.results, item => new KeywordItem
                        {
                            tag = item.word,
                            score = item.score
                        })
                    });
                }
                else
                {
                    Debug.LogError("关键词提取结果为空或格式异常");
                    onComplete?.Invoke(null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("关键词结果解析失败:" + e.Message + "，响应内容:" + request.downloadHandler.text);
                onComplete?.Invoke(null);
            }
        }
        else
        {
            Debug.LogError($"关键词提取接口调用失败！错误码:{request.result}，错误信息:{request.error}");
            Debug.LogError($"服务器返回内容:{request.downloadHandler.text}");
            onComplete?.Invoke(null);
        }

        // 7. 释放资源
        request.Dispose();
    }

    #region 百度接口数据模型（完全匹配官方返回）
    /// <summary>
    /// 百度关键词提取请求体
    /// </summary>
    [Serializable]
    private class BaiduKeywordRequestData
    {
        public string[] text; // 必选：原文本内容（数组格式）
        public int num;       // 可选：需要提取的关键词数量最大值
    }

    /// <summary>
    /// 百度关键词提取原始响应
    /// </summary>
    [Serializable]
    private class BaiduKeywordResult
    {
        public long log_id;          // 请求唯一标识码
        public KeywordRawItem[] results; // 关键词提取结果数组
    }

    /// <summary>
    /// 百度关键词原始项（word和score字段）
    /// </summary>
    [Serializable]
    private class KeywordRawItem
    {
        public float score; // 关键词置信度
        public string word; // 提取出的关键词
    }

    /// <summary>
    /// 对外暴露的关键词结果（保持原有结构，便于兼容）
    /// </summary>
    [Serializable]
    public class KeywordResult
    {
        public long log_id;        // 请求唯一标识码
        public KeywordItem[] items; // 关键词结果数组
    }

    /// <summary>
    /// 对外暴露的关键词项
    /// </summary>
    [Serializable]
    public class KeywordItem
    {
        public string tag;   // 提取出的关键词（与原word字段映射）
        public float score;  // 关键词置信度
    }
    #endregion

    // 以下是原有其他数据模型（保持不变）
    [Serializable]
    private class SiliconFlowResponse { public string text; }

    [Serializable]
    private class BaiduTokenResponse { public string access_token; }

    [Serializable]
    private class SentimentRequestData { public string text; }

    [Serializable]
    private class SentimentResult { public SentimentItem[] items; }

    [Serializable]
    private class SentimentItem { public int sentiment; }

    [Serializable]
    private class AnalysisResult
    {
        public List<string> sentimentTags = new List<string>();
        public List<string> keywordTags = new List<string>();
    }
}