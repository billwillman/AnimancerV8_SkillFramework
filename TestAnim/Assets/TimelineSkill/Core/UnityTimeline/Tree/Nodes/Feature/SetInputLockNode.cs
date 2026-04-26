using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SetInputLock")]
    [NodePath("UnityTimeline/Action/SetInputLock")]
    public class SetInputLockNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "LockKey"), Tooltip("持有者标识（如 \"Attack\", \"Dialogue\"）")]
        StringPropertyPort m_LockKey = new StringPropertyPort() { Value = "SkillPlay" };

        [SerializeField, PropertyPort(PortDirection.Input, "LockFlags"), Tooltip("要锁定的输入通道")]
        InputLockFlagsPropertyPort m_LockFlags = new InputLockFlagsPropertyPort();

        protected override void DoAction()
        {
            var controller = AbilityLinker?.SkillCharacterController;
            if (controller == null) return;

            string key = m_LockKey.Value;
            if (!string.IsNullOrEmpty(key))
                controller.AddInputLock(key, m_LockFlags.Value);
        }
    }
}
