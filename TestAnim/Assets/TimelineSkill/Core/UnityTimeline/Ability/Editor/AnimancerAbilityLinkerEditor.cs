using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimancerAbilityLinker))]
public class AnimancerAbilityLinkerEditor : Editor
{
    SerializedProperty m_AbilityCategoriesProp;
    SerializedProperty m_DefaultAbilityProp;
    SerializedProperty m_InputBindingsProp;
    SerializedProperty m_CinemachineInputProviderProp;

    private Dictionary<int, bool> m_FoldoutStates = new Dictionary<int, bool>();

    private static readonly string kHoldInteractionGuide =
        "如需「持续按住 N 秒后才触发」，请在 Input Action Asset 中为对应 Action 添加 Hold Interaction：\n\n" +
        "1. 双击打开 .inputactions 文件\n" +
        "2. 选中要配置的 Action（如 Fire）\n" +
        "3. 在右侧 Properties 面板点击 Interactions 旁的 +\n" +
        "4. 选择 Hold\n" +
        "5. 设置 Hold Time（秒），如 0.5 表示按住 0.5 秒后触发\n" +
        "6. 保存 Input Action Asset\n\n" +
        "配合 Trigger Mode 设为 OnPerformed，按住达到 Hold Time 后会触发 performed 回调，从而启动绑定的 Ability。\n\n" +
        "提示：如果 Hold Time 保持默认值 0，则使用 Input System 全局默认值（Project Settings > Input System > Default Hold Time）。";

    void OnEnable()
    {
        m_AbilityCategoriesProp = serializedObject.FindProperty("m_AbilityCategories");
        m_DefaultAbilityProp = serializedObject.FindProperty("m_DefaultAbility");
        m_InputBindingsProp = serializedObject.FindProperty("m_InputBindings");
        m_CinemachineInputProviderProp = serializedObject.FindProperty("m_CinemachineInputProvider");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制分组 Ability 列表
        DrawAbilityCategories();

        EditorGUILayout.Space(4);

        // 收集所有分组中的有效 Ability
        var validAbilities = CollectAllValidAbilities();

        // 运行时不显示配置部分
        if (!Application.isPlaying)
        {
            // ── Default Ability 下拉 ──
            DrawDefaultAbilityPopup(validAbilities);

            EditorGUILayout.Space(8);

            // ── Input Bindings ──
            DrawInputBindings(validAbilities);

            EditorGUILayout.Space(8);

            // ── Cinemachine ──
            EditorGUILayout.PropertyField(m_CinemachineInputProviderProp,
                new GUIContent("Cinemachine Input Provider", "拖入场景中的 CinemachineInputProvider，用于 CinemachineCamera 锁定时禁用相机输入"));
        }

        serializedObject.ApplyModifiedProperties();
    }

    #region Ability Categories

