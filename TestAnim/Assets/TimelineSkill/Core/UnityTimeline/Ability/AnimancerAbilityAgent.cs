using System;
using System.Collections.Generic;
using UnityEngine;
using Taco.Gameplay;
using TreeDesigner;

/// <summary>
/// 单个 Ability 在本角色实例上的运行时数据（per-instance，不存放在 SO 上）
/// </summary>
public class AbilityContext
{
    /// <summary>该 Ability 在本角色上是否激活</summary>
    public BoolExposedProperty  IsActive = new BoolExposedProperty()  { Name = "Active"   };
    /// <summary>该 Ability 在本角色上已运行的时长</summary>
    public FloatExposedProperty Duration = new FloatExposedProperty() { Name = "Duration" };
}

/// <summary>
/// Animancer Ability 的运行管理器，复用 AbilityRunner 的全部 Tag 阻塞/取消/缓冲逻辑
/// </summary>
public class AnimancerAbilityAgent
{
    /// <summary>Key = Ability SO，Value = 本角色对该 Ability 的运行时上下文</summary>
    public Dictionary<AnimancerAbility, AbilityContext> Abilities = new Dictionary<AnimancerAbility, AbilityContext>();
    public Dictionary<string, AnimancerAbility> AbilityMap = new Dictionary<string, AnimancerAbility>();

    public event Action<AnimancerAbility> OnAbilityStart;
    public event Action<AnimancerAbility> OnAbilityStop;

    bool m_Starting;
    public bool Starting
    {
        get => m_Starting;
        set
        {
            m_Starting = value;
            if (StartingBuffer.Count > 0)
                StartingBuffer.Dequeue().Invoke();
        }
    }
    public Queue<Action> StartingBuffer = new Queue<Action>();

    bool m_Stopping;
    public bool Stopping
    {
        get => m_Stopping;
        set
        {
            m_Stopping = value;
            if (StoppingBuffer.Count > 0)
                StoppingBuffer.Dequeue().Invoke();
        }
    }
    public Queue<Action> StoppingBuffer = new Queue<Action>();

    public List<string> ActiveTags = new List<string>();
    public List<string> BlockAbilitiesWithTag = new List<string>();
    public List<string> CanBufferAbilitiesTag = new List<string>();

    public List<AnimancerAbility> BufferedAbilities = new List<AnimancerAbility>();

    public AnimancerAbilityAgent() { }

    /// <summary>
    /// 默认 Ability 名称，由 AnimancerAbilityLinker 在初始化时设置
    /// </summary>
    public string DefaultAbilityName { get; set; }

    /// <summary>
    /// Agent 的所有者（Linker），由 Linker 在初始化时设置
    /// </summary>
    public IAnimancerAbilityAgentOwner Owner { get; set; }

    public virtual void Init()
    {
        Abilities.Clear();
        AbilityMap.Clear();
    }

    public virtual void Dispose()
    {
        foreach (var kv in Abilities)
        {
            TryStopAbility(kv.Key);
            kv.Key.DisposeTree();
        }
        Abilities.Clear();
        AbilityMap.Clear();
    }

    public virtual void AddAbility(AnimancerAbility ability)
    {
        if (!Abilities.ContainsKey(ability))
        {
            ability.InitTree(this);
            var ctx = new AbilityContext();
            Abilities[ability] = ctx;
            AbilityMap.Add(ability.name, ability);
            // 立即绑定，确保 m_Active / m_Duration 指向 per-instance 实例
            ApplyAbilityContext(ability);
        }
    }

    public virtual void RemoveAbility(AnimancerAbility ability)
    {
        if (Abilities.ContainsKey(ability))
        {
            ability.DisposeTree();
            Abilities.Remove(ability);
            AbilityMap.Remove(ability.name);
        }
    }

    public void AddToBuffer(AnimancerAbility abilityToBuffer)
    {
        foreach (var tag in CanBufferAbilitiesTag)
        {
            if (abilityToBuffer.AbilityTags.IsChildOf(tag))
            {
                if (!BufferedAbilities.Contains(abilityToBuffer))
                    BufferedAbilities.Add(abilityToBuffer);
                break;
            }
        }
    }

    /// <summary>
    /// 获取 Ability 的运行时上下文，若不存在则返回 null
    /// </summary>
    public AbilityContext GetContext(AnimancerAbility ability)
        => Abilities.TryGetValue(ability, out var ctx) ? ctx : null;

