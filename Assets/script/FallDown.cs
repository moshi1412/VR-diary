using UnityEngine;

public enum ControlMode { TeleportZone, FallDown, Idle }

[RequireComponent(typeof(Rigidbody), typeof(Renderer))] // 确保包含渲染器组件
public class BallMovementController : MonoBehaviour
{
    [Header("核心控制设置")]
    public ControlMode currentMode = ControlMode.Idle;
    public bool stopOnModeChange = true;

    [Header("=== 高度范围移动模式参数 ===")]
    [Tooltip("触发随机移动的最低高度（Y轴），球≥此高度时才会触发")]
    public float triggerHeight = 5f;
    [Tooltip("移动范围的中心（X/Z轴）")]
    public Vector2 horizontalCenter = new Vector2(0, 0);
    [Tooltip("X/Z轴上的移动范围（从中心向正负延伸）")]
    public Vector2 horizontalRange = new Vector2(5, 5);
    [Tooltip("在triggerHeight上方的浮动范围（仅向上延伸）")]
    public float upwardRange = 1f;
    public float moveSpeed = 5f;
    [Tooltip("到达目标后是否停止（否则持续移动）")]
    public bool stopAfterReach = true;

    [Header("=== 下落模式参数 ===")]
    public Vector3 fallTargetPosition = new Vector3(0, 0, 0);
    public float fallSmoothTime = 0.5f;
    public float fallMaxSpeed = 10f;
    public bool autoSwitchToIdleAfterFall = true;

    [Header("=== 预设材质 ===")]
    public Material pos;   // color=1 时使用
    public Material neu;   // color=0 时使用
    public Material neg;   // color=-1 时使用

    // 新增引用
    private Renderer ballRenderer; // 球的渲染器组件
    private DataManager dataManager; // 数据管理器引用

    // 状态变量
    public bool isAboveTriggerHeight = false; // 是否在触发高度之上
    private bool isTeleporting = false;       // 是否正在移动
    private Vector3 teleportTarget;           // 目标位置

    private bool isFalling = false;           // 是否正在下落
    private Vector3 fallCurrentVelocity;      // 下落的当前速度（用于平滑移动）
    private Rigidbody rb;                     // 球的刚体组件

    private void Awake()
    {
        // 获取刚体组件（必须）
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("球上未添加Rigidbody组件！请添加后再运行");
            enabled = false; // 禁用脚本避免报错
            return;
        }

        // 获取渲染器组件（用于切换材质）
        ballRenderer = GetComponent<Renderer>();
        if (ballRenderer == null)
        {
            Debug.LogError("球上未添加Renderer组件！无法切换材质");
            enabled = false;
            return;
        }

