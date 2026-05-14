using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using Taco.Gameplay;

/// <summary>
/// Visual Scripting 版本的 Ability 桥接组件，挂载到角色上
/// 管理一组 VisualScriptingAbility 资产，通过子 ScriptMachine 执行 Graph
/// 与 AnimancerAbilityAgent 的 GameplayTag 系统互通
/// </summary>
public class AnimancerVisualScriptingLinker : MonoBehaviour
{
    #region Data Structures

    /// <summary>
    /// VisualScriptingAbility 分类分组
    /// </summary>
    [Serializable]
    public class VSAbilityCategory
    {
        public string CategoryName;
        public List<VisualScriptingAbility> Abilities = new List<VisualScriptingAbility>();
    }

    /// <summary>
    /// 运行时信息：每个 VisualScriptingAbility 对应的 ScriptMachine 和激活状态
    /// </summary>
    private class RuntimeEntry
    {
        public VisualScriptingAbility Ability;
        public GameObject ChildObject;
        public ScriptMachine Machine;
        public bool IsActive;
    }

    #endregion

    #region Serialized Fields

    [SerializeField]
    [Tooltip("VisualScriptingAbility 分组列表")]
    private List<VSAbilityCategory> m_AbilityCategories = new List<VSAbilityCategory>();

    [SerializeField]
    [Tooltip("可选：关联同 GameObject 上的 AnimancerAbilityLinker，实现 Tag 系统互通")]
    private AnimancerAbilityLinker m_AbilityLinker;

    #endregion

    #region Runtime State

    /// <summary>
    /// 名称 → 运行时条目的映射
    /// </summary>
    private Dictionary<string, RuntimeEntry> m_EntryMap = new Dictionary<string, RuntimeEntry>();

    /// <summary>
    /// 所有运行时条目
    /// </summary>
    private List<RuntimeEntry> m_AllEntries = new List<RuntimeEntry>();

    /// <summary>
    /// 关联的 AnimancerAbilityAgent（通过 AbilityLinker 获取），用于 Tag 互通
    /// </summary>
    private AnimancerAbilityAgent m_AbilityAgent;

    #endregion

    #region Properties

    /// <summary>
    /// 获取分组列表（只读）
    /// </summary>
    public IReadOnlyList<VSAbilityCategory> AbilityCategories => m_AbilityCategories;

