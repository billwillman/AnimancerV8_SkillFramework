using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimancerVisualScriptingLinker))]
public class AnimancerVisualScriptingLinkerEditor : Editor
{
    SerializedProperty m_AbilityCategoriesProp;
    SerializedProperty m_DefaultAbilityProp;

    private Dictionary<int, bool> m_FoldoutStates = new Dictionary<int, bool>();

    void OnEnable()
    {
        m_AbilityCategoriesProp = serializedObject.FindProperty("m_AbilityCategories");
        m_DefaultAbilityProp = serializedObject.FindProperty("m_DefaultAbility");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── VS 节点提示 ──
        DrawVSNodeHelpBox();

        // ── 分组列表 ──
        DrawAbilityCategories();

        EditorGUILayout.Space(4);

        // 收集所有有效 VS Ability
        var validAbilities = CollectAllValidAbilities();

        // 运行时不显示配置部分
        if (!Application.isPlaying)
        {
            // ── Default Ability 下拉 ──
            DrawDefaultAbilityPopup(validAbilities);
        }

        // ── 运行时状态 ──
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(8);
            DrawRuntimeStatus();
        }

        serializedObject.ApplyModifiedProperties();
    }

    #region Ability Categories

    private void DrawAbilityCategories()
    {
        EditorGUILayout.LabelField("VS Ability Categories", EditorStyles.boldLabel);
        bool isPlaying = Application.isPlaying;

        for (int i = 0; i < m_AbilityCategoriesProp.arraySize; i++)
        {
            var categoryProp = m_AbilityCategoriesProp.GetArrayElementAtIndex(i);
            var categoryNameProp = categoryProp.FindPropertyRelative("CategoryName");
            var abilitiesProp = categoryProp.FindPropertyRelative("Abilities");

            string categoryName = categoryNameProp.stringValue;
            if (string.IsNullOrEmpty(categoryName))
                categoryName = "(未命名)";

            if (!m_FoldoutStates.ContainsKey(i))
                m_FoldoutStates[i] = true;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // 标题行
                EditorGUILayout.BeginHorizontal();
                {
                    m_FoldoutStates[i] = EditorGUILayout.Foldout(m_FoldoutStates[i], categoryName, true, EditorStyles.foldoutHeader);
                    GUILayout.FlexibleSpace();

                    if (!isPlaying)
                    {
                        // 重命名
                        if (GUILayout.Button("✎", GUILayout.Width(24), GUILayout.Height(18)))
                        {
                            ShowRenameCategoryDialog(i, categoryNameProp);
                        }
                        // 删除
                        if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
                        {
                            if (EditorUtility.DisplayDialog("删除分组",
                                $"确定要删除分组 \"{categoryName}\" 及其中所有 Ability 引用吗？", "删除", "取消"))
                            {
                                m_AbilityCategoriesProp.DeleteArrayElementAtIndex(i);
                                m_FoldoutStates.Clear();
                                break;
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                // 展开内容
                if (m_FoldoutStates.ContainsKey(i) && m_FoldoutStates[i])
                {
                    EditorGUI.indentLevel++;

                    for (int j = 0; j < abilitiesProp.arraySize; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            var abilityElem = abilitiesProp.GetArrayElementAtIndex(j);
                            using (new EditorGUI.DisabledScope(isPlaying))
                                EditorGUILayout.PropertyField(abilityElem, GUIContent.none);

                            if (!isPlaying && GUILayout.Button("−", GUILayout.Width(20), GUILayout.Height(18)))
                            {
                                if (abilityElem.objectReferenceValue != null)
                                    abilityElem.objectReferenceValue = null;
                                abilitiesProp.DeleteArrayElementAtIndex(j);
                                break;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (!isPlaying && GUILayout.Button("+ Add VS Ability", EditorStyles.miniButton))
                    {
                        abilitiesProp.InsertArrayElementAtIndex(abilitiesProp.arraySize);
                        var newElem = abilitiesProp.GetArrayElementAtIndex(abilitiesProp.arraySize - 1);
                        newElem.objectReferenceValue = null;
                    }

                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // 添加分组
        EditorGUILayout.Space(4);
        if (!Application.isPlaying && GUILayout.Button("+ Add Category"))
        {
            ShowAddCategoryDialog();
        }
    }

    private void ShowAddCategoryDialog()
    {
        VSCategoryNameInputWindow.Show("新建分组", "", (newName) =>
        {
            if (IsCategoryNameDuplicate(newName))
            {
                EditorUtility.DisplayDialog("错误", $"分组名 \"{newName}\" 已存在，请使用唯一的名称。", "确定");
                return;
            }

            serializedObject.Update();
            int newIndex = m_AbilityCategoriesProp.arraySize;
            m_AbilityCategoriesProp.InsertArrayElementAtIndex(newIndex);
            var newCategory = m_AbilityCategoriesProp.GetArrayElementAtIndex(newIndex);
            newCategory.FindPropertyRelative("CategoryName").stringValue = newName;
            newCategory.FindPropertyRelative("Abilities").ClearArray();
            serializedObject.ApplyModifiedProperties();
        });
    }

    private void ShowRenameCategoryDialog(int index, SerializedProperty categoryNameProp)
    {
        string oldName = categoryNameProp.stringValue;
        VSCategoryNameInputWindow.Show("重命名分组", oldName, (newName) =>
        {
            if (newName == oldName) return;
            if (IsCategoryNameDuplicate(newName))
            {
                EditorUtility.DisplayDialog("错误", $"分组名 \"{newName}\" 已存在，请使用唯一的名称。", "确定");
                return;
            }

            serializedObject.Update();
            var prop = m_AbilityCategoriesProp.GetArrayElementAtIndex(index);
            prop.FindPropertyRelative("CategoryName").stringValue = newName;
            serializedObject.ApplyModifiedProperties();
        });
    }

    private bool IsCategoryNameDuplicate(string name)
    {
        for (int i = 0; i < m_AbilityCategoriesProp.arraySize; i++)
        {
            var categoryProp = m_AbilityCategoriesProp.GetArrayElementAtIndex(i);
            if (categoryProp.FindPropertyRelative("CategoryName").stringValue == name)
                return true;
        }
        return false;
    }

    #endregion

    #region Collect Abilities

    private List<VisualScriptingAbility> CollectAllValidAbilities()
    {
        var validAbilities = new List<VisualScriptingAbility>();
        for (int i = 0; i < m_AbilityCategoriesProp.arraySize; i++)
        {
            var categoryProp = m_AbilityCategoriesProp.GetArrayElementAtIndex(i);
            var abilitiesProp = categoryProp.FindPropertyRelative("Abilities");
            for (int j = 0; j < abilitiesProp.arraySize; j++)
            {
                var elem = abilitiesProp.GetArrayElementAtIndex(j).objectReferenceValue as VisualScriptingAbility;
                if (elem != null)
                    validAbilities.Add(elem);
            }
        }
        return validAbilities;
    }

    #endregion

    #region Default Ability

    private void DrawDefaultAbilityPopup(List<VisualScriptingAbility> validAbilities)
    {
        var current = m_DefaultAbilityProp.objectReferenceValue as VisualScriptingAbility;
        if (current != null && !validAbilities.Contains(current))
        {
            m_DefaultAbilityProp.objectReferenceValue = null;
            current = null;
        }

        var displayNames = new string[] { "None" }.Concat(validAbilities.Select(a => a.name)).ToArray();
        int currentIndex = current == null ? 0 : validAbilities.IndexOf(current) + 1;

        using (new EditorGUI.DisabledScope(validAbilities.Count == 0))
        {
            int selectedIndex = EditorGUILayout.Popup("Default Ability", currentIndex, displayNames);
            m_DefaultAbilityProp.objectReferenceValue = selectedIndex == 0 ? null : validAbilities[selectedIndex - 1];
        }
    }

    #endregion

    #region Runtime Status

    private void DrawRuntimeStatus()
    {
        EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

        var linker = target as AnimancerVisualScriptingLinker;
        if (linker == null) return;

        var allAbilities = linker.GetAllAbilities();
        if (allAbilities.Count == 0)
        {
            EditorGUILayout.HelpBox("No VS Abilities registered.", MessageType.Info);
            return;
        }

        foreach (var ability in allAbilities)
        {
            if (ability == null) continue;
            bool isActive = linker.IsActive(ability.name);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                // 状态指示灯
                var originalColor = GUI.color;
                GUI.color = isActive ? Color.green : Color.gray;
                GUILayout.Label("●", GUILayout.Width(16));
                GUI.color = originalColor;

                EditorGUILayout.LabelField(ability.name, isActive ? EditorStyles.boldLabel : EditorStyles.label);

                // 运行时手动触发按钮
                if (isActive)
                {
                    if (GUILayout.Button("Exit", EditorStyles.miniButton, GUILayout.Width(40)))
                        linker.TriggerOnExit(ability.name);
                }
                else
                {
                    if (GUILayout.Button("Enter", EditorStyles.miniButton, GUILayout.Width(40)))
                        linker.TriggerOnEnter(ability.name);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        Repaint();
    }

    #endregion

    #region VS Node Help

    private void DrawVSNodeHelpBox()
    {
        EditorGUILayout.HelpBox(
            "若在 Script Graph 右键菜单中找不到 AnimancerLinkNodes 节点，请执行：\n" +
            "Edit → Project Settings → Visual Scripting → Regenerate Nodes\n" +
            "或点击下方按钮快速重建节点数据库。",
            MessageType.Info);

        if (GUILayout.Button("Regenerate VS Nodes"))
        {
            Unity.VisualScripting.UnitBase.Rebuild();
            EditorUtility.DisplayDialog("完成", "节点数据库已重新生成。\n现在可以在 Graph Editor 右键菜单 AnimancerLinkNodes 目录下找到所有节点。", "OK");
        }

        EditorGUILayout.Space(4);
    }

    #endregion
}

/// <summary>
/// VS 分组名输入弹窗
/// </summary>
public class VSCategoryNameInputWindow : EditorWindow
{
    private string m_InputName = "";
    private System.Action<string> m_OnConfirm;
    private bool m_Focused = false;

    public static void Show(string title, string defaultName, System.Action<string> onConfirm)
    {
        var window = GetWindow<VSCategoryNameInputWindow>(true, title, true);
        window.m_InputName = defaultName;
        window.m_OnConfirm = onConfirm;
        window.m_Focused = false;
        window.minSize = new Vector2(300, 80);
        window.maxSize = new Vector2(400, 80);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        GUI.SetNextControlName("VSCategoryNameField");
        m_InputName = EditorGUILayout.TextField("分组名", m_InputName);

        if (!m_Focused)
        {
            EditorGUI.FocusTextInControl("VSCategoryNameField");
            m_Focused = true;
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        bool enterPressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;

        if (GUILayout.Button("确定", GUILayout.Width(60)) || enterPressed)
        {
            if (string.IsNullOrWhiteSpace(m_InputName))
            {
                EditorUtility.DisplayDialog("错误", "分组名不能为空。", "确定");
            }
            else
            {
                m_OnConfirm?.Invoke(m_InputName.Trim());
                Close();
            }
        }

        if (GUILayout.Button("取消", GUILayout.Width(60)))
        {
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }
}