        // 初始化数据管理器引用
        dataManager = GameObject.FindWithTag("DataManager")?.GetComponent<DataManager>();
        if (dataManager == null)
        {
            Debug.LogError("未找到DataManager，请检查场景中是否存在该组件！");
        }
    }

    private void Update()
    {
        // 实时检测球是否在触发高度之上（核心判断）
        CheckHeightStatus();

        // 根据当前模式执行对应逻辑
        switch (currentMode)
        {
            case ControlMode.TeleportZone:
                if (isAboveTriggerHeight)
                {
                    // 若在高度范围内，确保刚体始终为运动学状态（屏蔽重力）
                    EnsureKinematicState(true);
                    if (isTeleporting)
                    {
                        UpdateTeleportMovement(); // 执行移动逻辑
                    }
                    else if (stopAfterReach == false)
                    {
                        // 若需要持续移动，且当前未移动，则生成目标并开始移动
                        GenerateTeleportTarget();
                        isTeleporting = true;
                    }
                }
                break;

            case ControlMode.FallDown:
                if (isFalling)
                {
                    UpdateFallMovement(); // 执行下落逻辑
                }
                break;

            case ControlMode.Idle:
                // 静止模式：确保刚体非运动学（受物理控制）
                EnsureKinematicState(false);
                break;
        }
    }

    #region 高度判断核心逻辑
    /// <summary>
    /// 检测球是否在触发高度之上
    /// </summary>
    private void CheckHeightStatus()
    {
        float currentY = transform.position.y;
        bool wasAbove = isAboveTriggerHeight;

        // 仅当Y轴在 [triggerHeight, triggerHeight + upwardRange] 范围内时，视为“在高度之上”
        isAboveTriggerHeight = currentY >= triggerHeight 
                            && currentY <= (triggerHeight + upwardRange);

        // 状态变化时触发对应逻辑
        if (isAboveTriggerHeight && !wasAbove)
        {
            OnEnterAboveHeight(); // 进入高度范围
        }
        else if (!isAboveTriggerHeight && wasAbove)
        {
            OnExitAboveHeight(); // 离开高度范围
        }
    }

    /// <summary>
    /// 进入高度范围时的处理（开始移动）
    /// </summary>
    private void OnEnterAboveHeight()
    {
        if (currentMode == ControlMode.TeleportZone)
        {
            GenerateTeleportTarget(); // 生成第一个目标位置
            isTeleporting = true;     // 开始移动
            EnsureKinematicState(true); // 强制开启运动学（屏蔽重力）
            Debug.Log($"球进入高度范围（Y≥{triggerHeight}），开始随机移动");
        }
    }

    /// <summary>
    /// 离开高度范围时的处理（停止移动，开始下落）
    /// </summary>
    private void OnExitAboveHeight()
    {
        if (currentMode == ControlMode.TeleportZone)
        {
            isTeleporting = false;    // 停止移动
            EnsureKinematicState(false); // 关闭运动学（受重力影响）
            Debug.Log($"球离开高度范围（Y<{triggerHeight}），开始自由下落");
        }
    }
    #endregion

    #region 高度范围内移动逻辑
    /// <summary>
    /// 生成高度范围内的随机目标位置
    /// </summary>
    private void GenerateTeleportTarget()
    {
        // X轴随机范围
        float randomX = Random.Range(
            horizontalCenter.x - horizontalRange.x / 2,
            horizontalCenter.x + horizontalRange.x / 2
        );

        // Z轴随机范围
        float randomZ = Random.Range(
            horizontalCenter.y - horizontalRange.y / 2,
            horizontalCenter.y + horizontalRange.y / 2
        );

        // Y轴随机范围（仅在触发高度上方）
        float randomY = Random.Range(
            triggerHeight,
            triggerHeight + upwardRange
        );

        teleportTarget = new Vector3(randomX, randomY, randomZ);
        Debug.Log($"生成新目标位置：{teleportTarget}");
    }

    /// <summary>
    /// 更新移动逻辑（向目标位置移动）
    /// </summary>
    private void UpdateTeleportMovement()
    {
        Vector3 currentPos = transform.position;
        float distanceToTarget = Vector3.Distance(currentPos, teleportTarget);

        // 到达目标位置
        if (distanceToTarget < 0.1f)
        {
            transform.position = teleportTarget; // 强制对齐目标
            isTeleporting = false;

            // 若需要持续移动，立即生成新目标
            if (!stopAfterReach)
            {
                GenerateTeleportTarget();
                isTeleporting = true;
            }
        }
        else
        {
            // 向目标移动（按速度计算每帧位移）
            Vector3 moveDir = (teleportTarget - currentPos).normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
    }
    #endregion

    #region 下落模式逻辑
    /// <summary>
    /// 开始下落（从当前位置移动到目标位置）
    /// </summary>
    public void StartFall()
    {
        if (currentMode != ControlMode.FallDown) return;

        isFalling = true;
        fallCurrentVelocity = Vector3.zero;
        EnsureKinematicState(true); // 下落过程中屏蔽物理干扰
        Debug.Log("开始下落模式移动");
    }

    /// <summary>
    /// 更新下落逻辑（平滑移动到目标位置）
    /// </summary>
    private void UpdateFallMovement()
    {
        // 平滑移动到目标位置
        transform.position = Vector3.SmoothDamp(
            transform.position,
            fallTargetPosition,
            ref fallCurrentVelocity,
            fallSmoothTime,
            fallMaxSpeed
        );

        // 到达目标位置
        if (Vector3.Distance(transform.position, fallTargetPosition) < 0.1f)
        {
            transform.position = fallTargetPosition; // 强制对齐
            isFalling = false;
            EnsureKinematicState(false); // 恢复物理控制
            rb.linearVelocity = Vector3.zero; // 清除速度，避免惯性
            Debug.Log("到达下落目标位置");

            // 关键逻辑：根据BallMemory的color属性切换材质
            UpdateMaterialByColor();

            // 自动切换到静止模式
            if (autoSwitchToIdleAfterFall)
            {
                SwitchToIdleMode();
                Debug.Log("自动切换到Idle模式");
            }
        }
    }

    /// <summary>
    /// 根据BallMemory的color属性更新材质
    /// </summary>
    private void UpdateMaterialByColor()
    {
        // 检查数据管理器和当前处理的球是否有效
        // if (dataManager == null || dataManager.BallOnProcess == null)
        // {
        //     Debug.LogError("数据管理器或当前处理的球为空，无法切换材质");
        //     return;
        // }

        // 获取球上的BallMemory组件
        BallMemory ballMemory = gameObject.GetComponent<BallMemory>();
        if (ballMemory == null || !ballMemory.BallData.HasValue)
        {
            Debug.LogError("球上未找到有效的BallMemory数据，无法切换材质");
            return;
        }

        // 根据color属性（-1、0、1）切换对应材质
        int colorValue = ballMemory.BallData.Value.color;
        switch (colorValue)
        {
            case 1:
                if (pos != null)
                {
                    ballRenderer.material = pos;
                    Debug.Log("材质已切换为：pos（对应color=1）");
                }
                else
                {
                    Debug.LogError("pos材质未赋值，无法切换");
                }
                break;
            case 0:
                if (neu != null)
                {
                    ballRenderer.material = neu;
                    Debug.Log("材质已切换为：neu（对应color=0）");
                }
                else
                {
                    Debug.LogError("neu材质未赋值，无法切换");
                }
                break;
            case -1:
                if (neg != null)
                {
                    ballRenderer.material = neg;
                    Debug.Log("材质已切换为：neg（对应color=-1）");
                }
                else
                {
                    Debug.LogError("neg材质未赋值，无法切换");
                }
                break;
            default:
                Debug.LogError($"无效的color值：{colorValue}，材质未切换");
                break;
        }
    }

    /// <summary>
    /// 停止下落（外部调用）
    /// </summary>
    private void StopFallMovement()
    {
        isFalling = false;
        EnsureKinematicState(false);
        fallCurrentVelocity = Vector3.zero;
    }
    #endregion

    #region 模式切换与状态管理
    /// <summary>
    /// 切换控制模式
    /// </summary>
    public void SwitchMode(ControlMode newMode)
    {
        if (currentMode == newMode) return; // 相同模式不处理

        // 切换前停止当前模式的运动
        if (stopOnModeChange)
        {
            switch (currentMode)
            {
                case ControlMode.TeleportZone:
                    isTeleporting = false;
                    EnsureKinematicState(false);
                    break;
                case ControlMode.FallDown:
                    StopFallMovement();
                    break;
                case ControlMode.Idle:
                    // 静止模式无需停止
                    break;
            }
        }

        // 更新模式并初始化新状态
        currentMode = newMode;
        switch (newMode)
        {
            case ControlMode.TeleportZone:
                // 若已在高度范围内，立即开始移动
                if (isAboveTriggerHeight)
                {
                    GenerateTeleportTarget();
                    isTeleporting = true;
                    EnsureKinematicState(true);
                }
                break;
            case ControlMode.FallDown:
                StartFall(); // 立即开始下落
                break;
            case ControlMode.Idle:
                EnsureKinematicState(false); // 恢复物理控制
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                break;
        }

        Debug.Log($"已切换到模式：{newMode}");
    }

    /// <summary>
    /// 确保刚体的运动学状态（防止意外状态）
    /// </summary>
    private void EnsureKinematicState(bool shouldBeKinematic)
    {
        if (rb == null) return;

        if (rb.isKinematic != shouldBeKinematic)
        {
            rb.isKinematic = shouldBeKinematic;
            // 调试日志：追踪状态变化（可删除）
            Debug.Log($"刚体运动学状态更新为：{shouldBeKinematic}");
        }
    }

    // 快捷切换方法（供UI或外部调用）
    public void SwitchToTeleportMode() => SwitchMode(ControlMode.TeleportZone);
    public void SwitchToFallMode() => SwitchMode(ControlMode.FallDown);
    public void SwitchToIdleMode() => SwitchMode(ControlMode.Idle);
    #endregion

    // 销毁时清理状态
    private void OnDestroy()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }
}