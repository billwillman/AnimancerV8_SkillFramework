using Unity.VisualScripting;

/// <summary>
/// 检测角色是否在地面上（稳定着地状态）
/// </summary>
[UnitTitle("Is Grounded")]
[UnitCategory("AnimancerLinkNodes/Condition")]
public class IsGroundedUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ValueOutput Result;

    protected override void Definition()
    {
        Result = ValueOutput<bool>("IsGrounded", GetIsGrounded);
    }

    private bool GetIsGrounded(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return false;

        return controller.Motor.GroundingStatus.IsStableOnGround;
    }
}
