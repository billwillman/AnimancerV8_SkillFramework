using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("RemoveInputLock")]
    [NodePath("UnityTimeline/Action/RemoveInputLock")]
    public class RemoveInputLockNode : UnityTimelineActionNode
    {
        /// <summary>要移除的持有者标识，需与 AddInputLock 时传入的 Key 一致</summary>
        [SerializeField, PropertyPort(PortDirection.Input, "LockKey (持有者标识)"), Tooltip("要移除的持有者标识，需与 AddInputLock 时传入的 Key 一致")]
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
