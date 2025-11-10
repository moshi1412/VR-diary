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
    private bool hasTriggered = false;
    private bool BallReachButtom=false;

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

    private void OnTriggerEnter(Collider other)
    {
       BallReachButtom=true;
    }
    private void OnTriggerExit(Collider other)
    {
       BallReachButtom=false;
       hasTriggered=false;
    }
    public void BallDelete()
    {
        
    }
    
}