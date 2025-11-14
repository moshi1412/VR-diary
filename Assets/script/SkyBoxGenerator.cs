using UnityEngine;

public class SkyboxSceneData : MonoBehaviour
{
    public static SkyboxSceneData Instance;
    public string targetPicturePath; // 存储要用作天空盒的图片路径

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保留
        }
        else
        {
            Destroy(gameObject);
        }
    }
}