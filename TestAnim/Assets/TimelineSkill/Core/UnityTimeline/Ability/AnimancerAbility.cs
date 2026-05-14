using System;
using System.Collections.Generic;
using UnityEngine;
using TreeDesigner;
using Taco.Gameplay;
using Animancer;
using EasyCharacterMovement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// SkillCharacterController 定义在 UnityTimeline 命名空间，本文件在全局命名空间需显式引入
using UnityTimeline;

[AcceptableNodePaths("Character", "AnimancerAbility")]
public partial class AnimancerAbility : OneRootTree
{
    [ShowInInspector, Tooltip("该技能自身的标签，用于被其他技能的 Cancel/Block 规则匹配")]
    public GameplayTagContainer AbilityTags;
    [ShowInInspector, Tooltip("激活该技能时，取消所有拥有这些标签的正在运行的技能")]
    public GameplayTagContainer CancelAbilitiesWithTag;
    [ShowInInspector, Tooltip("该技能激活期间，阻止拥有这些标签的技能启动")]
    public GameplayTagContainer BlockAbilitiesWithTag;

    [ShowInInspector, Tooltip("该技能激活期间，向角色添加的临时状态标签（技能结束时自动移除）")]
    public GameplayTagContainer ActiveTags;
    [ShowInInspector, Tooltip("启动该技能的前置条件：角色当前 ActiveTags 中必须包含这些标签")]
    public GameplayTagContainer RequiredTags;

    [SerializeField]
    protected string m_OnStartGUID;
    public string OnStartGUID { get => m_OnStartGUID; set => m_OnStartGUID = value; }

    [SerializeField]
    protected string m_OnStopGUID;
    public string OnStopGUID { get => m_OnStopGUID; set => m_OnStopGUID = value; }

    [NonSerialized]
    private AnimancerAbilityAgent m_Agent;
    /// <summary>
    /// 运行时 Agent 引用，由 Linker 在运行时直接注入
    /// </summary>
    public AnimancerAbilityAgent Runner => m_Agent;

    /// <summary>
    /// 由 Linker / Agent 在运行时调用，直接注入 Agent 引用
    /// </summary>
    public void SetContextAgent(AnimancerAbilityAgent agent) => m_Agent = agent;

    [NonSerialized]
    private AnimancerComponent m_AnimancerComponent;
    /// <summary>
    /// 运行时 AnimancerComponent 引用，由 AnimancerAbilityLinker 直接注入
    /// </summary>
    public AnimancerComponent AnimancerComponent => m_AnimancerComponent;

    /// <summary>
    /// 由 BeginContext 注入 AnimancerComponent 及可选的角色组件引用。
    /// character / skillController 若不传则默认为 null。
    /// </summary>
    public void SetContextAnimancerComponent(AnimancerComponent animancerComponent,
        Character character = null, SkillCharacterController skillController = null)
    {
        m_AnimancerComponent = animancerComponent;
        m_Character          = character;
        m_SkillController    = skillController;
    }

    private Character m_Character;
    public Character Character => m_Character;

    private SkillCharacterController m_SkillController;
    public SkillCharacterController SkillCharacterController => m_SkillController;

    protected BoolExposedProperty m_Active;
    public bool Active => m_Active != null && m_Active.Value;
    /// <summary>由 AnimancerAbilityAgent.BeginContext 注入 per-instance EP</summary>
    public void SetContextActiveEP(BoolExposedProperty ep) => m_Active = ep;

    protected FloatExposedProperty m_Duration;
    public float Duration => m_Duration?.Value ?? 0f;
    /// <summary>由 AnimancerAbilityAgent.BeginContext 注入 per-instance EP</summary>
    public void SetContextDurationEP(FloatExposedProperty ep) => m_Duration = ep;

    /// <summary>
    /// 用户自定义 EP 的 per-instance 重定向表，由 BeginContext 绑定、EndContext 清除。
    /// GetExposedProperty 优先从此表返回，使节点图中的 EP 访问读写 per-instance 副本。
    /// </summary>
    [NonSerialized]
    private Dictionary<string, BaseExposedProperty> m_CurrentEPMap;

    public void SetContextEPMap(Dictionary<string, BaseExposedProperty> map)
        => m_CurrentEPMap = map;

    public new BaseExposedProperty GetExposedProperty(string name)
    {
        if (m_CurrentEPMap != null && m_CurrentEPMap.TryGetValue(name, out var ep))
            return ep;
        return base.GetExposedProperty(name);
    }
    public new T GetExposedProperty<T>(string name) where T : BaseExposedProperty
        => GetExposedProperty(name) as T;

    /// <summary>
    /// 从上下文快照字典恢复所有 RunnableNode 的 m_State 及自定义状态（BeginContext 调用）
    /// </summary>
    public void RestoreNodeStates(Dictionary<string, NodeSnapshot> stateMap)
    {
        foreach (var kv in stateMap)
            if (m_GUIDNodeMap.TryGetValue(kv.Key, out var node) && node is RunnableNode rn)
            {
                rn.State = kv.Value.State;
                if (kv.Value.Custom != null)
                    rn.RestoreContextState(kv.Value.Custom);
            }
    }

