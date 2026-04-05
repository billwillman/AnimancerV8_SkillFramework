using UnityEngine;
using Taco.Timeline;

namespace UnityTimeline
{
    /// <summary>
    /// Unity Timeline Action 节点抽象基类。
    /// 所有需要控制 PlayableDirector 的 Action 节点都应继承此类。
    /// </summary>
    public abstract class UnityTimelineActionNode : TimelineActionNode
    {
        /// <summary>
        /// 获取 PlayableDirector 的控制接口，通过 UnityTimelineTree 提供。
        /// </summary>
        protected IDirectorController Controller => (TimelineRunningTree as UnityTimelineTree)?.DirectorController;

        /// <summary>
        /// 获取 AnimancerAbilityLinker 引用，通过 UnityTimelineTree 提供。
        /// </summary>
        protected AnimancerAbilityLinker AbilityLinker => (TimelineRunningTree as UnityTimelineTree)?.AbilityLinker;
    }
}
