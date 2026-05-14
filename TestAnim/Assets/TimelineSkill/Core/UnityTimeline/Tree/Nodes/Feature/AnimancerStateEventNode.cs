using System;
using System.Collections.Generic;
using UnityEngine;
using TreeDesigner;
using Animancer;

/// <summary>
/// 绑定 AnimancerState 事件的节点。
/// 支持设置 OnEnd 回调，以及在指定 NormalizedTime 添加 Animancer Event。
/// 事件触发后通过 Output 连线驱动下游节点。
/// 事件在树 Dispose/Reset 时自动清理。
/// 
/// 所有运行时状态存储在 NodeBlackboardData.RuntimeProperties 中（per-instance 安全）。
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

    public override void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties)
    {
        // 所有运行时状态存入 RuntimeProperties（per-instance 安全）
        properties["EventIndex"] = new IntExposedProperty { Name = "EventIndex" };
        properties["Registered"] = new BoolExposedProperty { Name = "Registered" };
        properties["EventTriggered"] = new BoolExposedProperty { Name = "EventTriggered" };
        // OutputResult 用 IntEP 存储 State 枚举值
        properties["OutputResult"] = new IntExposedProperty { Name = "OutputResult" };
        // 存储当前绑定的回调 Action 引用，用于精确取消订阅
        properties["BoundCallback"] = new StringExposedProperty { Name = "BoundCallback" };
    }

    public override void Init(BaseTree tree)
    {
        base.Init(tree);
        m_OutputChild = null;
        if (!string.IsNullOrEmpty(m_OutputEdgeGUID) && m_Owner.GUIDEdgeMap.TryGetValue(m_OutputEdgeGUID, out var edge))
        {
            m_OutputChild = edge.EndNode as RunnableNode;
        }
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

        var nodeData = NodeData;
        if (nodeData != null)
        {
            nodeData.GetRuntime<bool>("EventTriggered").Value = false;
            nodeData.GetRuntime<int>("OutputResult").Value = (int)State.None;
        }
    }

    protected override void DoAction()
    {
        var state = m_AnimacerState.Value;
        if (state == null)
            return;

        var nodeData = NodeData;
        if (nodeData == null) return;

        // 先清理旧事件，防止重复订阅
        CleanupEvents(state);

        // 用闭包捕获 Context（per-instance 安全）
        var capturedCtx = m_Owner?.CurrentContext;
        var capturedGUID = m_GUID;
        var outputChild = m_OutputChild;

        Action onEventTriggered = null;
        onEventTriggered = () =>
        {
            if (state != null && m_EventType == EventType.OnEnd)
                state.Events(this).OnEnd -= onEventTriggered;

            var nd = capturedCtx?.GetNodeData(capturedGUID);
            if (nd == null) return;

            nd.GetRuntime<bool>("Registered").Value = false;
            nd.GetRuntime<int>("EventIndex").Value = -1;
            nd.GetRuntime<bool>("EventTriggered").Value = true;
            nd.RuntimeProperties["BoundCallback"].SetValue(null);
            // 不在异步回调中驱动子节点（Context 已解绑，PropertyPort 读写不安全）
            // 子节点将由 OnUpdate() 在 Context 绑定期间安全驱动
        };

        // 存储回调引用到 RuntimeProperties
        nodeData.RuntimeProperties["BoundCallback"].SetValue(onEventTriggered);

        switch (m_EventType)
        {
            case EventType.OnEnd:
                state.Events(this).OnEnd += onEventTriggered;
                nodeData.GetRuntime<bool>("Registered").Value = true;
                break;

            case EventType.AtTime:
                var idx = state.Events(this).Add(m_NormalizedTime.Value, onEventTriggered);
                nodeData.GetRuntime<int>("EventIndex").Value = idx;
                nodeData.GetRuntime<bool>("Registered").Value = true;
                break;
        }
    }

    protected override State OnUpdate()
    {
        var nodeData = NodeData;
        if (nodeData == null) return State.None;

        // 事件尚未触发 → 持续等待
        if (!nodeData.GetRuntime<bool>("EventTriggered").Value)
            return State.Running;

        // 事件已触发，但没有 Output 子节点 → 直接成功
        if (m_OutputChild == null)
            return State.Success;

        // 有 Output 子节点 → 透传状态（非终态时继续驱动）
        var outputResult = (State)nodeData.GetRuntime<int>("OutputResult").Value;
        if (outputResult != State.Success && outputResult != State.Failure)
        {
            outputResult = m_OutputChild.UpdateNode();
            nodeData.GetRuntime<int>("OutputResult").Value = (int)outputResult;
        }

        return outputResult;
    }

    /// <summary>
    /// 清理已注册的事件
    /// </summary>
    private void CleanupEvents(AnimancerState state)
    {
        var nodeData = NodeData;
        if (nodeData == null) return;

        if (!nodeData.GetRuntime<bool>("Registered").Value || state == null)
            return;

        var boundCallback = nodeData.RuntimeProperties["BoundCallback"]?.GetValue() as Action;

        switch (m_EventType)
        {
            case EventType.OnEnd:
                if (boundCallback != null)
                    state.Events(this).OnEnd -= boundCallback;
                break;

            case EventType.AtTime:
                var eventIndex = nodeData.GetRuntime<int>("EventIndex").Value;
                if (eventIndex >= 0 && eventIndex < state.Events(this).Count)
                {
                    state.Events(this).Remove(eventIndex);
                }
                nodeData.GetRuntime<int>("EventIndex").Value = -1;
                break;
        }

        nodeData.GetRuntime<bool>("Registered").Value = false;
        nodeData.RuntimeProperties["BoundCallback"].SetValue(null);
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
