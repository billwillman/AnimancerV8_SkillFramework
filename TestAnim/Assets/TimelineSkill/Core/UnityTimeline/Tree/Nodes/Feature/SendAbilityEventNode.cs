using System;
using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SendAbilityEvent")]
    [NodePath("UnityTimeline/Action/SendAbilityEvent")]
    public class SendAbilityEventNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "EventKey")]
        StringPropertyPort m_EventKey = new StringPropertyPort();

        protected override void DoAction()
        {
            if (AbilityLinker == null || string.IsNullOrEmpty(m_EventKey.Value))
                return;

            EventDispatch.Instance.TriggerEvent(m_EventKey.Value);
        }
    }
}
