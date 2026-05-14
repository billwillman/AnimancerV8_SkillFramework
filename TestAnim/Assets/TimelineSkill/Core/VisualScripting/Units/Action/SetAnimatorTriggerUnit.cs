using Unity.VisualScripting;
using Animancer;

/// <summary>
/// 触发 AnimancerState 关联 Animator 的 Trigger 参数
/// </summary>
[UnitTitle("Set Animator Trigger")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class SetAnimatorTriggerUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;

    [DoNotSerialize] public ValueInput AnimancerStateIn;
    [DoNotSerialize] public ValueInput Key;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");

        AnimancerStateIn = ValueInput<AnimancerState>("AnimancerState");
        Key = ValueInput<string>("Key", "");

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var state = flow.GetValue<AnimancerState>(AnimancerStateIn);
        var key = flow.GetValue<string>(Key);

        if (state != null && !string.IsNullOrEmpty(key))
        {
            var animator = state.Graph?.Component?.Animator;
            if (animator != null)
                animator.SetTrigger(key);
        }
        return Exit;
    }
}
