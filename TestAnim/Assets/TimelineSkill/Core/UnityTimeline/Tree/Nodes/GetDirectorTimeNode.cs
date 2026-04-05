using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("GetDirectorTime")]
    [NodePath("UnityTimeline/Value/GetDirectorTime")]
    public class GetDirectorTimeNode : UnityTimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Time"), TreeDesigner.ReadOnly]
        FloatPropertyPort m_Time = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Time.Value = Controller?.IsValid == true ? (float)Controller.time : 0f;
        }
    }
}
