using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class BallOperation : MonoBehaviour
{
    // 在Inspector中拖拽Prefab到此处
    public GameObject prefabToSpawn;
    // 生成物体的位置（可选，默认使用当前物体位置）
    private Transform spawnPosition;
    private GameObject BallListObject;
    public Material pos;
    public Material neu;
    public Material neg;
    // 标记是否已触发
    //public bool hasTriggered = false;
    //public bool BallReachButtom=false;
    private void Start()
    {
            spawnPosition=prefabToSpawn.transform;
            spawnPosition.position = new Vector3(13.487f, -8.43f, 10.757f);
            BallListObject=GameObject.FindWithTag("BallList");

    }

    public void BallGenerate( BallMemory.MemoryData? MData)
    {
        Debug.Log("Start new generate ball");
        // GameObject newball=Instantiate(prefabToSpawn, spawnPosition.position, spawnPosition.rotation);
         // 实例化球（若指定了父物体，则生成在父物体下）
        GameObject newBall = Instantiate(
            prefabToSpawn, 
            spawnPosition.position, 
            spawnPosition.rotation, 
            BallListObject.transform // 可选：指定父物体，优化层级结构
        );
        newBall.transform.position = spawnPosition.position;
        //hasTriggered = true;
        if(MData.HasValue)
        {
            Debug.Log("picture:"+MData.Value.picturepath);
            newBall.GetComponent<BallMemory>().DataUpdate(MData);
            newBall.name="Ball"+MData.Value.memoryId.ToString();
            int colorValue = MData.Value.color;
            Renderer ballRenderer=newBall.GetComponent<Renderer>();
            switch (colorValue)
            {
                case 1:
                    if (pos != null)
                    {
                        ballRenderer.material = pos;
                        Debug.Log("材质已切换为：pos（对应color=1）");
                    }
                    else
                    {
                        Debug.LogError("pos材质未赋值，无法切换");
                    }
                    break;
                case 0:
                    if (neu != null)
                    {
                        ballRenderer.material = neu;
                        Debug.Log("材质已切换为：neu（对应color=0）");
                    }
                    else
                    {
                        Debug.LogError("neu材质未赋值，无法切换");
                    }
                    break;
                case -1:
                    if (neg != null)
                    {
                        ballRenderer.material = neg;
                        Debug.Log("材质已切换为：neg（对应color=-1）");
                    }
                    else
                    {
                        Debug.LogError("neg材质未赋值，无法切换");
                    }
                    break;
                default:
                    Debug.LogError($"无效的color值：{colorValue}，材质未切换");
                    break;
            }
        }
    }

    public void GenerateMultipleBalls(List<BallMemory.MemoryData?> MDList)
    {
        StartCoroutine(GenerateCoroutine(MDList));  
    }
    private IEnumerator GenerateCoroutine(List<BallMemory.MemoryData?> dataList)
    {
        Debug.Log("generate multiple balls");
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