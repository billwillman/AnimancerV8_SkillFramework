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
    [DoNotSerialize] public ValueOutput IsPressed;

    [Serialize, Inspectable] public InputActionReference Action;

    protected override void Definition()
    {
        IsPressed = ValueOutput<bool>("IsPressed", GetIsPressed);
    }

    private bool GetIsPressed(Flow flow)
    {
        var action = Action?.action;
        return action != null && action.IsPressed();
    }
}
