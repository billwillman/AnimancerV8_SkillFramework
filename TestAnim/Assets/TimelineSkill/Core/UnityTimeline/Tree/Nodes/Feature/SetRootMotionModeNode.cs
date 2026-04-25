using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SetRootMotionMode")]
    [NodePath("UnityTimeline/Action/SetRootMotionMode")]
    public class SetRootMotionModeNode : UnityTimelineActionNode
    {
        public enum Target
        {
            Ground,
            Air,
            Both,
        }

        [SerializeField, PropertyPort(PortDirection.Input, "Target")]
        IntPropertyPort m_Target = new IntPropertyPort() { Value = (int)Target.Both };

        [SerializeField, PropertyPort(PortDirection.Input, "FullRootMotion")]
        BoolPropertyPort m_FullRootMotion = new BoolPropertyPort() { Value = true };

        protected override void DoAction()
        {
            var controller = AbilityLinker?.SkillCharacterController;
            if (controller == null)
                return;

            var mode = m_FullRootMotion.Value
                ? RootMotionMode.FullRootMotion
                : RootMotionMode.IgnoreRootMotion;

            var target = (Target)m_Target.Value;

            if (target == Target.Ground || target == Target.Both)
                controller.GroundRootMotionMode = mode;

            if (target == Target.Air || target == Target.Both)
                controller.AirRootMotionMode = mode;
        }
    }
}
