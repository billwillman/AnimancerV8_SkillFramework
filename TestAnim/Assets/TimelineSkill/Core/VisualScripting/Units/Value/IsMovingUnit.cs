using UnityEngine;
using Unity.VisualScripting;

/// <summary>
/// 检测角色是否正在移动（水平速度 >= 阈值）
/// </summary>
[UnitTitle("Is Moving")]
[UnitCategory("AnimancerLinkNodes/Condition")]
public class IsMovingUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ValueOutput Result;

    [Serialize, Inspectable] public float SpeedThreshold = 0.1f;

    protected override void Definition()
    {
        Result = ValueOutput<bool>("IsMoving", GetIsMoving);
    }

    private bool GetIsMoving(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return false;

        var velocity = controller.Motor.Velocity;
        var horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        return horizontalSpeed >= SpeedThreshold;
    }
}
