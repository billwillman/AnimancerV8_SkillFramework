using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("RemoveInputLock")]
    [NodePath("UnityTimeline/Action/RemoveInputLock")]
    public class RemoveInputLockNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "LockKey"), Tooltip("要移除的持有者标识")]
        StringPropertyPort m_LockKey = new StringPropertyPort() { Value = "SkillPlay" };

        protected override void DoAction()
        {
            var controller = AbilityLinker?.SkillCharacterController;
            if (controller == null) return;

            string key = m_LockKey.Value;
            if (!string.IsNullOrEmpty(key))
                controller.RemoveInputLock(key);
        }
    }
}
