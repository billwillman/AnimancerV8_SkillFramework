using System;
using System.Collections.Generic;
using UnityEngine;
using Taco.Gameplay;

/// <summary>
/// Animancer Ability 的运行管理器，复用 AbilityRunner 的全部 Tag 阻塞/取消/缓冲逻辑
/// </summary>
public class AnimancerAbilityAgent
{
    public HashSet<AnimancerAbility> Abilities = new HashSet<AnimancerAbility>();
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

    public virtual void Init()
    {
        Abilities.Clear();
        AbilityMap.Clear();
    }

    public virtual void Dispose()
    {
        foreach (var ability in Abilities)
        {
            TryStopAbility(ability);
            ability.DisposeTree();
        }
        Abilities.Clear();
        AbilityMap.Clear();
    }

    public virtual void AddAbility(AnimancerAbility ability)
    {
        if (!Abilities.Contains(ability))
        {
            ability.InitTree(this);
            Abilities.Add(ability);
            AbilityMap.Add(ability.name, ability);
        }
    }

    public virtual void RemoveAbility(AnimancerAbility ability)
    {
        if (Abilities.Contains(ability))
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

        foreach (var ability in Abilities)
        {
            if (ability.Active && abilityToStart.AbilityTags.PartChildOf(ability.BlockAbilitiesWithTag))
            {
                Starting = false;
                AddToBuffer(abilityToStart);
                Debug.Log($"{abilityToStart} is blocked by {ability}");
                return false;
            }
        }

        if (!abilityToStart.CanStart())
        {
            Starting = false;
            AddToBuffer(abilityToStart);
            Debug.Log($"{abilityToStart} can't start");
            return false;
        }

        foreach (var ability in Abilities)
        {
            if (ability.Active)
            {
                if (ability.AbilityTags.PartChildOf(abilityToStart.CancelAbilitiesWithTag))
                {
                    ability.CancelAbility(abilityToStart);
                    TryStopAbility(ability);
                    Debug.Log($"{ability} is canceled by {abilityToStart}");
                    break;
                }
            }
        }

        BufferedAbilities.Clear();
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

        foreach (var ability in Abilities)
        {
            if (ability.Active)
            {
                ability.UpdateAbility(deltaTime);
            }
            else
            {
                ability.InactiveUpdate();
            }
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
