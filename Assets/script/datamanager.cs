using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public string dataPath;
    private List<BallMemory.MemoryData?> currentLabelDataList = new List<BallMemory.MemoryData?>();
    public GameObject BallOnProcess = null;

    [Serializable]
    private class DataWrapper
    {
        public List<BallMemory.MemoryData?> dataList;
    }

    private void Awake()
    {
        Debug.Log(Application.persistentDataPath);
        if(dataPath=="")
            dataPath = Path.Combine(Application.persistentDataPath, "playerData.json");
    }

    // 保存数据（自动生成唯一ID）
    public void AddData(BallMemory.MemoryData? newData)
    {
        // 检查新数据是否有效
        if (!newData.HasValue)
        {
            Debug.LogWarning("Cannot save null data!");
            return;
        }

        // 强制要求label不为空
        if (string.IsNullOrEmpty(newData.Value.label))
        {
            Debug.LogWarning("Data label cannot be empty! Data will not be saved.");
            return;
        }

        // 1. 读取所有现有数据
        List<BallMemory.MemoryData?> allData = LoadAllDataFromFile();

        // 2. 生成唯一ID（取现有最大ID + 1，若没有数据则从1开始）
        int newId = GenerateUniqueId(allData);

        // 3. 复制新数据到临时变量，修改其ID（解决可空结构体无法直接修改的问题）
        BallMemory.MemoryData tempData = newData.Value; // 先获取非空结构体
        tempData.memoryId = newId; // 赋值新ID
        BallMemory.MemoryData? dataWithId = tempData; // 转回可空类型

        // 4. 添加带新ID的数据到列表并保存
        allData.Add(dataWithId);
        SaveAllDataToFile(allData);

        Debug.Log($"Saved data with ID: {newId}, Label: {dataWithId.Value.label}");
    }

    // 生成唯一ID（核心逻辑）
    private int GenerateUniqueId(List<BallMemory.MemoryData?> dataList)
    {
        int maxId = 0;

        foreach (var data in dataList)
        {
            // 跳过空数据，只处理有值的条目
            if (data.HasValue && data.Value.memoryId > maxId)
            {
                maxId = data.Value.memoryId; // 更新最大ID
            }
        }

        // 新ID = 最大ID + 1（确保唯一）
        return maxId + 1;
    }

    // 核心功能：通过label查找数据
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
            if (!data.HasValue) continue; // 跳过空数据
            if (string.IsNullOrEmpty(data.Value.label)) continue;

            bool isMatch = exactMatch 
                ? string.Equals(data.Value.label, targetLabel, StringComparison.OrdinalIgnoreCase)
                : data.Value.label.IndexOf(targetLabel, StringComparison.OrdinalIgnoreCase) >= 0;

            if (isMatch)
            {
                currentLabelDataList.Add(data);
            }
        }

        Debug.Log($"Found {currentLabelDataList.Count} data entries matching label: {targetLabel}");
        return currentLabelDataList;
    }

    // 获取最近一次搜索结果
    public List<BallMemory.MemoryData?> GetLastLabelSearchResults()
    {
        return currentLabelDataList;
    }

    // 根据ID删除数据
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

    // 读取所有数据
    public List<BallMemory.MemoryData?> LoadAllDataFromFile()
    {
        if (File.Exists(dataPath))
        {
            string json = File.ReadAllText(dataPath);
            DataWrapper wrapper = JsonUtility.FromJson<DataWrapper>(json);
            return wrapper.dataList ?? new List<BallMemory.MemoryData?>();
        }
        return new List<BallMemory.MemoryData?>();
    }

    // 保存所有数据
    private void SaveAllDataToFile(List<BallMemory.MemoryData?> dataList)
    {
        DataWrapper wrapper = new DataWrapper { dataList = dataList };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(dataPath, json);
    }
}