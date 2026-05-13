using System;
using UnityEngine;
using TreeDesigner;
using Animancer;
using EasyCharacterMovement;

#if UNITY_EDITOR
using UnityEditor;
#endif

// SkillCharacterController 定义在 UnityTimeline 命名空间
using UnityTimeline;

/// <summary>
/// AnimancerAbility 的 Action 节点基类，提供 AnimancerComponent 访问
/// </summary>
public abstract class AnimancerAbilityActionNode : ActionNode
{
    public AnimancerAbility AnimancerAbility => Owner as AnimancerAbility;
    public AnimancerComponent Animancer => (Owner as AnimancerAbility)?.AnimancerComponent;

    /// <summary>返回 AnimancerAbility 上缓存的 ECM2 Character 组件</summary>
    protected Character GetCharacter() => (Owner as AnimancerAbility)?.Character;

    /// <summary>返回 AnimancerAbility 上缓存的 SkillCharacterController</summary>
    protected SkillCharacterController GetSkillController() => (Owner as AnimancerAbility)?.SkillCharacterController;

    protected override void OnStart()
    {
        if (!AnimancerAbility)
        {
            return;
        }
        else
        {
            base.OnStart();
        }
    }
}




/// <summary>
/// AnimancerAbility 的 Value 节点基类，提供 AnimancerComponent 访问 + DoOuput 模式
/// </summary>
public abstract class AnimancerAbilityValueNode : ValueNode
{
    public AnimancerAbility AnimancerAbility => Owner as AnimancerAbility;
    public AnimancerComponent Animancer => (Owner as AnimancerAbility)?.AnimancerComponent;

    protected sealed override void OutputValue()
    {
        base.OutputValue();
        if (AnimancerAbility)
            DoOuput();
    }
    public abstract void DoOuput();
}

/// <summary>
/// AnimancerAbility 是否可以开始的条件节点
/// </summary>
[NodeName("AnimancerAbilityCanStart")]
[NodePath("AnimancerAbility/Value/AnimancerAbilityCanStart")]
public class AnimancerAbilityCanStartNode : ValueNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "Condition")]
    protected BoolPropertyPort m_Condition = new BoolPropertyPort();

    public override void Init(BaseTree tree)
    {
        base.Init(tree);
        if (Owner.User == null) return;
        if (Owner is AnimancerAbility animancerAbility)
            animancerAbility.AnimancerAbilityCanStart = this;
    }

    public bool GetValue()
    {
        InputValue();
        return m_Condition.Value;
    }

#if UNITY_EDITOR
    public override NodeCapabilities Capabilities => base.Capabilities | NodeCapabilities.Deletable | NodeCapabilities.Copiable;
    public override bool Single => true;
#endif
}

/// <summary>
/// AnimancerAbility 被取消时触发的事件节点
/// </summary>
[NodeName("OnAnimancerAbilityCancel")]
[NodePath("AnimancerAbility/Entry/OnAnimancerAbilityCancel")]
public class OnAnimancerAbilityCancelNode : EnterNode
{
    [SerializeField, PropertyPort(PortDirection.Output, "Ability"), TreeDesigner.ReadOnly]
    protected AnimancerAbilityPropertyPort m_Ability = new AnimancerAbilityPropertyPort();

    public override void Init(BaseTree tree)
    {
        base.Init(tree);
        if (Owner.User == null) return;
        if (Owner is AnimancerAbility animancerAbility)
            animancerAbility.OnAnimancerAbilityCancel = this;
    }

    public void Trigger(AnimancerAbility ability)
    {
        m_Ability.Value = ability;
        UpdateNode();
    }

#if UNITY_EDITOR
    public override NodeCapabilities Capabilities => base.Capabilities | NodeCapabilities.Deletable | NodeCapabilities.Copiable;
    public override bool Single => true;
    protected override string GetNodeName()
    {
        return "OnAnimancerAbilityCancel";
    }
#endif
}

