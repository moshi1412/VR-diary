using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveButtonController : MonoBehaviour
{
    [Header("UI组件")]
    public Toggle saveToggle;       // 触发保存的Toggle
    public TextMeshProUGUI statusText; // 显示状态的文本

    [Header("数据关联")]
    public DataManager dataManager; // 数据管理器引用
    public AudioAnalyzer audioAnalyzer; // 音频分析器引用

    // 保存前的初始文本（可在Inspector中修改）
    [SerializeField] private string defaultText = "Save";
    
    [Header("=== 预设材质 ===")]
    public Material pos;   // color=1 时使用（正面）
    public Material neu;   // color=0 时使用（中性）
    public Material neg;   // color=-1 时使用（负面）

    private void Start()
    {
        // 初始化UI状态
        saveToggle.isOn = false;
        statusText.text = defaultText;
        
        // 自动查找必要组件
        AutoFindReferences();

        // 检查必要组件是否存在
        if (dataManager == null)
        {
            Debug.LogError("场景中未找到DataManager组件！");
            saveToggle.interactable = false;
            return;
        }

        if (audioAnalyzer == null)
        {
            Debug.LogError("未设置AudioAnalyzer组件！");
            saveToggle.interactable = false;
            return;
        }

        // 绑定Toggle事件（若需通过代码绑定，可取消注释）
        // saveToggle.onValueChanged.AddListener(OnSaveToggleChanged);
    }

    // 自动查找引用的组件
    private void AutoFindReferences()
    {
        // 查找DataManager
        if (dataManager == null)
        {
            dataManager = GameObject.FindWithTag("DataManager")?.GetComponent<DataManager>();
            if (dataManager == null)
            {
                dataManager = FindAnyObjectByType<DataManager>();
            }
        }

        // 查找AudioAnalyzer
        if (audioAnalyzer == null)
        {
            audioAnalyzer = FindAnyObjectByType<AudioAnalyzer>();
        }
    }

    // Toggle状态改变时触发
    public void OnSaveToggleChanged()
    {
        // 只有当Toggle被勾选时才执行保存逻辑
        if (saveToggle.isOn)
        {
            StartSaveProcess();
        }
    }

    // 保存流程：先分析音频获取标签，再保存数据并设置材质
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

        // 获取球的Renderer组件（用于设置材质）
        Renderer ballRenderer = dataManager.BallOnProcess.GetComponent<Renderer>();
        if (ballRenderer == null)
        {
            Debug.LogError("当前处理的球上未找到Renderer组件，无法设置材质！");
            ResetUI();
            return;
        }

        // 显示处理中状态
        statusText.text = "processing...";
        saveToggle.interactable = false;

        // 开始音频分析，分析完成后保存数据并设置材质
        audioAnalyzer.ProcessAudioAndGetResults((sentiment, keywords, combinedTags, isSuccessful) => 
        {
            if (!isSuccessful)
            {
                statusText.text = "analysis failed";
                Invoke(nameof(ResetUI), 1.0f);
                return;
            }

            // 更新球数据的标签信息
            var ballData = ballDataComponent.BallData;
            if (ballData.HasValue)
            {
                var updatedData = ballData.Value;
                // 更新标签信息
                updatedData.label = $"{sentiment} {keywords} {combinedTags}";

                // 根据sentiment设置color属性和材质
                if (sentiment.Contains("正面"))
                {
                    updatedData.color = 1;
                    if (pos != null) ballRenderer.material = pos;
                    else Debug.LogError("pos材质未赋值，无法设置正面材质！");
                }
                else if (sentiment.Contains("中性"))
                {
                    updatedData.color = 0;
                    if (neu != null) ballRenderer.material = neu;
                    else Debug.LogError("neu材质未赋值，无法设置中性材质！");
                }
                else if (sentiment.Contains("负面"))
                {
                    updatedData.color = -1;
                    if (neg != null) ballRenderer.material = neg;
                    else Debug.LogError("neg材质未赋值，无法设置负面材质！");
                }
                else
                {
                    // 未匹配到情感关键词时的默认值
                    updatedData.color = 0;
                    if (neu != null) ballRenderer.material = neu;
                    else Debug.LogError("neu材质未赋值，无法设置默认材质！");
                    Debug.LogWarning($"未识别到情感关键词，sentiment: {sentiment}，默认设为中性");
                }

                // 保存更新后的数据
                ballDataComponent.BallData = updatedData;
                dataManager.AddData(updatedData);
                Debug.Log($"已保存记忆球数据，label: {updatedData.label}，情感: {sentiment}，color: {updatedData.color}");
                
                // 显示成功状态
                statusText.text = "saved!";
            }
            else
            {
                Debug.LogError("球数据为空，无法保存");
                statusText.text = "save failed";
            }

            // 延迟重置UI
            Invoke(nameof(ResetUI), 1.0f);
        });
    }

    // 重置UI状态
    private void ResetUI()
    {
        statusText.text = defaultText;   // 恢复文本
        saveToggle.isOn = false;         // 取消勾选（根据需求决定是否保留）
        saveToggle.interactable = true;  // 重新启用Toggle
    }
}