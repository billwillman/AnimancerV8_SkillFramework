/// <summary>
/// AnimancerAbility 技能内事件总线扩展
/// 底层使用全局 EventDispatch 单例，以 GetInstanceID() 前缀隔离不同技能实例，防止跨实例串线
/// 供 SendAbilityEventNode / OnAbilityEventNode 使用
/// </summary>
public partial class AnimancerAbility
{
    /// <summary>
    /// 将技能内事件 key 转为全局唯一 key（含本实例 InstanceID 前缀）
    /// </summary>
    internal string ScopedEventKey(string key) => $"{GetInstanceID()}_{key}";
}