/// <summary>
/// AnimancerAbility 的属性端口
/// </summary>
[Serializable]
public class AnimancerAbilityPropertyPort : PropertyPort<AnimancerAbility>
{
}

/// <summary>
/// 节点完成的时机
/// </summary>
public enum NodeCompletionMode
{
    /// <summary>播放开始即返回 Success</summary>
    OnStart,
    /// <summary>等待动画播放结束(OnEnd)才返回 Success</summary>
    OnEnd,
}

/// <summary>
/// 通过 Animacer 播放 PlayableAssetTransitionAsset (Timeline)
/// 纯叶子节点：播放 Timeline，根据 CompletionMode 决定何时返回 Success
/// </summary>
[NodeName("PlayAnimancerTimeline")]
[NodePath("AnimancerAbility/Action/PlayAnimancerTimeline")]
public class PlayAnimancerTimelineNode : AnimancerAbilityActionNode
{
    [SerializeField, ShowInPanel]
    protected Animancer.TransitionAssetBase m_TimelineAsset;

    [SerializeField, PropertyPort(PortDirection.Input, "FadeDuration")]
    protected FloatPropertyPort m_FadeDuration = new FloatPropertyPort() { Value = 0.25f };

    [SerializeField, PropertyPort(PortDirection.Input, "BindSignal")]
    protected BoolPropertyPort m_BindSignal = new BoolPropertyPort() { Value = false };

    [SerializeField, ShowInPanel, Tooltip("OnStart=播放成功立即Success, OnEnd=等动画播放完才Success")]
    protected NodeCompletionMode m_CompletionMode = NodeCompletionMode.OnStart;

    [NonSerialized]
    protected bool m_Completed = false;

    [NonSerialized]
    protected bool m_IsFailure = false;

    [SerializeField, PropertyPort(PortDirection.Output, "AnimancerState"), TreeDesigner.ReadOnly]
    protected AnimancerStatePropertyPort m_AnimancerState = new AnimancerStatePropertyPort();

    public override void ResetNode()
    {
        base.ResetNode();
        m_Completed = false;
        m_IsFailure = false;
    }

    protected override State OnUpdate()
    {
        if (!m_Completed) return State.Running;
        return m_IsFailure ? State.Failure : State.Success;
    }

    protected override void DoAction()
    {
        m_IsFailure = false;
        m_Completed = false;

        if (Animancer != null)
        {
            AnimancerState state = Animancer.PlayTimeline(m_TimelineAsset, m_FadeDuration.Value, default, m_BindSignal.Value);
            m_AnimancerState.Value = state;

            if (state != null)
            {
                // 播放成功
                if (m_CompletionMode == NodeCompletionMode.OnStart)
                {
                    // 立即完成模式
                    m_Completed = true;
                }
                else
                {
                    // 等待完成模式：注册 OnEnd 回调，动画结束时标记完成
                    state.Events(this).OnEnd -= OnDone;
                    state.Events(this).OnEnd += OnDone;
                }
            }
            else
            {
                // 播放失败(state==null)，返回 Failure
                m_Completed = true;
                m_IsFailure = true;
            }
        }
        else
        {
            // Animacer 为空，播放失败，返回 Failure
            m_Completed = true;
            m_IsFailure = true;
        }
    }

    void OnDone()
    {
        if (m_AnimancerState.Value != null)
        {
            m_AnimancerState.Value.Events(this).OnEnd -= OnDone;
        }
        m_Completed = true;
        m_IsFailure = false;
    }
}

/// <summary>
/// 通过 Animancer 播放 AnimationClip
/// 纯叶子节点：播放 AnimationClip，根据 CompletionMode 决定何时返回 Success
/// </summary>
[NodeName("PlayAnimancerTranslate")]
[NodePath("AnimancerAbility/Action/PlayAnimancerTranslate")]
public class PlayAnimancerTranslateNode : AnimancerAbilityActionNode
{
    [SerializeField, ShowInPanel]
    protected Animancer.TransitionAssetBase m_TransitionAsset;

