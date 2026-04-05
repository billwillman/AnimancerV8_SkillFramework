using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("StopDirector")]
    [NodePath("UnityTimeline/Action/StopDirector")]
    public class StopDirectorNode : UnityTimelineActionNode
    {
        protected override void DoAction()
        {
            if (Controller?.IsValid == true)
            {
                Controller.Stop();
                Controller.time = 0;
            }
        }
    }
}
