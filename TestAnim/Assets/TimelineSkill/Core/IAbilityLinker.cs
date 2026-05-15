using UnityEngine;
using Animancer;
using UnityTimeline;

/// <summary>
/// Ability 系统桥接组件的公共接口。
/// 被 UnityTimelineTree 节点和 Visual Scripting Units 共同依赖。
/// AnimancerAbilityLinker（树系统）和 AnimancerVisualScriptingLinker（VS系统）都实现此接口。
/// </summary>
public interface IAbilityLinker
{
    /// <summary>角色 Transform（用于位置/旋转/子对象查找）</summary>
    Transform transform { get; }

    /// <summary>角色 GameObject（用于 GetComponent 等调用）</summary>
    GameObject gameObject { get; }

    /// <summary>Animancer 动画组件引用</summary>
    AnimancerComponent AnimancerComponent { get; }

    /// <summary>角色控制器引用（用于输入锁、移速、RootMotion 等）</summary>
    SkillCharacterController SkillCharacterController { get; }

    /// <summary>
    /// Ability Agent 引用（用于 GameplayTag 管理）。
    /// VS 版本不需要此成员，可返回 null。
    /// </summary>
    AnimancerAbilityAgent AnimancerAbilityAgent { get; }

    /// <summary>尝试启动指定名称的 Ability</summary>
    bool TryStartAbility(string abilityName);

    /// <summary>尝试停止指定名称的 Ability</summary>
    void TryStopAbility(string abilityName);
}