    [SerializeField, PropertyPort(PortDirection.Input, "FadeDuration")]
    protected FloatPropertyPort m_FadeDuration = new FloatPropertyPort() { Value = 0.25f };

    [SerializeField, PropertyPort(PortDirection.Input, "Speed")]
    protected FloatPropertyPort m_Speed = new FloatPropertyPort() { Value = 1f };

    [SerializeField, ShowInPanel, Tooltip("OnStart=播放成功立即Success, OnEnd=等动画播放完才Success")]
    protected NodeCompletionMode m_CompletionMode = NodeCompletionMode.OnStart;

    [NonSerialized]
    protected bool m_Completed = false;

    [NonSerialized]
    protected bool m_IsFailure = false;

    [SerializeField, PropertyPort(PortDirection.Output, "AnimancerState"), TreeDesigner.ReadOnly]
    protected AnimancerStatePropertyPort m_AnimancerState = new AnimancerStatePropertyPort();

    public override void ResetNode()
    {
        base.ResetNode();
        m_Completed = false;
        m_IsFailure = false;
    }

    protected override State OnUpdate()
    {
        if (!m_Completed) return State.Running;
        return m_IsFailure ? State.Failure : State.Success;
    }

    protected override void DoAction()
    {
        m_IsFailure = false;
        m_Completed = false;

        if (Animancer != null && m_TransitionAsset != null)
        {
            AnimancerState state = Animancer.Play(m_TransitionAsset, m_FadeDuration.Value);
            m_AnimancerState.Value = state;
            if (state != null)
            {
                state.Speed = m_Speed.Value;

                // 播放成功
                if (m_CompletionMode == NodeCompletionMode.OnStart)
                {
                    // 立即完成模式
                    m_Completed = true;
                }
                else
                {
                    // 等待完成模式：注册 OnEnd 回调，动画结束时标记完成
                    state.Events(this).OnEnd -= OnDone;
                    state.Events(this).OnEnd += OnDone;
                }
            }
            else
            {
                // 播放失败(state==null)，返回 Failure
                m_Completed = true;
                m_IsFailure = true;
            }
        }
        else
        {
            // Animacer 或 TransitionAsset 为空，播放失败，返回 Failure
            m_Completed = true;
            m_IsFailure = true;
        }
    }

    void OnDone()
    {
        if (m_AnimancerState.Value != null)
        {
            m_AnimancerState.Value.Events(this).OnEnd -= OnDone;
        }
        m_Completed = true;
        m_IsFailure = false;
    }
}

/// <summary>
/// 停止 Animancer 动画
/// 即时操作节点，执行后立即返回 Success
/// </summary>
[NodeName("StopAnimancer")]
[NodePath("AnimancerAbility/Action/StopAnimancer")]
public class StopAnimacerNode : AnimancerAbilityActionNode
{
    protected override void DoAction()
    {
        if (Animancer != null)
        {
            Animancer.Stop();
        }
    }
}

/// <summary>
/// 获取 AnimancerState
/// </summary>
[NodeName("GetAnimancerState")]
[NodePath("AnimancerAbility/Value/GetAnimancerState")]
public class GetAnimancerStateNode : AnimancerAbilityValueNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "Key")]
    protected StringPropertyPort m_Key = new StringPropertyPort();

    [SerializeField, PropertyPort(PortDirection.Output, "AnimancerState"), TreeDesigner.ReadOnly]
    protected AnimancerStatePropertyPort m_AnimancerState = new AnimancerStatePropertyPort();

    public override void Init(BaseTree tree)
    {
        base.Init(tree);
    }

    public override void DoOuput()
    {
        if (Animancer != null)
        {
            if (Animancer.States.TryGet(m_Key.Value, out AnimancerState state))
            {
                m_AnimancerState.Value = state;
            }
        }
    }

#if UNITY_EDITOR
    public override bool Single => true;
#endif
}

