using UnityEngine;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class PlayRecord : MonoBehaviour
{
    [Header("UI组件")]
    public Toggle playToggle;        // 控制播放/暂停的Toggle
    public TextMeshProUGUI statusText; // 显示播放状态的文本（可选）

    [Header("数据与音频配置")]
    public int targetMemoryId = 1001; // 要查找的记忆ID
    private DataManager dataManager;
    private AudioSource audioSource;
    private string targetRecordingPath; // 目标录音路径
    private bool isAudioLoaded = false; // 音频是否已加载完成
    private bool isProcessing = false;  // 防止重复处理的标志

    // 状态文本配置（可在Inspector修改）
    [SerializeField] private string defaultText = "Play Recording";
    [SerializeField] private string loadingText = "Loading...";
    [SerializeField] private string playingText = "Playing...";
    [SerializeField] private string pausedText = "Paused";
    [SerializeField] private string errorText = "Recording Not Found";

    private void Start()
    {
        // 初始化音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 查找DataManager
        dataManager = GameObject.FindWithTag("DataManager")?.GetComponent<DataManager>();
        if (dataManager == null)
        {
            Debug.LogError("DataManager not found! Check tag 'DataManager'");
            DisableUI();
            return;
        }

        // 初始化UI
        statusText.text = defaultText;
        playToggle.isOn = false;
        playToggle.onValueChanged.AddListener(OnPlayToggleChanged);
    }

    // Toggle状态改变时触发
    private void OnPlayToggleChanged(bool isOn)
    {
        // 防止重复处理或DataManager未找到时的操作
        if (dataManager == null || isProcessing)
        {
            playToggle.isOn = false;
            return;
        }

        if (isOn)
        {
            // 勾选Toggle：开始查找路径并播放
            isProcessing = true;
            playToggle.interactable = false;
            statusText.text = loadingText;
            
            // 从DataManager获取录音路径并播放
            FetchRecordingPathAndPlay();
        }
        else
        {
            // 取消勾选：停止播放
            StopAudio();
        }
    }

    // 从DataManager获取录音路径并播放
    private void FetchRecordingPathAndPlay()
    {
        if (dataManager.FetchDataById(targetMemoryId))
        {
            DataManager.MemoryData memoryData = dataManager.GetCurrentData();
            if (!string.IsNullOrEmpty(memoryData.recordingpath) && File.Exists(memoryData.recordingpath))
            {
                targetRecordingPath = memoryData.recordingpath;
                StartCoroutine(LoadAndPlayAudio(targetRecordingPath));
            }
            else
            {
                HandleErrorState("Invalid recording path: " + memoryData.recordingpath);
            }
        }
        else
        {
            HandleErrorState("No data found for memory ID: " + targetMemoryId);
        }
    }

    // 加载并播放音频
    private IEnumerator LoadAndPlayAudio(string path)
    {
        using (WWW www = new WWW("file://" + path))
        {
            yield return www;

            if (string.IsNullOrEmpty(www.error))
            {
                audioSource.clip = www.GetAudioClip();
                isAudioLoaded = true;
                PlayAudio();
                Debug.Log("Loaded and playing recording from: " + path);
            }
            else
            {
                HandleErrorState("Failed to load audio: " + www.error);
            }

            isProcessing = false;
        }
    }

    // 播放音频
    private void PlayAudio()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
            statusText.text = playingText;
            playToggle.interactable = true;
        }
    }

    // 停止音频
    private void StopAudio()
    {
        audioSource.Stop();
        statusText.text = pausedText;
    }

    // 处理错误状态
    private void HandleErrorState(string errorLog)
    {
        statusText.text = errorText;
        playToggle.isOn = false;
        playToggle.interactable = true;
        isProcessing = false;
        Debug.LogError(errorLog);
    }

    // 禁用UI（DataManager未找到时）
    private void DisableUI()
    {
        playToggle.interactable = false;
        statusText.text = errorText;
    }

    // 音频播放完成后自动重置Toggle
    private void Update()
    {
        if (isAudioLoaded && !audioSource.isPlaying && playToggle.isOn)
        {
            playToggle.isOn = false;
            statusText.text = defaultText;
        }
    }
}