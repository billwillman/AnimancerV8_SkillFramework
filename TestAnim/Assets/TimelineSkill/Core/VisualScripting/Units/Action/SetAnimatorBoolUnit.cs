using Unity.VisualScripting;
using Animancer;

/// <summary>
/// 设置 AnimancerState 关联 Animator 的 Bool 参数
/// </summary>
[UnitTitle("Set Animator Bool")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class SetAnimatorBoolUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;

    [DoNotSerialize] public ValueInput AnimancerStateIn;
    [DoNotSerialize] public ValueInput Key;
    [DoNotSerialize] public ValueInput Value;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");

        AnimancerStateIn = ValueInput<AnimancerState>("AnimancerState");
        Key = ValueInput<string>("Key", "");
        Value = ValueInput<bool>("Value", false);

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var state = flow.GetValue<AnimancerState>(AnimancerStateIn);
        var key = flow.GetValue<string>(Key);
        var value = flow.GetValue<bool>(Value);

        if (state != null && !string.IsNullOrEmpty(key))
        {
            var animator = state.Graph?.Component?.Animator;
            if (animator != null)
                animator.SetBool(key, value);
        }
        return Exit;
    }
}