/// <summary>
/// AnimancerState 属性端口
/// </summary>
[Serializable]
public class AnimancerStatePropertyPort : PropertyPort<AnimancerState>
{
}

/// <summary>
/// 对 AnimancerState 关联的 Animator 设置 Float 参数
/// </summary>
[NodeName("SetAnimatorFloat")]
[NodePath("AnimancerAbility/Action/SetAnimatorFloat")]
public class SetAnimatorFloatNode : AnimancerAbilityActionNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "AnimacerState")]
    protected AnimancerStatePropertyPort m_AnimacerState = new AnimancerStatePropertyPort();

    [SerializeField, PropertyPort(PortDirection.Input, "Key")]
    protected StringPropertyPort m_Key = new StringPropertyPort();

    [SerializeField, PropertyPort(PortDirection.Input, "Value")]
    protected FloatPropertyPort m_Value = new FloatPropertyPort();

    [SerializeField, ShowInPanel,
     Tooltip("若启用，无论参数设置是否成功均返回 Success；否则根据实际设置结果返回 Success 或 Failure")]
    protected bool m_IgnoreFailure = true;

    [NonSerialized]
    protected bool m_SetSuccess;

    public override State ReturnState => m_IgnoreFailure ? State.Success : (m_SetSuccess ? State.Success : State.Failure);

    public override void ResetNode()
    {
        base.ResetNode();
        m_SetSuccess = false;
    }

    protected override void DoAction()
    {
        m_SetSuccess = false;
        var state = m_AnimacerState.Value;
        if (state == null) return;
        if (string.IsNullOrEmpty(m_Key.Value)) return;
        // 优先通过 EP 获取当前实例的 Animator，避免多实例复用时 State 指向错误组件
        var animator = Animancer?.Animator ?? state.Graph?.Component?.Animator;
        if (animator == null) return;

        animator.SetFloat(m_Key.Value, m_Value.Value);
        m_SetSuccess = true;
    }
}

/// <summary>
/// 对 AnimancerState 关联的 Animator 设置 Int 参数
/// </summary>
[NodeName("SetAnimatorInt")]
[NodePath("AnimancerAbility/Action/SetAnimatorInt")]
public class SetAnimatorIntNode : AnimancerAbilityActionNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "AnimacerState")]
    protected AnimancerStatePropertyPort m_AnimacerState = new AnimancerStatePropertyPort();

    [SerializeField, PropertyPort(PortDirection.Input, "Key")]
    protected StringPropertyPort m_Key = new StringPropertyPort();

    [SerializeField, PropertyPort(PortDirection.Input, "Value")]
    protected IntPropertyPort m_Value = new IntPropertyPort();

    [SerializeField, ShowInPanel,
     Tooltip("若启用，无论参数设置是否成功均返回 Success；否则根据实际设置结果返回 Success 或 Failure")]
    protected bool m_IgnoreFailure = true;

    [NonSerialized]
    protected bool m_SetSuccess;

    public override State ReturnState => m_IgnoreFailure ? State.Success : (m_SetSuccess ? State.Success : State.Failure);

    public override void ResetNode()
    {
        base.ResetNode();
        m_SetSuccess = false;
    }

    protected override void DoAction()
    {
        m_SetSuccess = false;
        var state = m_AnimacerState.Value;
        if (state == null) return;
        if (string.IsNullOrEmpty(m_Key.Value)) return;
        // 优先通过 EP 获取当前实例的 Animator，避免多实例复用时 State 指向错误组件
        var animator = Animancer?.Animator ?? state.Graph?.Component?.Animator;
        if (animator == null) return;

        animator.SetInteger(m_Key.Value, m_Value.Value);
        m_SetSuccess = true;
    }
}

