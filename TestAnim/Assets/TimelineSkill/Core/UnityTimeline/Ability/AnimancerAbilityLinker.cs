using System;
using System.Collections.Generic;
using UnityEngine;
using Animancer;
using TreeDesigner;

/// <summary>
/// AnimancerAbility 的 MonoBehaviour 桥接组件，挂载到角色上
/// 持有 AnimancerComponent 引用并初始化 AnimancerAbilityAgent
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public class AnimancerAbilityLinker : MonoBehaviour, IAnimancerAbilityAgentOwner
{
    [SerializeField]
    private List<AnimancerAbility> m_Abilities = new List<AnimancerAbility>();

    public AnimancerAbilityAgent AnimancerAbilityAgent { get; set; }

    public AnimancerComponent AnimancerComponent { get; private set; }

    public event Action<AnimancerAbility> OnAbilityStart;
    public event Action<AnimancerAbility> OnAbilityStop;

    public event Action OnAbilityReady; // 都准备好了

    private bool m_IsReady = false;

    public bool IsReady => m_IsReady; // 是否准备好

    private void Awake()
    {
        AnimancerComponent = GetComponent<AnimancerComponent>();
        AnimancerAbilityAgent = new AnimancerAbilityAgent();
    }

    private void Start()
    {
        AnimancerAbilityAgent.Init();
        AnimancerAbilityAgent.OnAbilityStart += HandleAbilityStart;
        AnimancerAbilityAgent.OnAbilityStop += HandleAbilityStop;

        for (int i = 0; i < m_Abilities.Count; i++)
        {
            if (m_Abilities[i] != null)
            {
                m_Abilities[i] = m_Abilities[i].Clone();
                m_Abilities[i].AnimancerComponent = AnimancerComponent;
                AnimancerAbilityAgent.AddAbility(m_Abilities[i]);
            }
        }
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
        if (AnimancerAbilityAgent != null)
        {
            AnimancerAbilityAgent.OnAbilityStart -= HandleAbilityStart;
            AnimancerAbilityAgent.OnAbilityStop -= HandleAbilityStop;
            AnimancerAbilityAgent.Dispose();
            AnimancerAbilityAgent = null;
        }
    }

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
    /// 添加一个 Ability
    /// </summary>
    public void AddAbility(AnimancerAbility ability)
    {
        if (ability != null && AnimancerAbilityAgent != null)
        {
            ability = TreeUtility.Clone(ability);
            ability.AnimancerComponent = AnimancerComponent;
            AnimancerAbilityAgent.AddAbility(ability);
            if (!m_Abilities.Contains(ability))
                m_Abilities.Add(ability);
        }
    }

    /// <summary>
    /// 移除一个 Ability
    /// </summary>
    public void RemoveAbility(AnimancerAbility ability)
    {
        if (ability != null && AnimancerAbilityAgent != null)
        {
            AnimancerAbilityAgent.RemoveAbility(ability);
            m_Abilities.Remove(ability);
        }
    }
}
