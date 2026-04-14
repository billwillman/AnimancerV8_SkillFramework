using UnityEngine;
using TreeDesigner;
using ECM2;

namespace UnityTimeline
{
    [NodeName("LaunchCharacter")]
    [NodePath("UnityTimeline/Action/LaunchCharacter")]
    public class LaunchCharacterNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "LaunchVelocity")]
        Vector3PropertyPort m_LaunchVelocity = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "OverrideVertical")]
        BoolPropertyPort m_OverrideVertical = new BoolPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "OverrideLateral")]
        BoolPropertyPort m_OverrideLateral = new BoolPropertyPort();

        protected override void DoAction()
        {
            var character = AbilityLinker?.GetComponent<Character>();
            if (character != null)
            {
                character.LaunchCharacter(m_LaunchVelocity.Value, m_OverrideVertical.Value, m_OverrideLateral.Value);
            }
        }
    }
}
