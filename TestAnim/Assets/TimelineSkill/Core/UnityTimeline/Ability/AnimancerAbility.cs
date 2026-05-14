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

    // ── 上下文数据通过 GetExposedProperty 从 Blackboard EPMap 读取 ──

    /// <summary>运行时 Agent 引用，从 Blackboard EPMap 获取</summary>
    public AnimancerAbilityAgent Runner
        => GetExposedProperty<AnimancerAbilityAgentExposedProperty>("Agent")?.Value;

    /// <summary>运行时 AnimancerComponent 引用，从 Blackboard EPMap 获取</summary>
    public AnimancerComponent AnimancerComponent
        => GetExposedProperty<AnimancerComponentExposedProperty>("AnimancerComponent")?.Value;

    /// <summary>运行时 Character 组件引用，从 Blackboard EPMap 获取</summary>
    public Character Character
        => GetExposedProperty<CharacterExposedProperty>("Character")?.Value;

    /// <summary>运行时 SkillCharacterController 引用，从 Blackboard EPMap 获取</summary>
    public SkillCharacterController SkillCharacterController
        => GetExposedProperty<SkillCharacterControllerExposedProperty>("SkillController")?.Value;

    /// <summary>该技能是否激活</summary>
    public bool Active
    {
        get
        {
            var ep = GetExposedProperty<BoolExposedProperty>("Active");
            return ep != null && ep.Value;
        }
    }

    /// <summary>该技能已运行的时长</summary>
    public float Duration
    {
        get
        {
            var ep = GetExposedProperty<FloatExposedProperty>("Duration");
            return ep?.Value ?? 0f;
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
    }

    public override void DisposeTree()
    {
        base.DisposeTree();
        m_OnStart = null;
        m_OnStop  = null;
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
        return true;
    }

    public virtual void StartAbility()
    {
        var activeEP = GetExposedProperty<BoolExposedProperty>("Active");
        var durationEP = GetExposedProperty<FloatExposedProperty>("Duration");
        if (activeEP != null) activeEP.Value = true;
        if (durationEP != null) durationEP.Value = 0;
        ResetTree();
        OnStartAbility();
    }

    public virtual void StopAbility()
    {
        var activeEP = GetExposedProperty<BoolExposedProperty>("Active");
        if (activeEP != null) activeEP.Value = false;
        OnStopAbility();
        OnStop();
    }

    public virtual void UpdateAbility(float deltaTime)
    {
        var durationEP = GetExposedProperty<FloatExposedProperty>("Duration");
        if (durationEP != null) durationEP.Value += deltaTime;
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
            Runner?.ActiveTags.Add(tag);
        }
        m_OnStart?.UpdateNode();
    }

    protected virtual void OnStopAbility()
    {
        foreach (var tag in ActiveTags.Tags)
        {
            Runner?.ActiveTags.Remove(tag);
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
