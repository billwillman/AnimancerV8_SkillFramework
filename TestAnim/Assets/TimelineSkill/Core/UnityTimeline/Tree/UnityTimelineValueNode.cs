using UnityEngine;
using Taco.Timeline;

namespace UnityTimeline
{
    /// <summary>
    /// Unity Timeline Value 节点抽象基类。
    /// 所有需要输出/输入 PlayableDirector 数据的 Value 节点都应继承此类。
    /// </summary>
    public abstract class UnityTimelineValueNode : TimelineValueNode
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
