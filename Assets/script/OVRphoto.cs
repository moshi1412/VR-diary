// using UnityEngine;
// using Oculus.Platform;
// using Oculus.Platform.Models;

// public class OculusCameraController : MonoBehaviour
// {
//     [SerializeField] private Renderer targetRenderer;
//     private Texture2D cameraTexture;

//     void Start()
//     {
//         // 初始化Oculus平台（需在开发者后台配置App ID）
//         Core.Initialize();

//         // 请求摄像头权限
//         Requests.Camera.RequestPermissions().OnComplete(OnCameraPermission);
//     }

//     void OnCameraPermission(Message<CameraPermission> msg)
//     {
//         if (msg.IsError)
//         {
//             Debug.LogError("摄像头权限请求失败：" + msg.GetError().Message);
//             return;
//         }

//         if (msg.Data.Granted)
//         {
//             // 启动摄像头
//             OVRPlugin.camera.Start();
//             // 设置摄像头回调
//             OVRPlugin.camera.SetFrameCallback(OnCameraFrame);
//         }
//     }

//     void OnCameraFrame(OVRPlugin.CameraFrame frame)
//     {
//         // 转换帧数据为Texture2D
//         if (cameraTexture == null || cameraTexture.width != frame.Width || cameraTexture.height != frame.Height)
//         {
//             cameraTexture = new Texture2D((int)frame.Width, (int)frame.Height, TextureFormat.RGBA32, false);
//         }
//         cameraTexture.LoadRawTextureData(frame.ColorBuffer);
//         cameraTexture.Apply();

//         // 显示纹理
//         targetRenderer.material.mainTexture = cameraTexture;
//     }

//     void OnDestroy()
//     {
//         OVRPlugin.camera.Stop();
//     }
// }