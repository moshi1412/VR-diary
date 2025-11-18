using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq; 
using System.IO;
using System;
public class PhotoHoverGlow : MonoBehaviour
{
    [Header("发光配置")]
    public RawImage glowSubjectImage; // 上层发光图的RawImage
    public float defaultIntensity = 2f; // 默认强度
    public float hoverIntensity = 5f; // Hover时增强强度
    public float transitionSpeed = 10f; // 平滑过渡速度

    private Material glowMaterial;
    private float targetIntensity;

    void Start()
    {
        // 创建材质实例（避免全局修改）
        glowMaterial = new Material(glowSubjectImage.material);
        glowSubjectImage.material = glowMaterial;
        targetIntensity = defaultIntensity;
    }

    void Update()
    {
        // 平滑过渡发光强度
        float currentIntensity = glowMaterial.GetFloat("_GlowIntensity");
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * transitionSpeed);
        glowMaterial.SetFloat("_GlowIntensity", currentIntensity);
    }

    // Event Trigger - Pointer Enter触发（hover进入）
    public void OnHoverEnter()
    {
        Debug.Log("enter");
        targetIntensity = hoverIntensity;
        // 可选：同时加快闪烁速度
        glowMaterial.SetFloat("_BlinkSpeed", glowMaterial.GetFloat("_BlinkSpeed") * 1.5f);
    }

    // Event Trigger - Pointer Exit触发（hover离开）
    public void OnHoverExit()
    {
        Debug.Log("exit");
        targetIntensity = defaultIntensity;
        // 可选：恢复闪烁速度
        glowMaterial.SetFloat("_BlinkSpeed", glowMaterial.GetFloat("_BlinkSpeed") / 1.5f);
    }
    public void Ballgen()
    {
        // 1. 从漂浮组件（或数据组件）获取memoryId
        // 注意：建议优先从SubjectImageData组件获取，更符合数据存储逻辑
        int memoryId = -1;
        memoryId =transform.parent.gameObject.GetComponent<PicFloatEffect>().memoryId;
        // 2. 读取JSON文件，查找对应memoryId的MemoryData
        BallMemory.MemoryData? targetData = GetMemoryDataByID(memoryId);
        if (targetData == null)
        {
            Debug.LogError($"未找到memoryId={memoryId}对应的记录！");
            return;
        }

        // 3. 调用BallGenerate方法，传入找到的MemoryData
        BallOperation ballOp = GameObject.FindWithTag("BallManager").GetComponent<BallOperation>();
        if (ballOp != null)
        {
            ballOp.BallGenerate(targetData);
            Debug.Log($"已调用BallGenerate，传入memoryId={memoryId}的数据");
        }
        else
        {
            Debug.LogError("未找到BallManager或BallOperation组件！");
        }
    }

    /// <summary>
    /// 根据memoryId从JSON中查找对应的MemoryData
    /// </summary>
    private BallMemory.MemoryData? GetMemoryDataByID(int targetId)
    {
        // 读取JSON文件（路径需与之前的加载逻辑一致）
        string jsonPath = transform.parent.parent.gameObject.GetComponent<RandomMemoryLoader>().jsonFilePath; // 替换为你的JSON路径
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"JSON文件不存在：{jsonPath}");
            return null;
        }

        try
        {
            // 1. 读取JSON文本并解析为JObject
            string json = File.ReadAllText(jsonPath);
            JObject jsonObj = JObject.Parse(json);

            // 2. 获取dataList数组（转换为JArray）
            JArray dataArray = jsonObj["dataList"] as JArray;
            if (dataArray == null)
            {
                Debug.LogError("JSON中未找到dataList数组或其为空！");
                return null;
            }

            // 3. 遍历数组，查找目标memoryId
            foreach (JToken token in dataArray)
            {
                // 将JToken转换为BallMemory.MemoryData结构体
                BallMemory.MemoryData data = token.ToObject<BallMemory.MemoryData>();
                
                // 匹配memoryId
                if (data.memoryId == targetId)
                {
                    Debug.Log($"找到memoryId={targetId}的记录");
                    return data;
                }
            }

            // 4. 未找到匹配记录
            Debug.LogWarning($"未找到memoryId={targetId}的记录");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析JSON出错：{ex.Message}");
            return null;
        }
    }

}