using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;

public class VideoButton : MonoBehaviour
{
    [Header("UI动画控制器")]
    public Animator mainpanel;
    public Animator Videopanel;
    public Toggle vb;

    [Header("视频相关（外部传入/手动赋值）")]
    [Tooltip("需要控制的视频播放器")]
    public VideoPlayer videoPlayer; // 外部传入的VideoPlayer
    // [Tooltip("视频文件路径（本地路径/URL，外部传入）")]
    public string videoPath; // 改为操作视频路径
    
    [Header("视频渲染目标（可选）")]
    [Tooltip("显示视频的RawImage（需手动绑定RenderTexture）")]
    private RawImage videoDisplay;

    private DataManager dm;
    void Start()
    {
        // 初始化UI状态
        mainpanel.SetBool("videoin", false);
        mainpanel.SetBool("videoout", false);
        Videopanel.SetBool("videoin", false);
        Videopanel.SetBool("videoout", false);
        vb.isOn = false;
        dm=GameObject.FindWithTag("DataManager").GetComponent<DataManager>();       
    }

    /// <summary>
    /// 初始化视频播放器（使用已设置的RenderTexture）
    /// </summary>
    private void InitializeVideoPlayer()
    {
        // 禁用自动播放，使用Toggle控制
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.source = VideoSource.Url; // 设为路径模式（兼容本地/网络路径）

        // 若绑定了RawImage，确保RenderTexture已设置
        if (videoDisplay != null && videoPlayer.targetTexture != null)
        {
            videoDisplay.texture = videoPlayer.targetTexture;
        }
    }

    /// <summary>
    /// 供外部调用：更新视频路径并加载（核心修改点）
    /// </summary>
    /// <param name="newPath">新的视频路径（本地路径/URL）</param>
    public void SetVideoPath(string newPath)
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer未赋值，无法更新视频！");
            return;
        }

        // 验证路径非空
        if (string.IsNullOrEmpty(newPath))
        {
            Debug.LogWarning("传入的视频路径为空！");
            return;
        }

        // 停止当前播放，更新路径并加载
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        videoPath = newPath;
        LoadVideoFromPath(videoPath);
    }

    /// <summary>
    /// 从路径加载视频（核心逻辑）
    /// </summary>
    /// <param name="path">视频路径</param>
    private void LoadVideoFromPath(string path)
    {
        // 验证本地路径有效性（网络URL跳过验证）
        if (!IsValidVideoPath(path))
        {
            Debug.LogError($"视频路径无效或文件不存在：{path}");
            return;
        }

        // 设置视频源为路径
        videoPlayer.url = path;
        // 预加载视频
        videoPlayer.Prepare();

        // 准备完成回调（可选，确保播放流畅）
        videoPlayer.prepareCompleted += OnVideoPrepared;
        Debug.Log($"开始加载视频：{path}");
    }

    /// <summary>
    /// 视频准备完成回调
    /// </summary>
    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log($"视频加载完成：{videoPath}");
        // 若Toggle已开启，自动播放
        if (vb.isOn)
        {
            vp.Play();
        }
        // 移除回调，避免重复调用
        videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    /// <summary>
    /// 验证视频路径有效性（本地文件）
    /// </summary>
    /// <param name="path">待验证路径</param>
    /// <returns>路径是否有效</returns>
    private bool IsValidVideoPath(string path)
    {
        // 网络URL（以http/https开头）直接视为有效
        if (path.StartsWith("http://") || path.StartsWith("https://"))
        {
            return true;
        }

        // 本地文件路径验证是否存在
        return File.Exists(path);
    }

    /// <summary>
    /// Toggle状态变化：控制UI动画和视频播放
    /// </summary>
    public void ToggleChanged()
    {
        if (videoPlayer == null) return;

        if (vb.isOn)
        {
            Debug.Log("UI changes");
            // 显示视频面板，隐藏主面板
            mainpanel.SetBool("videoin", true);
            mainpanel.SetBool("videoout", false);
            Videopanel.SetBool("videoin", true);
            Videopanel.SetBool("videoout", false);
            if(dm.BallOnProcess.GetComponent<BallMemory>().BallData.HasValue)
                videoPath=dm.BallOnProcess.GetComponent<BallMemory>().BallData.Value.videopath;
            Debug.Log(videoPath);
             // 初始化视频播放器（如果已赋值）
            if (videoPlayer != null)
            {
                InitializeVideoPlayer();
                // 若初始路径不为空，直接加载
            
                if (!string.IsNullOrEmpty(videoPath))
                {
                    LoadVideoFromPath(videoPath);
                }
            }
            // 播放视频（确保已准备好且路径有效）
            if (!string.IsNullOrEmpty(videoPath) && (videoPlayer.isPrepared || videoPlayer.isPlaying))
            {
                videoPlayer.Play();
            }
            else if (!string.IsNullOrEmpty(videoPath))
            {
                // 未准备好则重新准备
                videoPlayer.Prepare();
            }
        }
        else
        {
            // 隐藏视频面板，显示主面板
            mainpanel.SetBool("videoin", false);
            mainpanel.SetBool("videoout", true);
            Videopanel.SetBool("videoin", false);
            Videopanel.SetBool("videoout", true);

            // 暂停视频
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
        }
    }

    private void OnDestroy()
    {
        // 清理播放状态和回调
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
        }
    }

    public void BackToChoicePanel()
    {
        // 初始化UI状态
        if (videoPlayer.isPlaying)
        {
                videoPlayer.Stop();
        }
        mainpanel.SetBool("videoout", true);
        mainpanel.SetBool("videoin", false);
        Videopanel.SetBool("videoout", true);
        Videopanel.SetBool("videoin", false); 
    }
}