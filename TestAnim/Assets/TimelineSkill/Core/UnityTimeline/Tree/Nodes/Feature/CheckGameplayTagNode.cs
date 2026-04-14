using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("CheckGameplayTag")]
    [NodePath("UnityTimeline/Value/CheckGameplayTag")]
    public class CheckGameplayTagNode : UnityTimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Tag")]
        StringPropertyPort m_Tag = new StringPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "HasTag"), ReadOnly]
        BoolPropertyPort m_HasTag = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            var agent = AbilityLinker?.AnimancerAbilityAgent;
            m_HasTag.Value = agent != null && !string.IsNullOrEmpty(m_Tag.Value) && agent.ActiveTags.Contains(m_Tag.Value);
        }
    }
}
