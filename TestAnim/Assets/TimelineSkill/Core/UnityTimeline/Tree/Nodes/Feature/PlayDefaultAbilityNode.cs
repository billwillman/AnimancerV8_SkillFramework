using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    /// <summary>
    /// 播放 AnimancerAbilityLinker 上配置的 DefaultAbility。
    /// </summary>
    [NodeName("PlayDefaultAbility")]
    [NodePath("UnityTimeline/Action/PlayDefaultAbility")]
    public class PlayDefaultAbilityNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Success"), ReadOnly]
        BoolPropertyPort m_Success = new BoolPropertyPort();

        protected override void DoAction()
        {
            m_Success.Value = false;

            if (AbilityLinker != null && AbilityLinker.DefaultAbility != null)
            {
                m_Success.Value = AbilityLinker.TryStartAbility(AbilityLinker.DefaultAbility.name);
            }
        }
    }
}
