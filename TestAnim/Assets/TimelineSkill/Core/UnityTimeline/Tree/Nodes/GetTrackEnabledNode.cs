using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("GetTrackEnabled")]
    [NodePath("UnityTimeline/Value/GetTrackEnabled")]
    public class GetTrackEnabledNode : UnityTimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "TrackIndex")]
        IntPropertyPort m_TrackIndex = new IntPropertyPort { Value = 0 };

        [SerializeField, PropertyPort(PortDirection.Output, "Enabled"), TreeDesigner.ReadOnly]
        BoolPropertyPort m_Enabled = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            if (Controller?.IsValid != true)
            {
                m_Enabled.Value = false;
                return;
            }

            m_Enabled.Value = Controller.IsTrackEnabled(m_TrackIndex.Value);
        }
    }
}
