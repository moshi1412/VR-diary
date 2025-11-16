using UnityEngine;

public class BallTeleportZone : MonoBehaviour
{
    [Header("移动范围设置")]
    [Tooltip("随机位置的中心原点")]
    public Vector3 randomRangeCenter;
    [Tooltip("随机位置在X/Y/Z轴上的范围（从中心向正负方向延伸）")]
    public Vector3 randomRangeSize;

    [Header("移动参数")]
    [Tooltip("移动速度（单位：米/秒）")]
    public float moveSpeed = 5f;
    [Tooltip("是否到达目标后停止（否则持续随机移动）")]
    public bool stopAfterReach = true;

    // 标记球是否正在移动（避免重复触发）
    private bool isMoving = false;
    // 目标位置
    private Vector3 targetPosition;
    // 当前要移动的球
    private Rigidbody targetBallRb;

    private void OnTriggerEnter(Collider other)
    {
        // 检测进入的是否是球（假设球的标签为"Ball"）
        if (other.CompareTag("Ball") && !isMoving)
        {
            // 获取球的Rigidbody（用于控制移动，避免物理冲突）
            targetBallRb = other.GetComponent<Rigidbody>();
            if (targetBallRb != null)
            {
                // 生成随机目标位置
                GenerateRandomTargetPosition();
                // 开始移动
                isMoving = true;
                // 暂时冻结球的物理（避免移动时受重力/碰撞影响）
                targetBallRb.isKinematic = true;
            }
        }
    }

    private void Update()
    {
        // 如果球正在移动，执行平滑移动
        if (isMoving && targetBallRb != null)
        {
            // 计算当前位置到目标位置的方向和距离
            Vector3 currentPosition = targetBallRb.transform.position;
            float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);

            // 如果到达目标位置（距离小于0.1米）
            if (distanceToTarget < 0.1f)
            {
                // 强制设置到目标位置（避免微小偏移）
                targetBallRb.transform.position = targetPosition;
                // 停止移动
                isMoving = false;
                // 恢复物理（如果需要）
                targetBallRb.isKinematic = false;

                // 如果不需要持续移动，直接退出
                if (stopAfterReach)
                {
                    targetBallRb = null;
                    return;
                }

                // 如果需要持续移动，生成新的目标位置
                GenerateRandomTargetPosition();
            }
            else
            {
                // 平滑移动：按速度向目标位置移动（每帧移动距离 = 速度 * 时间）
                Vector3 moveDirection = (targetPosition - currentPosition).normalized;
                targetBallRb.transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }
        }
    }

    // 生成范围内的随机目标位置
    private void GenerateRandomTargetPosition()
    {
        // 在指定范围内随机生成X/Y/Z坐标
        float randomX = Random.Range(
            randomRangeCenter.x - randomRangeSize.x / 2,
            randomRangeCenter.x + randomRangeSize.x / 2
        );
        float randomY = Random.Range(
            randomRangeCenter.y - randomRangeSize.y / 2,
            randomRangeCenter.y + randomRangeSize.y / 2
        );
        float randomZ = Random.Range(
            randomRangeCenter.z - randomRangeSize.z / 2,
            randomRangeCenter.z + randomRangeSize.z / 2
        );

        targetPosition = new Vector3(randomX, randomY, randomZ);
        Debug.Log($"生成新目标位置：{targetPosition}");
    }
   
}