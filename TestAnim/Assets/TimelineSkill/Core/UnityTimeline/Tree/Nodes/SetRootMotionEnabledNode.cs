using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SetRootMotionEnabled")]
    [NodePath("UnityTimeline/Action/SetRootMotionEnabled")]
    public class SetRootMotionEnabledNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Enable")]
        BoolPropertyPort m_Enable = new BoolPropertyPort { Value = true };

        protected override void DoAction()
        {
            if (Controller?.IsValid == true)
                Controller.SetRootMotionEnabled(m_Enable.Value);
        }
    }
}
