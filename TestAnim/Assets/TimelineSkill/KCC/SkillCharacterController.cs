using System;
using UnityEngine;
using KinematicCharacterController;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace UnityTimeline
{
    /// <summary>RootMotion 处理模式</summary>
    public enum RootMotionMode
    {
        FullRootMotion,
        IgnoreRootMotion
    }

    /// <summary>跳跃模式</summary>
    public enum JumpMode
    {
        BuiltIn,
        ExternalControlled
    }

    /// <summary>旋转朝向方法</summary>
    public enum OrientationMethod
    {
        TowardsReference,
        TowardsMovement,
        TowardsMouse
    }

    /// <summary>玩家角色输入数据</summary>
    [System.Serializable]
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public bool JumpDown;
        public Quaternion CameraRotation;
    }

    /// <summary>输入锁定标志位，支持分级控制</summary>
    [System.Flags]
    public enum InputLockFlags
    {
        None             = 0,
        Movement         = 1 << 0,  // 锁定移动输入 (MoveAxisForward / MoveAxisRight)
        Jump             = 1 << 1,  // 锁定跳跃输入 (JumpDown)
        AbilityInput     = 1 << 2,  // 锁定 AnimancerAbilityLinker 的所有 InputBinding 响应
        CinemachineCamera = 1 << 3, // 禁用 Cinemachine 相机输入 (CinemachineInputProvider)
        All              = Movement | Jump | AbilityInput | CinemachineCamera,
    }

    /// <summary>
    /// 基于 KCC + InputSystem + RootMotion 补偿的一体化角色控制器。
    /// 支持 RootMotion 双模式(完全/忽略)、跳跃双模式(内置/外部控制)、补偿 API。
    /// 只需挂载此脚本，所有依赖组件会自动添加到 GameObject 上。
    /// </summary>
    [RequireComponent(typeof(KinematicCharacterMotor), typeof(Animator))]
    public class SkillCharacterController : MonoBehaviour, ICharacterController
    {
        #region Serialized Fields - Input Settings

        [Header("Input Settings")]
        [Tooltip("移动输入 Action (Vector2)，从 Input Action Asset 拖入")]
        [SerializeField] private InputActionReference _moveAction;

        [Tooltip("跳跃输入 Action (Button)，从 Input Action Asset 拖入")]
        [SerializeField] private InputActionReference _jumpAction;

        [Tooltip("方向参照物 Transform（通常为相机），为空时使用角色自身朝向")]
        [SerializeField] private Transform _orientationReference;

        #endregion

        #region Serialized Fields - Root Motion Mode

        [Header("Root Motion Mode")]
        [Tooltip("地面时的 RootMotion 处理方式")]
        [SerializeField] private RootMotionMode _groundRootMotionMode = RootMotionMode.FullRootMotion;

        [Tooltip("空中时的 RootMotion 处理方式")]
        [SerializeField] private RootMotionMode _airRootMotionMode = RootMotionMode.IgnoreRootMotion;

        #endregion

        #region Serialized Fields - Jump Settings

        [Header("Jump Settings")]
        [Tooltip("跳跃模式选择")]
        [SerializeField] private JumpMode _jumpMode = JumpMode.BuiltIn;

        [Tooltip("起跳初始向上速度")]     [SerializeField] private float _jumpUpSpeed = 10f;
        [Tooltip("最大空中跳跃次数（-1=不限制，0=不可空中跳，1=1次空中跳，2=2次…）")] [SerializeField] private int _maxAirJumps = -1;
        [Tooltip("起跳时前进速度")]         [SerializeField] private float _jumpScalableForwardSpeed = 10f;
        [Tooltip("起跳前离地宽容时间(秒)")] [SerializeField] private float _jumpPreGroundingGraceTime = 0f;
        [Tooltip("起跳后落地宽容时间(秒)")] [SerializeField] private float _jumpPostGroundingGraceTime = 0f;
        [Tooltip("斜坡滑动时是否允许起跳")] [SerializeField] private bool _allowJumpingWhenSliding = true;

        #endregion

        #region Serialized Fields - Stable Movement (IgnoreRM)

        [Header("Stable Movement (Ignore Root Motion)")]
        [Tooltip("最大地面移动速度")]       [SerializeField] private float _maxStableMoveSpeed = 6f;
        [Tooltip("地面速度变化锐度")]        [SerializeField] private float _stableMovementSharpness = 15f;
        [Tooltip("旋转朝向插值锐度")]        [SerializeField] private float _orientationSharpness = 20f;
        [Tooltip("朝向策略")]                [SerializeField] private OrientationMethod _orientationMethod = OrientationMethod.TowardsMovement;
        [Tooltip("角色朝向与目标方向夹角超过此角度时，必须先旋转到位再移动(度)。0=禁用该机制，始终可边转边移")] 
        [SerializeField] private float _rotationLockAngle = 0f;

        [Tooltip("TowardsMouse 模式：鼠标距角色小于此距离时不更新朝向（避免抖动）")]
        [SerializeField] private float _mouseDeadzone = 0.5f;

        #endregion

        #region Serialized Fields - Air Movement (IgnoreRM)

        [Header("Air Movement (Ignore Root Motion)")]
        [Tooltip("最大空中速度")]            [SerializeField] private float _maxAirMoveSpeed = 10f;
        [Tooltip("空中加速度")]              [SerializeField] private float _airAccelerationSpeed = 30f;
        [Tooltip("空气阻力")]                [SerializeField] private float _drag = 0.5f;

        #endregion

        #region Serialized Fields - Misc

        [Header("Misc")]
        [Tooltip("重力向量")]               [SerializeField] private Vector3 _gravity = new Vector3(0, -30f, 0);
        [Tooltip("角色模型根节点")]          [SerializeField] private Transform _meshRoot;
        [Tooltip("忽略的碰撞体列表")]        [SerializeField] private Collider[] _ignoredColliders;

        #endregion

        #region Public Properties

        public KinematicCharacterMotor Motor { get; private set; }
        public Animator CharacterAnimator { get; private set; }
        public bool CompensationEnabled { get; set; } = true;
        public InputActionReference MoveAction => _moveAction;
        public InputActionReference JumpAction => _jumpAction;
        public Transform OrientationReference => _orientationReference;
        public RootMotionMode GroundRootMotionMode { get => _groundRootMotionMode; set => _groundRootMotionMode = value; }
        public RootMotionMode AirRootMotionMode { get => _airRootMotionMode; set => _airRootMotionMode = value; }
        public JumpMode CurrentJumpMode => _jumpMode;
        public float RotationLockAngle { get => _rotationLockAngle; set => _rotationLockAngle = value; }
        public float MaxStableMoveSpeed { get => _maxStableMoveSpeed; set => _maxStableMoveSpeed = value; }
        public float MaxAirMoveSpeed { get => _maxAirMoveSpeed; set => _maxAirMoveSpeed = value; }

        /// <summary>
        /// 外部目标朝向方向（世界空间）。
        /// 设为非 zero 时优先使用此方向作为旋转目标；设为 Vector3.zero 则回退到输入计算。
        /// 典型用法：技能系统控制角色面向特定目标/方向。
        /// </summary>
        public Vector3 ExternalTargetDirection { get; set; } = Vector3.zero;

        #endregion

        #region Input Lock API — 每通道独立 Tag 系统

        /// <summary>输入锁变化事件，参数为当前生效的总锁标志位</summary>
        public event Action<InputLockFlags> OnInputLockChanged;

        // ====================================================================
        //  核心 API
        // ====================================================================

        /// <summary>
        /// 为指定通道添加锁标记。多通道可通过 flags 组合（如 Movement | Jump）。
        /// 同一 tag 可在多个通道独立存在，各通道 tag 集合非空即视为锁定。
        /// </summary>
        /// <param name="tag">持有者标识（如 "SkillPlay", "Dialogue", "Cutscene"）</param>
        /// <param name="channels">要锁定的通道</param>
        public void AddInputLock(string tag, InputLockFlags channels)
        {
            if (string.IsNullOrEmpty(tag) || channels == InputLockFlags.None) return;
            bool changed = false;
            foreach (var channel in _singleChannels)
            {
                if ((channels & channel) != 0)
                {
                    if (!_channelLockTags.TryGetValue(channel, out var tags))
                    {
                        tags = new HashSet<string>();
                        _channelLockTags[channel] = tags;
                    }
                    changed |= tags.Add(tag);
                }
            }
            if (changed) OnInputLockChanged?.Invoke(GetEffectiveLocks());
        }

        /// <summary>
        /// 移除指定 tag 在指定通道的锁（精细控制）。
        /// </summary>
        public void RemoveInputLock(string tag, InputLockFlags channels)
        {
            if (string.IsNullOrEmpty(tag)) return;
            bool changed = false;
            foreach (var channel in _singleChannels)
            {
                if ((channels & channel) != 0)
                {
                    if (_channelLockTags.TryGetValue(channel, out var tags))
                        changed |= tags.Remove(tag);
                }
            }
            if (changed) OnInputLockChanged?.Invoke(GetEffectiveLocks());
        }

        /// <summary>
        /// 移除指定 tag 在所有通道的锁（快捷方式）。
        /// </summary>
        public void RemoveInputLock(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            bool changed = false;
            foreach (var kvp in _channelLockTags)
                changed |= kvp.Value.Remove(tag);
            if (changed) OnInputLockChanged?.Invoke(GetEffectiveLocks());
        }

        /// <summary>查询指定通道是否被锁定（任一通道有 tag 即返回 true）。</summary>
        public bool IsInputLocked(InputLockFlags channels)
        {
            foreach (var channel in _singleChannels)
            {
                if ((channels & channel) != 0)
                {
                    if (_channelLockTags.TryGetValue(channel, out var tags) && tags.Count > 0)
                        return true;
                }
            }
            return false;
        }

        /// <summary>查询是否有任何通道被锁定。</summary>
        public bool IsInputLocked() => GetEffectiveLocks() != InputLockFlags.None;

        /// <summary>检查指定 tag 是否在任何通道持有锁。</summary>
        public bool HasInputLockTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            foreach (var kvp in _channelLockTags)
            {
                if (kvp.Value.Contains(tag)) return true;
            }
            return false;
        }

        /// <summary>清除所有通道的所有锁标记。</summary>
        public void ClearAllInputLocks()
        {
            foreach (var kvp in _channelLockTags)
                kvp.Value.Clear();
            OnInputLockChanged?.Invoke(InputLockFlags.None);
        }

        // ====================================================================
        //  兼容 API（门面）
        // ====================================================================

        /// <summary>
        /// 输入总开关。false=全部锁定（builtin tag），true=解除 builtin tag。
        /// </summary>
        public bool InputEnabled
        {
            get => GetEffectiveLocks() == InputLockFlags.None;
            set
            {
                if (value)
                    RemoveInputLock(kBuiltinLockTag);
                else
                    AddInputLock(kBuiltinLockTag, InputLockFlags.All);
            }
        }

        /// <summary>直接设置输入锁标志位（覆盖式，仅影响 builtin tag）。</summary>
        public void SetInputLock(InputLockFlags flags)
        {
            // 先移除 builtin 在所有通道的标记
            RemoveInputLock(kBuiltinLockTag);
            // 再在指定通道加回
            if (flags != InputLockFlags.None)
                AddInputLock(kBuiltinLockTag, flags);
        }

        // ====================================================================
        //  内部方法
        // ====================================================================

        /// <summary>聚合所有通道，返回当前被锁定的通道集合。</summary>
        private InputLockFlags GetEffectiveLocks()
        {
            InputLockFlags result = InputLockFlags.None;
            foreach (var channel in _singleChannels)
            {
                if (_channelLockTags.TryGetValue(channel, out var tags) && tags.Count > 0)
                    result |= channel;
            }
            return result;
        }

        #endregion

        #region Private State

        private Vector3 _rootMotionPositionDelta;
        private Quaternion _rootMotionRotationDelta;
        private PlayerCharacterInputs _inputs;
        private bool _jumpInputBuffered;
        private int _runtimeAirJumps;
        private float _timeSinceLeftGround;

        // Compensation state
        private Vector3 _compensationPosition;
        private Vector3 _compensationRotationEuler;
        private int _compensationFrames;
        private const int kDefaultCompensationFrames = 2;

        // Cached effective target direction (shared between UpdateRotation and UpdateVelocity)
        private Vector3 _effectiveMoveDir;

        // Cached raw input move direction (includes left/right strafe for TowardsReference mode)
        private Vector3 _inputMoveDir;

        // Rotation lock: when angle between current facing and target > threshold, movement is locked
        private bool _movementLocked;

        // Cached camera for TowardsMouse mode
        private Camera _cachedCamera;

        // === Input Lock System — 每通道独立 Tag 存储 ===
        private const string kBuiltinLockTag = "__builtin__";

        /// <summary>所有单通道枚举值，用于遍历</summary>
        private static readonly InputLockFlags[] _singleChannels = new[]
        {
            InputLockFlags.Movement,
            InputLockFlags.Jump,
            InputLockFlags.AbilityInput,
            InputLockFlags.CinemachineCamera,
        };

        /// <summary>每个通道独立维护的 Tag 集合</summary>
        private readonly Dictionary<InputLockFlags, HashSet<string>> _channelLockTags = new()
        {
            { InputLockFlags.Movement,          new HashSet<string>() },
            { InputLockFlags.Jump,              new HashSet<string>() },
            { InputLockFlags.AbilityInput,      new HashSet<string>() },
            { InputLockFlags.CinemachineCamera, new HashSet<string>() },
        };

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Motor = GetComponent<KinematicCharacterMotor>();
            CharacterAnimator = GetComponent<Animator>();
            Motor.CharacterController = this;
            KinematicCharacterSystem.EnsureCreation();
            if (CharacterAnimator != null)
                CharacterAnimator.applyRootMotion = true;
            ResetRootMotionState();
            ClearCompensation();
            _timeSinceLeftGround = -1f;
        }

        private void Update() => ReadInput();

        private void OnAnimatorMove()
        {
            if (!CompensationEnabled || _compensationFrames <= 0)
            {
                if (CharacterAnimator != null)
                {
                    _rootMotionPositionDelta += CharacterAnimator.deltaPosition;
                    _rootMotionRotationDelta = CharacterAnimator.deltaRotation * _rootMotionRotationDelta;
                }
            }
            else
            {
                transform.position = _compensationPosition;
                transform.rotation = Quaternion.Euler(_compensationRotationEuler);
                _compensationFrames--;
                if (_compensationFrames <= 0)
                    ClearCompensation();
            }
        }

        private void Reset()
        {
            if (!TryGetComponent(out KinematicCharacterMotor motor))
                motor = gameObject.AddComponent<KinematicCharacterMotor>();
            Motor = motor;

            if (!TryGetComponent(out Animator animator))
                animator = gameObject.AddComponent<Animator>();
            CharacterAnimator = animator;
        }

        #endregion

        #region Input Reading

        private void ReadInput()
        {
            _inputs = default;

            InputLockFlags locks = GetEffectiveLocks();

            // 移动输入（受 Movement 锁控制）
            if ((locks & InputLockFlags.Movement) == 0)
            {
                if (_moveAction != null && _moveAction.action != null)
                {
                    Vector2 moveInput = _moveAction.action.ReadValue<Vector2>();
                    _inputs.MoveAxisForward = moveInput.y;
                    _inputs.MoveAxisRight = moveInput.x;
                }
            }

            // 跳跃输入（受 Jump 锁控制）— 累积式缓存
            // triggered 本身是边缘触发（PressOnly），按住不会重复
            if ((locks & InputLockFlags.Jump) == 0)
            {
                if (_jumpAction != null && _jumpAction.action != null)
                {
                    if (_jumpAction.action.triggered)
                        _jumpInputBuffered = true;
                }
            }

            // CameraRotation 不受锁影响（朝向参照物始终有效）
            _inputs.CameraRotation = _orientationReference != null
                ? Quaternion.Euler(0, _orientationReference.eulerAngles.y, 0)
                : Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

        public void SetInputs(PlayerCharacterInputs inputs) => _inputs = inputs;

        #endregion

        #region ICharacterController Implementation

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            bool isGrounded = Motor.GroundingStatus.IsStableOnGround;
            RootMotionMode mode = isGrounded ? _groundRootMotionMode : _airRootMotionMode;

            // === 计算有效目标方向（外部优先 > 输入计算）===
            Vector3 forward = _inputs.CameraRotation * Vector3.forward;
            Vector3 right = _inputs.CameraRotation * Vector3.right;
            Vector3 inputMoveDir = forward * _inputs.MoveAxisForward + right * _inputs.MoveAxisRight;
            if (inputMoveDir.sqrMagnitude > 1f) inputMoveDir.Normalize();

            // 外部目标朝向优先
            if (ExternalTargetDirection.sqrMagnitude > 0.001f)
            {
                _effectiveMoveDir = ExternalTargetDirection.normalized;
                _inputMoveDir = _effectiveMoveDir;
            }
            else if (_orientationMethod == OrientationMethod.TowardsMouse)
            {
                // TowardsMouse: 面向鼠标在 XZ 平面的投影方向，移动方向=完整输入(支持横移)
                _inputMoveDir = inputMoveDir;

                if (_cachedCamera == null && _orientationReference != null)
                    _cachedCamera = _orientationReference.GetComponent<Camera>();

                if (_cachedCamera != null && Mouse.current != null)
                {
                    Vector2 mousePos = Mouse.current.position.ReadValue();
                    Plane groundPlane = new Plane(Vector3.up, Motor.Transform.position);
                    Ray ray = _cachedCamera.ScreenPointToRay(mousePos);
                    if (groundPlane.Raycast(ray, out float dist))
                    {
                        Vector3 dir = ray.GetPoint(dist) - Motor.Transform.position;
                        dir.y = 0f;
                        if (dir.sqrMagnitude > _mouseDeadzone * _mouseDeadzone)
                            _effectiveMoveDir = dir.normalized;
                    }
                }
            }
            else if (_orientationMethod == OrientationMethod.TowardsReference)
            {
                // TowardsReference: 旋转目标=forward(面朝参照物)，但移动方向=完整输入(支持横移)
                _effectiveMoveDir = forward;
                _inputMoveDir = inputMoveDir;
            }
            else
            {
                _effectiveMoveDir = inputMoveDir;
                _inputMoveDir = inputMoveDir;
            }

            // === 旋转锁定检测：仅 TowardsMovement 模式下生效 ===
            _movementLocked = false;
            if (_rotationLockAngle > 0.001f && _effectiveMoveDir.sqrMagnitude > 0.001f
                && _orientationMethod == OrientationMethod.TowardsMovement)
            {
                Vector3 currentForward = currentRotation * Vector3.forward;
                float angleToTarget = Vector3.Angle(currentForward, _effectiveMoveDir);
                if (angleToTarget > _rotationLockAngle)
                    _movementLocked = true;
            }

            // === 应用旋转 ===
            if (mode == RootMotionMode.FullRootMotion)
            {
                // FullRootMotion: 动画旋转叠加在目标朝向之上
                currentRotation = _rootMotionRotationDelta * currentRotation;

                if (_effectiveMoveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(_effectiveMoveDir, Motor.CharacterUp);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRot,
                        1f - Mathf.Exp(-_orientationSharpness * deltaTime));
                }
            }
            else
            {
                // IgnoreRootMotion: 完全由目标方向控制旋转
                if (_effectiveMoveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(_effectiveMoveDir, Motor.CharacterUp);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRot,
                        1f - Mathf.Exp(-_orientationSharpness * deltaTime));
                }
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            bool isGrounded = Motor.GroundingStatus.IsStableOnGround;
            RootMotionMode mode = isGrounded ? _groundRootMotionMode : _airRootMotionMode;

            if (_jumpMode == JumpMode.BuiltIn)
                HandleBuiltInJump(ref currentVelocity, deltaTime);

            if (isGrounded)
                HandleGroundedVelocity(ref currentVelocity, deltaTime, mode);
            else
                HandleAirVelocity(ref currentVelocity, deltaTime, mode);

            // 重力（非稳定着地时施加）
            if (!isGrounded || !Motor.GroundingStatus.IsStableOnGround)
                currentVelocity += _gravity * deltaTime;
        }

        private void HandleGroundedVelocity(ref Vector3 velocity, float dt, RootMotionMode mode)
        {
            if (mode == RootMotionMode.FullRootMotion)
            {
                velocity = _rootMotionPositionDelta.sqrMagnitude > 0.0001f
                    ? _rootMotionPositionDelta / dt
                    : Vector3.zero;

                // 重定向到地面法线平面
                Vector3 groundNormal = Motor.GroundingStatus.GroundNormal;
                if (groundNormal != Motor.CharacterUp)
                    velocity = Vector3.ProjectOnPlane(velocity, groundNormal);
            }
            else
            {
                // === 旋转锁定：角度差过大时只旋转不移动 ===
                if (_movementLocked)
                {
                    // 锁定移动，仅保留地面法线平面内的速度分量（防止滑动）
                    Vector3 groundNormal = Motor.GroundingStatus.GroundNormal;
                    Vector3 velOnPlane = Vector3.ProjectOnPlane(velocity, groundNormal);
                    // 仅衰减水平速度，保持与地面的贴合
                    velocity = Vector3.Lerp(velOnPlane, Vector3.zero,
                        1f - Mathf.Exp(-_stableMovementSharpness * dt));
                    return;
                }

                // 正常移动：根据朝向策略选择移动方向
                bool hasMoveInput = _inputs.MoveAxisForward != 0f || _inputs.MoveAxisRight != 0f
                                     || ExternalTargetDirection.sqrMagnitude > 0.001f;

                // TowardsReference/TowardsMouse 模式: 使用完整输入方向(含横移)，旋转目标仍为 forward/鼠标
                // TowardsMovement 模式: 使用 _effectiveMoveDir(与旋转朝向一致)
                Vector3 moveDirForVelocity = ((_orientationMethod == OrientationMethod.TowardsReference
                    || _orientationMethod == OrientationMethod.TowardsMouse)
                    && ExternalTargetDirection.sqrMagnitude < 0.001f)
                    ? _inputMoveDir : _effectiveMoveDir;

                if (hasMoveInput && moveDirForVelocity.sqrMagnitude > 0.001f)
                {
                    Vector3 targetVel = moveDirForVelocity * _maxStableMoveSpeed;
                    velocity = Vector3.Lerp(velocity, targetVel,
                        1f - Mathf.Exp(-_stableMovementSharpness * dt));
                }
                else
                {
                    // 无输入时减速停止
                    velocity = Vector3.Lerp(velocity, Vector3.zero,
                        1f - Mathf.Exp(-_stableMovementSharpness * dt));
                }
            }
        }

        private void HandleAirVelocity(ref Vector3 velocity, float dt, RootMotionMode mode)
        {
            if (mode == RootMotionMode.FullRootMotion)
            {
                if (_rootMotionPositionDelta.sqrMagnitude > 0.0001f)
                    velocity = _rootMotionPositionDelta / dt;
                // 否则保持当前速度，重力在末尾统一加
            }
            else
            {
                // 根据朝向策略选择空中移动方向（TowardsReference/TowardsMouse 支持横移）
                Vector3 airMoveDir = ((_orientationMethod == OrientationMethod.TowardsReference
                    || _orientationMethod == OrientationMethod.TowardsMouse)
                    && ExternalTargetDirection.sqrMagnitude < 0.001f)
                    ? _inputMoveDir : _effectiveMoveDir;

                bool hasMoveInput = _inputs.MoveAxisForward != 0f || _inputs.MoveAxisRight != 0f
                                     || ExternalTargetDirection.sqrMagnitude > 0.001f;
                Vector3 hVel = new Vector3(velocity.x, 0f, velocity.z);

                if (hasMoveInput && airMoveDir.sqrMagnitude > 0.0001f)
                {
                    hVel += airMoveDir * _airAccelerationSpeed * dt;
                    if (hVel.magnitude > _maxAirMoveSpeed)
                        hVel = hVel.normalized * _maxAirMoveSpeed;
                }
                else
                {
                    hVel /= (1f + _drag * dt);
                }

                velocity.x = hVel.x;
                velocity.z = hVel.z;
            }
        }

        private void HandleBuiltInJump(ref Vector3 velocity, float dt)
        {
            if (!_jumpInputBuffered)
                return;

            bool canGroundJump = Motor.GroundingStatus.IsStableOnGround ||
                                 (_timeSinceLeftGround >= 0f &&
                                  _timeSinceLeftGround < _jumpPreGroundingGraceTime);

            bool canAirJump = !canGroundJump;

            // 空中跳次数限制检查：次数用完则消费输入但不跳（-1=不限制）
            if (canAirJump && _maxAirJumps >= 0 && _runtimeAirJumps >= _maxAirJumps)
            {
                _jumpInputBuffered = false;
                return;
            }

            if (canGroundJump)
            {
                Motor.ForceUnground();

                Vector3 forward = _inputs.CameraRotation * Vector3.forward;
                Vector3 right = _inputs.CameraRotation * Vector3.right;
                Vector3 moveDir = (forward * _inputs.MoveAxisForward + right * _inputs.MoveAxisRight).normalized;

                velocity += Motor.CharacterUp * _jumpUpSpeed + moveDir * _jumpScalableForwardSpeed;
                _jumpInputBuffered = false;
                _timeSinceLeftGround = _jumpPreGroundingGraceTime;
                _runtimeAirJumps = 0;
            }
            else if (canAirJump)
            {
                Vector3 forward = _inputs.CameraRotation * Vector3.forward;
                Vector3 right = _inputs.CameraRotation * Vector3.right;
                Vector3 moveDir = (forward * _inputs.MoveAxisForward + right * _inputs.MoveAxisRight).normalized;

                velocity += Motor.CharacterUp * _jumpUpSpeed + moveDir * _jumpScalableForwardSpeed;
                _jumpInputBuffered = false;
                ++_runtimeAirJumps;
            }
        }

        public void BeforeCharacterUpdate(float deltaTime) { }

        public void PostGroundingUpdate(float deltaTime)
        {
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                _timeSinceLeftGround = -1f;
                _runtimeAirJumps = 0; // 落地重置空中跳次数
            }

            if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
                _timeSinceLeftGround = 0f;
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            ResetRootMotionState();

            if (!Motor.GroundingStatus.IsStableOnGround && _timeSinceLeftGround >= 0f)
                _timeSinceLeftGround += deltaTime;
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (_ignoredColliders != null)
            {
                for (int i = 0; i < _ignoredColliders.Length; i++)
                    if (coll == _ignoredColliders[i]) return false;
            }
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport) { }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport) { }

        public void ProcessHitStabilityReport(
            Collider hitCollider,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 hitPositionInLocalSpace,
            Quaternion hitRotation,
            ref HitStabilityReport hitStabilityReport) { }

        public void OnDiscreteCollisionDetected(Collider coll) { }

        #endregion

        #region RootMotion State

        private void ResetRootMotionState()
        {
            _rootMotionPositionDelta = Vector3.zero;
            _rootMotionRotationDelta = Quaternion.identity;
        }

        #endregion

        #region Compensation API

        /// <summary>设置补偿位置偏移</summary>
        public void SetCompensationPosition(Vector3 position)
        {
            _compensationPosition = position;
            _compensationRotationEuler = transform.rotation.eulerAngles;
            _compensationFrames = kDefaultCompensationFrames;
        }

        /// <summary>设置补偿旋转偏移（Euler 角度）</summary>
        public void SetCompensationRotation(Vector3 eulerAngles)
        {
            _compensationPosition = transform.position;
            _compensationRotationEuler = eulerAngles;
            _compensationFrames = kDefaultCompensationFrames;
        }

        /// <summary>同时设置位置和旋转补偿</summary>
        public void SetCompensation(Vector3 position, Vector3 rotationEuler)
        {
            _compensationPosition = position;
            _compensationRotationEuler = rotationEuler;
            _compensationFrames = kDefaultCompensationFrames;
        }

        /// <summary>清除所有补偿</summary>
        public void ClearCompensation()
        {
            _compensationPosition = Vector3.zero;
            _compensationRotationEuler = Vector3.zero;
            _compensationFrames = 0;
        }

        #endregion
    }
}
