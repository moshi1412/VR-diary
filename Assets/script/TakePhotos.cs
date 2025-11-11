// using UnityEngine;
// using UnityEngine.XR.MagicLeap;
// using System.IO;

// public class TakePhotoWithPassthrough : MonoBehaviour
// {
//     private Texture2D capturedTexture;
//     public string photoSavePath = "/Screenshots/";

//     void Start()
//     {
//         // 确保保存照片的路径存在
//         string fullPath = Application.persistentDataPath + photoSavePath;
//         if (!Directory.Exists(fullPath))
//         {
//             Directory.CreateDirectory(fullPath);
//         }
//     }

//     public void TakePhoto()
//     {
//         // 创建一个与屏幕分辨率相同的Texture2D
//         int width = Screen.width;
//         int height = Screen.height;
//         capturedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);

//         // 渲染当前的Passthrough画面到Texture2D
//         RenderTexture.active = RenderTexture.GetTemporary(width, height, 24);
//         Camera.main.Render();
//         capturedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//         capturedTexture.Apply();
//         RenderTexture.ReleaseTemporary(RenderTexture.active);

//         // 保存照片到指定路径
//         string fullPath = Application.persistentDataPath + photoSavePath + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
//         byte[] bytes = capturedTexture.EncodeToPNG();
//         File.WriteAllBytes(fullPath, bytes);
//         Debug.Log("Photo saved to: " + fullPath);

//         // 释放Texture2D资源
//         Destroy(capturedTexture);
//     }
// }