/// <summary>
/// 对 AnimancerState 关联的 Animator 设置 Bool 参数
/// </summary>
[NodeName("SetAnimatorBool")]
[NodePath("AnimancerAbility/Action/SetAnimatorBool")]
public class SetAnimatorBoolNode : AnimancerAbilityActionNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "AnimacerState")]
    protected AnimancerStatePropertyPort m_AnimacerState = new AnimancerStatePropertyPort();

    [SerializeField, PropertyPort(PortDirection.Input, "Key")]
    protected StringPropertyPort m_Key = new StringPropertyPort();

    [SerializeField, PropertyPort(PortDirection.Input, "Value")]
    protected BoolPropertyPort m_Value = new BoolPropertyPort();

    [SerializeField, ShowInPanel,
     Tooltip("若启用，无论参数设置是否成功均返回 Success；否则根据实际设置结果返回 Success 或 Failure")]
    protected bool m_IgnoreFailure = true;

    [NonSerialized]
    protected bool m_SetSuccess;

    public override State ReturnState => m_IgnoreFailure ? State.Success : (m_SetSuccess ? State.Success : State.Failure);

    public override void ResetNode()
    {
        base.ResetNode();
        m_SetSuccess = false;
    }

    protected override void DoAction()
    {
        m_SetSuccess = false;
        var state = m_AnimacerState.Value;
        if (state == null) return;
        if (string.IsNullOrEmpty(m_Key.Value)) return;
        // 优先通过 EP 获取当前实例的 Animator，避免多实例复用时 State 指向错误组件
        var animator = Animancer?.Animator ?? state.Graph?.Component?.Animator;
        if (animator == null) return;

        animator.SetBool(m_Key.Value, m_Value.Value);
        m_SetSuccess = true;
    }
}

/// <summary>
/// 对 AnimancerState 关联的 Animator 触发 Trigger
/// </summary>
[NodeName("SetAnimatorTrigger")]
[NodePath("AnimancerAbility/Action/SetAnimatorTrigger")]
public class SetAnimatorTriggerNode : AnimancerAbilityActionNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "AnimacerState")]
    protected AnimancerStatePropertyPort m_AnimacerState = new AnimancerStatePropertyPort();

    [SerializeField, PropertyPort(PortDirection.Input, "Key")]
    protected StringPropertyPort m_Key = new StringPropertyPort();

    [SerializeField, ShowInPanel,
     Tooltip("若启用，无论参数设置是否成功均返回 Success；否则根据实际设置结果返回 Success 或 Failure")]
    protected bool m_IgnoreFailure = true;

    [NonSerialized]
    protected bool m_SetSuccess;

    public override State ReturnState => m_IgnoreFailure ? State.Success : (m_SetSuccess ? State.Success : State.Failure);

    public override void ResetNode()
    {
        base.ResetNode();
        m_SetSuccess = false;
    }

    protected override void DoAction()
    {
        m_SetSuccess = false;
        var state = m_AnimacerState.Value;
        if (state == null) return;
        if (string.IsNullOrEmpty(m_Key.Value)) return;
        // 优先通过 EP 获取当前实例的 Animator，避免多实例复用时 State 指向错误组件
        var animator = Animancer?.Animator ?? state.Graph?.Component?.Animator;
        if (animator == null) return;

        animator.SetTrigger(m_Key.Value);
        m_SetSuccess = true;
    }
}

#region KCC 状态判断 ActionNode

/// <summary>
/// 判断角色是否正在移动（水平速度 >= 阈值），始终返回 Success
/// </summary>
[NodeName("IsMoving")]
[NodePath("AnimancerAbility/Condition/IsMoving")]
public class IsMovingNode : ValueNode
{
    [SerializeField, ShowInPanel, Tooltip("移动判断的速度阈值")]
    protected float m_SpeedThreshold = 0.1f;

    [SerializeField, PropertyPort(PortDirection.Output, "IsMoving"), ReadOnly]
    protected BoolPropertyPort m_IsMoving = new BoolPropertyPort();