    public virtual bool TryStartAbility(string name)
    {
        if (AbilityMap.TryGetValue(name, out AnimancerAbility abilityToStart))
        {
            return TryStartAbility(abilityToStart);
        }
        return false;
    }

    public virtual bool TryStartAbility(AnimancerAbility abilityToStart)
    {
        if (Starting)
        {
            StartingBuffer.Enqueue(() => TryStartAbility(abilityToStart));
            return false;
        }

        Starting = true;

        foreach (var requiredTag in abilityToStart.RequiredTags.Tags)
        {
            bool isChild = false;
            foreach (var activeTag in ActiveTags)
            {
                if (activeTag.StartTagIs(requiredTag))
                {
                    isChild = true;
                    break;
                }
            }
            if (!isChild)
            {
                Starting = false;
                AddToBuffer(abilityToStart);
                Debug.Log($"{abilityToStart} required tag {requiredTag}");
                return false;
            }
        }

        foreach (var blockTag in BlockAbilitiesWithTag)
        {
            if (abilityToStart.AbilityTags.IsChildOf(blockTag))
            {
                Starting = false;
                AddToBuffer(abilityToStart);
                Debug.Log($"{abilityToStart} is blocked by tag {blockTag}");
                return false;
            }
        }

        foreach (var kv in Abilities)
        {
            ApplyAbilityContext(kv.Key);
            if (kv.Key.Active && abilityToStart.AbilityTags.PartChildOf(kv.Key.BlockAbilitiesWithTag))
            {
                Starting = false;
                AddToBuffer(abilityToStart);
                Debug.Log($"{abilityToStart} is blocked by {kv.Key}");
                return false;
            }
        }

        ApplyAbilityContext(abilityToStart);
        if (!abilityToStart.CanStart())
        {
            Starting = false;
            AddToBuffer(abilityToStart);
            Debug.Log($"{abilityToStart} can't start");
            return false;
        }

        foreach (var kv in Abilities)
        {
            ApplyAbilityContext(kv.Key);
            if (kv.Key.Active)
            {
                if (kv.Key.AbilityTags.PartChildOf(abilityToStart.CancelAbilitiesWithTag))
                {
                    kv.Key.CancelAbility(abilityToStart);
                    TryStopAbility(kv.Key);
                    Debug.Log($"{kv.Key} is canceled by {abilityToStart}");
                    break;
                }
            }
        }

        BufferedAbilities.Clear();
        ApplyAbilityContext(abilityToStart);
        abilityToStart.StartAbility();
        OnAbilityStart?.Invoke(abilityToStart);

        Starting = false;
        return true;
    }

    public virtual void TryStopAbility(string name)
    {
        if (AbilityMap.TryGetValue(name, out AnimancerAbility abilityToStop))
        {
            TryStopAbility(abilityToStop);
        }
    }

    public virtual void TryStopAbility(AnimancerAbility abilityToStop)
    {
        if (Stopping)
        {
            StoppingBuffer.Enqueue(() => TryStopAbility(abilityToStop));
            return;
        }

        Stopping = true;
        ApplyAbilityContext(abilityToStop);
        if (abilityToStop.Active)
        {
            abilityToStop.StopAbility();
            OnAbilityStop?.Invoke(abilityToStop);
        }
        Stopping = false;
    }

    public virtual void Update(float deltaTime)
    {
        for (int i = BufferedAbilities.Count - 1; i >= 0; i--)
        {
            AnimancerAbility ability = BufferedAbilities[i];
            if (TryStartAbility(ability))
                break;
        }

        foreach (var kv in Abilities)
        {
            ApplyAbilityContext(kv.Key);
            if (kv.Key.Active)
            {
                kv.Key.UpdateAbility(deltaTime);
            }
            else
            {
                kv.Key.InactiveUpdate();
            }
        }
    }

    void ApplyAbilityContext(AnimancerAbility ability)
    {
        var linker = Owner as AnimancerAbilityLinker;
        if (linker != null)
        {
            ability.SetContextAnimancerComponent(linker.AnimancerComponent);
            ability.SetContextAgent(this);
        }

        if (Abilities.TryGetValue(ability, out var ctx))
        {
            ability.SetContextActiveEP(ctx.IsActive);
            ability.SetContextDurationEP(ctx.Duration);
        }
    }
}

/// <summary>
/// AnimancerAbilityAgent 的所有者接口
/// </summary>
public interface IAnimancerAbilityAgentOwner
{
    AnimancerAbilityAgent AnimancerAbilityAgent { get; set; }
}
