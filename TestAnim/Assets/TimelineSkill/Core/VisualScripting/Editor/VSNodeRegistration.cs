using UnityEditor;
using Unity.VisualScripting;

/// <summary>
/// 编辑器工具：重新生成 Visual Scripting 节点数据库
/// 确保所有 AnimancerLinkNodes 目录下的自定义节点能在 Graph Editor 右键菜单中出现
/// 
/// 使用方式：菜单 Tools → AnimancerLinkNodes → Regenerate Nodes
/// </summary>
public static class VSNodeRegistration
{
    [MenuItem("Tools/AnimancerLinkNodes/Regenerate Nodes")]
    public static void RegenerateNodes()
    {
        // 重建节点数据库（等同于 Project Settings → Visual Scripting → Regenerate Nodes）
        UnitBase.Rebuild();
        UnityEngine.Debug.Log("[VSNodeRegistration] Node database regenerated successfully.");

        EditorUtility.DisplayDialog(
            "AnimancerLinkNodes",
            "节点数据库已重新生成。\n请在 Graph Editor 中右键搜索 AnimancerLinkNodes 即可找到所有节点。",
            "OK");
    }
}