    /// <summary>
    /// 关联的 AbilityLinker
    /// </summary>
    public AnimancerAbilityLinker AbilityLinker => m_AbilityLinker;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        // 尝试自动获取同 GameObject 上的 AbilityLinker
        if (m_AbilityLinker == null)
            m_AbilityLinker = GetComponent<AnimancerAbilityLinker>();
    }

    private void Start()
    {
        // 缓存 Agent 引用
        if (m_AbilityLinker != null)
            m_AbilityAgent = m_AbilityLinker.AnimancerAbilityAgent;

        // 初始化所有 VisualScriptingAbility
        foreach (var category in m_AbilityCategories)
        {
            if (category == null) continue;
            foreach (var ability in category.Abilities)
            {
                if (ability == null || ability.ScriptGraph == null) continue;
                RegisterAbility(ability);
            }
        }
    }

    private void OnDestroy()
    {
        // 清理所有运行时 ScriptMachine 子对象
        foreach (var entry in m_AllEntries)
        {
            if (entry.IsActive)
                DeactivateEntry(entry);

            if (entry.ChildObject != null)
                Destroy(entry.ChildObject);
        }
        m_AllEntries.Clear();
        m_EntryMap.Clear();
    }

    #endregion

    #region Registration

    /// <summary>
    /// 注册一个 VisualScriptingAbility，创建子 ScriptMachine（初始禁用）
    /// </summary>
    private void RegisterAbility(VisualScriptingAbility ability)
    {
        if (m_EntryMap.ContainsKey(ability.name))
        {
            Debug.LogWarning($"[AnimancerVSLinker] Duplicate ability name: {ability.name}, skipping.");
            return;
        }

        // 初始化 Tag 容器
        ability.InitTags();

        // 创建子 GameObject 挂载 ScriptMachine
        var childObj = new GameObject($"[VS] {ability.name}");
        childObj.transform.SetParent(transform, false);
        childObj.SetActive(false);

        var machine = childObj.AddComponent<ScriptMachine>();
        machine.nest.macro = ability.ScriptGraph;

        // 设置 Object Variables，将角色 GameObject 传入图中
        var variables = childObj.GetComponent<Variables>();
        if (variables == null)
            variables = childObj.AddComponent<Variables>();
        variables.declarations.Set("Owner", gameObject);

        var entry = new RuntimeEntry
        {
            Ability = ability,
            ChildObject = childObj,
            Machine = machine,
            IsActive = false
        };

        m_EntryMap[ability.name] = entry;
        m_AllEntries.Add(entry);
    }

    #endregion

    #region Public API — Trigger Events

    /// <summary>
    /// 触发指定 Ability 的 OnEnter 事件（启用 ScriptMachine 并触发 CustomEvent "OnEnter"）
    /// </summary>
    public bool TriggerOnEnter(string abilityName)
    {
        if (!m_EntryMap.TryGetValue(abilityName, out var entry))
        {
            Debug.LogWarning($"[AnimancerVSLinker] Ability not found: {abilityName}");
            return false;
        }

        if (entry.IsActive)
        {
            Debug.LogWarning($"[AnimancerVSLinker] Ability already active: {abilityName}");
            return false;
        }

        // Tag 前置检查
        if (!CheckTagRequirements(entry.Ability))
            return false;

        // 处理 Cancel 逻辑
        ProcessCancelTags(entry.Ability);

        // 激活
        ActivateEntry(entry);

        // 添加 ActiveTags 到 Agent
        AddActiveTags(entry.Ability);

        // 添加 BlockAbilitiesWithTag 到 Agent
        AddBlockTags(entry.Ability);

        // 触发 OnEnter 事件
        CustomEvent.Trigger(entry.ChildObject, "OnEnter");
        return true;
    }

    /// <summary>
    /// 触发指定 Ability 的 OnUpdate 事件
    /// </summary>
    public void TriggerOnUpdate(string abilityName, float deltaTime)
    {
        if (!m_EntryMap.TryGetValue(abilityName, out var entry))
            return;

        if (!entry.IsActive)
            return;

        CustomEvent.Trigger(entry.ChildObject, "OnUpdate", deltaTime);
    }

    /// <summary>
    /// 触发指定 Ability 的 OnExit 事件（触发 CustomEvent "OnExit" 并禁用 ScriptMachine）
    /// </summary>
    public void TriggerOnExit(string abilityName)
    {
        if (!m_EntryMap.TryGetValue(abilityName, out var entry))
            return;

        if (!entry.IsActive)
            return;

        // 触发 OnExit 事件
        CustomEvent.Trigger(entry.ChildObject, "OnExit");

        // 移除 BlockAbilitiesWithTag
        RemoveBlockTags(entry.Ability);

        // 移除 ActiveTags
        RemoveActiveTags(entry.Ability);

        // 停用
        DeactivateEntry(entry);
    }

    /// <summary>
    /// 对所有激活中的 Ability 触发 OnUpdate
    /// </summary>
    public void TriggerOnUpdateAll(float deltaTime)
    {
        foreach (var entry in m_AllEntries)
        {
            if (entry.IsActive)
                CustomEvent.Trigger(entry.ChildObject, "OnUpdate", deltaTime);
        }
    }

    /// <summary>
    /// 强制停止所有激活中的 Ability
    /// </summary>
    public void StopAll()
    {
        foreach (var entry in m_AllEntries)
        {
            if (entry.IsActive)
                TriggerOnExit(entry.Ability.name);
        }
    }

    /// <summary>
    /// 检查指定 Ability 是否正在激活
    /// </summary>
    public bool IsActive(string abilityName)
    {
        return m_EntryMap.TryGetValue(abilityName, out var entry) && entry.IsActive;
    }

    /// <summary>
    /// 获取所有已注册的 VisualScriptingAbility
    /// </summary>
    public List<VisualScriptingAbility> GetAllAbilities()
    {
        var all = new List<VisualScriptingAbility>();
        foreach (var category in m_AbilityCategories)
        {
            if (category == null) continue;
            foreach (var ability in category.Abilities)
            {
                if (ability != null)
                    all.Add(ability);
            }
        }
        return all;
    }

    #endregion

    #region Tag Integration

    /// <summary>
    /// 检查 RequiredTags 和 BlockAbilitiesWithTag 前置条件
    /// </summary>
    private bool CheckTagRequirements(VisualScriptingAbility ability)
    {
        if (m_AbilityAgent == null) return true;

        // 检查 RequiredTags
        if (ability.RequiredTags != null && ability.RequiredTags.Tags != null)
        {
            foreach (var requiredTag in ability.RequiredTags.Tags)
            {
                bool found = false;
                foreach (var activeTag in m_AbilityAgent.ActiveTags)
                {
                    if (activeTag.StartTagIs(requiredTag))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Debug.Log($"[AnimancerVSLinker] {ability.name} required tag {requiredTag} not found");
                    return false;
                }
            }
        }

        // 检查是否被 Block
        if (ability.AbilityTags != null && ability.AbilityTags.Tags != null)
        {
            foreach (var blockTag in m_AbilityAgent.BlockAbilitiesWithTag)
            {
                if (ability.AbilityTags.IsChildOf(blockTag))
                {
                    Debug.Log($"[AnimancerVSLinker] {ability.name} is blocked by tag {blockTag}");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 处理 CancelAbilitiesWithTag：取消匹配标签的正在运行的 AnimancerAbility
    /// </summary>
    private void ProcessCancelTags(VisualScriptingAbility ability)
    {
        if (m_AbilityAgent == null) return;
        if (ability.CancelAbilitiesWithTag == null || ability.CancelAbilitiesWithTag.Tags == null) return;

        foreach (var abilityInAgent in m_AbilityAgent.Abilities)
        {
            if (abilityInAgent.Active)
            {
                if (abilityInAgent.AbilityTags.PartChildOf(ability.CancelAbilitiesWithTag))
                {
                    abilityInAgent.CancelAbility(null);
                    m_AbilityAgent.TryStopAbility(abilityInAgent);
                    Debug.Log($"[AnimancerVSLinker] {abilityInAgent.name} canceled by VS ability {ability.name}");
                    break;
                }
            }
        }
    }

    private void AddActiveTags(VisualScriptingAbility ability)
    {
        if (m_AbilityAgent == null || ability.ActiveTags == null) return;
        foreach (var tag in ability.ActiveTags.Tags)
            m_AbilityAgent.ActiveTags.Add(tag);
    }

    private void RemoveActiveTags(VisualScriptingAbility ability)
    {
        if (m_AbilityAgent == null || ability.ActiveTags == null) return;
        foreach (var tag in ability.ActiveTags.Tags)
            m_AbilityAgent.ActiveTags.Remove(tag);
    }

    private void AddBlockTags(VisualScriptingAbility ability)
    {
        if (m_AbilityAgent == null || ability.BlockAbilitiesWithTag == null) return;
        foreach (var tag in ability.BlockAbilitiesWithTag.Tags)
        {
            if (!m_AbilityAgent.BlockAbilitiesWithTag.Contains(tag))
                m_AbilityAgent.BlockAbilitiesWithTag.Add(tag);
        }
    }

    private void RemoveBlockTags(VisualScriptingAbility ability)
    {
        if (m_AbilityAgent == null || ability.BlockAbilitiesWithTag == null) return;
        foreach (var tag in ability.BlockAbilitiesWithTag.Tags)
            m_AbilityAgent.BlockAbilitiesWithTag.Remove(tag);
    }

    #endregion

    #region Internal

    private void ActivateEntry(RuntimeEntry entry)
    {
        entry.ChildObject.SetActive(true);
        entry.IsActive = true;
    }

    private void DeactivateEntry(RuntimeEntry entry)
    {
        entry.ChildObject.SetActive(false);
        entry.IsActive = false;
    }

    #endregion
}
