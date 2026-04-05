using UnityEngine;
using System;
using TreeDesigner;
using Taco.Timeline;

namespace UnityTimeline
{
    /// <summary>
    /// 用于驱动/控制 Unity 官方 PlayableDirector 的行为树。
    /// 作为 TreeClip 嵌套在 Taco.Timeline 中运行。
    /// </summary>
    [AcceptableNodePaths("UnityTimeline")]
    public class UnityTimelineTree : TimelineRunningTree
    {
        [NonSerialized]
        private IDirectorController m_DirectorController;

        /// <summary>
        /// 获取 PlayableDirector 的控制接口。
        /// </summary>
        public IDirectorController DirectorController => m_DirectorController;

        [NonSerialized]
        private AnimancerAbilityLinker m_AbilityLinker;

        /// <summary>
        /// 获取或设置 AnimancerAbilityLinker 引用。
        /// 由 UnityTimelinePlayableBehaviour 在 SpawnRuntimeTree 时注入。
        /// </summary>
        public AnimancerAbilityLinker AbilityLinker
        {
            get => m_AbilityLinker;
            set => m_AbilityLinker = value;
        }

        /// <summary>
        /// 设置 PlayableDirector 控制接口。
        /// </summary>
        public void SetDirectorController(IDirectorController controller)
        {
            m_DirectorController = controller;
        }

        public override void DisposeTree()
        {
            base.DisposeTree();
            m_DirectorController = null;
            m_AbilityLinker = null;
        }
#if UNITY_EDITOR

        [UnityEditor.MenuItem("Assets/Create/AnimancerSkillSystem/Unity Timeline")]
        public static void CreateUnityTimeline()
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            if (string.IsNullOrEmpty(path))
                path = "Assets";

            // 创建 TimelineAsset
            var timeline = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Timeline.TimelineAsset>();
            string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path + "/New Unity Timeline.playable");
            UnityEditor.AssetDatabase.CreateAsset(timeline, assetPathAndName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.Selection.activeObject = timeline;
        }

        [UnityEditor.MenuItem("Assets/Create/AnimancerSkillSystem/UnityTimelineTree")]
        public static void CreateUnityTimelineTree()
        {
            UnityTimelineTree tree = CreateInstance<UnityTimelineTree>();
            tree.RootGUID = tree.CreateNode(typeof(RootNode)).GUID;

            var OnEnable = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            OnEnable.EnterType = TimelineEnterNode.NodeEnterType.OnEnable;
            OnEnable.Position = new Vector2(0, 200);
            tree.OnEnableGUID = OnEnable.GUID;

            var OnDisable = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            OnDisable.EnterType = TimelineEnterNode.NodeEnterType.OnDisable;
            OnDisable.Position = new Vector2(0, 400);
            tree.OnDisableGUID = OnDisable.GUID;

            var OnDestroy = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            OnDestroy.EnterType = TimelineEnterNode.NodeEnterType.OnDestroy;
            OnDestroy.Position = new Vector2(0, 600);
            tree.OnDestroyGUID = OnDestroy.GUID;

            var OnInterrupt = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            OnInterrupt.EnterType = TimelineEnterNode.NodeEnterType.OnInterrupt;
            OnInterrupt.Position = new Vector2(0, 800);
            tree.OnInterruptGUID = OnInterrupt.GUID;

            string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path + "/New UnityTimelineTree.asset");
            UnityEditor.AssetDatabase.CreateAsset(tree, assetPathAndName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.Selection.activeObject = tree;
        }
#endif
    }
}
