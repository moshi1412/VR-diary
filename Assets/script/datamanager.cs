using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq; // 需导入Newtonsoft.Json

public class DataManager : MonoBehaviour
{
    public string dataPath;
    private List<BallMemory.MemoryData?> currentLabelDataList = new List<BallMemory.MemoryData?>();
    public GameObject BallOnProcess = null;

    private void Awake()
    {
        Debug.Log(Application.persistentDataPath);
        if (string.IsNullOrEmpty(dataPath))
            dataPath = Path.Combine(Application.persistentDataPath, "playerData.json");
    }

    // 原有方法名不变：添加数据
    public void AddData(BallMemory.MemoryData? newData)
    {
        if (!newData.HasValue)
        {
            Debug.LogWarning("Cannot save null data!");
            return;
        }

        if (string.IsNullOrEmpty(newData.Value.label))
        {
            Debug.LogWarning("Data label cannot be empty! Data will not be saved.");
            return;
        }

        List<BallMemory.MemoryData?> allData = LoadAllDataFromFile();
        int newId = GenerateUniqueId(allData);

        BallMemory.MemoryData tempData = newData.Value;
        tempData.memoryId = newId;
        if (string.IsNullOrEmpty(tempData.createTime))
            tempData.createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
        BallMemory.MemoryData? dataWithId = tempData;

        allData.Add(dataWithId);
        SaveAllDataToFile(allData);

        Debug.Log($"Saved data with ID: {newId}, Label: {dataWithId.Value.label}");
    }

    // 原有方法名不变：生成唯一ID
    private int GenerateUniqueId(List<BallMemory.MemoryData?> dataList)
    {
        int maxId = 0;
        foreach (var data in dataList)
        {
            if (data.HasValue && data.Value.memoryId > maxId)
            {
                maxId = data.Value.memoryId;
            }
        }
        return maxId + 1;
    }

    // 原有方法名不变：按标签搜索
    public List<BallMemory.MemoryData?> SearchByLabel(string targetLabel, bool exactMatch = false)
    {
        currentLabelDataList.Clear();

        if (string.IsNullOrEmpty(targetLabel))
        {
            Debug.LogWarning("Search label cannot be null or empty!");
            return currentLabelDataList;
        }

        List<BallMemory.MemoryData?> allData = LoadAllDataFromFile();
        foreach (var data in allData)
        {
            if (!data.HasValue || string.IsNullOrEmpty(data.Value.label))
                continue;

            bool isMatch = exactMatch 
                ? string.Equals(data.Value.label, targetLabel, StringComparison.OrdinalIgnoreCase)
                : data.Value.label.IndexOf(targetLabel, StringComparison.OrdinalIgnoreCase) >= 0;

            if (isMatch)
                currentLabelDataList.Add(data);
        }

        Debug.Log($"Found {currentLabelDataList.Count} data entries matching label: {targetLabel}");
        return currentLabelDataList;
    }

    // 原有方法名不变：获取搜索结果
    public List<BallMemory.MemoryData?> GetLastLabelSearchResults()
    {
        return currentLabelDataList;
    }

    // 原有方法名不变：按ID删除
    public void DeleteDataById(int targetId)
    {
        List<BallMemory.MemoryData?> allData = LoadAllDataFromFile();
        int removedCount = allData.RemoveAll(data => data.HasValue && data.Value.memoryId == targetId);

        if (removedCount > 0)
        {
            SaveAllDataToFile(allData);
            Debug.Log($"Deleted data with ID: {targetId}");
            currentLabelDataList.RemoveAll(data => data.HasValue && data.Value.memoryId == targetId);
        }
        else
        {
            Debug.LogWarning($"Data with ID: {targetId} not found (delete failed)");
        }
    }

    // 原有方法名不变：读取数据（核心修改：无结构体解析）
    public List<BallMemory.MemoryData?> LoadAllDataFromFile()
    {
        var result = new List<BallMemory.MemoryData?>();
        if (!File.Exists(dataPath))
            return result;

        try
        {
            string json = File.ReadAllText(dataPath);
            JObject jsonObj = JObject.Parse(json); // 解析根对象
            JArray dataArray = jsonObj["dataList"] as JArray; // 获取数据数组

            if (dataArray == null)
                return result;

            foreach (var item in dataArray)
            {
                if (item.Type == JTokenType.Null)
                {
                    result.Add(null);
                    continue;
                }

                // 逐层读取字段（手动映射）
                BallMemory.MemoryData data = new BallMemory.MemoryData
                {
                    memoryId = item["memoryId"]?.Value<int>() ?? 0,
                    picturepath = item["picturepath"]?.Value<string>() ?? "",
                    recordingpath = item["recordingpath"]?.Value<string>() ?? "",
                    createTime = item["createTime"]?.Value<string>() ?? "",
                    label = item["label"]?.Value<string>() ?? "",
                    color = item["color"]?.Value<int>() ?? 0
                };
                result.Add(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"读取JSON失败: {e.Message}");
        }

        return result;
    }

    // 原有方法名不变：保存数据（核心修改：无结构体构建）
    private void SaveAllDataToFile(List<BallMemory.MemoryData?> dataList)
    {
        try
        {
            JObject jsonObj = new JObject();
            JArray dataArray = new JArray();

            foreach (var data in dataList)
            {
                if (!data.HasValue)
                {
                    dataArray.Add(JValue.CreateNull());
                    continue;
                }

                // 逐层构建JSON对象
                JObject item = new JObject
                {
                    { "memoryId", data.Value.memoryId },
                    { "picturepath", data.Value.picturepath },
                    { "recordingpath", data.Value.recordingpath },
                    { "createTime", data.Value.createTime },
                    { "label", data.Value.label },
                    { "color", data.Value.color }
                };
                dataArray.Add(item);
            }

            jsonObj["dataList"] = dataArray;
            string json = jsonObj.ToString(Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(dataPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存JSON失败: {e.Message}");
        }
    }
}