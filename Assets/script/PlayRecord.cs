using UnityEngine;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class PlayRecord : MonoBehaviour
{
    [Header("UI组件")]
    public Toggle playToggle;        // 控制播放/暂停的Toggle
    public TextMeshProUGUI statusText; // 显示播放状态的文本

    [Header("数据与音频配置")]
    public DataManager dataManager;  // 直接引用DataManager（建议在Inspector赋值）
    private AudioSource audioSource;
    private string targetRecordingPath; // 目标录音路径
    private bool isAudioLoaded = false; // 音频是否已加载完成
    private bool isProcessing = false;  // 防止重复处理的标志

    // 状态文本配置（可在Inspector修改）
    [SerializeField] private string defaultText = "Play Recording";
    [SerializeField] private string loadingText = "Loading...";
    [SerializeField] private string playingText = "Playing...";
    [SerializeField] private string pausedText = "Stopped";
    [SerializeField] private string errorText = "Recording Not Found";
    // [SerializeField] private string recordOverText = "Record is over"; // 新增：播放结束文本

    private void Start()
    {
        // 初始化音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

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
            Debug.LogError("DataManager not found!");
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
        // 防止重复处理或依赖项缺失时的操作
        if (dataManager == null || isProcessing)
        {
            playToggle.isOn = false;
            return;
        }

        if (isOn)
        {
            // 勾选Toggle：从BallOnProcess获取路径并播放
            isProcessing = true;
            playToggle.interactable = false;
            statusText.text = loadingText;
            
            // 核心逻辑：从BallOnProcess的结构体中获取录音路径
            FetchPathFromBallOnProcessAndPlay();
        }
        else
        {
            // 取消勾选：停止播放
            StopAudio();
        }
    }

    // 从BallOnProcess的结构体属性中获取录音路径并播放
    private void FetchPathFromBallOnProcessAndPlay()
    {
        // 检查BallOnProcess是否存在
        if (dataManager.BallOnProcess == null)
        {
            HandleErrorState("BallOnProcess is not assigned in DataManager!");
            return;
        }

        // 获取BallOnProcess上存储结构体的组件（根据实际组件名修改）
        BallMemory ballMemory = dataManager.BallOnProcess.GetComponent<BallMemory>();
        if (ballMemory == null)
        {
            HandleErrorState("BallOnProcess has no BallMemory component!");
            return;
        }

        // 获取结构体数据
        BallMemory.MemoryData? memoryData = ballMemory.BallData;
        if (!memoryData.HasValue)
        {
            HandleErrorState("BallOnProcess has no valid MemoryData!");
            return;
        }

        // 提取录音路径并验证
        string recordingPath = memoryData.Value.recordingpath;
        if (string.IsNullOrEmpty(recordingPath) || !File.Exists(recordingPath))
        {
            HandleErrorState("Invalid or missing recording path: " + recordingPath);
            return;
        }

        // 路径有效，加载并播放音频
        targetRecordingPath = recordingPath;
        StartCoroutine(LoadAndPlayAudio(targetRecordingPath));
    }

    // 加载并播放音频
    private IEnumerator LoadAndPlayAudio(string path)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(
            "file://" + path, 
            UnityEngine.AudioType.WAV // 根据实际音频格式修改
        ))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                audioSource.clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
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

    // 禁用UI（依赖项缺失时）
    private void DisableUI()
    {
        playToggle.interactable = false;
        statusText.text = errorText;
    }
     private void Update()
    {
        // 当音频加载完成、不在播放中，且Toggle是勾选状态（说明是播放完毕而非手动暂停）
        if (isAudioLoaded && !audioSource.isPlaying )
        {
            
            statusText.text = pausedText; // 显示“Record is over”
        }
    }
    
}