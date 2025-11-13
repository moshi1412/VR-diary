using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveButtonController : MonoBehaviour
{
    [Header("UI组件")]
    public Toggle saveToggle;       // 触发保存的Toggle
    public TextMeshProUGUI statusText; // 显示状态的文本

    [Header("数据关联")]
    public DataManager dataManager; // 直接引用DataManager（建议在Inspector手动赋值）

    // 保存前的初始文本（可在Inspector中修改）
    [SerializeField] private string defaultText = "Save";


    private void Start()
    {
        // 初始化UI状态
        statusText.text = defaultText;
        
        // 自动查找DataManager（如果未手动赋值）
        if (dataManager == null)
        {
            dataManager = GameObject.FindWithTag("DataManager")?.GetComponent<DataManager>();
            if (dataManager == null)
            {
                dataManager = FindObjectOfType<DataManager>();
            }
        }

        // 检查DataManager是否存在
        if (dataManager == null)
        {
            Debug.LogError("场景中未找到DataManager组件！");
            saveToggle.interactable = false; // 禁用Toggle防止错误
            return;
        }

        // 绑定Toggle事件
        saveToggle.onValueChanged.AddListener(OnSaveToggleChanged);
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


    // 保存流程：从BallOnProcess中获取BallData并保存
    private void StartSaveProcess()
    {
        // 检查必要引用是否存在
        if (dataManager == null)
        {
            Debug.LogError("DataManager为null，无法保存");
            ResetUI();
            return;
        }

        if (dataManager.BallOnProcess == null)
        {
            Debug.LogError("DataManager的BallOnProcess未赋值！");
            ResetUI();
            return;
        }

        BallMemory ballDataComponent = dataManager.BallOnProcess.GetComponent<BallMemory>();
        if (ballDataComponent == null)
        {
            Debug.LogError($"BallOnProcess上未找到BallMemory组件！");
            ResetUI();
            return;
        }

        // 获取要保存的结构体（BallData）
        BallMemory.MemoryData? dataToSave = ballDataComponent.BallData;

        // 显示保存中状态
        statusText.text = "saving......";
        saveToggle.interactable = false; // 禁用Toggle防止重复点击

        // 调用DataManager保存数据
        dataManager.AddData(dataToSave);
        Debug.Log($"已保存气球数据，label: {dataToSave.Value.label}");

        // 延迟重置UI（给用户反馈）
        Invoke(nameof(ResetUI), 1.0f);
    }


    // 重置UI状态
    private void ResetUI()
    {
        statusText.text = defaultText;   // 恢复文本
        saveToggle.isOn = false;         // 取消勾选
        saveToggle.interactable = true;  // 重新启用Toggle
    }
}