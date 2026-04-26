using System;
using UnityEngine;
using TreeDesigner;
using Animancer;

/// <summary>
/// 绑定 AnimancerState 事件的节点。
/// 支持设置 OnEnd 回调，以及在指定 NormalizedTime 添加 Animancer Event。
/// 事件触发后通过 Output 连线驱动下游节点。
/// 事件在树 Dispose/Reset 时自动清理。
/// </summary>
[NodeName("AnimancerStateEvent")]
[NodePath("AnimancerAbility/Action/AnimancerStateEvent")]
[Output("Output", PortCapacity.Single)]
public class AnimancerStateEventNode : AnimancerAbilityActionNode
{
    public enum EventType
    {
        /// <summary>动画结束时触发</summary>
        OnEnd,
        /// <summary>在指定 NormalizedTime 触发</summary>
        AtTime,
    }

    [SerializeField, PropertyPort(PortDirection.Input, "AnimancerState")]
    AnimancerStatePropertyPort m_AnimacerState = new AnimancerStatePropertyPort();

    [SerializeField, ShowInPanel("Event Type")]
    EventType m_EventType = EventType.OnEnd;

    [SerializeField, PropertyPort(PortDirection.Input, "NormalizedTime")]
    FloatPropertyPort m_NormalizedTime = new FloatPropertyPort() { Value = 0.5f };

    [SerializeField]
    string m_OutputEdgeGUID;
    public string OutputEdgeGUID => m_OutputEdgeGUID;

    [NonSerialized]
    private RunnableNode m_OutputChild;

    /// <summary>AtTime 事件在 Sequence 中的索引，用于精确移除</summary>
    private int m_EventIndex = -1;
    private bool m_Registered;

    public override void Init(BaseTree tree)
    {
        base.Init(tree);
        // 通过 Output 连线解析目标节点
        m_OutputChild = null;
        if (!string.IsNullOrEmpty(m_OutputEdgeGUID) && m_Owner.GUIDEdgeMap.TryGetValue(m_OutputEdgeGUID, out var edge))
        {
            m_OutputChild = edge.EndNode as RunnableNode;
        }
        m_Registered = false;
        m_EventIndex = -1;
    }

    public override void OnAfterDeserialize()
    {
        base.OnAfterDeserialize();
        m_OutputEdgeGUID = string.Empty;
        m_OutputChild = null;
    }

    public override void Dispose()
    {
        var state = m_AnimacerState.Value;
        CleanupEvents(state);
        base.Dispose();
        m_OutputChild = null;
    }

    public override void ResetNode()
    {
        var state = m_AnimacerState.Value;
        CleanupEvents(state);
        base.ResetNode();
        m_OutputChild?.ResetNode();
    }

    protected override void DoAction()
    {
        var state = m_AnimacerState.Value;
        if (state == null)
            return;

        // 先清理旧事件，防止重复订阅
        CleanupEvents(state);

        switch (m_EventType)
        {
            case EventType.OnEnd:
                state.Events(this).OnEnd += OnEventTriggered;
                m_Registered = true;
                break;

            case EventType.AtTime:
                m_EventIndex = state.Events(this).Add(m_NormalizedTime.Value, OnEventTriggered);
                m_Registered = true;
                break;
        }
    }

    private void OnEventTriggered()
    {
        var state = m_AnimacerState.Value;
        if (state != null && m_EventType == EventType.OnEnd)
        {
            state.Events(this).OnEnd -= OnEventTriggered;
        }

        m_Registered = false;
        m_EventIndex = -1;
        m_OutputChild?.UpdateNode();
    }

    /// <summary>
    /// 清理已注册的事件
    /// </summary>
    private void CleanupEvents(AnimancerState state)
    {
        if (!m_Registered || state == null)
            return;

        switch (m_EventType)
        {
            case EventType.OnEnd:
                state.Events(this).OnEnd -= OnEventTriggered;
                break;

            case EventType.AtTime:
                if (m_EventIndex >= 0 && m_EventIndex < state.Events(this).Count)
                {
                    state.Events(this).Remove(m_EventIndex);
                }
                m_EventIndex = -1;
                break;
        }

        m_Registered = false;
    }

#if UNITY_EDITOR
    public override void OnOutputLinked(BaseEdge edge)
    {
        base.OnOutputLinked(edge);
        m_OutputEdgeGUID = edge.GUID;
        m_OutputChild = edge.EndNode as RunnableNode;
    }
    public override void OnOutputUnlinked(BaseEdge edge)
    {
        base.OnOutputUnlinked(edge);
        m_OutputEdgeGUID = string.Empty;
        m_OutputChild = null;
    }
#endif
}
