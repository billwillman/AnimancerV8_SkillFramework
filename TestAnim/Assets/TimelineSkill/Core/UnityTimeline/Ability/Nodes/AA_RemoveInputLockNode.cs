using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("RemoveInputLock")]
    [NodePath("AnimancerAbility/Action/RemoveInputLock")]
    public class AA_RemoveInputLockNode : AnimancerAbilityActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "LockKey"), Tooltip("要移除的持有者标识")]
        StringPropertyPort m_LockKey = new StringPropertyPort() { Value = "SkillPlay" };

        protected override void DoAction()
        {
            var controller = GetSkillController();
            if (controller == null) return;

            string key = m_LockKey.Value;
            if (!string.IsNullOrEmpty(key))
                controller.RemoveInputLock(key);
        }
    }
}
