using UnityEngine;
using Unity.VisualScripting;
using Animancer;

/// <summary>
/// 通过 Animancer 播放 PlayableAsset Timeline (TransitionAsset)
/// 支持两种完成模式：OnStart 立即传递执行流，OnEnd 等待动画结束后触发 Done 输出
/// </summary>
[UnitTitle("Play Animancer Timeline")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class PlayAnimancerTimelineUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;
    [DoNotSerialize] public ControlOutput Done;

    [DoNotSerialize] public ValueInput TransitionAssetInput;
    [DoNotSerialize] public ValueInput FadeDuration;
    [DoNotSerialize] public ValueInput BindSignal;

    [DoNotSerialize] public ValueOutput AnimancerStateOut;

    [Serialize, Inspectable] public NodeCompletionMode CompletionMode = NodeCompletionMode.OnStart;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");
        Done = ControlOutput("Done");

        TransitionAssetInput = ValueInput<TransitionAssetBase>("TransitionAsset", null);
        FadeDuration = ValueInput<float>("FadeDuration", 0.25f);
        BindSignal = ValueInput<bool>("BindSignal", false);

        AnimancerStateOut = ValueOutput<AnimancerState>("AnimancerState");

        Succession(Enter, Exit);
        Succession(Enter, Done);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var animancer = GetAnimancer(flow);
        var transitionAsset = flow.GetValue<TransitionAssetBase>(TransitionAssetInput);
        if (animancer == null || transitionAsset == null)
            return Exit;

        float fadeDuration = flow.GetValue<float>(FadeDuration);
        bool bindSignal = flow.GetValue<bool>(BindSignal);

        AnimancerState state = animancer.PlayTimeline(transitionAsset, fadeDuration, default, bindSignal);

        if (state == null)
            return Exit;

        // 将 state 存入 flow 变量，避免多实例数据冲突
        flow.SetValue(AnimancerStateOut, state);

        if (CompletionMode == NodeCompletionMode.OnStart)
        {
            return Exit;
        }
        else
        {
            // OnEnd 模式：注册回调，通过协程等待
            state.Events(this).OnEnd += () =>
            {
                state.Events(this).OnEnd = null;
                Flow doneFlow = Flow.New(flow.stack.AsReference());
                doneFlow.Invoke(Done);
                doneFlow.Dispose();
            };
            return Exit;
        }
    }
}
