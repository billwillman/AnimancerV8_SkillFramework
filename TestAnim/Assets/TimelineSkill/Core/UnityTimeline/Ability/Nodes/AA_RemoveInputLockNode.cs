using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("RemoveInputLock")]
    [NodePath("AnimancerAbility/Action/RemoveInputLock")]
    public class AA_RemoveInputLockNode : AnimancerAbilityActionNode
    {
        /// <summary>要移除的持有者标识，需与 AddInputLock 时传入的 Key 一致</summary>
        [SerializeField, PropertyPort(PortDirection.Input, "LockKey (持有者标识)"), Tooltip("要移除的持有者标识，需与 AddInputLock 时传入的 Key 一致")]
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
