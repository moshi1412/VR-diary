using UnityEngine;
using System.IO;

public class SkyboxSetup : MonoBehaviour
{
    void Start()
    {
        if (SkyboxSceneData.Instance == null)
        {
            Debug.LogError("未找到SkyboxSceneData实例，请确保在源场景中存在该物体");
            return;
        }

        string picturePath = SkyboxSceneData.Instance.targetPicturePath;
        if (string.IsNullOrEmpty(picturePath) || !File.Exists(picturePath))
        {
            Debug.LogError($"图片路径无效或不存在: {picturePath}");
            return;
        }

        // 加载图片并设置为天空盒
        LoadAndSetSkybox(picturePath);
    }

    private void LoadAndSetSkybox(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError("图片文件不存在：" + imagePath);
            return;
        }

        // 读取图片文件
        byte[] imageData = File.ReadAllBytes(imagePath);
        Texture2D skyboxTexture = new Texture2D(2048, 2048, TextureFormat.RGB24, false);
        bool loadSuccess = skyboxTexture.LoadImage(imageData); // 自动解析图片格式
        if (!loadSuccess)
        {
            Debug.LogError("图片加载失败，可能格式不支持：" + imagePath);
            return;
        }

        // 设置纹理属性（全景图需要Clamp模式避免边缘重复）
        skyboxTexture.wrapMode = TextureWrapMode.Clamp; // 全景图用Clamp更合适
        skyboxTexture.filterMode = FilterMode.Trilinear;
        skyboxTexture.anisoLevel = 9;
        skyboxTexture.Apply();

        // 创建天空盒材质：使用支持2D全景图的着色器
        Material skyboxMat = new Material(Shader.Find("Skybox/Panoramic"));
        if (skyboxMat == null)
        {
            Debug.LogError("找不到Skybox/Panoramic着色器，请确保包含该着色器");
            return;
        }

        // 给全景图着色器赋值2D纹理（主纹理属性名为"_MainTex"）
        skyboxMat.SetTexture("_MainTex", skyboxTexture);
        // 可选：设置全景图投影方式（球形、圆柱形等，默认是球形）
        skyboxMat.SetInt("_Projection", 0); // 0=球形，1=圆柱形，2=镜像球形

        // 应用天空盒
        RenderSettings.skybox = skyboxMat;
        DynamicGI.UpdateEnvironment();
        Debug.Log("全景天空盒设置成功");
    }
    }