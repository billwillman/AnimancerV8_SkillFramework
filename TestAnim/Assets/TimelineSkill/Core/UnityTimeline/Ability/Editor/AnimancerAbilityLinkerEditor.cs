using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimancerAbilityLinker))]
public class AnimancerAbilityLinkerEditor : Editor
{
    SerializedProperty m_AbilitiesProp;
    SerializedProperty m_DefaultAbilityProp;

    void OnEnable()
    {
        m_AbilitiesProp = serializedObject.FindProperty("m_Abilities");
        m_DefaultAbilityProp = serializedObject.FindProperty("m_DefaultAbility");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制 m_Abilities 列表
        EditorGUILayout.PropertyField(m_AbilitiesProp, true);

        EditorGUILayout.Space(4);

        // 运行时不显示 DefaultAbility 配置
        if (Application.isPlaying)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // 收集当前 m_Abilities 中所有非空项
        var validAbilities = new List<AnimancerAbility>();
        for (int i = 0; i < m_AbilitiesProp.arraySize; i++)
        {
            var elem = m_AbilitiesProp.GetArrayElementAtIndex(i).objectReferenceValue as AnimancerAbility;
            if (elem != null)
                validAbilities.Add(elem);
        }

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

        serializedObject.ApplyModifiedProperties();
    }
}
