using Unity.VisualScripting;

/// <summary>
/// VS 版本的 Ability 启动条件检查（参考树系统 AnimancerAbilityCanStartNode 的拉取模式）
/// 
/// 设计原理：
/// 树系统中 AnimancerAbilityCanStartNode 是一个 ValueNode，Agent 调用 ability.CanStart() 时
/// 同步拉取上游 ValueNode 的输出来计算条件。
/// 
/// VS 版本中，由于 Visual Scripting 使用 CustomEvent 驱动，采用如下流程：
/// 1. Linker 触发 "CheckCanStart" CustomEvent
/// 2. Graph 中的 Custom Event 节点 → AbilityCanStartUnit → 将条件写入 Variables["CanStart"]
/// 3. Linker 读取 Variables["CanStart"] 获取结果
/// 
/// 用法：Custom Event "CheckCanStart" → 连接条件逻辑 → AbilityCanStartUnit 的 Condition 输入
/// </summary>
[UnitTitle("Ability Can Start")]
[UnitCategory("AnimancerLinkNodes/Special")]
public class AbilityCanStartUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ControlInput Enter;
    [DoNotSerialize] public ControlOutput Exit;

    /// <summary>
    /// 条件输入：连接上游的条件判断逻辑（如 IsGrounded、InputKeyCondition 等 ValueUnit 的输出）
    /// 参考树系统中 AnimancerAbilityCanStartNode 的 m_Condition 输入端口
    /// </summary>
    [DoNotSerialize] public ValueInput Condition;

    protected override void Definition()
    {
        Enter = ControlInput("Enter", OnEnter);
        Exit = ControlOutput("Exit");

        Condition = ValueInput<bool>("Condition", true);

        Succession(Enter, Exit);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        // 拉取上游条件逻辑的结果（类似树系统的 InputValue() → m_Condition.Value）
        bool canStart = flow.GetValue<bool>(Condition);

        // 将结果写入 Object Variables，供 Linker 的 CheckCanStartCondition 同步读取
        var variables = Variables.Object(flow.stack.gameObject);
        if (variables != null)
            variables.Set("CanStart", canStart);

        return Exit;
    }
}
