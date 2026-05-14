using System;
using System.Collections.Generic;
using UnityEngine;
using Taco.Gameplay;
using TreeDesigner;
using Animancer;
using EasyCharacterMovement;
using UnityTimeline;

/// <summary>
/// 单个 RunnableNode 的 per-instance 快照：执行状态 + 节点自定义状态字段。
/// </summary>
public class NodeSnapshot
{
    public State State;
    /// <summary>
    /// 节点子类的自定义 [NonSerialized] 字段（懒分配）。
    /// 无自定义状态的节点此字段为 null，避免不必要的内存分配。
    /// </summary>
    public Dictionary<string, object> Custom;
}

/// <summary>
/// 单个 Ability 在本角色实例上的运行时数据（per-instance，不存放在 SO 上）
/// </summary>
public class AbilityContext
{
    /// <summary>该 Ability 在本角色上是否激活</summary>
    public BoolExposedProperty  IsActive = new BoolExposedProperty()  { Name = "Active"   };
    /// <summary>该 Ability 在本角色上已运行的时长</summary>
    public FloatExposedProperty Duration = new FloatExposedProperty() { Name = "Duration" };

    // ── 角色组件直接引用（由 AddAbility 时初始化，BeginContext 时注入到 SO）──
    public AnimancerComponent        AnimancerComponent;
    public Character                  Character;
    public SkillCharacterController   SkillController;

    /// <summary>
    /// 用户自定义 EP 的 per-instance 副本（含 IsActive / Duration）。
    /// Key = EP.Name，由 AddAbility 时从 SO 克隆初始化。
    /// </summary>
    public Dictionary<string, BaseExposedProperty> EPMap = new Dictionary<string, BaseExposedProperty>();

    /// <summary>
    /// per-instance 节点快照（GUID → NodeSnapshot）。
    /// 由 BeginContext/EndContext 做 save-restore，同时覆盖 m_State 和自定义字段。
    /// </summary>
    public Dictionary<string, NodeSnapshot> NodeStateMap = new Dictionary<string, NodeSnapshot>();
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

    /// <summary>默认 Ability 名称，由 AnimancerAbilityLinker 在初始化时设置</summary>
    public string DefaultAbilityName { get; set; }

