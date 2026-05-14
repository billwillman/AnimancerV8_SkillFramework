using Unity.VisualScripting;

/// <summary>
/// 清除角色所有输入锁定
/// </summary>
[UnitTitle("Clear All Input Locks")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class ClearAllInputLocksUnit : VSAbilityUnitBase
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
        var controller = GetSkillController(flow);
        if (controller != null)
            controller.ClearAllInputLocks();
        return Exit;
    }
}
