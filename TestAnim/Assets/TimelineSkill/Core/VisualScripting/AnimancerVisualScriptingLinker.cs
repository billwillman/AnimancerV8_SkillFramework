using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using Taco.Gameplay;
using Cinemachine;

using Animancer;
using UnityTimeline;

/// <summary>
/// Visual Scripting 版本的 Ability 桥接组件，挂载到角色上
/// 管理一组 VisualScriptingAbility 资产，通过子 ScriptMachine 执行 Graph
/// 自身维护 ActiveTags/BlockTags，设计上参考 AnimancerAbilityLinker 但完全独立
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
[RequireComponent(typeof(SkillCharacterController))]
public class AnimancerVisualScriptingLinker : MonoBehaviour
{
    #region Data Structures

    /// <summary>
    /// 输入触发模式
    /// </summary>
    public enum InputTriggerMode
    {
        /// <summary>按下时触发（started）</summary>
        OnStarted,
        /// <summary>执行中触发（performed，适合持续按住）</summary>
        OnPerformed,
        /// <summary>松开时触发（canceled）</summary>
        OnCanceled,
    }

    /// <summary>
    /// 输入触发绑定：一个 InputActionReference 对应一个要触发的 VisualScriptingAbility
    /// </summary>
    [Serializable]
    public class InputAbilityBinding
    {
        [Tooltip("输入 Action 引用，从 Input Action Asset 中拖入")]
        public InputActionReference InputAction;

        [Tooltip("该输入触发的 VS Ability")]
        public VisualScriptingAbility Ability;

