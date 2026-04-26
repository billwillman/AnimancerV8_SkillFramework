using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("ClearAllInputLocks")]
    [NodePath("UnityTimeline/Action/ClearAllInputLocks")]
    public class ClearAllInputLocksNode : UnityTimelineActionNode
    {
        protected override void DoAction()
        {
            var controller = AbilityLinker?.SkillCharacterController;
            if (controller == null) return;

            controller.ClearAllInputLocks();
        }
    }
}