    protected override void OutputValue()
    {
        base.OutputValue();
        var animancerAbility = Owner as AnimancerAbility;
        var controller = animancerAbility?.SkillCharacterController;
        if (controller == null) return;

        var velocity = controller.Motor.Velocity;
        var horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        m_IsMoving.Value = horizontalSpeed >= m_SpeedThreshold;
    }
}

/// <summary>
/// 判断角色是否在空中（非稳定着地状态），始终返回 Success
/// </summary>
[NodeName("IsInAir")]
[NodePath("AnimancerAbility/Condition/IsInAir")]
public class IsInAirNode : ValueNode
{
    [SerializeField, PropertyPort(PortDirection.Output, "IsInAir"), ReadOnly]
    protected BoolPropertyPort m_IsInAir = new BoolPropertyPort();

    protected override void OutputValue()
    {
        base.OutputValue();
        var animancerAbility = Owner as AnimancerAbility;
        var controller = animancerAbility?.SkillCharacterController;
        if (controller == null) return;

        m_IsInAir.Value = !controller.Motor.GroundingStatus.IsStableOnGround;
    }
}

/// <summary>
/// 判断角色是否在地面上（稳定着地状态），始终返回 Success
/// </summary>
[NodeName("IsGrounded")]
[NodePath("AnimancerAbility/Condition/IsGrounded")]
public class IsGroundedNode : ValueNode
{
    [SerializeField, PropertyPort(PortDirection.Output, "IsGrounded"), ReadOnly]
    protected BoolPropertyPort m_IsGrounded = new BoolPropertyPort();

    protected override void OutputValue()
    {
        base.OutputValue();
        var animancerAbility = Owner as AnimancerAbility;
        var controller = animancerAbility?.SkillCharacterController;
        if (controller == null) return;

        m_IsGrounded.Value = controller.Motor.GroundingStatus.IsStableOnGround;
    }
}

/// <summary>
/// 判断角色是否在地面移动（稳定着地 AND 水平速度 >= 阈值），始终返回 Success
/// </summary>
[NodeName("IsGroundMoving")]
[NodePath("AnimancerAbility/Condition/IsGroundMoving")]
public class IsGroundMovingNode : ValueNode
{
    [SerializeField, ShowInPanel, Tooltip("移动判断的速度阈值")]
    protected float m_SpeedThreshold = 0.1f;

    [SerializeField, PropertyPort(PortDirection.Output, "IsGroundMoving"), ReadOnly]
    protected BoolPropertyPort m_IsGroundMoving = new BoolPropertyPort();

    protected override void OutputValue()
    {
        base.OutputValue();
        var animancerAbility = Owner as AnimancerAbility;
        var controller = animancerAbility?.SkillCharacterController;
        if (controller == null) return;

        // 必须先在地面，否则输出 false
        if (!controller.Motor.GroundingStatus.IsStableOnGround)
        {
            m_IsGroundMoving.Value = false;
            return;
        }

        var velocity = controller.Motor.Velocity;
        var horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        m_IsGroundMoving.Value = horizontalSpeed >= m_SpeedThreshold;
    }
}

/// <summary>
/// 获取移动输入 Vector2 及角色局部坐标系下的相对移动方向 Vector2。
/// - MoveInput: 原始输入 (x=右, y=前)，直接从 InputAction 读取
/// - LocalMoveDir: 世界空间移动方向投影到角色 transform.right / transform.forward 得到的 (x, y)
/// </summary>
[NodeName("GetMoveInput")]
[NodePath("AnimancerAbility/Value/GetMoveInput")]
public class GetMoveInputNode : ValueNode
{
    [SerializeField, PropertyPort(PortDirection.Output, "MoveInput"), ReadOnly]
    protected Vector2PropertyPort m_MoveInput = new Vector2PropertyPort();

    [SerializeField, PropertyPort(PortDirection.Output, "LocalMoveDir"), ReadOnly]
    protected Vector2PropertyPort m_LocalMoveDir = new Vector2PropertyPort();

