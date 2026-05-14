using System;
using UnityEngine;
using Taco.Gameplay;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Visual Scripting 版本的 Ability 数据资产
/// 持有一个 ScriptGraphAsset 引用，以及与 AnimancerAbility 相同的 GameplayTag 字段
/// 用于与 AnimancerAbilityAgent 的 Tag 系统互通
/// </summary>
public class VisualScriptingAbility : ScriptableObject
{
    [Tooltip("Visual Scripting 的 Script Graph 资产，包含 OnEnter/OnExit/OnUpdate 自定义事件节点")]
    public ScriptGraphAsset ScriptGraph;

    [Header("Gameplay Tags")]

    [Tooltip("该 Ability 自身的标签，用于被其他技能的 Cancel/Block 规则匹配")]
    public GameplayTagContainer AbilityTags;

    [Tooltip("激活该 Ability 时，取消所有拥有这些标签的正在运行的技能")]
    public GameplayTagContainer CancelAbilitiesWithTag;

    [Tooltip("该 Ability 激活期间，阻止拥有这些标签的技能启动")]
    public GameplayTagContainer BlockAbilitiesWithTag;

    [Tooltip("该 Ability 激活期间，向角色添加的临时状态标签（结束时自动移除）")]
    public GameplayTagContainer ActiveTags;

    [Tooltip("启动该 Ability 的前置条件：角色当前 ActiveTags 中必须包含这些标签")]
    public GameplayTagContainer RequiredTags;

    /// <summary>
    /// 初始化 Tag 容器（运行时调用）
    /// </summary>
    public void InitTags()
    {
        AbilityTags?.Init();
        CancelAbilitiesWithTag?.Init();
        BlockAbilitiesWithTag?.Init();
        ActiveTags?.Init();
        RequiredTags?.Init();
    }

#if UNITY_EDITOR
    [MenuItem("Assets/Create/AnimancerSkillSystem/VisualScriptingAbility")]
    public static void CreateVisualScriptingAbility()
    {
        // 创建 VisualScriptingAbility 资产
        var ability = CreateInstance<VisualScriptingAbility>();

        // 创建配套的 ScriptGraphAsset，预置 OnEnter/OnExit/OnUpdate 自定义事件节点
        var graphAsset = CreateInstance<ScriptGraphAsset>();
        var graph = graphAsset.graph;

        // 创建三个 CustomEvent 节点并设置名称和位置
        var onEnterEvent = new CustomEvent();
        onEnterEvent.defaultValues["name"] = "OnEnter";
        onEnterEvent.position = new Vector2(-200, 0);
        graph.units.Add(onEnterEvent);

        var onExitEvent = new CustomEvent();
        onExitEvent.defaultValues["name"] = "OnExit";
        onExitEvent.position = new Vector2(-200, 250);
        graph.units.Add(onExitEvent);

        var onUpdateEvent = new CustomEvent();
        onUpdateEvent.defaultValues["name"] = "OnUpdate";
        onUpdateEvent.position = new Vector2(-200, 500);
        graph.units.Add(onUpdateEvent);

        // 确定保存路径
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
            path = "Assets";
        else if (System.IO.Path.GetExtension(path) != "")
            path = System.IO.Path.GetDirectoryName(path);

        // 保存 ScriptGraphAsset
        string graphPath = AssetDatabase.GenerateUniqueAssetPath(path + "/New VSAbility Graph.asset");
        AssetDatabase.CreateAsset(graphAsset, graphPath);

        // 关联图到 ability
        ability.ScriptGraph = graphAsset;

        // 保存 VisualScriptingAbility
        string abilityPath = AssetDatabase.GenerateUniqueAssetPath(path + "/New VisualScriptingAbility.asset");
        AssetDatabase.CreateAsset(ability, abilityPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = ability;
    }
#endif
}