    private void DrawAbilityCategories()
    {
        EditorGUILayout.LabelField("Ability Categories", EditorStyles.boldLabel);
        bool isPlaying = Application.isPlaying;

        for (int i = 0; i < m_AbilityCategoriesProp.arraySize; i++)
        {
            var categoryProp = m_AbilityCategoriesProp.GetArrayElementAtIndex(i);
            var categoryNameProp = categoryProp.FindPropertyRelative("CategoryName");
            var abilitiesProp = categoryProp.FindPropertyRelative("Abilities");

            string categoryName = categoryNameProp.stringValue;
            if (string.IsNullOrEmpty(categoryName))
                categoryName = "(未命名)";

            // Foldout 状态
            if (!m_FoldoutStates.ContainsKey(i))
                m_FoldoutStates[i] = true;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // 标题行
                EditorGUILayout.BeginHorizontal();
                {
                    m_FoldoutStates[i] = EditorGUILayout.Foldout(m_FoldoutStates[i], categoryName, true, EditorStyles.foldoutHeader);

                    GUILayout.FlexibleSpace();

                    // 重命名按钮（仅编辑模式可见）
                    if (!isPlaying)
                    {
                        if (GUILayout.Button("✎", GUILayout.Width(24), GUILayout.Height(18)))
                        {
                            ShowRenameCategoryDialog(i, categoryNameProp);
                        }

                        // 删除按钮
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

                    // 绘制分组内的 Ability 列表
                    for (int j = 0; j < abilitiesProp.arraySize; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            var abilityElem = abilitiesProp.GetArrayElementAtIndex(j);
                            EditorGUILayout.PropertyField(abilityElem, GUIContent.none);

                            // 删除单个 Ability（仅编辑模式可见）
                            if (!isPlaying && GUILayout.Button("−", GUILayout.Width(20), GUILayout.Height(18)))
                            {
                                // 如果引用非空，先清空再删除（Unity 序列化的特殊处理）
                                if (abilityElem.objectReferenceValue != null)
                                    abilityElem.objectReferenceValue = null;
                                abilitiesProp.DeleteArrayElementAtIndex(j);
                                break;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    // 添加 Ability 按钮（仅编辑模式可见）
                    if (!isPlaying && GUILayout.Button("+ Add Ability", EditorStyles.miniButton))
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

        // 添加分组按钮（仅编辑模式可见）
        EditorGUILayout.Space(4);
        if (!isPlaying && GUILayout.Button("+ Add Category"))
        {
            ShowAddCategoryDialog();
        }
    }

    private void ShowAddCategoryDialog()
    {
        CategoryNameInputWindow.Show("新建分组", "", (newName) =>
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
        CategoryNameInputWindow.Show("重命名分组", oldName, (newName) =>
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

    private List<AnimancerAbility> CollectAllValidAbilities()
    {
        var validAbilities = new List<AnimancerAbility>();
        for (int i = 0; i < m_AbilityCategoriesProp.arraySize; i++)
        {
            var categoryProp = m_AbilityCategoriesProp.GetArrayElementAtIndex(i);
            var abilitiesProp = categoryProp.FindPropertyRelative("Abilities");
            for (int j = 0; j < abilitiesProp.arraySize; j++)
            {
                var elem = abilitiesProp.GetArrayElementAtIndex(j).objectReferenceValue as AnimancerAbility;
                if (elem != null)
                    validAbilities.Add(elem);
            }
        }
        return validAbilities;
    }

    #endregion

    #region Default Ability

    private void DrawDefaultAbilityPopup(List<AnimancerAbility> validAbilities)
    {
        var current = m_DefaultAbilityProp.objectReferenceValue as AnimancerAbility;
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

    #region Input Bindings

    private void DrawInputBindings(List<AnimancerAbility> validAbilities)
    {
        EditorGUILayout.LabelField("Input Bindings", EditorStyles.boldLabel);

        var abilityDisplayNames = new string[] { "None" }.Concat(validAbilities.Select(a => a.name)).ToArray();

        for (int i = 0; i < m_InputBindingsProp.arraySize; i++)
        {
            var element = m_InputBindingsProp.GetArrayElementAtIndex(i);
            var inputActionProp = element.FindPropertyRelative("InputAction");
            var abilityProp = element.FindPropertyRelative("Ability");
            var triggerModeProp = element.FindPropertyRelative("TriggerMode");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Binding [{i}]", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("\u00d7", GUILayout.Width(20)))
                {
                    m_InputBindingsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(inputActionProp, new GUIContent("Input Action"));

                var currentAbility = abilityProp.objectReferenceValue as AnimancerAbility;
                if (currentAbility != null && !validAbilities.Contains(currentAbility))
                {
                    abilityProp.objectReferenceValue = null;
                    currentAbility = null;
                }

                int abilityIndex = currentAbility == null ? 0 : validAbilities.IndexOf(currentAbility) + 1;
                using (new EditorGUI.DisabledScope(validAbilities.Count == 0))
                {
                    int newIndex = EditorGUILayout.Popup("Ability", abilityIndex, abilityDisplayNames);
                    abilityProp.objectReferenceValue = newIndex == 0 ? null : validAbilities[newIndex - 1];
                }

                EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("Trigger Mode"));

                if (triggerModeProp.enumValueIndex == (int)AnimancerAbilityLinker.InputTriggerMode.OnPerformed)
                {
                    DrawHoldDurationHint();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        if (GUILayout.Button("+ Add Input Binding"))
        {
            m_InputBindingsProp.InsertArrayElementAtIndex(m_InputBindingsProp.arraySize);
            var newElement = m_InputBindingsProp.GetArrayElementAtIndex(m_InputBindingsProp.arraySize - 1);
            newElement.FindPropertyRelative("InputAction").objectReferenceValue = null;
            newElement.FindPropertyRelative("Ability").objectReferenceValue = null;
            newElement.FindPropertyRelative("TriggerMode").enumValueIndex = 0;
        }
    }

    private void DrawHoldDurationHint()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField(
                new GUIContent("持续多久触发", "需要在 Input Action Asset 中配置 Hold Interaction 来设置持续按住时长"),
                EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("?", "点击查看 Hold Interaction 配置流程"),
                EditorStyles.miniButton, GUILayout.Width(20)))
            {
                EditorUtility.DisplayDialog(
                    "如何设置「持续按住触发」",
                    kHoldInteractionGuide,
                    "知道了");
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion
}

/// <summary>
/// 分组名输入弹窗
/// </summary>
public class CategoryNameInputWindow : EditorWindow
{
    private string m_InputName = "";
    private System.Action<string> m_OnConfirm;
    private bool m_Focused = false;

    public static void Show(string title, string defaultName, System.Action<string> onConfirm)
    {
        var window = GetWindow<CategoryNameInputWindow>(true, title, true);
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

        GUI.SetNextControlName("CategoryNameField");
        m_InputName = EditorGUILayout.TextField("分组名", m_InputName);

        if (!m_Focused)
        {
            EditorGUI.FocusTextInControl("CategoryNameField");
            m_Focused = true;
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // 回车确认
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
