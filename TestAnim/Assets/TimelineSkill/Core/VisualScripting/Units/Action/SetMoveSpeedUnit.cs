using Unity.VisualScripting;

/// <summary>
/// 设置角色的地面移动速度和/或空中移动速度
/// 值为 -1 表示不修改该速度
/// </summary>
[UnitTitle("Set Move Speed")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class SetMoveSpeedUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;

    [DoNotSerialize] public ValueInput GroundSpeed;
    [DoNotSerialize] public ValueInput AirSpeed;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");

        GroundSpeed = ValueInput<float>("GroundSpeed", -1f);
        AirSpeed = ValueInput<float>("AirSpeed", -1f);

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return Exit;

        float groundSpeed = flow.GetValue<float>(GroundSpeed);
        float airSpeed = flow.GetValue<float>(AirSpeed);

        if (groundSpeed >= 0f)
            controller.MaxStableMoveSpeed = groundSpeed;
        if (airSpeed >= 0f)
            controller.MaxAirMoveSpeed = airSpeed;

        return Exit;
    }
}