    protected override void OutputValue()
    {
        base.OutputValue();
        var animancerAbility = Owner as AnimancerAbility;
        var controller = animancerAbility?.SkillCharacterController;
        if (controller == null) return;

        // 原始移动输入
        Vector2 rawInput = Vector2.zero;
        if (controller.MoveAction != null && controller.MoveAction.action != null)
        {
            rawInput = controller.MoveAction.action.ReadValue<Vector2>();
        }
        m_MoveInput.Value = rawInput;

        // 世界空间移动方向（基于相机参照，只取 Y 轴旋转，与 SkillCharacterController 一致）
        Transform charTransform = controller.transform;
        Quaternion camRot = controller.OrientationReference != null
            ? Quaternion.Euler(0f, controller.OrientationReference.eulerAngles.y, 0f)
            : Quaternion.Euler(0f, charTransform.eulerAngles.y, 0f);

        Vector3 camForward = camRot * Vector3.forward;
        Vector3 camRight = camRot * Vector3.right;
        Vector3 worldMoveDir = camForward * rawInput.y + camRight * rawInput.x;

        // 投影到角色局部坐标系
        Vector3 charForward = charTransform.forward;
        Vector3 charRight = charTransform.right;
        float localX = Vector3.Dot(worldMoveDir, charRight);
        float localY = Vector3.Dot(worldMoveDir, charForward);
        m_LocalMoveDir.Value = new Vector2(localX, localY);
    }
}

/// <summary>
/// 设置角色的地面移动速度和/或空中移动速度
/// </summary>
[Serializable]
[NodeName("SetMoveSpeed")]
[NodePath("AnimancerAbility/Action/SetMoveSpeed")]
public class SetMoveSpeedAbilityNode : AnimancerAbilityActionNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "GroundSpeed"), ShowInPanel, Tooltip("地面最大移动速度，-1 表示不修改")]
    FloatPropertyPort m_GroundSpeed = new FloatPropertyPort() { Value = -1f };

    [SerializeField, PropertyPort(PortDirection.Input, "AirSpeed"), ShowInPanel, Tooltip("空中最大移动速度，-1 表示不修改")]
    FloatPropertyPort m_AirSpeed = new FloatPropertyPort() { Value = -1f };

    protected override void DoAction()
    {
        var controller = GetSkillController();
        if (controller == null) return;

        if (m_GroundSpeed.Value >= 0f)
            controller.MaxStableMoveSpeed = m_GroundSpeed.Value;

        if (m_AirSpeed.Value >= 0f)
            controller.MaxAirMoveSpeed = m_AirSpeed.Value;
    }
}

#endregion

#region Branch 分支节点

/// <summary>
/// 条件分支 Action 节点（通用基础类型）
/// 根据 Bool 条件选择执行 True 或 False 分支的子节点
/// 可在任何 RunnableTree 中使用
/// </summary>
[NodeName("Branch")]
[NodePath("Base/Action/Branch")]
[Output("True", PortCapacity.Single)]
[Output("False", PortCapacity.Single)]
public class BranchNode : ActionNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "Condition"), ShowInPanel]
    protected BoolPropertyPort m_Condition = new BoolPropertyPort();

    [SerializeField]
    protected string m_TrueEdgeGUID;

    [SerializeField]
    protected string m_FalseEdgeGUID;

    [NonSerialized]
    protected RunnableNode m_TrueChild;

    [NonSerialized]
    protected RunnableNode m_FalseChild;

    [NonSerialized]
    protected RunnableNode m_ActiveChild;

    public override void Init(BaseTree tree)
    {
        base.Init(tree);

        if (!string.IsNullOrEmpty(m_TrueEdgeGUID) && m_Owner.GUIDEdgeMap.ContainsKey(m_TrueEdgeGUID))
            m_TrueChild = m_Owner.GUIDEdgeMap[m_TrueEdgeGUID].EndNode as RunnableNode;

        if (!string.IsNullOrEmpty(m_FalseEdgeGUID) && m_Owner.GUIDEdgeMap.ContainsKey(m_FalseEdgeGUID))
            m_FalseChild = m_Owner.GUIDEdgeMap[m_FalseEdgeGUID].EndNode as RunnableNode;
    }

    public override void Dispose()
    {
        base.Dispose();
        m_TrueChild = null;
        m_FalseChild = null;
        m_ActiveChild = null;
    }

    public override void OnAfterDeserialize()
    {
        base.OnAfterDeserialize();
        m_TrueEdgeGUID = string.Empty;
        m_FalseEdgeGUID = string.Empty;
        m_TrueChild = null;
        m_FalseChild = null;
        m_ActiveChild = null;
    }

    public override void ResetNode()
    {
        base.ResetNode();
        m_ActiveChild?.ResetNode();
        m_ActiveChild = null;
    }

    protected override State OnUpdate()
    {
        if (m_ActiveChild != null)
            return m_ActiveChild.UpdateNode();
        return State.Success;
    }

    protected override void DoAction()
    {
        // 根据条件选择活跃分支
        m_ActiveChild = m_Condition.Value ? m_TrueChild : m_FalseChild;
    }

