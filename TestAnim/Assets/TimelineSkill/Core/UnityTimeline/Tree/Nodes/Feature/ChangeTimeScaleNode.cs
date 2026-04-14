using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("ChangeTimeScale")]
    [NodePath("UnityTimeline/Action/ChangeTimeScale")]
    public class ChangeTimeScaleNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Scale")]
        FloatPropertyPort m_Scale = new FloatPropertyPort() { Value = 0.5f };

        [SerializeField, PropertyPort(PortDirection.Input, "Duration")]
        FloatPropertyPort m_Duration = new FloatPropertyPort() { Value = 1f };

        [SerializeField, PropertyPort(PortDirection.Input, "BlendIn")]
        FloatPropertyPort m_BlendIn = new FloatPropertyPort() { Value = 0.1f };

        [SerializeField, PropertyPort(PortDirection.Input, "BlendOut")]
        FloatPropertyPort m_BlendOut = new FloatPropertyPort() { Value = 0.1f };

        protected override void DoAction()
        {
            if (TimeMananger.Instance != null)
            {
                TimeMananger.Instance.ChangeTimeScale(m_Scale.Value, m_Duration.Value, m_BlendIn.Value, m_BlendOut.Value);
            }
        }
    }
}
