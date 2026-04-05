using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("JumpToTime")]
    [NodePath("UnityTimeline/Action/JumpToTime")]
    public class JumpToTimeNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "TargetTime")]
        FloatPropertyPort m_TargetTime = new FloatPropertyPort { Value = 0f };

        protected override void DoAction()
        {
            if (Controller?.IsValid == true)
                Controller.time = m_TargetTime.Value;
        }
    }
}
