using Unity.VisualScripting;

/// <summary>
/// VS 版本的 Ability 被取消事件入口（参考树系统 OnAnimancerAbilityCancelNode 的推送模式）
/// 
/// 设计原理：
/// 树系统中 OnAnimancerAbilityCancelNode 是一个 EnterNode，当 Agent 调用 ability.CancelAbility(who) 时：
/// 1. Trigger(abilityCancelBy) 设置输出端口的 Ability 引用
/// 2. 调用 UpdateNode() 驱动后续连接的 ActionNode 链执行（如播放取消动画、清理状态）
/// 
/// VS 版本中，Linker 的 TriggerOnCancel 方法：
/// 1. 将 "CancelledBy" 写入 Variables（提供取消来源信息）
/// 2. 触发 CustomEvent "OnCancel" → Graph 中的 Custom Event 节点 → OnAbilityCancelUnit
/// 3. OnAbilityCancelUnit 从 Variables 读取取消者名称，输出给后续节点
/// 4. 后续可连接清理逻辑（停止 VFX、恢复输入、播放取消动画等）
/// 
/// 用法：Custom Event "OnCancel" → OnAbilityCancelUnit → 后续清理/响应逻辑
/// </summary>
[UnitTitle("On Ability Cancel")]
[UnitCategory("AnimancerLinkNodes/Special")]
public class OnAbilityCancelUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;

    /// <summary>
    /// 输出：取消此 Ability 的来源名称
    /// 对应树系统中 OnAnimancerAbilityCancelNode 的 m_Ability 输出端口
    /// （VS 版本输出 string 名称而非 Ability 引用，因为 VS Graph 不直接引用 ScriptableObject）
    /// </summary>
    [DoNotSerialize] public ValueOutput CancelledBy;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");

        // 使用 lambda 从 flow 上下文中获取值，避免 Unit 实例上的状态问题
        CancelledBy = ValueOutput<string>("CancelledBy", GetCancelledBy);

        Succession(Enter, Exit);
        Assignment(Enter, CancelledBy);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        // 从 Variables 读取 Linker 设置的 "CancelledBy"（推送过来的数据）
        var variables = Variables.Object(flow.stack.gameObject);
        string cancelledBy = "";
        if (variables != null && variables.IsDefined("CancelledBy"))
            cancelledBy = variables.Get("CancelledBy") as string ?? "";

        // 存入 flow 本地数据，供 ValueOutput 的 lambda 读取（线程安全）
        flow.SetValue(CancelledBy, cancelledBy);

        return Exit;
    }

    private string GetCancelledBy(Flow flow)
    {
        // 从 Variables 读取 Linker 设置的 "CancelledBy"
        var variables = Variables.Object(flow.stack.gameObject);
        if (variables != null && variables.IsDefined("CancelledBy"))
            return variables.Get("CancelledBy") as string ?? "";
        return "";
    }
}
