using UnityEngine;
using static OVRInput; // 用于简化Meta控制器输入检测

public class BallSceneSwitcher : MonoBehaviour
{
    public string targetSceneName = "SkyboxScene"; // 目标场景名称
    public Controller targetController = Controller.RTouch; // 检测右手控制器（A键）
    private DataManager database; // 数据管理器引用

    private void Start()
    {
        // 初始化DataManager（根据你的项目实际获取方式，例如通过单例或场景中的对象）
        // 假设DataManager挂载在名为"DataManager"的游戏对象上
        database = GameObject.FindWithTag("DataManager").GetComponent<DataManager>();
        
        if (database == null)
        {
            Debug.LogError("未找到DataManager，请检查场景中是否存在该组件！");
        }
    }

    private void Update()
    {
        // 检测右手控制器的A键按下（Quest 3的A键对应Button.One）
        if (GetDown(Button.One, targetController))
        {
            // 从DataManager中获取当前处理的球
            if (database != null && database.BallOnProcess != null)
            {
                // 获取球上的BallMemory组件（假设球的数据存在该组件中）
                BallMemory ballMemory = database.BallOnProcess.GetComponent<BallMemory>();
                
                if (ballMemory != null && ballMemory.BallData.HasValue)
                {
                    // 从球的数据中获取图片路径
                    string skyboxPath = ballMemory.BallData.Value.picturepath;
                    
                    if (!string.IsNullOrEmpty(skyboxPath))
                    {
                        // 存储路径到全局数据，供新场景使用
                        SkyboxSceneData.Instance.targetPicturePath = skyboxPath;
                        // 切换场景
                        UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
                        Debug.Log($"已切换到场景：{targetSceneName}，使用图片路径：{skyboxPath}");
                    }
                    else
                    {
                        Debug.LogError("球的图片路径为空！");
                    }
                }
                else
                {
                    Debug.LogError("球上未找到有效的BallMemory数据！");
                }
            }
            else
            {
                Debug.LogWarning("当前没有正在处理的球（BallOnProcess为null）");
            }
        }
    }
}