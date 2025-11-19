using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引入TextMeshPro命名空间
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

[RequireComponent(typeof(Toggle))]
public class AudioVisualizerWithSave : MonoBehaviour
{
    [Header("=== 录音控制 ===")]
    public int sampleRate = 44100;
    public int sampleWindow = 1024;
    public float sensitivity = 15f;
    public TMP_ToggleAudioRecorder audioRecorder; // 引用原有录音类

    [Header("=== 状态文本 ===")]
    public TextMeshProUGUI statusText; // 用于用于显示状态的TextMeshPro组件

    [Header("=== 环形指示器 ===")]
    public Image ringImage;
    public float minRingScale = 1f;
    public float maxRingScale = 1.8f;
    public float ringSmoothness = 15f;

    [Header("=== 条形容器设置 ===")]
    public List<Transform> barContainers;
    public float minBarHeight = 10f;
    public float maxBarHeight = 100f;
    public float barSmoothness = 20f;
    [Range(0f, 0.3f)] public float barRandomness = 0.15f;

    [Header("=== 按钮 ===")]
    public LabelSearchHandler ConfirmButton;

    // 状态文本内容常量
    private const string DEFAULT_TEXT = "Press to Speak";
    private const string RECORDING_TEXT = "Speaking";

    private Toggle recordToggle;
    private string microphoneName;
    private AudioClip recordingClip;
    private bool isRecording;
    private List<Image> allAudioBars = new List<Image>();
    private float[] barTargetHeights;
    private float currentRingScale;
    private AudioAnalyzer aa;

    void Start()
    {
        // 初始化Toggle
        recordToggle = GetComponent<Toggle>();
        recordToggle.onValueChanged.AddListener(OnToggleChanged);
        recordToggle.isOn = false;

        // 初始化状态文本
        if (statusText != null)
        {
            statusText.text = DEFAULT_TEXT; // 默认显示"Press to Speak"
        }
        else
        {
            Debug.LogWarning("请赋值statusText（TextMeshPro组件）");
        }

        // 获取音频分析器
        aa = GameObject.FindWithTag("DataManager").GetComponent<AudioAnalyzer>();

        // 初始化环形
        if (ringImage != null)
        {
            currentRingScale = minRingScale;
            ringImage.rectTransform.localScale = Vector3.one * currentRingScale;
        }

        // 自动收集所有条形
        CollectAllBars();

        // 检查录音类引用
        if (audioRecorder == null)
        {
            Debug.LogError("请引用TMP_ToggleAudioRecorder脚本实例！");
            recordToggle.interactable = false;
        }
        else
        {
            // 同步原有录音类的麦克风设备
            microphoneName = audioRecorder.GetComponent<AudioSource>() != null
                ? Microphone.devices.Length > 0 ? Microphone.devices[0] : ""
                : "";
        }
    }

    // 自动收集所有容器下的条形Image
    private void CollectAllBars()
    {
        allAudioBars.Clear();
        if (barContainers == null || barContainers.Count == 0) return;

        foreach (var container in barContainers)
        {
            if (container == null) continue;
            Image[] containerBars = container.GetComponentsInChildren<Image>(true);
            foreach (var bar in containerBars)
            {
                if (bar.name.Contains("Bar"))
                {
                    bar.rectTransform.pivot = new Vector2(0.5f, 0f);
                    bar.rectTransform.sizeDelta = new Vector2(bar.rectTransform.sizeDelta.x, minBarHeight);
                    allAudioBars.Add(bar);
                }
            }
        }

        if (allAudioBars.Count > 0)
        {
            barTargetHeights = new float[allAudioBars.Count];
            Array.Fill(barTargetHeights, minBarHeight);
        }
    }

    void Update()
    {
        if (isRecording)
        {
            float volume = GetVolumeLevel();
            UpdateRing(volume);
            UpdateBars(volume);
        }
    }

    // Toggle状态变化（核心：关联原有录音逻辑）
    private void OnToggleChanged(bool isOn)
    {
        if (audioRecorder == null) return;

        if (isOn)
        {
            // 开始录音：调用原有类的StartRecording
            audioRecorder.StartRecording();
            isRecording = true;
            Debug.Log("开始录音，可视化启动");
            // 更新状态文本为"Speaking"
            if (statusText != null)
                statusText.text = RECORDING_TEXT;
        }
        else
        {
            // 停止录音：调用原有类的停止保存方法
            string afpath = audioRecorder.StopAndSaveRecording();
            isRecording = false;
            ResetVisualizers();
            Debug.Log("停止录音，触发保存逻辑");
            // 恢复状态文本为"Press to Speak"
            if (statusText != null)
                statusText.text = "waiting for analysis";

            Debug.Log("STT");
            StartCoroutine(aa.AudioToText(
                afpath,
                (result) => ReceiveAnalysisText(result)
            ));
        }
    }

