using UnityEngine;
using Unity.VisualScripting;

/// <summary>
/// 检测角色是否在地面移动（稳定着地 AND 水平速度 >= 阈值）
/// </summary>
[UnitTitle("Is Ground Moving")]
[UnitCategory("AnimancerLinkNodes/Condition")]
public class IsGroundMovingUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ValueOutput Result;

    [Serialize, Inspectable] public float SpeedThreshold = 0.1f;

    protected override void Definition()
    {
        Result = ValueOutput<bool>("IsGroundMoving", GetIsGroundMoving);
    }

    private bool GetIsGroundMoving(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return false;

        if (!controller.Motor.GroundingStatus.IsStableOnGround)
            return false;

        var velocity = controller.Motor.Velocity;
        var horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        return horizontalSpeed >= SpeedThreshold;
    }
}
