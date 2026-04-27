using System;
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

    /// <summary>
    /// 缓存的 ECM2 Character 组件，首次访问时从 User 上 GetComponent 并缓存
    /// </summary>
    private Character m_Character;
    public Character Character
    {
        get
        {
            if (m_Character == null)
            {
                var userComponent = User as UnityEngine.Component;
                if (userComponent != null)
                    m_Character = userComponent.GetComponent<Character>();
            }
            return m_Character;
        }
    }

    /// <summary>
    /// 缓存的 SkillCharacterController，首次访问时从 User 上 GetComponent 并缓存
    /// </summary>
    private SkillCharacterController m_SkillController;
    public SkillCharacterController SkillCharacterController
    {
        get
        {
            if (m_SkillController == null)
            {
                var userComponent = this.AnimancerComponent;
                if (userComponent != null)
                    m_SkillController = userComponent.GetComponent<SkillCharacterController>();
            }
            return m_SkillController;
        }
    }

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
        m_Character = null;
        m_SkillController = null;
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
        // [MODIFY] 幂等保护：已在运行则先正确停止，确保 RunnableTree 状态(m_Running/m_State)归位
        if (m_Active.Value)
        {
            OnStop();
        }
        // [/MODIFY]

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
