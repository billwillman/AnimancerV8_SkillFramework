using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SetInputLock")]
    [NodePath("AnimancerAbility/Action/SetInputLock")]
    public class AA_SetInputLockNode : AnimancerAbilityActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "LockKey"), Tooltip("持有者标识（如 \"Attack\", \"Dialogue\"）")]
        StringPropertyPort m_LockKey = new StringPropertyPort() { Value = "SkillPlay" };

        [SerializeField, PropertyPort(PortDirection.Input, "LockFlags"), Tooltip("要锁定的输入通道: None=0, Movement=1, Jump=2, All=3")]
        IntPropertyPort m_LockFlags = new IntPropertyPort() { Value = (int)InputLockFlags.All };

        protected override void DoAction()
        {
            var controller = GetSkillController();
            if (controller == null) return;

            string key = m_LockKey.Value;
            if (!string.IsNullOrEmpty(key))
                controller.AddInputLock(key, (InputLockFlags)m_LockFlags.Value);
        }
    }
}
