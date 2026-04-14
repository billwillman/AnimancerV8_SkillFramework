using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("AddGameplayTag")]
    [NodePath("UnityTimeline/Action/AddGameplayTag")]
    public class AddGameplayTagNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Tag")]
        StringPropertyPort m_Tag = new StringPropertyPort();

        protected override void DoAction()
        {
            var agent = AbilityLinker?.AnimancerAbilityAgent;
            if (agent != null && !string.IsNullOrEmpty(m_Tag.Value))
            {
                if (!agent.ActiveTags.Contains(m_Tag.Value))
                    agent.ActiveTags.Add(m_Tag.Value);
            }
        }
    }
}
