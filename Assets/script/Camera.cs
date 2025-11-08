using UnityEngine;
using UnityEngine.XR;
using System.IO;
using Valve.VR; // SteamVR命名空间（若用其他SDK需替换）

public class VRCameraCapture : MonoBehaviour
{
    // 配置参数
    [Header("VR设置")]
    public SteamVR_Action_Boolean captureButton; // 绑定的手柄按钮（如扳机键）
    public SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.Any; // 手柄来源

    [Header("拍摄设置")]
    public Camera captureCamera; // 用于拍摄的相机（可设为VR主相机）
    public RawImage displayImage; // 显示拍摄画面的UI（可选）
    public string savePath = "VR_Captures/"; // 保存路径

    private RenderTexture renderTexture; // 临时渲染纹理


   
    void Update()
    {
        // 检测手柄按钮按下（根据SDK替换输入检测逻辑）
        if (captureButton.GetDown(inputSource))
        {
            CaptureEnvironment();
        }
    }

    /// <summary>
    /// 拍摄环境并处理（保存/显示）
    /// </summary>
    private WebCamTexture webCamTexture;

    void Start()
    {
        // 初始化真实摄像头
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            webCamTexture = new WebCamTexture(devices[0].name); // 使用第一个摄像头
            webCamTexture.Play();
        }
    }

    void CaptureEnvironment()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            Debug.LogError("未检测到可用摄像头");
            return;
        }

        // 将摄像头画面转为Texture2D
        Texture2D capturedTexture = new Texture2D(
            webCamTexture.width,
            webCamTexture.height,
            TextureFormat.RGB24,
            false
        );
        capturedTexture.SetPixels(webCamTexture.GetPixels());
        capturedTexture.Apply();

        // 后续显示/保存逻辑同上...
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}