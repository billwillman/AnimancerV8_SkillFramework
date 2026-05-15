using Unity.VisualScripting;

/// <summary>
/// 检测角色是否在空中（非稳定着地状态）
/// </summary>
[UnitTitle("Is In Air")]
[UnitCategory("AnimancerLinkNodes/Condition")]
public class IsInAirUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ValueOutput Result;

    protected override void Definition()
    {
        Result = ValueOutput<bool>("IsInAir", GetIsInAir);
    }

    private bool GetIsInAir(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return false;

        return !controller.Motor.GroundingStatus.IsStableOnGround;
    }
}
