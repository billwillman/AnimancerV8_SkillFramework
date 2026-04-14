using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("ApplyDamage")]
    [NodePath("UnityTimeline/Action/ApplyDamage")]
    public class ApplyDamageNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Damage")]
        FloatPropertyPort m_Damage = new FloatPropertyPort() { Value = 10f };

        protected override void DoAction()
        {
            if (AbilityLinker == null)
                return;

            Debug.Log($"[ApplyDamageNode] Damage={m_Damage.Value} — IDamageable not implemented yet. Implement IDamageable on target to receive damage.");
        }
    }
}
