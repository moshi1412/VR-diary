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

    // 添加数据
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
        BallOnProcess.name="Ball"+newId.ToString();
        BallMemory.MemoryData tempData = newData.Value;
        tempData.memoryId = newId;
        if (string.IsNullOrEmpty(tempData.createTime))
            tempData.createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
        BallMemory.MemoryData? dataWithId = tempData;

        allData.Add(dataWithId);
        SaveAllDataToFile(allData);

        Debug.Log($"Saved data with ID: {newId}, Label: {dataWithId.Value.label}");
    }

    // 生成唯一ID
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

    // 按标签搜索
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

    // 获取搜索结果
    public List<BallMemory.MemoryData?> GetLastLabelSearchResults()
    {
        return currentLabelDataList;
    }

    // 按ID删除
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

    // 读取数据（已添加videopath处理）
    public List<BallMemory.MemoryData?> LoadAllDataFromFile()
    {
        var result = new List<BallMemory.MemoryData?>();
        if (!File.Exists(dataPath))
            return result;

        try
        {
            string json = File.ReadAllText(dataPath);
            JObject jsonObj = JObject.Parse(json);
            JArray dataArray = jsonObj["dataList"] as JArray;

            if (dataArray == null)
                return result;

            foreach (var item in dataArray)
            {
                if (item.Type == JTokenType.Null)
                {
                    result.Add(null);
                    continue;
                }

                // 读取数据时包含videopath
                BallMemory.MemoryData data = new BallMemory.MemoryData
                {
                    memoryId = item["memoryId"]?.Value<int>() ?? 0,
                    picturepath = item["picturepath"]?.Value<string>() ?? "",
                    recordingpath = item["recordingpath"]?.Value<string>() ?? "",
                    createTime = item["createTime"]?.Value<string>() ?? "",
                    label = item["label"]?.Value<string>() ?? "",
                    color = item["color"]?.Value<int>() ?? 0,
                    videopath = item["videopath"]?.Value<string>() ?? "" // 新增：读取videopath
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

    // 保存数据（已添加videopath处理）
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

                // 保存数据时包含videopath
                JObject item = new JObject
                {
                    { "memoryId", data.Value.memoryId },
                    { "picturepath", data.Value.picturepath },
                    { "recordingpath", data.Value.recordingpath },
                    { "createTime", data.Value.createTime },
                    { "label", data.Value.label },
                    { "color", data.Value.color },
                    { "videopath", data.Value.videopath } // 新增：保存videopath
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