    /// <summary>Agent 的所有者（Linker），由 Linker 在初始化时设置</summary>
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
            if (kv.Value.IsActive.Value)
            {
                BeginContext(kv.Key, kv.Value);
                kv.Key.StopAbility();
                EndContext(kv.Key, kv.Value);
            }
            kv.Key.DisposeTree();
        }
        Abilities.Clear();
        AbilityMap.Clear();
    }

    public virtual void AddAbility(AnimancerAbility ability)
    {
        if (Abilities.ContainsKey(ability)) return;

        ability.InitTree(this);
        var ctx = new AbilityContext();

        // 克隆所有 SO 上的用户 EP 到 per-instance EPMap
        foreach (var ep in ability.ExposedProperties)
            ctx.EPMap[ep.Name] = CloneEP(ep);

        // 让 EPMap 里的 Active / Duration 指向 ctx 的专用实例，保持一致
        ctx.EPMap["Active"]   = ctx.IsActive;
        ctx.EPMap["Duration"] = ctx.Duration;

        // 初始化节点快照（State = None，无自定义状态）
        foreach (var node in ability.Nodes)
            if (node is RunnableNode)
                ctx.NodeStateMap[node.GUID] = new NodeSnapshot { State = State.None };

        // 初始化角色组件引用
        var linkerForInit = Owner as AnimancerAbilityLinker;
        if (linkerForInit?.AnimancerComponent != null)
        {
            ctx.AnimancerComponent = linkerForInit.AnimancerComponent;
            ctx.Character          = linkerForInit.AnimancerComponent.GetComponent<Character>();
            ctx.SkillController    = linkerForInit.AnimancerComponent.GetComponent<SkillCharacterController>();
        }

        Abilities[ability] = ctx;
        AbilityMap.Add(ability.name, ability);

        // 立即绑定，确保初始状态正确
        BeginContext(ability, ctx);
    }

    public virtual void RemoveAbility(AnimancerAbility ability)
    {
        if (!Abilities.ContainsKey(ability)) return;
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

    /// <summary>获取 Ability 的运行时上下文，若不存在则返回 null</summary>
    public AbilityContext GetContext(AnimancerAbility ability)
        => Abilities.TryGetValue(ability, out var ctx) ? ctx : null;

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

        // RequiredTags 检查（不涉及树执行，无需 Begin/End）
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

        // 检查现有激活 Ability 是否阻止启动（直接读 ctx，无需 Begin/End）
        foreach (var kv in Abilities)
        {
            if (kv.Value.IsActive.Value && abilityToStart.AbilityTags.PartChildOf(kv.Key.BlockAbilitiesWithTag))
            {
                Starting = false;
                AddToBuffer(abilityToStart);
                Debug.Log($"{abilityToStart} is blocked by {kv.Key}");
                return false;
            }
        }

        // CanStart 检查（ValueNode 计算，不改变 RunnableNode.m_State，只需 BeginContext）
        if (!Abilities.TryGetValue(abilityToStart, out var startCtx))
        {
            Starting = false;
            return false;
        }
        BeginContext(abilityToStart, startCtx);
        bool canStart = abilityToStart.CanStart();
        if (!canStart)
        {
            Starting = false;
            AddToBuffer(abilityToStart);
            Debug.Log($"{abilityToStart} can't start");
            return false;
        }

        // 取消冲突的激活 Ability（CancelAbility 会执行 UpdateNode，需要 Begin/End）
        foreach (var kv in Abilities)
        {
            if (kv.Value.IsActive.Value && kv.Key.AbilityTags.PartChildOf(abilityToStart.CancelAbilitiesWithTag))
            {
                BeginContext(kv.Key, kv.Value);
                kv.Key.CancelAbility(abilityToStart);
                EndContext(kv.Key, kv.Value);
                TryStopAbility(kv.Key);
                Debug.Log($"{kv.Key} is canceled by {abilityToStart}");
                break;
            }
        }

        // 启动目标 Ability
        BufferedAbilities.Clear();
        BeginContext(abilityToStart, startCtx);
        abilityToStart.StartAbility();
        EndContext(abilityToStart, startCtx);
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
        if (Abilities.TryGetValue(abilityToStop, out var ctx) && ctx.IsActive.Value)
        {
            BeginContext(abilityToStop, ctx);
            abilityToStop.StopAbility();
            EndContext(abilityToStop, ctx);
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
            BeginContext(kv.Key, kv.Value);
            if (kv.Value.IsActive.Value)
            {
                kv.Key.UpdateAbility(deltaTime);
            }
            else
            {
                kv.Key.InactiveUpdate();
            }
            EndContext(kv.Key, kv.Value);
        }
    }

    // ── Context Bind / Save ───────────────────────────────────────────────────

    /// <summary>
    /// 执行前调用：注入角色上下文（直接字段）+ EP 重定向 + 恢复节点状态。
    /// Unity 单线程保证各角色调用顺序执行，此模式可安全隔离共享 SO 的运行态。
    /// </summary>
    void BeginContext(AnimancerAbility ability, AbilityContext ctx)
    {
        ability.SetContextAnimancerComponent(ctx.AnimancerComponent, ctx.Character, ctx.SkillController);
        ability.SetContextAgent(this);
        ability.SetContextActiveEP(ctx.IsActive);
        ability.SetContextDurationEP(ctx.Duration);
        ability.SetContextEPMap(ctx.EPMap);
        ability.RestoreNodeStates(ctx.NodeStateMap);
    }

    /// <summary>
    /// 执行后调用：将当前节点状态保存回上下文，清除 EP 重定向。
    /// </summary>
    void EndContext(AnimancerAbility ability, AbilityContext ctx)
    {
        ability.SaveNodeStates(ctx.NodeStateMap);
        ability.SetContextEPMap(null);
    }

    // ── EP Clone Helper ───────────────────────────────────────────────────────

    /// <summary>
    /// 克隆单个 EP：创建同类型新实例并复制 Name / GUID / Value。
    /// 对值类型 T（bool, float, int, Vector3 等）等效深拷贝。
    /// </summary>
    static BaseExposedProperty CloneEP(BaseExposedProperty source)
    {
        var clone = (BaseExposedProperty)Activator.CreateInstance(source.GetType());
        clone.Name = source.Name;
        clone.GUID = source.GUID;
        clone.SetValue(source.GetValue());
        return clone;
    }
}

/// <summary>
/// AnimancerAbilityAgent 的所有者接口
/// </summary>
public interface IAnimancerAbilityAgentOwner
{
    AnimancerAbilityAgent AnimancerAbilityAgent { get; set; }
}
