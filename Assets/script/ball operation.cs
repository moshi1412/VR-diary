using UnityEngine;

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
    public int ballnum;
    private void Start()
    {
            spawnPosition=prefabToSpawn.transform;
            spawnPosition.position = new Vector3(15.7399998f,-10.04f,20.8700008f);

    }

    public void BallGenerate()
    {
        Debug.Log("Start new generate ball");
        if (hasTriggered && triggerOnce) return;
        Instantiate(prefabToSpawn, spawnPosition.position, spawnPosition.rotation);
        hasTriggered = true;
    }

    public void GenerateMultipleBalls()
    {
        for (int i = 0; i < ballnum; i++)
        {
            Invoke("CallFunctionAfterDelay", 1f);
        }
    }
    
}