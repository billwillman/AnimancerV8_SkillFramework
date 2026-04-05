using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SetTrackEnabled")]
    [NodePath("UnityTimeline/Action/SetTrackEnabled")]
    public class SetTrackEnabledNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "TrackIndex")]
        IntPropertyPort m_TrackIndex = new IntPropertyPort { Value = 0 };

        [SerializeField, PropertyPort(PortDirection.Input, "Enabled")]
        BoolPropertyPort m_Enabled = new BoolPropertyPort { Value = true };

        protected override void DoAction()
        {
            if (Controller?.IsValid == true)
                Controller.SetTrackEnabled(m_TrackIndex.Value, m_Enabled.Value);
        }
    }
}
