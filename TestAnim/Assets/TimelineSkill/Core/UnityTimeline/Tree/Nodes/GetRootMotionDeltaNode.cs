using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("GetRootMotionDelta")]
    [NodePath("UnityTimeline/Value/GetRootMotionDelta")]
    public class GetRootMotionDeltaNode : UnityTimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "DeltaPosition"), TreeDesigner.ReadOnly]
        Vector3PropertyPort m_DeltaPosition = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "DeltaRotation"), TreeDesigner.ReadOnly]
        Vector3PropertyPort m_DeltaRotation = new Vector3PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            if (Controller?.IsValid != true)
            {
                m_DeltaPosition.Value = Vector3.zero;
                m_DeltaRotation.Value = Vector3.zero;
                return;
            }

            m_DeltaPosition.Value = Controller.GetRootMotionDeltaPosition();
            m_DeltaRotation.Value = Controller.GetRootMotionDeltaRotation();
        }
    }
}
