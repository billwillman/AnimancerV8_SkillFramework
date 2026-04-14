using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("RemoveGameplayTag")]
    [NodePath("UnityTimeline/Action/RemoveGameplayTag")]
    public class RemoveGameplayTagNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Tag")]
        StringPropertyPort m_Tag = new StringPropertyPort();

        protected override void DoAction()
        {
            var agent = AbilityLinker?.AnimancerAbilityAgent;
            if (agent != null && !string.IsNullOrEmpty(m_Tag.Value))
            {
                agent.ActiveTags.Remove(m_Tag.Value);
            }
        }
    }
}
