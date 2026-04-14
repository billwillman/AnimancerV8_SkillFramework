using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("TryStopAbility")]
    [NodePath("UnityTimeline/Action/TryStopAbility")]
    public class TryStopAbilityNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "AbilityName")]
        StringPropertyPort m_AbilityName = new StringPropertyPort();

        protected override void DoAction()
        {
            if (AbilityLinker != null && !string.IsNullOrEmpty(m_AbilityName.Value))
            {
                AbilityLinker.TryStopAbility(m_AbilityName.Value);
            }
        }
    }
}
