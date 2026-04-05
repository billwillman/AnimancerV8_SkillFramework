using System;
using UnityEngine;
using TreeDesigner;
using Taco.Gameplay;
using Animancer;
#if UNITY_EDITOR
using UnityEditor;
#endif

[AcceptableNodePaths("Character", "AnimancerAbility")]
public partial class AnimancerAbility : OneRootTree
{
    [ShowInInspector]
    public GameplayTagContainer AbilityTags;
    [ShowInInspector]
    public GameplayTagContainer CancelAbilitiesWithTag;
    [ShowInInspector]
    public GameplayTagContainer BlockAbilitiesWithTag;

    [ShowInInspector]
    public GameplayTagContainer ActiveTags;
    [ShowInInspector]
    public GameplayTagContainer RequiredTags;

    [SerializeField]
    protected string m_OnStartGUID;
    public string OnStartGUID { get => m_OnStartGUID; set => m_OnStartGUID = value; }

    [SerializeField]
    protected string m_OnStopGUID;
    public string OnStopGUID { get => m_OnStopGUID; set => m_OnStopGUID = value; }

    public AnimancerAbilityAgent Runner { get; private set; }
    public AnimancerComponent AnimancerComponent { get; set; }

    protected BoolExposedProperty m_Active;
    public bool Active => m_Active.Value;

    protected FloatExposedProperty m_Duration;
    public float Duration => m_Duration.Value;

    protected EnterNode m_OnStart;
    protected EnterNode m_OnStop;

    [NonSerialized]
    public AnimancerAbilityCanStartNode AnimancerAbilityCanStart;
    [NonSerialized]
    public OnAnimancerAbilityCancelNode OnAnimancerAbilityCancel;

    public override void InitTree(object user)
    {
        Runner = user as AnimancerAbilityAgent;

        base.InitTree(user);
        if (!string.IsNullOrEmpty(m_OnStartGUID))
            m_OnStart = m_GUIDNodeMap[m_OnStartGUID] as EnterNode;
        if (!string.IsNullOrEmpty(m_OnStopGUID))
            m_OnStop = m_GUIDNodeMap[m_OnStopGUID] as EnterNode;

        m_Active = GetExposedProperty<BoolExposedProperty>("Active");
        m_Duration = GetExposedProperty<FloatExposedProperty>("Duration");
    }

    public override void DisposeTree()
    {
        base.DisposeTree();
        m_OnStart = null;
        m_OnStop = null;
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
        CreateInternalExposedProperty(typeof(BoolExposedProperty), "Active", false);
        CreateInternalExposedProperty(typeof(FloatExposedProperty), "Duration", false);
    }
}
#endif
