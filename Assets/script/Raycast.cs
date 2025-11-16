using UnityEngine;
using UnityEngine.UI;
// using static OVRInput;

public class BallSceneSwitcher : MonoBehaviour
{
    public string targetSceneName = "SkyboxScene"; // 目标场景名称
    private DataManager database; // 数据管理器引用
    private bool isProcessing = false; // 防止重复处理的标记
    public Toggle changescenetoggle;
    private void Start()
    {
        // 初始化DataManager
        database = GameObject.FindWithTag("DataManager")?.GetComponent<DataManager>();
        changescenetoggle.isOn=false;
        if (database == null)
        {
            Debug.LogError("未找到DataManager，请检查场景中是否存在该组件！");
        }
    }

    /// <summary>
    /// 公开的触发方法，供Unity编辑器中绑定Toggle事件使用
    /// </summary>
    /// <param name="isOn">Toggle的状态（可根据需要使用或忽略）</param>
    public void OnToggleChanged()
    {
        // 防止重复处理
        if (isProcessing) return;
        
        isProcessing = true;

        try
        {
            // 这里可以根据需要判断是开启还是关闭时触发
            // 示例：只在Toggle开启时触发
            if (changescenetoggle.isOn)
            {
                ProcessSceneSwitch();
            }
        }
        finally
        {
            isProcessing = false;
        }
    }

    /// <summary>
    /// 处理场景切换的核心逻辑
    /// </summary>
    private void ProcessSceneSwitch()
    {
        if (database != null && database.BallOnProcess != null)
        {
            BallMemory ballMemory = database.BallOnProcess.GetComponent<BallMemory>();
            
            if (ballMemory != null && ballMemory.BallData.HasValue)
            {
                string skyboxPath = ballMemory.BallData.Value.picturepath;
                
                if (!string.IsNullOrEmpty(skyboxPath))
                {
                    SkyboxSceneData.Instance.targetPicturePath = skyboxPath;
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