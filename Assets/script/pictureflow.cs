using UnityEngine;

public class PicFloatEffect : MonoBehaviour
{
    private Vector3 originalLocalPos; // 初始位置（关键：必须在启用时记录）
    private float range; // 漂浮范围
    private float speed; // 漂浮速度
    private Vector2 phaseOffset; // 随机相位
    private bool isInitialized = false; // 是否初始化成功
    [Tooltip("当前图片对应的memoryId")]
    public int memoryId; // 存储记忆ID

    // 初始化漂浮参数（必须调用此方法才会生效）
    public void Init(float floatRange, float floatSpeed)
    {
        originalLocalPos = transform.localPosition; // 记录初始位置
        range = floatRange;
        speed = floatSpeed;
        // 随机相位（确保每个物体漂浮轨迹不同）
        phaseOffset = new Vector2(
            Random.Range(0, 2f * Mathf.PI),
            Random.Range(0, 2f * Mathf.PI)
        );
        isInitialized = true;
        Debug.Log($"{gameObject.name} 漂浮效果初始化完成：范围={range}，速度={speed}");
    }

    void Update()
    {
        if (!isInitialized)
        {
            // 未初始化时持续提示（排查是否漏调用Init）
            Debug.LogWarning($"{gameObject.name} 的漂浮效果未初始化！请调用Init方法");
            return;
        }

        // 计算漂浮偏移（基于正弦函数，平滑往复运动）
        float x = Mathf.Sin(Time.time * speed + phaseOffset.x) * range;
        float y = Mathf.Sin(Time.time * speed + phaseOffset.y + Mathf.PI/2) * range; // Y轴相位差，运动更自然
        transform.localPosition = new Vector3(
            originalLocalPos.x + x,
            originalLocalPos.y + y,
            originalLocalPos.z
        );
    }
}