using Unity.VisualScripting;
using UnityTimeline;

/// <summary>
/// 移除输入锁定（基于 Key + Flags 通道）
/// LockFlags 为 None 时移除该 Key 在所有通道的锁
/// </summary>
[UnitTitle("Remove Input Lock")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class RemoveInputLockUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;

    [DoNotSerialize] public ValueInput LockKey;
    [DoNotSerialize] public ValueInput LockFlags;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");

        LockKey = ValueInput<string>("LockKey", "SkillPlay");
        LockFlags = ValueInput<InputLockFlags>("LockFlags", InputLockFlags.None);

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return Exit;

        string key = flow.GetValue<string>(LockKey);
        InputLockFlags flags = flow.GetValue<InputLockFlags>(LockFlags);

        if (!string.IsNullOrEmpty(key))
        {
            if (flags == InputLockFlags.None)
                controller.RemoveInputLock(key);
            else
                controller.RemoveInputLock(key, flags);
        }

        return Exit;
    }
}
