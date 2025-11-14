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
        // 读取图片文件
        byte[] imageData = File.ReadAllBytes(imagePath);
        Texture2D skyboxTexture = new Texture2D(2048, 2048, TextureFormat.RGB24, false);
        skyboxTexture.LoadImage(imageData); // 自动解析图片格式

        // 设置纹理属性（重要：天空盒需要重复模式和过滤模式设置）
        skyboxTexture.wrapMode = TextureWrapMode.Repeat;
        skyboxTexture.filterMode = FilterMode.Trilinear;
        skyboxTexture.anisoLevel = 9;
        skyboxTexture.Apply();

        // 创建天空盒材质
        Material skyboxMat = new Material(Shader.Find("Skybox/Cubemap"));
        // 如果是全景图，需要转换为立方体贴图（这里简化处理，实际可能需要根据图片类型处理）
        // 注意：普通PNG可能需要特殊处理（如球形映射），建议使用专门的天空盒Shader
        skyboxMat.mainTexture = skyboxTexture;

        // 应用天空盒
        RenderSettings.skybox = skyboxMat;
        DynamicGI.UpdateEnvironment(); // 更新全局光照
    }
}