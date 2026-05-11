#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine;
using Cinemachine;

/// <summary>
/// 继承 CinemachineInputProvider，在 Body 为 Transposer 模式下，
/// 通过 Z Axis（鼠标滚轮等）输入实现相机拉近/拉远操作。
/// 
/// 用法：将此组件替代 CinemachineInputProvider 挂载到 VirtualCamera 上。
/// Body 设置为 Transposer 或 OrbitalTransposer 均可使用。
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
[AddComponentMenu("Cinemachine/Cinemachine Transposer Zoom")]
public class CinemachineTransposerZoom : CinemachineInputProvider
{
    [Header("Zoom Settings")]
    [Tooltip("缩放速度倍率")]
    public float ZoomSpeed = 2f;

    [Tooltip("最小 Follow Offset 距离（相机最近距离）")]
    public float MinOffset = 2f;

    [Tooltip("最大 Follow Offset 距离（相机最远距离）")]
    public float MaxOffset = 20f;

    [Tooltip("缩放平滑时间（越大越平滑）")]
    [Range(0f, 1f)]
    public float SmoothTime = 0.1f;

    private CinemachineVirtualCamera m_VCam;
    private CinemachineTransposer m_Transposer;
    private float m_TargetDistance;
    private float m_CurrentVelocity;

    private void Start()
    {
        m_VCam = GetComponent<CinemachineVirtualCamera>();
        ResolveTransposer();
    }

    private void ResolveTransposer()
    {
        if (m_VCam == null) return;

        // 同时支持 Transposer 和 OrbitalTransposer（OrbitalTransposer 继承自 Transposer）
        m_Transposer = m_VCam.GetCinemachineComponent<CinemachineTransposer>();
        if (m_Transposer == null)
            m_Transposer = m_VCam.GetCinemachineComponent<CinemachineOrbitalTransposer>();

        if (m_Transposer != null)
        {
            // 初始化目标距离为当前 FollowOffset 的长度
            m_TargetDistance = m_Transposer.m_FollowOffset.magnitude;
            m_TargetDistance = Mathf.Clamp(m_TargetDistance, MinOffset, MaxOffset);
        }
    }

    private void Update()
    {
        if (m_Transposer == null)
        {
            ResolveTransposer();
            if (m_Transposer == null) return;
        }

        // 读取 Z Axis 输入（axis 2，通常是鼠标滚轮）
        float zInput = GetAxisValue(2);
        if (Mathf.Abs(zInput) > 0.001f)
        {
            m_TargetDistance -= zInput * ZoomSpeed;
            m_TargetDistance = Mathf.Clamp(m_TargetDistance, MinOffset, MaxOffset);
        }

        // 平滑插值到目标距离
        float currentDistance = m_Transposer.m_FollowOffset.magnitude;
        if (currentDistance < 0.001f) return; // 避免除零

        float newDistance;
        if (SmoothTime > 0.001f)
        {
            newDistance = Mathf.SmoothDamp(currentDistance, m_TargetDistance,
                ref m_CurrentVelocity, SmoothTime);
        }
        else
        {
            newDistance = m_TargetDistance;
        }

        // 保持 FollowOffset 方向不变，只改变长度
        Vector3 direction = m_Transposer.m_FollowOffset.normalized;
        m_Transposer.m_FollowOffset = direction * newDistance;
    }

    private void OnValidate()
    {
        // 确保 Min 不大于 Max
        if (MinOffset > MaxOffset)
            MinOffset = MaxOffset;
        if (MinOffset < 0.1f)
            MinOffset = 0.1f;
    }
}
#else
using UnityEngine;
using Cinemachine;

/// <summary>
/// 需要安装 Input System Package 才能使用此组件。
/// </summary>
[AddComponentMenu("")] // Hide in menu when Input System not available
public class CinemachineTransposerZoom : CinemachineInputProvider { }
#endif
