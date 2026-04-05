using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("PauseDirector")]
    [NodePath("UnityTimeline/Action/PauseDirector")]
    public class PauseDirectorNode : UnityTimelineActionNode
    {
        protected override void DoAction()
        {
            if (Controller?.IsValid == true)
                Controller.Pause();
        }
    }
}
