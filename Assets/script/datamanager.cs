using UnityEngine;
using System;
using System.IO;

using System.Collections.Generic; // 添加这一行，导入List所在的命名空间
public class DataManager : MonoBehaviour
{
    private string dataPath;
    // 存储唯一匹配的记忆数据（不再是列表）
    private MemoryData currentData;
    // 标记是否已找到有效数据
    private bool hasValidData = false;

    // 可序列化的记忆数据结构（保持不变）
    [Serializable]
    public struct MemoryData
    {
        public int memoryId; // 唯一标识，用于搜索
        public string description;
        public string picturepath;
        public string recordingpath;
        public string createTime;
    }

    // 用于JSON序列化的包装类（仍需保留，因文件存储多条数据）
    [Serializable]
    private class DataWrapper
    {
        public List<MemoryData> dataList; // 文件中仍存储多条数据，仅读取时筛选
    }

    private void Awake()
    {
        // 初始化数据路径
        Debug.Log(Application.persistentDataPath);
        dataPath = Path.Combine(Application.persistentDataPath, "playerData.json");
        // 初始化为空数据
        currentData = new MemoryData();
        hasValidData = false;
    }

    // 保存单条数据（覆盖式保存到文件，仍保留所有数据列表）
    public void AddData(MemoryData newData)
    {
        // 先读取文件中所有数据
        List<MemoryData> allData = LoadAllDataFromFile();
        
        // 移除相同ID的旧数据（避免重复）
        allData.RemoveAll(data => data.memoryId == newData.memoryId);
        // 添加新数据
        allData.Add(newData);
        
        // 保存更新后的所有数据到文件
        DataWrapper wrapper = new DataWrapper();
        wrapper.dataList = allData;
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(dataPath, json);
        
        Debug.Log("Saved data with ID: " + newData.memoryId + " to: " + dataPath);
        
        // 自动更新当前内存中的数据（如果保存的ID与当前查找的ID一致）
        if (currentData.memoryId == newData.memoryId)
        {
            currentData = newData;
            hasValidData = true;
        }
    }

    // 核心功能：通过memoryId从文件查找数据，赋值给currentData
    public bool FetchDataById(int targetId)
    {
        // 读取文件中所有数据
        List<MemoryData> allData = LoadAllDataFromFile();
        
        // 遍历查找匹配ID的数据
        foreach (var data in allData)
        {
            if (data.memoryId == targetId)
            {
                currentData = data; // 赋值给唯一的struct
                hasValidData = true;
                Debug.Log("Fetched data for ID: " + targetId);
                return true; // 查找成功
            }
        }
        
        // 未找到时重置数据
        currentData = new MemoryData();
        hasValidData = false;
        Debug.LogWarning("Data with ID: " + targetId + " not found");
        return false; // 查找失败
    }

    // 获取当前已匹配的唯一数据（需先调用FetchDataById成功）
    public MemoryData GetCurrentData()
    {
        if (!hasValidData)
        {
            Debug.LogWarning("No valid data fetched yet! Call FetchDataById first.");
        }
        return currentData;
    }

    // 检查是否有有效数据
    public bool HasValidData()
    {
        return hasValidData;
    }

    // 从文件删除指定ID的数据
    public void DeleteDataById(int targetId)
    {
        List<MemoryData> allData = LoadAllDataFromFile();
        int removeCount = allData.RemoveAll(data => data.memoryId == targetId);
        
        if (removeCount > 0)
        {
            // 保存删除后的所有数据到文件
            DataWrapper wrapper = new DataWrapper();
            wrapper.dataList = allData;
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(dataPath, json);
            
            Debug.Log("Deleted data with ID: " + targetId);
            
            // 如果删除的是当前存储的数据，重置状态
            if (currentData.memoryId == targetId)
            {
                currentData = new MemoryData();
                hasValidData = false;
            }
        }
        else
        {
            Debug.LogWarning("Data with ID: " + targetId + " not found (delete failed)");
        }
    }

    // 私有方法：从文件读取所有数据（仅用于内部筛选）
    private List<MemoryData> LoadAllDataFromFile()
    {
        if (File.Exists(dataPath))
        {
            string json = File.ReadAllText(dataPath);
            DataWrapper wrapper = JsonUtility.FromJson<DataWrapper>(json);
            return wrapper.dataList ?? new List<MemoryData>();
        }
        else
        {
            Debug.Log("Data file not exists, return empty list");
            return new List<MemoryData>();
        }
    }
}