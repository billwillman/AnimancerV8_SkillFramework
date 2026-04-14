using UnityEngine;
using TreeDesigner;
using EasyCharacterMovement;

namespace UnityTimeline
{
    [NodeName("RotateTowards")]
    [NodePath("UnityTimeline/Action/RotateTowards")]
    public class RotateTowardsNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Direction")]
        Vector3PropertyPort m_Direction = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "IsPlanar")]
        BoolPropertyPort m_IsPlanar = new BoolPropertyPort() { Value = true };

        protected override void DoAction()
        {
            var character = AbilityLinker?.GetComponent<Character>();
            if (character != null)
            {
                character.RotateTowards(m_Direction.Value, m_IsPlanar.Value);
            }
        }
    }
}
