using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("PlayDirector")]
    [NodePath("UnityTimeline/Action/PlayDirector")]
    public class PlayDirectorNode : UnityTimelineActionNode
    {
        protected override void DoAction()
        {
            if (Controller?.IsValid == true)
                Controller.Play();
        }
    }
}
