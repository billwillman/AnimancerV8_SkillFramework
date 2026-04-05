using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    public enum DirectorState
    {
        Playing,
        Paused,
        Stopped
    }

    [NodeName("GetDirectorState")]
    [NodePath("UnityTimeline/Value/GetDirectorState")]
    public class GetDirectorStateNode : UnityTimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "State"), TreeDesigner.ReadOnly]
        IntPropertyPort m_State = new IntPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            if (Controller?.IsValid != true)
            {
                m_State.Value = (int)DirectorState.Stopped;
                return;
            }

            m_State.Value = (int)Controller.state;
        }
    }
}
