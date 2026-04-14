using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("TryStartAbility")]
    [NodePath("UnityTimeline/Action/TryStartAbility")]
    public class TryStartAbilityNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "AbilityName")]
        StringPropertyPort m_AbilityName = new StringPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Success"), ReadOnly]
        BoolPropertyPort m_Success = new BoolPropertyPort();

        protected override void DoAction()
        {
            m_Success.Value = false;
            if (AbilityLinker != null && !string.IsNullOrEmpty(m_AbilityName.Value))
            {
                m_Success.Value = AbilityLinker.TryStartAbility(m_AbilityName.Value);
            }
        }
    }
}
