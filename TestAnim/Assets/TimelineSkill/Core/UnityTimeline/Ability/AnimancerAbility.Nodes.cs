using System;
using UnityEngine;
using TreeDesigner;
using Animancer;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AnimancerAbility 的 Action 节点基类，提供 AnimancerComponent 访问
/// </summary>
public abstract class AnimancerAbilityActionNode : ActionNode
{
    public AnimancerAbility AnimancerAbility => Owner as AnimancerAbility;
    public AnimancerComponent Animancer => (Owner as AnimancerAbility)?.AnimancerComponent;

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
/// 通过 Animancer 播放 PlayableAssetTransitionAsset (Timeline)
/// </summary>
[NodeName("PlayAnimancerTimeline")]
[NodePath("AnimancerAbility/Action/PlayAnimancerTimeline")]
public class PlayAnimancerTimelineNode : AnimancerAbilityActionNode
{
    [SerializeField]
    protected string m_OutputEdgeGUID;
    public string OutputGUID => m_OutputEdgeGUID;

    [NonSerialized]
    protected RunnableNode m_Child;
    public RunnableNode Child => m_Child;

    [SerializeField, ShowInPanel]
    protected Animancer.TransitionAssetBase m_TimelineAsset;

    [SerializeField, PropertyPort(PortDirection.Input, "FadeDuration")]
    protected FloatPropertyPort m_FadeDuration = new FloatPropertyPort() { Value = 0.25f };

    [SerializeField, PropertyPort(PortDirection.Input, "BindSignal")]
    protected BoolPropertyPort m_BindSignal = new BoolPropertyPort() { Value = false };

    [SerializeField, PropertyPort(PortDirection.Output, "AnimancerState"), TreeDesigner.ReadOnly]
    protected AnimancerStatePropertyPort m_AnimancerState = new AnimancerStatePropertyPort();

    public override void Init(BaseTree tree)
    {
        base.Init(tree);
        if (!string.IsNullOrEmpty(m_OutputEdgeGUID) && m_Owner.GUIDEdgeMap.ContainsKey(m_OutputEdgeGUID))
            m_Child = m_Owner.GUIDEdgeMap[m_OutputEdgeGUID].EndNode as RunnableNode;
    }

    public override void Dispose()
    {
        base.Dispose();
        m_Child = null;
    }

    public override void OnAfterDeserialize()
    {
        base.OnAfterDeserialize();
        m_OutputEdgeGUID = string.Empty;
        m_Child = null;
    }

    public override void ResetNode()
    {
        base.ResetNode();
        m_Child?.ResetNode();
    }

    protected override State OnUpdate()
    {
        return State.Running;
    }

    protected override void DoAction()
    {
        if (Animancer != null)
        {
            AnimancerState state = Animancer.PlayTimeline(m_TimelineAsset, m_FadeDuration.Value, default, m_BindSignal.Value);
            m_AnimancerState.Value = state;

            if (state != null && m_Child != null)
            {
                state.Events(this).OnEnd -= OnDone;
                state.Events(this).OnEnd += OnDone;
            }
            else if (m_Child != null)
            {
                m_Child.UpdateNode();
            }
        }
    }

    void OnDone()
    {
        if (m_AnimancerState.Value != null)
        {
            m_AnimancerState.Value.Events(this).OnEnd -= OnDone;
            m_Child?.UpdateNode();
        }
    }

#if UNITY_EDITOR
    public override void OnOutputLinked(BaseEdge edge)
    {
        base.OnOutputLinked(edge);
        m_OutputEdgeGUID = edge.GUID;
        m_Child = edge.EndNode as RunnableNode;
    }

    public override void OnOutputUnlinked(BaseEdge edge)
    {
        base.OnOutputUnlinked(edge);
        m_OutputEdgeGUID = string.Empty;
        m_Child = null;
    }
#endif
}

/// <summary>
/// 通过 Animancer 播放 AnimationClip
/// </summary>
[NodeName("PlayAnimancerTranslate")]
[NodePath("AnimancerAbility/Action/PlayAnimancerTranslate")]
public class PlayAnimancerTranslateNode : AnimancerAbilityActionNode
{
    [SerializeField]
    protected string m_OutputEdgeGUID;
    public string OutputGUID => m_OutputEdgeGUID;

    [NonSerialized]
    protected RunnableNode m_Child;
    public RunnableNode Child => m_Child;

    [SerializeField, ShowInPanel]
    protected Animancer.TransitionAssetBase m_TransitionAsset;

    [SerializeField, PropertyPort(PortDirection.Input, "FadeDuration")]
    protected FloatPropertyPort m_FadeDuration = new FloatPropertyPort() { Value = 0.25f };

    [SerializeField, PropertyPort(PortDirection.Input, "Speed")]
    protected FloatPropertyPort m_Speed = new FloatPropertyPort() { Value = 1f };

    [SerializeField, PropertyPort(PortDirection.Output, "AnimancerState"), TreeDesigner.ReadOnly]
    protected AnimancerStatePropertyPort m_AnimancerState = new AnimancerStatePropertyPort();

    public override void Init(BaseTree tree)
    {
        base.Init(tree);
        if (!string.IsNullOrEmpty(m_OutputEdgeGUID) && m_Owner.GUIDEdgeMap.ContainsKey(m_OutputEdgeGUID))
            m_Child = m_Owner.GUIDEdgeMap[m_OutputEdgeGUID].EndNode as RunnableNode;
    }

    public override void Dispose()
    {
        base.Dispose();
        m_Child = null;
    }

    public override void OnAfterDeserialize()
    {
        base.OnAfterDeserialize();
        m_OutputEdgeGUID = string.Empty;
        m_Child = null;
    }

    public override void ResetNode()
    {
        base.ResetNode();
        m_Child?.ResetNode();
    }

    protected override State OnUpdate()
    {
        return State.Running;
    }

    protected override void DoAction()
    {
        if (Animancer != null && m_TransitionAsset != null)
        {
            AnimancerState state = Animancer.Play(m_TransitionAsset, m_FadeDuration.Value);
            state.Speed = m_Speed.Value;
            m_AnimancerState.Value = state;

            if (m_Child != null)
            {
                state.Events(this).OnEnd -= OnDone;
                state.Events(this).OnEnd += OnDone;
            }
            else
            {
                m_Child?.UpdateNode();
            }
        }
        else if (m_Child != null)
        {
            m_Child.UpdateNode();
        }
    }

    void OnDone()
    {
        if (m_AnimancerState.Value != null)
        {
            m_AnimancerState.Value.Events(this).OnEnd -= OnDone;
            m_Child?.UpdateNode();
        }
    }

#if UNITY_EDITOR
    public override void OnOutputLinked(BaseEdge edge)
    {
        base.OnOutputLinked(edge);
        m_OutputEdgeGUID = edge.GUID;
        m_Child = edge.EndNode as RunnableNode;
    }

    public override void OnOutputUnlinked(BaseEdge edge)
    {
        base.OnOutputUnlinked(edge);
        m_OutputEdgeGUID = string.Empty;
        m_Child = null;
    }
#endif
}

/// <summary>
/// 停止 Animancer 动画
/// </summary>
[NodeName("StopAnimancer")]
[NodePath("AnimancerAbility/Action/StopAnimancer")]
public class StopAnimancerNode : AnimancerAbilityActionNode
{
    protected override State OnUpdate()
    {
        return State.Running;
    }

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
