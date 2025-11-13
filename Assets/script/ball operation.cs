using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class BallOperation : MonoBehaviour
{
    // 在Inspector中拖拽Prefab到此处
    public GameObject prefabToSpawn;
    // 生成物体的位置（可选，默认使用当前物体位置）
    private Transform spawnPosition;
    // 只触发一次（可选）
    public bool triggerOnce = true;
    // 标记是否已触发
    public bool hasTriggered = false;
    //public bool BallReachButtom=false;
    private void Start()
    {
            spawnPosition=prefabToSpawn.transform;
            spawnPosition.position = new Vector3(15.7399998f,-10.04f,20.8700008f);

    }

    public void BallGenerate( BallMemory.MemoryData? MData)
    {
        Debug.Log("Start new generate ball");
        if (hasTriggered && triggerOnce) return;
        GameObject newball=Instantiate(prefabToSpawn, spawnPosition.position, spawnPosition.rotation);
        hasTriggered = true;
        if(!MData.HasValue)
        {
            newball.GetComponent<BallMemory>().BallData=MData;
        }
    }

    public void GenerateMultipleBalls(List<BallMemory.MemoryData?> MDList)
    {
        StartCoroutine(GenerateCoroutine(MDList));  
    }
    private IEnumerator GenerateCoroutine(List<BallMemory.MemoryData?> dataList)
    {
        // 遍历列表中的每个元素
        for (int i = 0; i < dataList.Count; i++)
        {
            // 调用Generate函数，传入第i个元素
            BallGenerate(dataList[i]);
            
            // 等待1秒钟
            yield return new WaitForSeconds(0.8f);
        }
    }
}