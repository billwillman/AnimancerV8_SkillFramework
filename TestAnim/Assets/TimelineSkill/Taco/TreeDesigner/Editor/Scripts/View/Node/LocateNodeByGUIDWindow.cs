using UnityEngine;
using UnityEditor;

namespace TreeDesigner.Editor
{
    public class LocateNodeByGUIDWindow : EditorWindow
    {
        static BaseTreeView s_TreeView;
        string m_GUID = "";

        public static void Show(BaseTreeView treeView)
        {
            s_TreeView = treeView;
            var window = GetWindow<LocateNodeByGUIDWindow>(true, "Locate Node By GUID");
            window.minSize = new Vector2(360, 80);
            window.maxSize = new Vector2(480, 80);

            // 默认填入剪贴板内容
            var clipboard = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clipboard))
                window.m_GUID = clipboard;
        }

        void OnGUI()
        {
            if (s_TreeView == null)
            {
                EditorGUILayout.HelpBox("TreeView reference lost.", MessageType.Warning);
                return;
            }

            m_GUID = EditorGUILayout.TextField("Node GUID", m_GUID);

            if (GUILayout.Button("Locate"))
            {
                if (string.IsNullOrEmpty(m_GUID))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter a GUID.", "OK");
                    return;
                }

                var targetView = s_TreeView.FindNodeView(m_GUID.Trim());
                if (targetView != null)
                {
                    s_TreeView.ClearSelection();
                    s_TreeView.AddToSelection(targetView);
                    s_TreeView.FrameSelection();
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found",
                        $"Node with GUID \"{m_GUID.Trim()}\" not found (may have been deleted).", "OK");
                }
            }
        }
    }
}
