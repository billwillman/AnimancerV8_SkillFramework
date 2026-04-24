using UnityEngine;
using KinematicCharacterController;
using UnityEngine.InputSystem;

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
        TowardsMovement
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
        public RootMotionMode GroundRootMotionMode => _groundRootMotionMode;
        public RootMotionMode AirRootMotionMode => _airRootMotionMode;
        public JumpMode CurrentJumpMode => _jumpMode;

        #endregion

        #region Private State

        private Vector3 _rootMotionPositionDelta;
        private Quaternion _rootMotionRotationDelta;
        private PlayerCharacterInputs _inputs;
        private bool _jumpRequested;
        private bool _jumpedThisFrame;
        private float _timeSinceLastAbleToJump;

        // Compensation state
        private Vector3 _compensationPosition;
        private Vector3 _compensationRotationEuler;
        private int _compensationFrames;
        private const int kDefaultCompensationFrames = 2;

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
            _timeSinceLastAbleToJump = -1f;
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

            if (_moveAction != null && _moveAction.action != null)
            {
                Vector2 moveInput = _moveAction.action.ReadValue<Vector2>();
                _inputs.MoveAxisForward = moveInput.y;
                _inputs.MoveAxisRight = moveInput.x;
            }

            if (_jumpAction != null && _jumpAction.action != null)
                _inputs.JumpDown = _jumpAction.action.triggered;

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

            // 计算输入方向（两种模式都需要）
            Vector3 forward = _inputs.CameraRotation * Vector3.forward;
            Vector3 right = _inputs.CameraRotation * Vector3.right;
            Vector3 moveDir = forward * _inputs.MoveAxisForward + right * _inputs.MoveAxisRight;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            Vector3 targetDir = _orientationMethod == OrientationMethod.TowardsReference
                ? forward : moveDir;

            if (mode == RootMotionMode.FullRootMotion)
            {
                // FullRootMotion: 动画旋转叠加在输入朝向之上
                // 先应用动画的旋转增量（如转身动画等）
                currentRotation = _rootMotionRotationDelta * currentRotation;

                // 然后如果有输入，用输入方向插值修正朝向（大多数走/跑动画不含有效旋转）
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(targetDir, Motor.CharacterUp);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRot,
                        1f - Mathf.Exp(-_orientationSharpness * deltaTime));
                }
            }
            else
            {
                // IgnoreRootMotion: 完全由输入控制旋转
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(targetDir, Motor.CharacterUp);
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
                Vector3 forward = _inputs.CameraRotation * Vector3.forward;
                Vector3 right = _inputs.CameraRotation * Vector3.right;
                Vector3 moveDir = (forward * _inputs.MoveAxisForward + right * _inputs.MoveAxisRight).normalized;

                Vector3 targetVel = moveDir * _maxStableMoveSpeed;
                velocity = Vector3.Lerp(velocity, targetVel,
                    1f - Mathf.Exp(-_stableMovementSharpness * dt));
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
                Vector3 forward = _inputs.CameraRotation * Vector3.forward;
                Vector3 right = _inputs.CameraRotation * Vector3.right;
                Vector3 moveDir = (forward * _inputs.MoveAxisForward + right * _inputs.MoveAxisRight).normalized;

                Vector3 targetVel = moveDir * _maxAirMoveSpeed;
                Vector3 hVel = new Vector3(velocity.x, 0f, velocity.z);

                if (targetVel.sqrMagnitude > 0.0001f)
                {
                    hVel += moveDir * _airAccelerationSpeed * dt;
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
            _jumpedThisFrame = false;
            _timeSinceLastAbleToJump += dt;

            if (!_inputs.JumpDown) return;

            bool canJump = Motor.GroundingStatus.IsStableOnGround ||
                           (_timeSinceLastAbleToJump >= 0f && _timeSinceLastAbleToJump < _jumpPreGroundingGraceTime);

            if (canJump && _timeSinceLastAbleToJump < 0f) canJump = false;

            if (canJump)
            {
                Motor.ForceUnground();

                Vector3 forward = _inputs.CameraRotation * Vector3.forward;
                Vector3 right = _inputs.CameraRotation * Vector3.right;
                Vector3 moveDir = (forward * _inputs.MoveAxisForward + right * _inputs.MoveAxisRight).normalized;

                velocity += Motor.CharacterUp * _jumpUpSpeed + moveDir * _jumpScalableForwardSpeed;

                _jumpRequested = false;
                _jumpedThisFrame = true;
                _timeSinceLastAbleToJump = -1f;
            }
            else
            {
                _jumpRequested = true;
            }
        }

        public void BeforeCharacterUpdate(float deltaTime) { }

        public void PostGroundingUpdate(float deltaTime)
        {
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                if (_jumpMode == JumpMode.BuiltIn && !_jumpedThisFrame &&
                    deltaTime < _jumpPostGroundingGraceTime)
                {
                    _timeSinceLastAbleToJump = _jumpPreGroundingGraceTime -
                                               (_jumpPostGroundingGraceTime - deltaTime);
                }
                else
                {
                    _timeSinceLastAbleToJump = 0f;
                }
            }

            if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
                _timeSinceLastAbleToJump = -1f;
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            ResetRootMotionState();

            if (_jumpRequested && Motor.GroundingStatus.IsStableOnGround)
            {
                _jumpRequested = false;
                _timeSinceLastAbleToJump = 0f;
            }
            else if (!_jumpedThisFrame)
            {
                _timeSinceLastAbleToJump += deltaTime;
            }
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

        public void OnDiscreteCollisionDetected(Collider hitCollider) { }

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
