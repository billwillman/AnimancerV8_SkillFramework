using System;
using UnityEditor;
using UnityEngine;

namespace Taco.Gameplay.Editor
{
    public class DescriptionInputWindow : EditorWindow
    {
        string m_TagName;
        string m_Description;
        Action<string> m_OnConfirm;

        public void Init(string tagName, string currentDescription, Action<string> onConfirm)
        {
            m_TagName = tagName;
            m_Description = currentDescription ?? string.Empty;
            m_OnConfirm = onConfirm;
            titleContent = new GUIContent("Edit Description");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Tag", m_TagName, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            m_Description = EditorGUILayout.TextField("Description", m_Description);
            if (EditorGUI.EndChangeCheck())
            {
                // 实时保存
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Confirm"))
            {
                m_OnConfirm?.Invoke(m_Description);
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
