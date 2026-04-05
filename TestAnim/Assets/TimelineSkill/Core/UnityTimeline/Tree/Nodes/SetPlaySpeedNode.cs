using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SetPlaySpeed")]
    [NodePath("UnityTimeline/Action/SetPlaySpeed")]
    public class SetPlaySpeedNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Speed")]
        FloatPropertyPort m_Speed = new FloatPropertyPort { Value = 1f };

        protected override void DoAction()
        {
            if (Controller?.IsValid == true)
                Controller.SetSpeed(m_Speed.Value);
        }
    }
}