        [Tooltip("输入触发模式")]
        public InputTriggerMode TriggerMode = InputTriggerMode.OnStarted;
    }

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
        public Variables Variables;
        public bool IsActive;
    }

    #endregion

    #region Serialized Fields

    [SerializeField]
    [Tooltip("VisualScriptingAbility 分组列表")]
    private List<VSAbilityCategory> m_AbilityCategories = new List<VSAbilityCategory>();

    [SerializeField]
    private VisualScriptingAbility m_DefaultAbility;

    [SerializeField]
    [Tooltip("输入绑定列表：配置 InputAction 与 VS Ability 的映射关系")]
    private List<InputAbilityBinding> m_InputBindings = new List<InputAbilityBinding>();

    [SerializeField]
    [Tooltip("Cinemachine 相机输入提供者（场景中拖入），用于 CinemachineCamera 锁定时禁用相机输入")]
    private CinemachineInputProvider m_CinemachineInputProvider;

    #endregion

    #region Runtime State

    private AnimancerComponent m_AnimancerComponent;
    private SkillCharacterController m_SkillCharacterController;

    /// <summary>
    /// 名称 → 运行时条目的映射
    /// </summary>
    private Dictionary<string, RuntimeEntry> m_EntryMap = new Dictionary<string, RuntimeEntry>();

    /// <summary>
    /// 所有运行时条目
    /// </summary>
    private List<RuntimeEntry> m_AllEntries = new List<RuntimeEntry>();

    /// <summary>
    /// 当前激活的标签列表（类似 AnimancerAbilityAgent.ActiveTags）
    /// </summary>
    private List<string> m_ActiveTags = new List<string>();

    /// <summary>
    /// 当前阻止的标签列表（类似 AnimancerAbilityAgent.BlockAbilitiesWithTag）
    /// </summary>
    private List<string> m_BlockTags = new List<string>();

    #endregion

    #region Properties

    /// <summary>
    /// 获取分组列表（只读）
    /// </summary>
    public IReadOnlyList<VSAbilityCategory> AbilityCategories => m_AbilityCategories;

    /// <summary>
    /// 当前激活的标签（只读）
    /// </summary>
    public IReadOnlyList<string> ActiveTags => m_ActiveTags;

    /// <summary>
    /// 当前阻止的标签（只读）
    /// </summary>
    public IReadOnlyList<string> BlockTags => m_BlockTags;

    /// <summary>
    /// 配置的默认 Ability
    /// </summary>
    public VisualScriptingAbility DefaultAbility => m_DefaultAbility;

    /// <summary>
    /// 获取输入绑定列表（只读）
    /// </summary>
    public IReadOnlyList<InputAbilityBinding> InputBindings => m_InputBindings;

    /// <summary>
    /// 缓存的 AnimancerComponent，为 null 时自动 GetComponent
    /// </summary>
    public AnimancerComponent AnimancerComponent
    {
        get
        {
            if (m_AnimancerComponent == null)
                m_AnimancerComponent = GetComponent<AnimancerComponent>();
            return m_AnimancerComponent;
        }
    }

    /// <summary>
    /// 缓存的 SkillCharacterController，为 null 时自动 GetComponent
    /// </summary>
    public SkillCharacterController SkillCharacterController
    {
        get
        {
            if (m_SkillCharacterController == null)
                m_SkillCharacterController = GetComponent<SkillCharacterController>();
            return m_SkillCharacterController;
        }
    }

    #endregion

    #region Lifecycle

    private void Start()
    {
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

        // 注册输入绑定
        RegisterInputBindings();

        // 订阅输入锁变化事件
        if (SkillCharacterController != null)
            SkillCharacterController.OnInputLockChanged += HandleInputLockChanged;

        // 启动时自动播放 DefaultAbility
        if (m_DefaultAbility != null)
            TryStartAbility(m_DefaultAbility.name);
    }

    private void OnEnable()
    {
        EnableInputActions();
    }

    private void OnDisable()
    {
        DisableInputActions();
    }

    private void OnDestroy()
    {
        UnregisterInputBindings();

        if (SkillCharacterController != null)
            SkillCharacterController.OnInputLockChanged -= HandleInputLockChanged;

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

        // 设置 Object Variables，将角色组件引用传入图中（避免 Unit 每次 GetComponent）
        var variables = childObj.GetComponent<Variables>();
        if (variables == null)
            variables = childObj.AddComponent<Variables>();
        variables.declarations.Set("Owner", gameObject);
        variables.declarations.Set("Animancer", AnimancerComponent);
        variables.declarations.Set("SkillController", SkillCharacterController);
        variables.declarations.Set("Linker", this);

        var entry = new RuntimeEntry
        {
            Ability = ability,
            ChildObject = childObj,
            Machine = machine,
            Variables = variables,
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

        // VS Graph 中的 CanStart 条件检查
        if (!CheckCanStartCondition(entry))
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
    /// 尝试启动指定名称的 VS Ability（执行 Tag 检查 → Cancel → 激活 → OnEnter）
    /// </summary>
    public bool TryStartAbility(string abilityName)
    {
        return TriggerOnEnter(abilityName);
    }

    /// <summary>
    /// 尝试停止指定名称的 VS Ability（OnExit → 移除 Tags → 停用）
    /// </summary>
    public void TryStopAbility(string abilityName)
    {
        TriggerOnExit(abilityName);
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

    #region Tag Management

    /// <summary>
    /// 检查 RequiredTags 和 Block 前置条件
    /// </summary>
    private bool CheckTagRequirements(VisualScriptingAbility ability)
    {
        // 检查 RequiredTags：当前 ActiveTags 中必须包含所有 RequiredTags
        if (ability.RequiredTags != null && ability.RequiredTags.Tags != null)
        {
            foreach (var requiredTag in ability.RequiredTags.Tags)
            {
                bool found = false;
                foreach (var activeTag in m_ActiveTags)
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

        // 检查是否被当前 BlockTags 阻止
        if (ability.AbilityTags != null && ability.AbilityTags.Tags != null)
        {
            foreach (var blockTag in m_BlockTags)
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
    /// 检查 VS Graph 中的 CanStart 条件
    /// 
    /// 参考树系统设计：
    /// 树系统中 AnimancerAbilityCanStartNode 是一个 ValueNode，Agent 调用 ability.CanStart() 时
    /// 同步通过 GetValue() → InputValue() 拉取上游条件。
    /// 
    /// VS 版本中 Visual Scripting 不支持同步拉取（需要 CustomEvent 触发执行流），
    /// 因此采用：临时启用子对象 → 触发 "CheckCanStart" 事件 → AbilityCanStartUnit 计算条件 → 
    /// 写入 Variables["CanStart"] → Linker 读取结果。
    /// 
    /// 如果 Graph 中没有 AbilityCanStartUnit 节点，Variables["CanStart"] 保持默认 true，即允许启动。
    /// </summary>
    private bool CheckCanStartCondition(RuntimeEntry entry)
    {
        var childObj = entry.ChildObject;
        var variables = entry.Variables;
        if (variables == null) return true;

        // 设置默认值为 true（如果 Graph 中没有 AbilityCanStartUnit 节点，则允许启动）
        variables.declarations.Set("CanStart", true);

        // 临时启用子对象以允许 ScriptMachine 接收事件
        // 注意：VS 的 CustomEvent 需要 GameObject 处于 active 状态才能触发
        bool wasActive = childObj.activeSelf;
        if (!wasActive) childObj.SetActive(true);

        // 触发 "CheckCanStart" 事件，Graph 中的 Custom Event 节点会驱动 AbilityCanStartUnit 执行
        CustomEvent.Trigger(childObj, "CheckCanStart");

        // 读取 AbilityCanStartUnit 写入的条件结果
        bool canStart = true;
        if (variables.declarations.IsDefined("CanStart"))
            canStart = (bool)variables.declarations.Get("CanStart");

        // 如果条件不满足，恢复子对象的禁用状态
        if (!wasActive) childObj.SetActive(false);

        if (!canStart)
            Debug.Log($"[AnimancerVSLinker] {entry.Ability.name} CanStart condition returned false");

        return canStart;
    }

    /// <summary>
    /// 处理 CancelAbilitiesWithTag：取消匹配标签的正在运行的 VS Ability
    /// </summary>
    private void ProcessCancelTags(VisualScriptingAbility ability)
    {
        if (ability.CancelAbilitiesWithTag == null || ability.CancelAbilitiesWithTag.Tags == null) return;

        // 收集需要取消的 entry（避免遍历中修改）
        var toCancel = new List<string>();
        foreach (var entry in m_AllEntries)
        {
            if (!entry.IsActive) continue;
            if (entry.Ability.AbilityTags != null &&
                entry.Ability.AbilityTags.PartChildOf(ability.CancelAbilitiesWithTag))
            {
                toCancel.Add(entry.Ability.name);
            }
        }

        foreach (var name in toCancel)
        {
            Debug.Log($"[AnimancerVSLinker] {name} canceled by VS ability {ability.name}");
            TriggerOnCancel(name, ability.name);
        }
    }

    /// <summary>
    /// 触发 Ability 的 OnCancel 事件后执行 OnExit 流程
    /// 
    /// 参考树系统设计：
    /// 树系统中 AnimancerAbility.CancelAbility(abilityCancelBy) 调用
    /// OnAnimancerAbilityCancelNode.Trigger(ability)：
    /// 1. 设置 m_Ability 输出端口 = 取消者引用
    /// 2. 调用 UpdateNode() 驱动后续 ActionNode 链执行
    /// 
    /// VS 版本中：
    /// 1. 将 "CancelledBy" 写入 Variables（取消者名称，供 OnAbilityCancelUnit 读取）
    /// 2. 触发 CustomEvent "OnCancel" → Graph 中的 Custom Event 节点 → OnAbilityCancelUnit → 后续清理逻辑
    /// 3. 最后执行正常的 TriggerOnExit 流程
    /// </summary>
    private void TriggerOnCancel(string abilityName, string cancelledBy)
    {
        if (!m_EntryMap.TryGetValue(abilityName, out var entry))
            return;

        if (!entry.IsActive)
            return;

        // 设置 CancelledBy 变量供 OnAbilityCancelUnit 读取
        var variables = entry.Variables;
        if (variables != null)
            variables.declarations.Set("CancelledBy", cancelledBy);

        // 触发 OnCancel 事件
        CustomEvent.Trigger(entry.ChildObject, "OnCancel");

        // 然后执行正常的 Exit 流程
        TriggerOnExit(abilityName);
    }

    private void AddActiveTags(VisualScriptingAbility ability)
    {
        if (ability.ActiveTags == null || ability.ActiveTags.Tags == null) return;
        foreach (var tag in ability.ActiveTags.Tags)
            m_ActiveTags.Add(tag);
    }

    private void RemoveActiveTags(VisualScriptingAbility ability)
    {
        if (ability.ActiveTags == null || ability.ActiveTags.Tags == null) return;
        foreach (var tag in ability.ActiveTags.Tags)
            m_ActiveTags.Remove(tag);
    }

    private void AddBlockTags(VisualScriptingAbility ability)
    {
        if (ability.BlockAbilitiesWithTag == null || ability.BlockAbilitiesWithTag.Tags == null) return;
        foreach (var tag in ability.BlockAbilitiesWithTag.Tags)
        {
            if (!m_BlockTags.Contains(tag))
                m_BlockTags.Add(tag);
        }
    }

    private void RemoveBlockTags(VisualScriptingAbility ability)
    {
        if (ability.BlockAbilitiesWithTag == null || ability.BlockAbilitiesWithTag.Tags == null) return;
        foreach (var tag in ability.BlockAbilitiesWithTag.Tags)
            m_BlockTags.Remove(tag);
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

    #region Input Lock Response

    private void HandleInputLockChanged(InputLockFlags effectiveLocks)
    {
        // CinemachineCamera 锁：禁用/启用相机输入
        if (m_CinemachineInputProvider != null)
        {
            bool cameraLocked = (effectiveLocks & InputLockFlags.CinemachineCamera) != 0;
            m_CinemachineInputProvider.enabled = !cameraLocked;
        }
    }

    #endregion

    #region Input Bindings

    private Dictionary<string, Action<InputAction.CallbackContext>> m_CallbackCache
        = new Dictionary<string, Action<InputAction.CallbackContext>>();

    private void RegisterInputBindings()
    {
        for (int i = 0; i < m_InputBindings.Count; i++)
        {
            var binding = m_InputBindings[i];
            if (binding == null || binding.InputAction == null || binding.Ability == null)
                continue;

            var action = binding.InputAction.action;
            if (action == null)
                continue;

            string abilityName = binding.Ability.name;
            var callback = CreateInputCallback(abilityName);

            switch (binding.TriggerMode)
            {
                case InputTriggerMode.OnStarted:
                    action.started += callback;
                    break;
                case InputTriggerMode.OnPerformed:
                    action.performed += callback;
                    break;
                case InputTriggerMode.OnCanceled:
                    action.canceled += callback;
                    break;
            }
        }
    }

    private void UnregisterInputBindings()
    {
        for (int i = 0; i < m_InputBindings.Count; i++)
        {
            var binding = m_InputBindings[i];
            if (binding == null || binding.InputAction == null)
                continue;

            var action = binding.InputAction.action;
            if (action == null)
                continue;

            string abilityName = binding.Ability != null ? binding.Ability.name : null;
            if (string.IsNullOrEmpty(abilityName))
                continue;

            var callback = CreateInputCallback(abilityName);

            switch (binding.TriggerMode)
            {
                case InputTriggerMode.OnStarted:
                    action.started -= callback;
                    break;
                case InputTriggerMode.OnPerformed:
                    action.performed -= callback;
                    break;
                case InputTriggerMode.OnCanceled:
                    action.canceled -= callback;
                    break;
            }
        }

        m_CallbackCache.Clear();
    }

    private Action<InputAction.CallbackContext> CreateInputCallback(string abilityName)
    {
        if (!m_CallbackCache.TryGetValue(abilityName, out var callback))
        {
            callback = (ctx) =>
            {
                if (SkillCharacterController != null
                    && SkillCharacterController.IsInputLocked(InputLockFlags.AbilityInput))
                    return;
                TryStartAbility(abilityName);
            };
            m_CallbackCache[abilityName] = callback;
        }
        return callback;
    }

    private void EnableInputActions()
    {
        for (int i = 0; i < m_InputBindings.Count; i++)
        {
            var binding = m_InputBindings[i];
            if (binding?.InputAction?.action != null)
                binding.InputAction.action.Enable();
        }
    }

    private void DisableInputActions()
    {
        for (int i = 0; i < m_InputBindings.Count; i++)
        {
            var binding = m_InputBindings[i];
            if (binding?.InputAction?.action != null)
                binding.InputAction.action.Disable();
        }
    }

    #endregion
}
