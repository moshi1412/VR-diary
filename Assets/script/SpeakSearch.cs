using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引入TextMeshPro命名空间
using System.Collections.Generic;
using System.Linq;
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
                statusText.text = DEFAULT_TEXT;

            Debug.Log("STT");
            StartCoroutine(aa.AudioToText(
                afpath, 
                (result) => ReceiveAnalysisText(result)
            ));
        }
    }

    private void ReceiveAnalysisText(string p1)
    {
        Debug.Log($"label1: {p1}");
        ConfirmButton.LabelBySpeak = p1;
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