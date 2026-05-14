using System;
using System.Collections.Generic;
using UnityEngine;
using Taco.Gameplay;
using TreeDesigner;
using Animancer;
using EasyCharacterMovement;
using UnityTimeline;

/// <summary>
/// Animancer Ability 的运行管理器。
/// Blackboard 模式：BeginContext 仅绑定 BlackboardContext，EndContext 仅解绑。
/// 所有运行时数据存在 CommonBlackboard 中，SO 完全只读。
/// </summary>
public class AnimancerAbilityAgent
{
    /// <summary>Key = Ability SO，Value = 对应的 BlackboardContext（由 CommonBlackboard 管理）</summary>
    public Dictionary<AnimancerAbility, BlackboardContext> Abilities = new Dictionary<AnimancerAbility, BlackboardContext>();
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

    public string DefaultAbilityName { get; set; }
    public IAnimancerAbilityAgentOwner Owner { get; set; }

    /// <summary>CommonBlackboard 组件引用，由 Linker 注入</summary>
    public CommonBlackboard Blackboard { get; set; }

    public virtual void Init()
    {
        Abilities.Clear();
        AbilityMap.Clear();
    }

    public virtual void Dispose()
    {
        foreach (var kv in Abilities)
        {
            var activeEP = kv.Value.EPMap.TryGetValue("Active", out var ep) ? ep as BoolExposedProperty : null;
            if (activeEP != null && activeEP.Value)
            {
                BeginContext(kv.Key);
                kv.Key.StopAbility();
                EndContext(kv.Key);
            }
            Blackboard?.UnregisterTree(kv.Key);
            kv.Key.DisposeTree();
        }
        Abilities.Clear();
        AbilityMap.Clear();
    }

    public virtual void AddAbility(AnimancerAbility ability)
    {
        if (Abilities.ContainsKey(ability)) return;

        ability.InitTree(this);

        // 通过 CommonBlackboard 注册 Tree，创建 BlackboardContext + 克隆 SO EP
        var ctx = Blackboard.RegisterTree(ability);

        // 注入上下文 EP（Agent、AnimancerComponent、Character、SkillController）
        var linker = Owner as AnimancerAbilityLinker;
        ctx.EPMap["Agent"] = new AnimancerAbilityAgentExposedProperty { Name = "Agent", Value = this };

        if (linker?.AnimancerComponent != null)
        {
            ctx.EPMap["AnimancerComponent"] = new AnimancerComponentExposedProperty
                { Name = "AnimancerComponent", Value = linker.AnimancerComponent };

            var character = linker.AnimancerComponent.GetComponent<Character>();
            if (character != null)
                ctx.EPMap["Character"] = new CharacterExposedProperty { Name = "Character", Value = character };

            var skillController = linker.AnimancerComponent.GetComponent<SkillCharacterController>();
            if (skillController != null)
                ctx.EPMap["SkillController"] = new SkillCharacterControllerExposedProperty
                    { Name = "SkillController", Value = skillController };
        }

        // 确保 Active / Duration EP 存在于 EPMap（RegisterTree 已从 SO 克隆）
        // 无需额外处理

        Abilities[ability] = ctx;
        AbilityMap[ability.name] = ability;

        // 立即绑定确保初始状态正确
        BeginContext(ability);
    }

