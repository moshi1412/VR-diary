using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class LabelSearchHandler:MonoBehaviour
{
    public DataManager dataManager;
    public  Toggle  ButtonToggle;
    public string LabelBySpeak;
    // 存储所有匹配label的记录
    private List<BallMemory.MemoryData?> matchedLabelRecords = new List<BallMemory.MemoryData?>();
    private BallOperation ballop;
    private void Start()
    {
        ballop=GameObject.FindWithTag("BallManager").GetComponent<BallOperation>();
    }
    public void IfWhenChanged()
    {
            Debug.Log("Start Search");
            bool ifsuccess=SearchByLabel();
            if(!ifsuccess)
            { 
                Debug.Log("not found");
                return;
            }
            ballop.GenerateMultipleBalls(matchedLabelRecords);
            // ButtonToggle.isOn=false;
    }
    
    /// <summary>
    /// 通过label查找所有匹配的记录
    /// </summary>
    /// <param name="label">要搜索的标签（外部传入）</param>
    /// <returns>是否找到匹配记录</returns>
    public bool SearchByLabel()
    {
        // 清空之前的搜索结果
        matchedLabelRecords.Clear();

        // 直接调用DataManager的公共方法获取所有数据（无需反射）
        List<BallMemory.MemoryData?> allData = dataManager.LoadAllDataFromFile();
        Debug.Log($"label:{LabelBySpeak}");
        // 遍历筛选匹配label的记录（注意：需先在MemoryData中添加label字段）
        foreach (var data in allData)
        {
            Debug.Log($"label in data:{data.Value.label}");
            // 不区分大小写匹配，若需精确匹配可改为 StringComparison.Ordinal
            if (string.Equals(data.Value.label, LabelBySpeak, StringComparison.OrdinalIgnoreCase))
            {
                matchedLabelRecords.Add(data);
            }
        }
        Debug.Log($"{matchedLabelRecords.Count} memories found");
        return matchedLabelRecords.Count > 0;
    }

    /// <summary>
    /// 获取搜索到的所有匹配记录
    /// </summary>
    /// <returns>匹配的记录列表（返回副本，避免外部修改内部数据）</returns>
    public List<BallMemory.MemoryData?> GetMatchedRecords()
    {
        return new List<BallMemory.MemoryData?>(matchedLabelRecords);
    }

    /// <summary>
    /// 获取匹配记录的数量
    /// </summary>
    public int GetMatchedCount()
    {
        return matchedLabelRecords.Count;
    }
    public void DeleteLabel(){
        matchedLabelRecords = new List<BallMemory.MemoryData?>();
        LabelBySpeak="";
    }
}