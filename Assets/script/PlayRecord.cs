using UnityEngine;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

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
    [SerializeField] private string recordOverText = "Record is over"; // 播放结束文本

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
                dataManager = FindAnyObjectByType<DataManager>();
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
        Debug.Log("Fetch audio");
        // 检查BallOnProcess是否存在
        if (dataManager.BallOnProcess == null)
        {
            HandleErrorState("BallOnProcess is not assigned in DataManager!");
            return;
        }

        // 获取BallOnProcess上存储结构体的组件
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
        Debug.Log(recordingPath);
        StartCoroutine(LoadAndPlayAudio(targetRecordingPath));
    }

    // 加载并播放音频（优化版：自动识别格式+跨平台路径）
    private IEnumerator LoadAndPlayAudio(string path)
    {
        // 自动识别音频格式（只保留普遍支持的格式）
        AudioType audioType = GetAudioTypeFromPath(path);
        if (audioType == AudioType.UNKNOWN)
        {
            HandleErrorState($"不支持的音频格式：{Path.GetExtension(path)}");
            yield break;
        }

        // 构建跨平台有效URL
        string url = GetValidUrl(path);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            www.timeout = 5; // 5秒超时
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    audioSource.clip = clip;
                    isAudioLoaded = true;
                    PlayAudio();
                    Debug.Log("Loaded and playing recording from: " + path);
                }
                else
                {
                    HandleErrorState("音频解析失败，可能编码不支持");
                }
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

    // 检测播放结束状态
    private void Update()
    {
        // 当音频加载完成、不在播放中，且Toggle是勾选状态（说明是播放完毕而非手动暂停）
        if (playToggle.isOn && isAudioLoaded && !audioSource.isPlaying)
        {
            statusText.text = recordOverText;
        }
    }

    // 辅助方法：根据文件后缀自动识别AudioType（只保留所有Unity版本都支持的格式）
    private AudioType GetAudioTypeFromPath(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        return extension switch
        {
            ".wav" => AudioType.WAV,   // 所有版本均支持
            ".mp3" => AudioType.MPEG,  // 所有版本均支持（MP3的标准枚举值）
            ".ogg" => AudioType.OGGVORBIS, // 多数版本支持
            _ => AudioType.UNKNOWN     // 不支持的格式
        };
    }

    // 辅助方法：构建跨平台有效URL
    private string GetValidUrl(string localPath)
    {
        string formattedPath = localPath.Replace("\\", "/");
#if UNITY_STANDALONE_WIN
        return "file:///" + formattedPath; // Windows系统路径格式
#else
        return "file://" + formattedPath;  // macOS/Linux系统路径格式
#endif
    }
}