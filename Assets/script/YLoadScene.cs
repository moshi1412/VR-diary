using UnityEngine;
using UnityEngine.SceneManagement; // 场景管理命名空间

public class YButtonLoadScene : MonoBehaviour
{
    [Header("要加载的场景名称")]
    public string sceneName = "MainScene"; // 在Inspector中设置目标场景名

    void Update()
    {
        // 检测左手柄Y键按下（刚按下瞬间）
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            LoadTargetScene();
        }
    }

    /// <summary>
    /// 加载目标场景
    /// </summary>
    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("请在Inspector中设置场景名称！");
            return;
        }

        try
        {
            // 同步加载场景（简单场景推荐）
            SceneManager.LoadScene(sceneName);
            Debug.Log($"开始加载场景：{sceneName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"加载场景失败：{ex.Message}");
        }
    }
}