using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("RemoveInputLock")]
    [NodePath("AnimancerAbility/Action/RemoveInputLock")]
    public class AA_RemoveInputLockNode : AnimancerAbilityActionNode
    {
        /// <summary>要移除的持有者标识，需与 AddInputLock 时传入的 Tag 一致</summary>
        [SerializeField, PropertyPort(PortDirection.Input, "LockKey"), Tooltip("要移除的持有者标识，需与 AddInputLock 时传入的 Tag 一致")]
        StringPropertyPort m_LockKey = new StringPropertyPort() { Value = "SkillPlay" };

        /// <summary>要移除的通道，None 表示移除该 Tag 在所有通道的锁</summary>
        [SerializeField, PropertyPort(PortDirection.Input, "LockFlags"), Tooltip("要移除的通道（None=移除所有通道的该 Tag）")]
        InputLockFlagsPropertyPort m_LockFlags = new InputLockFlagsPropertyPort() { Value = InputLockFlags.None };

        protected override void DoAction()
        {
            var controller = GetSkillController();
            if (controller == null) return;

            string key = m_LockKey.Value;
            if (!string.IsNullOrEmpty(key))
            {
                if (m_LockFlags.Value == InputLockFlags.None)
                    controller.RemoveInputLock(key);
                else
                    controller.RemoveInputLock(key, m_LockFlags.Value);
            }
        }
    }
}