    public virtual void RemoveAbility(AnimancerAbility ability)
    {
        if (!Abilities.ContainsKey(ability)) return;
        EndContext(ability);
        Blackboard?.UnregisterTree(ability);
        ability.DisposeTree();
        Abilities.Remove(ability);
        AbilityMap.Remove(ability.name);
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

    public BlackboardContext GetContext(AnimancerAbility ability)
        => Abilities.TryGetValue(ability, out var ctx) ? ctx : null;

    /// <summary>判断指定 Ability 是否激活（从 Blackboard EPMap 读取）</summary>
    public bool IsAbilityActive(AnimancerAbility ability)
    {
        if (Abilities.TryGetValue(ability, out var ctx)
            && ctx.EPMap.TryGetValue("Active", out var ep)
            && ep is BoolExposedProperty activeEP)
            return activeEP.Value;
        return false;
    }

    public virtual bool TryStartAbility(string name)
    {
        if (AbilityMap.TryGetValue(name, out AnimancerAbility ability))
            return TryStartAbility(ability);
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

        // RequiredTags 检查
        foreach (var requiredTag in abilityToStart.RequiredTags.Tags)
        {
            bool isChild = false;
            foreach (var activeTag in ActiveTags)
            {
                if (activeTag.StartTagIs(requiredTag)) { isChild = true; break; }
            }
            if (!isChild)
            {
                Starting = false;
                AddToBuffer(abilityToStart);
                Debug.Log($"{abilityToStart} required tag {requiredTag}");
                return false;
            }
        }

        // BlockAbilitiesWithTag 检查
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

        // 检查现有激活 Ability 是否阻止启动
        foreach (var kv in Abilities)
        {
            if (IsAbilityActive(kv.Key) && abilityToStart.AbilityTags.PartChildOf(kv.Key.BlockAbilitiesWithTag))
            {
                Starting = false;
                AddToBuffer(abilityToStart);
                Debug.Log($"{abilityToStart} is blocked by {kv.Key}");
                return false;
            }
        }

        // CanStart 检查
        if (!Abilities.ContainsKey(abilityToStart))
        {
            Starting = false;
            return false;
        }
        BeginContext(abilityToStart);
        bool canStart = abilityToStart.CanStart();
        if (!canStart)
        {
            Starting = false;
            AddToBuffer(abilityToStart);
            Debug.Log($"{abilityToStart} can't start");
            return false;
        }

        // 取消冲突的激活 Ability
        foreach (var kv in Abilities)
        {
            if (IsAbilityActive(kv.Key) && kv.Key.AbilityTags.PartChildOf(abilityToStart.CancelAbilitiesWithTag))
            {
                BeginContext(kv.Key);
                kv.Key.CancelAbility(abilityToStart);
                EndContext(kv.Key);
                TryStopAbility(kv.Key);
                Debug.Log($"{kv.Key} is canceled by {abilityToStart}");
                break;
            }
        }

        // 启动目标 Ability
        BufferedAbilities.Clear();
        BeginContext(abilityToStart);
        abilityToStart.StartAbility();
        EndContext(abilityToStart);
        OnAbilityStart?.Invoke(abilityToStart);

        Starting = false;
        return true;
    }

    public virtual void TryStopAbility(string name)
    {
        if (AbilityMap.TryGetValue(name, out AnimancerAbility ability))
            TryStopAbility(ability);
    }

    public virtual void TryStopAbility(AnimancerAbility abilityToStop)
    {
        if (Stopping)
        {
            StoppingBuffer.Enqueue(() => TryStopAbility(abilityToStop));
            return;
        }

        Stopping = true;
        if (IsAbilityActive(abilityToStop))
        {
            BeginContext(abilityToStop);
            abilityToStop.StopAbility();
            EndContext(abilityToStop);
            OnAbilityStop?.Invoke(abilityToStop);
        }
        Stopping = false;
    }

    public virtual void Update(float deltaTime)
    {
        // 尝试从缓冲启动
        for (int i = BufferedAbilities.Count - 1; i >= 0; i--)
        {
            if (TryStartAbility(BufferedAbilities[i]))
                break;
        }

        foreach (var kv in Abilities)
        {
            BeginContext(kv.Key);
            if (IsAbilityActive(kv.Key))
            {
                kv.Key.UpdateAbility(deltaTime);
            }
            else
            {
                kv.Key.InactiveUpdate();
            }
            EndContext(kv.Key);
        }
    }

    // ── Blackboard Context Bind ──

    void BeginContext(AnimancerAbility ability)
    {
        Blackboard?.BindTree(ability);
    }

    void EndContext(AnimancerAbility ability)
    {
        Blackboard?.UnbindTree(ability);
    }
}

/// <summary>
/// AnimancerAbilityAgent 的所有者接口
/// </summary>
public interface IAnimancerAbilityAgentOwner
{
    AnimancerAbilityAgent AnimancerAbilityAgent { get; set; }
}