#if UNITY_EDITOR
    public override void OnOutputLinked(BaseEdge edge)
    {
        base.OnOutputLinked(edge);

        if (edge.StartPortName == "True")
        {
            m_TrueEdgeGUID = edge.GUID;
            m_TrueChild = edge.EndNode as RunnableNode;
        }
        else if (edge.StartPortName == "False")
        {
            m_FalseEdgeGUID = edge.GUID;
            m_FalseChild = edge.EndNode as RunnableNode;
        }
    }

    public override void OnOutputUnlinked(BaseEdge edge)
    {
        base.OnOutputUnlinked(edge);

        if (edge.StartPortName == "True")
        {
            m_TrueEdgeGUID = string.Empty;
            m_TrueChild = null;
        }
        else if (edge.StartPortName == "False")
        {
            m_FalseEdgeGUID = string.Empty;
            m_FalseChild = null;
        }
    }
#endif
}

#endregion

#region Vector2 分解

/// <summary>
/// 将 Vector2 分解为 X、Y 两个 float 输出
/// </summary>
[Serializable]
[NodeName("Vector2Split")]
[NodePath("Base/Value/Operate/Vector2Split")]
public class Vector2SplitNode : ValueNode
{
    [SerializeField, PropertyPort(PortDirection.Input, "Vector2"), ShowInPanel]
    protected Vector2PropertyPort m_Input = new Vector2PropertyPort();

    [SerializeField, PropertyPort(PortDirection.Output, "X"), ReadOnly]
    protected FloatPropertyPort m_X = new FloatPropertyPort();

    [SerializeField, PropertyPort(PortDirection.Output, "Y"), ReadOnly]
    protected FloatPropertyPort m_Y = new FloatPropertyPort();

    protected override void OutputValue()
    {
        base.OutputValue();
        m_X.Value = m_Input.Value.x;
        m_Y.Value = m_Input.Value.y;
    }
}

#endregion

#region Input 输入节点

/// <summary>
/// 检测指定 InputAction 是否被按下（IsPressed），输出 bool 值。
/// </summary>
[NodeName("InputKeyCondition")]
[NodePath("AnimancerAbility/Value/InputKeyCondition")]
public class AnimancerAbilityInputKeyConditionNode : ValueNode
{
    [SerializeField, ShowInPanel, Tooltip("要检测的 Input Action（从 Input Action Asset 拖入）")]
    protected UnityEngine.InputSystem.InputActionReference m_Action;

    [SerializeField, PropertyPort(PortDirection.Output, "Success"), ReadOnly]
    protected BoolPropertyPort m_IsPressed = new BoolPropertyPort();

    protected override void OutputValue()
    {
        base.OutputValue();
        var action = m_Action?.action;
        m_IsPressed.Value = action != null && action.IsPressed();
    }
}

#endregion
