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
