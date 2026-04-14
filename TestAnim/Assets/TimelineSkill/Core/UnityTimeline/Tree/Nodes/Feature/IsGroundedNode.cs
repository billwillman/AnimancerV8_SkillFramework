using UnityEngine;
using TreeDesigner;
using EasyCharacterMovement;

namespace UnityTimeline
{
    [NodeName("IsGrounded")]
    [NodePath("UnityTimeline/Value/IsGrounded")]
    public class IsGroundedNode : UnityTimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "IsGrounded"), ReadOnly]
        BoolPropertyPort m_IsGrounded = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            var character = AbilityLinker?.GetComponent<Character>();
            m_IsGrounded.Value = character != null && character.IsGrounded();
        }
    }
}
