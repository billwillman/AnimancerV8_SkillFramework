using UnityEngine;
using Unity.VisualScripting;
using Animancer;
using UnityTimeline;

/// <summary>
/// 所有 VS Ability Unit 的抽象基类
/// 所有组件引用由 AnimancerVisualScriptingLinker.RegisterAbility 预注册到 Variables 中
/// Unit 直接从 Variables 读取，无需 GetComponent
/// </summary>
public abstract class VSAbilityUnitBase : Unit
{
    /// <summary>
    /// 从 flow 中获取 Owner GameObject（通过 Variables 中的 "Owner" 变量）
    /// </summary>
    protected GameObject GetOwner(Flow flow)
    {
        var variables = Variables.Object(flow.stack.gameObject);
        if (variables == null) return null;
        if (variables.IsDefined("Owner"))
            return variables.Get("Owner") as GameObject;
        return null;
    }

    /// <summary>
    /// 从 Variables 获取预注册的 AnimancerComponent
    /// </summary>
    protected AnimancerComponent GetAnimancer(Flow flow)
    {
        var variables = Variables.Object(flow.stack.gameObject);
        if (variables == null) return null;
        if (variables.IsDefined("Animancer"))
            return variables.Get("Animancer") as AnimancerComponent;
        return null;
    }

    /// <summary>
    /// 从 Variables 获取预注册的 SkillCharacterController
    /// </summary>
    protected SkillCharacterController GetSkillController(Flow flow)
    {
        var variables = Variables.Object(flow.stack.gameObject);
        if (variables == null) return null;
        if (variables.IsDefined("SkillController"))
            return variables.Get("SkillController") as SkillCharacterController;
        return null;
    }

    /// <summary>
    /// 从 Variables 获取预注册的 AnimancerVisualScriptingLinker
    /// </summary>
    protected AnimancerVisualScriptingLinker GetLinker(Flow flow)
    {
        var variables = Variables.Object(flow.stack.gameObject);
        if (variables == null) return null;
        if (variables.IsDefined("Linker"))
            return variables.Get("Linker") as AnimancerVisualScriptingLinker;
        return null;
    }
}
