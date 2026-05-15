using UnityEngine;
using Unity.VisualScripting;

/// <summary>
/// 获取移动输入 Vector2 及角色局部坐标系下的相对移动方向 Vector2
/// MoveInput: 原始输入 (x=右, y=前)
/// LocalMoveDir: 世界空间移动方向投影到角色 transform.right / transform.forward
/// </summary>
[UnitTitle("Get Move Input")]
[UnitCategory("AnimancerLinkNodes/Value")]
public class GetMoveInputUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ValueOutput MoveInput;
    [DoNotSerialize] public ValueOutput LocalMoveDir;

    protected override void Definition()
    {
        MoveInput = ValueOutput<Vector2>("MoveInput", GetMoveInput);
        LocalMoveDir = ValueOutput<Vector2>("LocalMoveDir", GetLocalMoveDir);
    }

    private Vector2 GetMoveInput(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return Vector2.zero;

        if (controller.MoveAction != null && controller.MoveAction.action != null)
            return controller.MoveAction.action.ReadValue<Vector2>();
        return Vector2.zero;
    }

    private Vector2 GetLocalMoveDir(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return Vector2.zero;

        Vector2 rawInput = Vector2.zero;
        if (controller.MoveAction != null && controller.MoveAction.action != null)
            rawInput = controller.MoveAction.action.ReadValue<Vector2>();

        Transform charTransform = controller.transform;
        Quaternion camRot = controller.OrientationReference != null
            ? Quaternion.Euler(0f, controller.OrientationReference.eulerAngles.y, 0f)
            : Quaternion.Euler(0f, charTransform.eulerAngles.y, 0f);

        Vector3 camForward = camRot * Vector3.forward;
        Vector3 camRight = camRot * Vector3.right;
        Vector3 worldMoveDir = camForward * rawInput.y + camRight * rawInput.x;

        Vector3 charForward = charTransform.forward;
        Vector3 charRight = charTransform.right;
        float localX = Vector3.Dot(worldMoveDir, charRight);
        float localY = Vector3.Dot(worldMoveDir, charForward);
        return new Vector2(localX, localY);
    }
}
