using Unity.VisualScripting;
using UnityTimeline;

/// <summary>
/// 添加输入锁定（基于 Key + Flags 通道）
/// </summary>
[UnitTitle("Set Input Lock")]
[UnitCategory("AnimancerLinkNodes/Action")]
public class SetInputLockUnit : VSAbilityUnitBase
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
        LockFlags = ValueInput<InputLockFlags>("LockFlags", InputLockFlags.All);

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        var controller = GetSkillController(flow);
        if (controller == null) return Exit;

        string key = flow.GetValue<string>(LockKey);
        InputLockFlags flags = flow.GetValue<InputLockFlags>(LockFlags);

        if (!string.IsNullOrEmpty(key))
            controller.AddInputLock(key, flags);

        return Exit;
    }
}
