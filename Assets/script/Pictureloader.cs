using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class RandomMemoryLoader : MonoBehaviour
{
    // （保留所有配置参数，与之前一致）
    [Header("配置")]
    public string jsonFilePath = "memories";
    public Transform containerParent;
    public string containerNamePrefix = "PicContainer";

    [Header("图片组件路径")]
    public string sourceImagePath = "SourceImage";
    public string subjectImagePath = "SubjectImage";

    [Header("漂浮效果参数")]
    public float floatRange = 0.05f;
    public float floatSpeed = 1.5f;

    void Start()
    {
        StartCoroutine(LoadRandomMemories());
    }

    // （保留其他方法，与之前一致）
    private IEnumerator LoadRandomMemories()
    {
        int containerCount = CountContainers();
        if (containerCount <= 0)
        {
            Debug.LogError("未找到Container！");
            yield break;
        }

        List<BallMemory.MemoryData> allMemories = LoadAllMemoriesFromJson();
        if (allMemories == null || allMemories.Count == 0)
        {
            Debug.LogError("JSON无有效数据！");
            yield break;
        }

        List<BallMemory.MemoryData> randomMemories = allMemories
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(containerCount)
            .ToList();

        Transform[] containers = GetAllContainers();
        for (int i = 0; i < containers.Length && i < randomMemories.Count; i++)
        {
            LoadMemoryToContainer(randomMemories[i], containers[i]);
            yield return null;
        }
    }

    private int CountContainers()
    {
        return GetAllContainers().Length;
    }

    private Transform[] GetAllContainers()
    {
        List<Transform> containers = new List<Transform>();
        foreach (Transform child in containerParent)
        {
            if (child.name.Contains(containerNamePrefix))
                containers.Add(child);
        }
        return containers.ToArray();
    }

    private List<BallMemory.MemoryData> LoadAllMemoriesFromJson()
    {
        string fullPath = jsonFilePath;
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"JSON不存在：{fullPath}");
            return null;
        }

        string json = File.ReadAllText(fullPath);
        MemoryDataWrapper wrapper = JsonUtility.FromJson<MemoryDataWrapper>(json);
        return wrapper?.dataList;
    }

    /// <summary>
    /// 修正：container是Transform，需通过.gameObject获取GameObject再AddComponent
    /// </summary>
    private void LoadMemoryToContainer(BallMemory.MemoryData data, Transform container)
    {
        // 处理图片路径（与之前一致）
        Debug.Log(data.picturepath);
        string dirPath = Path.GetDirectoryName(data.picturepath);
        string fileName = Path.GetFileNameWithoutExtension(data.picturepath);
        string extension = Path.GetExtension(data.picturepath);

        string sourceImgPath = Path.Combine(dirPath, $"{fileName}1{extension}");
        string subjectImgPath = Path.Combine(dirPath, $"{fileName}1_no_bg{extension}");

        // 加载图片到RawImage（与之前一致）
        RawImage sourceImage = container.Find(sourceImagePath)?.GetComponent<RawImage>();
        RawImage subjectImage = container.Find(subjectImagePath)?.GetComponent<RawImage>();

        if (sourceImage != null)
            LoadImageToRawImage(sourceImgPath, sourceImage);
        if (subjectImage != null)
            LoadImageToRawImage(subjectImgPath, subjectImage);

        // 错误修正：container.gameObject.AddComponent（关键修改）
        if (container.GetComponent<PicFloatEffect>() == null)
        {
            // 修正点：通过Transform获取其所属的GameObject，再添加组件
            PicFloatEffect floatEffect = container.gameObject.AddComponent<PicFloatEffect>();
           
        }
        container.GetComponent<PicFloatEffect>().Init(floatRange, floatSpeed);
        container.GetComponent<PicFloatEffect>().memoryId=data.memoryId;
        Debug.Log($"Container {container.name} 已加载记录：memoryId={data.memoryId}");

    }

    private void LoadImageToRawImage(string path, RawImage rawImage)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"图片不存在：{path}");
            return;
        }

        byte[] imgData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(imgData))
        {
            rawImage.texture = tex;
            RectTransform rect = rawImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(tex.width, tex.height);

            float maxSize = 500;
            if (rect.sizeDelta.x > maxSize || rect.sizeDelta.y > maxSize)
            {
                float scale = Mathf.Min(maxSize / rect.sizeDelta.x, maxSize / rect.sizeDelta.y);
                rect.sizeDelta *= scale;
            }
        }
    }

    [Serializable]
    private class MemoryDataWrapper
    {
        public List<BallMemory.MemoryData> dataList;
    }
}