    private void ReceiveAnalysisText(string p1)
    {
        // 1. 清理文本：去除emoji，标点替换为空格
        string cleanedText = CleanRecognitionText(p1);

        // 2. 使用清理后的文本进行后续操作
        Debug.Log($"label1: {cleanedText}");
        ConfirmButton.LabelBySpeak = cleanedText;
        statusText.text = cleanedText;
    }
    private string CleanRecognitionText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (char c in text)
        {
            // 1. 判断是否为emoji（通过Unicode区块范围排除）
            if (IsEmoji(c))
                continue; // 跳过emoji

            // 2. 判断是否为标点（替换为空格）
            if (IsPunctuation(c))
            {
                sb.Append(' '); // 标点替换为空格
            }
            else
            {
                sb.Append(c); // 保留其他有效字符
            }
        }

        // 3. 合并连续空格并去除首尾空格
        string result = sb.ToString();
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
        return result;
    }

    // 判断是否为emoji（覆盖常见emoji的Unicode范围）
    private bool IsEmoji(char c)
    {
        // emoji主要分布在以下Unicode区块，不在这些范围的字符视为非emoji
        return (c >= 0x1F600 && c <= 0x1F64F) || // 表情符号
            (c >= 0x1F300 && c <= 0x1F5FF) || // 符号与图案
            (c >= 0x1F680 && c <= 0x1F6FF) || // 交通与地图符号
            (c >= 0x1F1E0 && c <= 0x1F1FF) || // 国旗表情
            (c >= 0x2600 && c <= 0x26FF) ||   // 杂项符号
            (c >= 0x2700 && c <= 0x27BF);     // 装饰符号
    }

    // 判断是否为标点（中英文标点）
    private bool IsPunctuation(char c)
    {
        // 中文标点
        string chinesePunct = "。！？，；：“”‘’（）【】《》、…—";
        // 英文标点（来自ASCII表）
        bool isEnglishPunct = (c >= 33 && c <= 47) || (c >= 58 && c <= 64) ||
                              (c >= 91 && c <= 96) || (c >= 123 && c <= 126);

        return isEnglishPunct || chinesePunct.Contains(c.ToString());
    }
    // 获取音量（基于原有录音类的AudioClip）
    private float GetVolumeLevel()
    {
        if (audioRecorder == null || !isRecording) return 0f;

        // 从原有录音类获取当前录音片段
        recordingClip = audioRecorder.GetType().GetField("recordedClip",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(audioRecorder) as AudioClip;

        if (recordingClip == null) return 0f;

        float[] samples = new float[sampleWindow];
        int micPosition = Microphone.GetPosition(microphoneName) - (sampleWindow + 1);
        if (micPosition < 0) return 0f;

        recordingClip.GetData(samples, micPosition);

        float sum = 0f;
        foreach (float sample in samples) sum += sample * sample;
        float rms = Mathf.Sqrt(sum / sampleWindow);
        return Mathf.Clamp01(rms * sensitivity);
    }

    // 更新环形缩放
    private void UpdateRing(float volume)
    {
        if (ringImage == null) return;
        float targetScale = Mathf.Lerp(minRingScale, maxRingScale, volume);
        currentRingScale = Mathf.Lerp(currentRingScale, targetScale, Time.deltaTime * ringSmoothness);
        ringImage.rectTransform.localScale = Vector3.one * currentRingScale;
    }

    // 更新条形高度
    private void UpdateBars(float volume)
    {
        if (allAudioBars.Count == 0) return;
        for (int i = 0; i < allAudioBars.Count; i++)
        {
            Image bar = allAudioBars[i];
            if (bar == null) continue;

            float baseHeight = Mathf.Lerp(minBarHeight, maxBarHeight, volume);
            float randomFactor = UnityEngine.Random.Range(1f - barRandomness, 1f + barRandomness);
            float targetHeight = Mathf.Clamp(baseHeight * randomFactor, minBarHeight, maxBarHeight);

            barTargetHeights[i] = Mathf.Lerp(barTargetHeights[i], targetHeight, Time.deltaTime * barSmoothness);
            bar.rectTransform.sizeDelta = new Vector2(bar.rectTransform.sizeDelta.x, barTargetHeights[i]);
        }
    }

    // 重置可视化效果
    private void ResetVisualizers()
    {
        if (ringImage != null)
        {
            currentRingScale = minRingScale;
            ringImage.rectTransform.localScale = Vector3.one * currentRingScale;
        }

        foreach (var bar in allAudioBars)
        {
            if (bar != null)
            {
                bar.rectTransform.sizeDelta = new Vector2(bar.rectTransform.sizeDelta.x, minBarHeight);
            }
        }

        if (barTargetHeights != null) Array.Fill(barTargetHeights, minBarHeight);
    }

    // 编辑器模式下更新条形收集
    private void OnValidate()
    {
        if (Application.isPlaying) CollectAllBars();
    }

    // 退出时确保停止录音
    private void OnDestroy()
    {
        if (isRecording && audioRecorder != null)
        {
            audioRecorder.StopAndSaveRecording();
            // 销毁时恢复文本
            if (statusText != null)
                statusText.text = DEFAULT_TEXT;
        }
    }

    // 暂停时处理
    private void OnApplicationPause(bool pause)
    {
        if (pause && isRecording && audioRecorder != null)
        {
            audioRecorder.StopAndSaveRecording();
            recordToggle.isOn = false;
            isRecording = false;
            ResetVisualizers();
            // 暂停时恢复文本
            if (statusText != null)
                statusText.text = DEFAULT_TEXT;
        }
    }
}