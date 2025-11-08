using UnityEngine;
using System;
using System.IO;



public class DataManager : MonoBehaviour
{
    private string dataPath;
    public MemoryData data; // 这里使用MemoryData类型
    // 确保你已经定义了MemoryData结构体（可序列化）
    [Serializable]
    public struct MemoryData
    {
        // 这里放你的数据字段，例如：
        public int memoryId;
        public string description;
        public string createTime;
        public Vector3[] historyPositions;
    }
    private void Awake()
    {
        dataPath = Path.Combine(Application.persistentDataPath, "playerData.json");
    }

    // 修正：为参数添加类型声明（MemoryData）
    public void SaveData(MemoryData dataToSave)
    {
        string json = JsonUtility.ToJson(dataToSave, true);
        File.WriteAllText(dataPath, json);
        Debug.Log("数据已保存到：" + dataPath);
    }

    // 修正：返回类型改为MemoryData（与你要读取的数据类型一致）
    public MemoryData LoadData()
    {
        if (File.Exists(dataPath))
        {
            string json = File.ReadAllText(dataPath);
            // 反序列化为MemoryData类型
            return JsonUtility.FromJson<MemoryData>(json);
        }
        else
        {
            Debug.LogWarning("数据文件不存在，返回默认数据");
            return new MemoryData(); // 返回默认值
        }
    }
}