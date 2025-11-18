// using UnityEngine;
// using Oculus.Platform; // 注意：部分版本SDK可能无需此命名空间，若报错可删除

// public class ControllerHandler : MonoBehaviour
// {
//     // 初始化时注册手柄连接事件
//     void OnEnable()
//     {
//         // 监听手柄连接/断开事件（来自OVRManager）
//         OVRManager.ControllerConnected += OnControllerConnected;
//         OVRManager.ControllerDisconnected += OnControllerDisconnected;
//     }

//     // 脚本禁用时移除事件监听（避免内存泄漏）
//     void OnDisable()
//     {
//         OVRManager.ControllerConnected -= OnControllerConnected;
//         OVRManager.ControllerDisconnected -= OnControllerDisconnected;
//     }

//     // 每帧检测手柄输入（Update中执行，确保实时性）
//     void Update()
//     {
//         // 检测左手柄和右手柄的输入
//         CheckControllerInput(OVRInput.Controller.LTouch); // 左手柄
//         CheckControllerInput(OVRInput.Controller.RTouch); // 右手柄
//     }

//     // 手柄连接事件回调
//     void OnControllerConnected(OVRInput.Controller controller)
//     {
//         string controllerName = GetControllerName(controller);
//         Debug.Log($"✅ 手柄已连接：{controllerName}");
//     }

//     // 手柄断开事件回调
//     void OnControllerDisconnected(OVRInput.Controller controller)
//     {
//         string controllerName = GetControllerName(controller);
//         Debug.Log($"❌ 手柄已断开：{controllerName}");
//     }

//     // 检测单个手柄的按键/轴输入
//     void CheckControllerInput(OVRInput.Controller targetController)
//     {
//         // 先判断手柄是否连接（避免无效检测）
//         if (!OVRInput.IsControllerConnected(targetController))
//             return;

//         string controllerName = GetControllerName(targetController);

//         // 1. 检测按键按下（一次触发）
//         if (OVRInput.GetDown(OVRInput.Button.One, targetController))
//             Debug.Log($"{controllerName}：A键 按下");

//         if (OVRInput.GetDown(OVRInput.Button.Two, targetController))
//             Debug.Log($"{controllerName}：B键 按下");

//         // 2. 检测菜单键（肩键）按住
//         if (OVRInput.Get(OVRInput.Button.PrimaryShoulder, targetController))
//             Debug.Log($"{controllerName}：菜单键 按住");

//         // 3. 检测扳机键压力（0~1）
//         float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, targetController);
//         if (triggerValue > 0.1f) // 超过10%压力时输出
//             Debug.Log($"{controllerName}：扳机键 压力值：{triggerValue:F2}");

//         // 4. 检测握柄键压力（0~1）
//         float gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, targetController);
//         if (gripValue > 0.1f)
//             Debug.Log($"{controllerName}：握柄键 压力值：{gripValue:F2}");

//         // 5. 检测摇杆位置（X/Y轴）
//         Vector2 stickValue = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, targetController);
//         if (stickValue.sqrMagnitude > 0.1f) // 摇杆有明显移动时输出
//             Debug.Log($"{controllerName}：摇杆位置：X={stickValue.x:F2}, Y={stickValue.y:F2}");

//         // 6. 检测摇杆按下
//         if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, targetController))
//             Debug.Log($"{controllerName}：摇杆 按下");
//     }

//     // 辅助方法：将控制器枚举转换为“左手柄/右手柄”名称
//     string GetControllerName(OVRInput.Controller controller)
//     {
//         if (controller == OVRInput.Controller.LTouch) return "左手柄";
//         if (controller == OVRInput.Controller.RTouch) return "右手柄";
//         return "未知手柄";
//     }
// }