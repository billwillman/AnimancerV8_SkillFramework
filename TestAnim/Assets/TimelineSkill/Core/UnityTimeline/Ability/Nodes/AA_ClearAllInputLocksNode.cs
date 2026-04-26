using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("ClearAllInputLocks")]
    [NodePath("AnimancerAbility/Action/ClearAllInputLocks")]
    public class AA_ClearAllInputLocksNode : AnimancerAbilityActionNode
    {
        protected override void DoAction()
        {
            var controller = GetSkillController();
            if (controller == null) return;

            controller.ClearAllInputLocks();
        }
    }
}
