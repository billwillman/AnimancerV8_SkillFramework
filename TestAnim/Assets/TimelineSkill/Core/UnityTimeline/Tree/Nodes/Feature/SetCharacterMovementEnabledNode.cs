using UnityEngine;
using TreeDesigner;
using ECM2;

namespace UnityTimeline
{
    [NodeName("SetCharacterMovementEnabled")]
    [NodePath("UnityTimeline/Action/SetCharacterMovementEnabled")]
    public class SetCharacterMovementEnabledNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Enabled")]
        BoolPropertyPort m_Enabled = new BoolPropertyPort() { Value = true };

        protected override void DoAction()
        {
            var character = AbilityLinker?.GetComponent<Character>();
            if (character != null)
            {
                character.SetMovementMode(m_Enabled.Value ? MovementMode.Walking : MovementMode.None);
            }
        }
    }
}
