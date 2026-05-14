using Unity.VisualScripting;
using Animancer;

/// <summary>
/// 设置 AnimancerState 关联 Animator 的 Float 参数
/// </summary>
[UnitTitle("Set Animator Float")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class SetAnimatorFloatUnit : VSAbilityUnitBase
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
        Value = ValueInput<float>("Value", 0f);

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var state = flow.GetValue<AnimancerState>(AnimancerStateIn);
        var key = flow.GetValue<string>(Key);
        var value = flow.GetValue<float>(Value);

        if (state != null && !string.IsNullOrEmpty(key))
        {
            var animator = state.Graph?.Component?.Animator;
            if (animator != null)
                animator.SetFloat(key, value);
        }
        return Exit;
    }
}