    /// <summary>
    /// 将所有 RunnableNode 的 m_State 及自定义状态保存回上下文快照字典（EndContext 调用）
    /// </summary>
    public void SaveNodeStates(Dictionary<string, NodeSnapshot> stateMap)
    {
        foreach (var kv in m_GUIDNodeMap)
        {
            if (kv.Value is not RunnableNode rn) continue;

            if (!stateMap.TryGetValue(kv.Key, out var snap))
                snap = stateMap[kv.Key] = new NodeSnapshot();

            snap.State = rn.State;

            if (snap.Custom == null) snap.Custom = new Dictionary<string, object>();
            else snap.Custom.Clear();

            rn.SaveContextState(snap.Custom);

            // 无自定义字段则释放字典，避免持续占用内存
            if (snap.Custom.Count == 0) snap.Custom = null;
        }
    }

    protected EnterNode m_OnStart;
    protected EnterNode m_OnStop;

    [NonSerialized]
    public AnimancerAbilityCanStartNode AnimancerAbilityCanStart;
    [NonSerialized]
    public OnAnimancerAbilityCancelNode OnAnimancerAbilityCancel;

    public override void InitTree(object user)
    {
        base.InitTree(user);
        if (!string.IsNullOrEmpty(m_OnStartGUID))
            m_OnStart = m_GUIDNodeMap[m_OnStartGUID] as EnterNode;
        if (!string.IsNullOrEmpty(m_OnStopGUID))
            m_OnStop = m_GUIDNodeMap[m_OnStopGUID] as EnterNode;

        m_Active   = GetExposedProperty<BoolExposedProperty>("Active");
        m_Duration = GetExposedProperty<FloatExposedProperty>("Duration");
    }

    public override void DisposeTree()
    {
        base.DisposeTree();
        m_OnStart            = null;
        m_OnStop             = null;
        m_Agent              = null;
        m_AnimancerComponent = null;
        m_Character          = null;
        m_SkillController    = null;
    }

    public override void OnReset()
    {
        base.OnReset();
        m_OnStart?.ResetNode();
        m_OnStop?.ResetNode();
    }

    public override State OnUpdate()
    {
        m_Root.DeltaTime = DeltaTime;
        m_Root.UpdateNode();
        return State.Running;
    }

    public virtual bool CanStart()
    {
        if (AnimancerAbilityCanStart != null)
            return AnimancerAbilityCanStart.GetValue();
        else
            return true;
    }

    public virtual void StartAbility()
    {
        m_Active.Value = true;
        m_Duration.Value = 0;
        ResetTree();
        OnStartAbility();
    }

    public virtual void StopAbility()
    {
        m_Active.Value = false;
        OnStopAbility();
        OnStop();
    }

    public virtual void UpdateAbility(float deltaTime)
    {
        m_Duration.Value += deltaTime;
        UpdateTree(deltaTime);
    }

    public virtual void InactiveUpdate() { }

    public virtual void CancelAbility(AnimancerAbility abilityCancelBy)
    {
        OnAnimancerAbilityCancel?.Trigger(abilityCancelBy);
    }

    protected virtual void OnStartAbility()
    {
        foreach (var tag in ActiveTags.Tags)
        {
            Runner.ActiveTags.Add(tag);
        }
        m_OnStart?.UpdateNode();
    }

    protected virtual void OnStopAbility()
    {
        foreach (var tag in ActiveTags.Tags)
        {
            Runner.ActiveTags.Remove(tag);
        }
        m_OnStop?.UpdateNode();
    }
}

#if UNITY_EDITOR
public partial class AnimancerAbility
{
    public override bool CheckInit()
    {
        bool dirty = base.CheckInit();
        if (!string.IsNullOrEmpty(m_OnStartGUID))
            m_OnStart = m_GUIDNodeMap[m_OnStartGUID] as EnterNode;
        if (!string.IsNullOrEmpty(m_OnStopGUID))
            m_OnStop = m_GUIDNodeMap[m_OnStopGUID] as EnterNode;

        return dirty;
    }

    [MenuItem("Assets/Create/AnimancerSkillSystem/AnimancerAbility")]
    public static void CreateAnimancerAbility()
    {
        AnimancerAbility tree = CreateInstance<AnimancerAbility>();
        tree.RootGUID = tree.CreateNode(typeof(RootNode)).GUID;

        var OnEnable = tree.CreateNode(typeof(EnterNode)) as EnterNode;
        OnEnable.NodeName = "OnStart";
        OnEnable.Position = new Vector2(0, 200);
        tree.OnStartGUID = OnEnable.GUID;

        var OnDisable = tree.CreateNode(typeof(EnterNode)) as EnterNode;
        OnDisable.NodeName = "OnStop";
        OnDisable.Position = new Vector2(0, 400);
        tree.OnStopGUID = OnDisable.GUID;

        tree.CreateInternalExposedProperties();

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New AnimancerAbility.asset");
        AssetDatabase.CreateAsset(tree, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = tree;
    }

    public virtual void CreateInternalExposedProperties()
    {
        CreateInternalExposedProperty(typeof(BoolExposedProperty),  "Active",   false);
        CreateInternalExposedProperty(typeof(FloatExposedProperty), "Duration", false);
    }
}
#endif
