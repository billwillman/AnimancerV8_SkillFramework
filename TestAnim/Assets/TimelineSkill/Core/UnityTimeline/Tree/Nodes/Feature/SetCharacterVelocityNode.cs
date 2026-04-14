using UnityEngine;
using TreeDesigner;
using ECM2;

namespace UnityTimeline
{
    [NodeName("SetCharacterVelocity")]
    [NodePath("UnityTimeline/Action/SetCharacterVelocity")]
    public class SetCharacterVelocityNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Velocity")]
        Vector3PropertyPort m_Velocity = new Vector3PropertyPort();

        protected override void DoAction()
        {
            var character = AbilityLinker?.GetComponent<Character>();
            if (character != null)
            {
                character.SetVelocity(m_Velocity.Value);
            }
        }
    }
}
