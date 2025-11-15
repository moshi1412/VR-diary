using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;

public class VideoOperation : MonoBehaviour
{
    [Header("视频组件")]
    [Tooltip("视频播放器组件")]
    public VideoPlayer videoPlayer;
    [Tooltip("音频源（用于控制音量，需与VideoPlayer关联）")]
    public AudioSource audioSource;

    [Header("控制UI - 播放相关")]
    [Tooltip("播放/暂停开关")]
    public Toggle playPauseToggle;
    [Tooltip("进度条（Slider组件）")]
    public Slider progressSlider;

    [Header("控制UI - 音量调节")]
    [Tooltip("音量调节滑块（0-1范围）")]
    public Slider volumeSlider;

    // 标记是否正在拖动进度滑块
    private bool isDraggingProgressSlider = false;

    private void Start()
    {
        // 初始化检查
        if (videoPlayer == null)
        {
            Debug.LogError("请赋值VideoPlayer组件！");
            enabled = false;
            return;
        }
        if (audioSource == null)
        {
            Debug.LogError("请赋值AudioSource组件！");
            enabled = false;
            return;
        }
        if (playPauseToggle == null)
        {
            Debug.LogError("请赋值播放/暂停Toggle！");
            enabled = false;
            return;
        }
        if (progressSlider == null)
        {
            Debug.LogError("请赋值进度条Slider！");
            enabled = false;
            return;
        }
        if (volumeSlider == null)
        {
            Debug.LogError("请赋值音量调节Slider！");
            enabled = false;
            return;
        }

        // 关联视频播放器和音频源（关键：确保视频声音通过AudioSource输出）
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        // 初始化UI状态
        // 进度条设置
        progressSlider.minValue = 0;
        progressSlider.maxValue = 1;
        progressSlider.value = 0;
        // 音量条设置（0-1范围，默认音量0.7）
        volumeSlider.minValue = 0;
        volumeSlider.maxValue = 1;
        volumeSlider.value = 0.7f;
        audioSource.volume = 0.7f; // 初始音量同步
        // 播放状态初始化
        playPauseToggle.isOn = false;
        videoPlayer.Stop();

        // 绑定事件
        playPauseToggle.onValueChanged.AddListener(OnPlayPauseToggle);
        progressSlider.onValueChanged.AddListener(OnProgressSliderValueChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged); // 音量调节事件
        AddProgressSliderDragEvents();

        // 监听视频准备完成事件
        videoPlayer.prepareCompleted += OnVideoPrepared;
        if (!videoPlayer.isPrepared)
        {
            videoPlayer.Prepare();
        }
        else
        {
            OnVideoPrepared(videoPlayer);
        }
    }

    private void Update()
    {
        // 同步视频进度到进度条（非拖动状态）
        if (videoPlayer.isPlaying && videoPlayer.isPrepared && !isDraggingProgressSlider)
        {
            float progress = (float)(videoPlayer.time / videoPlayer.length);
            progressSlider.value = Mathf.Clamp01(progress);
        }
    }

    #region 播放/暂停控制
    private void OnPlayPauseToggle(bool isOn)
    {
        if (!videoPlayer.isPrepared) return;

        if (isOn)
        {
            if (!videoPlayer.isPlaying) videoPlayer.Play();
        }
        else
        {
            if (videoPlayer.isPlaying) videoPlayer.Pause();
        }
    }
    #endregion

    #region 进度条控制
    private void AddProgressSliderDragEvents()
    {
        EventTrigger trigger = progressSlider.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = progressSlider.gameObject.AddComponent<EventTrigger>();
        }
        trigger.triggers.Clear();

        AddSliderEvent(trigger, EventTriggerType.BeginDrag, () => isDraggingProgressSlider = true);
        AddSliderEvent(trigger, EventTriggerType.EndDrag, () => 
        {
            isDraggingProgressSlider = false;
            SyncProgressToVideo();
        });
        AddSliderEvent(trigger, EventTriggerType.PointerExit, () => 
        {
            if (isDraggingProgressSlider)
            {
                isDraggingProgressSlider = false;
                SyncProgressToVideo();
            }
        });
    }

    private void AddSliderEvent(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(_ => action.Invoke());
        trigger.triggers.Add(entry);
    }

    private void OnProgressSliderValueChanged(float value)
    {
        if (isDraggingProgressSlider && videoPlayer.isPrepared)
        {
            SyncProgressToVideo();
        }
    }

    private void SyncProgressToVideo()
    {
        if (videoPlayer.length <= 0) return;
        double targetTime = progressSlider.value * videoPlayer.length;
        videoPlayer.time = targetTime;
    }
    #endregion

    #region 音量调节逻辑（核心）
    /// <summary>
    /// 音量滑块变化时实时更新音频源音量
    /// </summary>
    private void OnVolumeSliderChanged(float value)
    {
        // 直接将滑块值（0-1）同步到音频源音量
        audioSource.volume = value;
        // 日志提示（可选，用于调试）
        // Debug.Log($"音量调整为：{value:F2}");
    }
    #endregion

    #region 视频准备完成事件
    private void OnVideoPrepared(VideoPlayer source)
    {
        progressSlider.value = 0; // 重置进度
        Debug.Log($"视频准备完成，总时长：{source.length:F2}秒");
    }
    #endregion

    // 清理事件监听
    private void OnDestroy()
    {
        if (playPauseToggle != null)
            playPauseToggle.onValueChanged.RemoveListener(OnPlayPauseToggle);
        if (progressSlider != null)
            progressSlider.onValueChanged.RemoveListener(OnProgressSliderValueChanged);
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        if (videoPlayer != null)
            videoPlayer.prepareCompleted -= OnVideoPrepared;
    }
}