using Unity.VisualScripting;

/// <summary>
/// 停止 Animancer 动画播放
/// </summary>
[UnitTitle("Stop Animancer")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class StopAnimancerUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var animancer = GetAnimancer(flow);
        if (animancer != null)
            animancer.Stop();
        return Exit;
    }
}
