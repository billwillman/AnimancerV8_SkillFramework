using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

/// <summary>
/// 检测指定 InputAction 是否被按下
/// </summary>
[UnitTitle("Input Key Condition")]
[UnitCategory("AnimancerLinkNodes/Value")]
public class InputKeyConditionUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ValueInput ActionInput;
    [DoNotSerialize] public ValueOutput IsPressed;

    protected override void Definition()
    {
        ActionInput = ValueInput<InputActionReference>("Action", null);
        IsPressed = ValueOutput<bool>("IsPressed", GetIsPressed);
    }

    private bool GetIsPressed(Flow flow)
    {
        var actionRef = flow.GetValue<InputActionReference>(ActionInput);
        var action = actionRef?.action;
        return action != null && action.IsPressed();
    }
}
