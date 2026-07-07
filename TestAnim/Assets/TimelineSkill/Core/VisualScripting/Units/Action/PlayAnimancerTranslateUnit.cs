using UnityEngine;
using Unity.VisualScripting;
using Animancer;
using UnityTimeline;

/// <summary>
/// 通过 Animancer 播放 AnimationClip / TransitionAsset
/// 支持两种完成模式：OnStart 立即传递执行流，OnEnd 等待动画结束后触发 Done 输出
/// </summary>
[UnitTitle("Play Animancer Translate")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class PlayAnimancerTranslateUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;
    [DoNotSerialize] public ControlOutput Done;

    [DoNotSerialize] public ValueInput TransitionAssetInput;
    [DoNotSerialize] public ValueInput FadeDuration;
    [DoNotSerialize] public ValueInput Speed;
    [DoNotSerialize] public ValueInput CompletionMode;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");
        Done = ControlOutput("Done");

        TransitionAssetInput = ValueInput<TransitionAssetBase>("TransitionAsset", null);
        FadeDuration = ValueInput<float>("FadeDuration", 0.25f);
        Speed = ValueInput<float>("Speed", 1f);
        CompletionMode = ValueInput<NodeCompletionMode>("CompletionMode", NodeCompletionMode.OnStart);

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
        float speed = flow.GetValue<float>(Speed);
        var completionMode = flow.GetValue<NodeCompletionMode>(CompletionMode);

        AnimancerState state = animancer.Play(transitionAsset, fadeDuration);

        if (state == null)
            return Exit;

        state.Speed = speed;

        if (completionMode == NodeCompletionMode.OnStart)
            return Exit;

        var graphRef = flow.stack.AsReference();
        state.Events(this).OnEnd += () =>
        {
            state.Events(this).OnEnd = null;
            Flow doneFlow = Flow.New(graphRef);
            doneFlow.Invoke(Done);
            doneFlow.Dispose();
        };
        return Exit;
    }
}
