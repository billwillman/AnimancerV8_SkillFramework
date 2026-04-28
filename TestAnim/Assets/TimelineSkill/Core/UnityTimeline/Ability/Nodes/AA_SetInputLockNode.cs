using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SetInputLock")]
    [NodePath("AnimancerAbility/Action/SetInputLock")]
    public class AA_SetInputLockNode : AnimancerAbilityActionNode
    {
        /// <summary>持有者标识，用于区分不同来源的锁定（如 "Attack", "Dialogue"），解锁时需传入相同 Key</summary>
        [SerializeField, PropertyPort(PortDirection.Input, "LockKey"), Tooltip("持有者标识（如 \"Attack\", \"Dialogue\"），解锁时需传入相同 Key")]
        StringPropertyPort m_LockKey = new StringPropertyPort() { Value = "SkillPlay" };

        /// <summary>要锁定的输入通道标志位，可组合多个通道（如 Move|Jump 同时锁定移动和跳跃）</summary>
        [SerializeField, PropertyPort(PortDirection.Input, "LockFlags"), Tooltip("要锁定的输入通道标志位，可组合多个通道（如 Move|Jump 同时锁定移动和跳跃）")]
        InputLockFlagsPropertyPort m_LockFlags = new InputLockFlagsPropertyPort();

        protected override void DoAction()
        {
            var controller = GetSkillController();
            if (controller == null) return;

            string key = m_LockKey.Value;
            if (!string.IsNullOrEmpty(key))
                controller.AddInputLock(key, m_LockFlags.Value);
        }
    }
}
