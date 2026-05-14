using UnityEngine;
using Unity.VisualScripting;
using Animancer;

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

    [Serialize, Inspectable] public NodeCompletionMode CompletionMode = NodeCompletionMode.OnStart;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");
        Done = ControlOutput("Done");

        TransitionAssetInput = ValueInput<TransitionAssetBase>("TransitionAsset", null);
        FadeDuration = ValueInput<float>("FadeDuration", 0.25f);
        Speed = ValueInput<float>("Speed", 1f);

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

        AnimancerState state = animancer.Play(transitionAsset, fadeDuration);

        if (state == null)
            return Exit;

        state.Speed = speed;

        if (CompletionMode == NodeCompletionMode.OnStart)
        {
            return Exit;
        }
        else
        {
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
