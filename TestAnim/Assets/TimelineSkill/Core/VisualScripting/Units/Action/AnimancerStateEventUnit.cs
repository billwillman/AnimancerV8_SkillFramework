using System;
using UnityEngine;
using Unity.VisualScripting;
using Animancer;

/// <summary>
/// 绑定 AnimancerState 事件的 Unit
/// 支持 OnEnd（动画结束时触发）和 AtTime（指定归一化时间触发）两种模式
/// 注册后通过 OnEvent ControlOutput 异步触发下游节点
/// </summary>
[UnitTitle("Animancer State Event")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class AnimancerStateEventUnit : VSAbilityUnitBase
{
    public enum EventType
    {
        OnEnd,
        AtTime,
    }

    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;
    [DoNotSerialize] public ControlOutput OnEvent;

    [DoNotSerialize] public ValueInput AnimancerStateIn;
    [DoNotSerialize] public ValueInput NormalizedTime;

    [Serialize, Inspectable] public EventType EvtType = EventType.OnEnd;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");
        OnEvent = ControlOutput("OnEvent");

        AnimancerStateIn = ValueInput<AnimancerState>("AnimancerState");
        NormalizedTime = ValueInput<float>("NormalizedTime", 0.5f);

        Succession(Enter, Exit);
        Succession(Enter, OnEvent);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var state = flow.GetValue<AnimancerState>(AnimancerStateIn);
        if (state == null)
            return Exit;

        // 捕获 GraphReference 用于异步回调
        var graphRef = flow.stack.AsReference();

        switch (EvtType)
        {
            case EventType.OnEnd:
                state.Events(this).OnEnd += () =>
                {
                    state.Events(this).OnEnd = null;
                    TriggerEvent(graphRef);
                };
                break;

            case EventType.AtTime:
                float normalizedTime = flow.GetValue<float>(NormalizedTime);
                state.Events(this).Add(normalizedTime, () =>
                {
                    TriggerEvent(graphRef);
                });
                break;
        }

        return Exit;
    }

    private void TriggerEvent(GraphReference graphRef)
    {
        Flow eventFlow = Flow.New(graphRef);
        eventFlow.Invoke(OnEvent);
        eventFlow.Dispose();
    }
}
