using Unity.VisualScripting;
using Animancer;

/// <summary>
/// 设置 AnimancerState 关联 Animator 的 Int 参数
/// </summary>
[UnitTitle("Set Animator Int")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class SetAnimatorIntUnit : VSAbilityUnitBase
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

        AnimancerStateIn = ValueInput<AnimancerState>("AnimancerState", null);
        Key = ValueInput<string>("Key", "");
        Value = ValueInput<int>("Value", 0);

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var state = flow.GetValue<AnimancerState>(AnimancerStateIn);
        var key = flow.GetValue<string>(Key);
        var value = flow.GetValue<int>(Value);

        if (state != null && !string.IsNullOrEmpty(key))
        {
            var animator = state.Graph?.Component?.Animator;
            if (animator != null)
                animator.SetInteger(key, value);
        }
        return Exit;
    }
}
