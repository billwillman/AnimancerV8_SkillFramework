using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Animancer;
using TreeDesigner;
using UnityTimeline;
using Cinemachine;

/// <summary>
/// AnimancerAbility 的 MonoBehaviour 桥接组件，挂载到角色上
/// 持有 AnimancerComponent 引用并初始化 AnimancerAbilityAgent
/// 支持通过 InputActionReference 绑定输入触发 Ability
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public class AnimancerAbilityLinker : MonoBehaviour, IAnimancerAbilityAgentOwner
{
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
    /// 输入触发绑定：一个 InputActionReference 对应一个要触发的 Ability
    /// </summary>
    [Serializable]
    public class InputAbilityBinding
    {
        [Tooltip("输入 Action 引用，从 Input Action Asset 中拖入")]
        public InputActionReference InputAction;

        [Tooltip("该输入触发的 Ability")]
        public AnimancerAbility Ability;

        [Tooltip("输入触发模式")]
        public InputTriggerMode TriggerMode = InputTriggerMode.OnStarted;
    }

    /// <summary>
    /// Ability 分类分组：每个分组有一个唯一的分类名和该分类下的 Ability 列表
    /// </summary>
    [Serializable]
    public class AbilityCategory
    {
        public string CategoryName;
        public List<AnimancerAbility> Abilities = new List<AnimancerAbility>();
    }

    [SerializeField]
    private List<AbilityCategory> m_AbilityCategories = new List<AbilityCategory>();

    [SerializeField]
    private AnimancerAbility m_DefaultAbility;

    /// <summary>
    /// 配置的默认 Ability
    /// </summary>
    public AnimancerAbility DefaultAbility => m_DefaultAbility;

    [SerializeField]
    [Tooltip("输入绑定列表：配置 InputAction 与 Ability 的映射关系")]
    private List<InputAbilityBinding> m_InputBindings = new List<InputAbilityBinding>();

    [SerializeField]
    [Tooltip("Cinemachine 相机输入提供者（场景中拖入），用于 CinemachineCamera 锁定时禁用相机输入")]
    private CinemachineInputProvider m_CinemachineInputProvider;

    public AnimancerAbilityAgent AnimancerAbilityAgent { get; set; }

    public AnimancerComponent AnimancerComponent { get; private set; }

    /// <summary>
    /// 缓存的 SkillCharacterController，避免重复 GetComponent
    /// </summary>
    public SkillCharacterController SkillCharacterController { get; private set; }

    public event Action<AnimancerAbility> OnAbilityStart;
    public event Action<AnimancerAbility> OnAbilityStop;

    public event Action OnAbilityReady; // 都准备好了

    private bool m_IsReady = false;

    public bool IsReady => m_IsReady; // 是否准备好

    /// <summary>
    /// 获取输入绑定列表（只读）
    /// </summary>
    public IReadOnlyList<InputAbilityBinding> InputBindings => m_InputBindings;

    private void Awake()
    {
        AnimancerComponent = GetComponent<AnimancerComponent>();
        SkillCharacterController = GetComponent<SkillCharacterController>();
        AnimancerAbilityAgent = new AnimancerAbilityAgent();
        AnimancerAbilityAgent.Owner = this;
    }

    private void Start()
    {
        AnimancerAbilityAgent.Init();
        AnimancerAbilityAgent.OnAbilityStart += HandleAbilityStart;
        AnimancerAbilityAgent.OnAbilityStop += HandleAbilityStop;

        foreach (var category in m_AbilityCategories)
        {
            if (category == null) continue;
            foreach (var ability in category.Abilities)
            {
                if (ability != null)
                {
                    AnimancerAbilityAgent.AddAbility(ability);
                    //ability.SetContextAnimancerComponent(AnimancerComponent);
                }
            }
        }

        // 注册输入绑定
        RegisterInputBindings();

        // 订阅输入锁变化事件
        if (SkillCharacterController != null)
            SkillCharacterController.OnInputLockChanged += HandleInputLockChanged;

        // 设置默认 Ability 名称
        if (m_DefaultAbility != null)
            AnimancerAbilityAgent.DefaultAbilityName = m_DefaultAbility.name;

        // 启动时自动播放 DefaultAbility
        if (m_DefaultAbility != null)
            AnimancerAbilityAgent.TryStartAbility(m_DefaultAbility.name);

        // Ability都准备好了
        m_IsReady = true;
        if (OnAbilityReady != null)
            OnAbilityReady();
    }

    private void Update()
    {
        AnimancerAbilityAgent?.Update(Time.deltaTime);
    }

    private void OnDestroy()
    {
        UnregisterInputBindings();

        if (SkillCharacterController != null)
            SkillCharacterController.OnInputLockChanged -= HandleInputLockChanged;

        if (AnimancerAbilityAgent != null)
        {
            AnimancerAbilityAgent.OnAbilityStart -= HandleAbilityStart;
            AnimancerAbilityAgent.OnAbilityStop -= HandleAbilityStop;
            AnimancerAbilityAgent.Dispose();
            AnimancerAbilityAgent = null;
        }
    }

    private void OnEnable()
    {
        EnableInputActions();
    }

    private void OnDisable()
    {
        DisableInputActions();
    }

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

    private Dictionary<string, Action<InputAction.CallbackContext>> m_CallbackCache
        = new Dictionary<string, Action<InputAction.CallbackContext>>();

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

    private void HandleAbilityStart(AnimancerAbility ability)
    {
        OnAbilityStart?.Invoke(ability);
    }

    private void HandleAbilityStop(AnimancerAbility ability)
    {
        OnAbilityStop?.Invoke(ability);
    }

    /// <summary>
    /// 尝试启动指定名称的 Ability
    /// </summary>
    public bool TryStartAbility(string abilityName)
    {
        return AnimancerAbilityAgent?.TryStartAbility(abilityName) ?? false;
    }

    /// <summary>
    /// 尝试停止指定名称的 Ability
    /// </summary>
    public void TryStopAbility(string abilityName)
    {
        AnimancerAbilityAgent?.TryStopAbility(abilityName);
    }

    /// <summary>
    /// 获取所有分组中的全部 Ability（只读）
    /// </summary>
    /// <summary>
    /// 获取分组列表（只读）
    /// </summary>
    public IReadOnlyList<AbilityCategory> AbilityCategories => m_AbilityCategories;

    /// <summary>
    /// 添加一个 Ability 到指定分组（若分组不存在则创建）
    /// </summary>
    public void AddAbility(AnimancerAbility ability, string categoryName = "Default")
    {
        if (ability != null && AnimancerAbilityAgent != null)
        {
            AnimancerAbilityAgent.AddAbility(ability);
            ability.SetContextAnimancerComponent(AnimancerComponent);

            var category = m_AbilityCategories.Find(c => c.CategoryName == categoryName);
            if (category == null)
            {
                category = new AbilityCategory { CategoryName = categoryName };
                m_AbilityCategories.Add(category);
            }
            if (!category.Abilities.Contains(ability))
                category.Abilities.Add(ability);
        }
    }

    /// <summary>
    /// 移除一个 Ability（从所有分组中移除）
    /// </summary>
    public void RemoveAbility(AnimancerAbility ability)
    {
        if (ability != null && AnimancerAbilityAgent != null)
        {
            AnimancerAbilityAgent.RemoveAbility(ability);
            foreach (var category in m_AbilityCategories)
            {
                category.Abilities.Remove(ability);
            }
        }
    }
}
