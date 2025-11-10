// using UnityEngine;
// using Meta.XR.Camera;

// public class QuestCameraController : MonoBehaviour
// {
//     [SerializeField] private Renderer targetRenderer; // 用于显示摄像头画面的渲染器
//     private XRCameraSubsystem cameraSubsystem;

//     void Start()
//     {
//         // 获取XR摄像头子系统
//         cameraSubsystem = XRCameraSubsystem.GetActiveSubsystem<XRCameraSubsystem>();
//         if (cameraSubsystem == null)
//         {
//             Debug.LogError("未找到XR摄像头子系统，请检查配置");
//             return;
//         }

//         // 启动摄像头
//         cameraSubsystem.Start();
//         // 注册帧更新回调
//         cameraSubsystem.frameUpdated += OnCameraFrameUpdated;
//     }

//     void OnCameraFrameUpdated(XRCameraFrame frame)
//     {
//         // 获取摄像头纹理
//         if (frame.textures.TryGetValue(XRCameraTextureType.Color, out Texture2D colorTexture))
//         {
//             // 将纹理显示到目标渲染器（如平面）
//             targetRenderer.material.mainTexture = colorTexture;
//         }
//     }

//     void OnDestroy()
//     {
//         // 停止摄像头并清理回调
//         if (cameraSubsystem != null)
//         {
//             cameraSubsystem.frameUpdated -= OnCameraFrameUpdated;
//             cameraSubsystem.Stop();
//         }
//     }
// }