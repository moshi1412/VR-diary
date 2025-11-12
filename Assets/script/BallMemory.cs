using UnityEngine;
using System;

public class BallMemory : MonoBehaviour
{

    public MemoryData? BallData;
    // public int memoryId; // 唯一标识，用于搜索
    // // public string description;
    // public string picturepath;
    // public string recordingpath;
    // public string createTime;
    // public string label;
    // public int color;
    [Serializable]
    public struct MemoryData
    {
        public int memoryId; // 仍保留ID用于唯一标识和操作
        public string picturepath;
        public string recordingpath;
        public string createTime;
        public string label; // 核心搜索字段
        public int color;
    }
}
