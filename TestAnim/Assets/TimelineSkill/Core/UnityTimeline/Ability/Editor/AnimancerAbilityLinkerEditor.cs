using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimancerAbilityLinker))]
public class AnimancerAbilityLinkerEditor : Editor
{
    SerializedProperty m_AbilitiesProp;
    SerializedProperty m_DefaultAbilityProp;
    SerializedProperty m_InputBindingsProp;

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
        m_AbilitiesProp = serializedObject.FindProperty("m_Abilities");
        m_DefaultAbilityProp = serializedObject.FindProperty("m_DefaultAbility");
        m_InputBindingsProp = serializedObject.FindProperty("m_InputBindings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制 m_Abilities 列表
        EditorGUILayout.PropertyField(m_AbilitiesProp, true);

        EditorGUILayout.Space(4);

        // 收集当前 m_Abilities 中所有非空项
        var validAbilities = new List<AnimancerAbility>();
        for (int i = 0; i < m_AbilitiesProp.arraySize; i++)
        {
            var elem = m_AbilitiesProp.GetArrayElementAtIndex(i).objectReferenceValue as AnimancerAbility;
            if (elem != null)
                validAbilities.Add(elem);
        }

        // 运行时不显示配置部分
        if (!Application.isPlaying)
        {
            // ── Default Ability 下拉 ──
            DrawDefaultAbilityPopup(validAbilities);

            EditorGUILayout.Space(8);

            // ── Input Bindings ──
            DrawInputBindings(validAbilities);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDefaultAbilityPopup(List<AnimancerAbility> validAbilities)
    {
        // 若 DefaultAbility 已不在列表中，自动置空
        var current = m_DefaultAbilityProp.objectReferenceValue as AnimancerAbility;
        if (current != null && !validAbilities.Contains(current))
        {
            m_DefaultAbilityProp.objectReferenceValue = null;
            current = null;
        }

        // 绘制 DefaultAbility 下拉菜单，只允许从 m_Abilities 中选择
        var displayNames = new string[] { "None" }.Concat(validAbilities.Select(a => a.name)).ToArray();
        int currentIndex = current == null ? 0 : validAbilities.IndexOf(current) + 1;

        using (new EditorGUI.DisabledScope(validAbilities.Count == 0))
        {
            int selectedIndex = EditorGUILayout.Popup("Default Ability", currentIndex, displayNames);
            m_DefaultAbilityProp.objectReferenceValue = selectedIndex == 0 ? null : validAbilities[selectedIndex - 1];
        }
    }

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

                // InputAction 引用（使用标准 PropertyField，支持从 Input Action Asset 拖入）
                EditorGUILayout.PropertyField(inputActionProp, new GUIContent("Input Action"));

                // Ability 下拉菜单（只允许从 m_Abilities 中选择）
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

                // 触发模式
                EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("Trigger Mode"));

                // 当 TriggerMode 为 OnPerformed 时，显示 Hold 提示
                if (triggerModeProp.enumValueIndex == (int)AnimancerAbilityLinker.InputTriggerMode.OnPerformed)
                {
                    DrawHoldDurationHint();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // 添加按钮
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
}
