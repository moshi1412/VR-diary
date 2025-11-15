using UnityEngine;
using System;

public class BallMemory : MonoBehaviour
{
    // 可空结构体（最终存储数据）
    public MemoryData? BallData;

    // 单独暴露在面板的字段（供编辑）
    [Header("结构体字段编辑区")]
    [Tooltip("唯一标识ID")]
    public int memoryId;
    [Tooltip("图片路径")]
    public string picturepath;
    [Tooltip("录音路径")]
    public string recordingpath;
    [Tooltip("创建时间")]
    public string createTime;
    [Tooltip("标签")]
    public string label;
    [Tooltip("颜色值")]
    public int color;
    [Tooltip("视频路径")]
    public string videopath;

    // 结构体定义
    [Serializable]
    public struct MemoryData
    {
        public int memoryId;
        public string picturepath;
        public string recordingpath;
        public string createTime;
        public string label;
        public int color;
        public string videopath;
    }

    private void Start()
    {
        // 在Start中初始化可空结构体，将面板编辑的值赋值给BallData
        BallData = new MemoryData
        {
            memoryId = this.memoryId,
            picturepath = this.picturepath,
            recordingpath = this.recordingpath,
            createTime = this.createTime,
            label = this.label,
            color = this.color,
            videopath = this.videopath
        };

        // 验证是否初始化成功
        if (BallData.HasValue)
        {
            Debug.Log($"BallData初始化成功：ID={BallData.Value.memoryId}，标签={BallData.Value.label}");
        }
    }
    public void DataUpdate(MemoryData? MData)
    {
        BallData=MData;
        
    }
}