using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveButtonController : MonoBehaviour
{
    [Header("UI组件")]
    public Toggle saveToggle;       // 触发保存的Toggle
    public TextMeshProUGUI statusText; // 显示状态的文本

    [Header("数据关联")] // 数据管理器引用
    public int targetMemoryId;      // 要保存的记忆ID（需与DataManager中的数据对应）

    // 保存前的初始文本（可在Inspector中修改）
    [SerializeField] private string defaultText = "Save";


    private DataManager dataManager;
    private void Start()
    {
        // 初始化UI状态
        statusText.text = defaultText;
        dataManager = GameObject.FindWithTag("DataManager")?.GetComponent<DataManager>();
        // 绑定Toggle事件
        saveToggle.onValueChanged.AddListener(OnSaveToggleChanged);

        // 自动查找DataManager（如果未手动赋值）
        if (dataManager == null)
        {
            dataManager = FindObjectOfType<DataManager>();
            if (dataManager == null)
            {
                Debug.LogError("场景中未找到DataManager组件！");
                saveToggle.interactable = false; // 禁用Toggle防止错误
            }
        }
    }


    // Toggle状态改变时触发
    private void OnSaveToggleChanged(bool isOn)
    {
        // 只有当Toggle被勾选时才执行保存逻辑
        if (isOn)
        {
            StartSaveProcess();
        }
    }


    // 保存流程
    private void StartSaveProcess()
    {
        if (dataManager == null) return;

        // 1. 显示保存中状态
        statusText.text = "saving……";
        saveToggle.interactable = false; // 禁用Toggle防止重复点击

        // 2. 调用DataManager保存当前数据（假设已通过FetchDataById获取了当前struct）
        // 先确认DataManager中有需要保存的有效数据
        if (dataManager.FetchDataById(targetMemoryId))
        {
            // 获取当前内存中的struct并保存
            DataManager.MemoryData currentData = dataManager.GetCurrentData();
            dataManager.AddData(currentData); // 保存到文件
            Debug.Log($"已保存ID为 {targetMemoryId} 的数据");
        }
        else
        {
            Debug.LogWarning($"没有找到ID为 {targetMemoryId} 的数据，保存失败");
        }

        // 3. 延迟重置UI（给用户看到"保存中"的反馈）
        Invoke(nameof(ResetUI), 1.0f); // 1秒后重置
    }


    // 重置UI状态
    private void ResetUI()
    {
        statusText.text = defaultText;   // 恢复文本
        saveToggle.isOn = false;         // 取消勾选
        saveToggle.interactable = true;  // 重新启用Toggle
    }
}