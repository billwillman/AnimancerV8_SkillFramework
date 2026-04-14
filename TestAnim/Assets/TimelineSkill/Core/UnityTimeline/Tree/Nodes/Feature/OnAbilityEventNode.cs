using System;
using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("OnAbilityEvent")]
    [NodePath("UnityTimeline/Action/OnAbilityEvent")]
    public class OnAbilityEventNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "EventKey")]
        StringPropertyPort m_EventKey = new StringPropertyPort();

        [NonSerialized] private string m_ScopedKey;
        [NonSerialized] private Action m_Callback;

        public override void Init(BaseTree tree)
        {
            base.Init(tree);

            if (AbilityLinker == null || string.IsNullOrEmpty(m_EventKey.Value))
                return;

            m_ScopedKey = m_EventKey.Value;
            m_Callback = OnEventTriggered;
            EventDispatch.Instance.AddEvent(m_ScopedKey, m_Callback);
        }

        public override void Dispose()
        {
            if (!string.IsNullOrEmpty(m_ScopedKey) && m_Callback != null)
            {
                EventDispatch.Instance.RemoveEvent(m_ScopedKey, m_Callback);
                m_ScopedKey = null;
                m_Callback = null;
            }
            base.Dispose();
        }

        private void OnEventTriggered()
        {
            UpdateNode();
        }

        protected override void DoAction()
        {
        }
    }
}
