using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    /// <summary>
    /// 设置角色的地面移动速度和/或空中移动速度
    /// </summary>
    [NodeName("SetMoveSpeed")]
    [NodePath("UnityTimeline/Action/SetMoveSpeed")]
    public class SetMoveSpeedNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "GroundSpeed"), ShowInPanel, Tooltip("地面最大移动速度，-1 表示不修改")]
        FloatPropertyPort m_GroundSpeed = new FloatPropertyPort() { Value = -1f };

        [SerializeField, PropertyPort(PortDirection.Input, "AirSpeed"), ShowInPanel, Tooltip("空中最大移动速度，-1 表示不修改")]
        FloatPropertyPort m_AirSpeed = new FloatPropertyPort() { Value = -1f };

        protected override void DoAction()
        {
            var controller = AbilityLinker?.SkillCharacterController;
            if (controller == null) return;

            if (m_GroundSpeed.Value >= 0f)
                controller.MaxStableMoveSpeed = m_GroundSpeed.Value;

            if (m_AirSpeed.Value >= 0f)
                controller.MaxAirMoveSpeed = m_AirSpeed.Value;
        }
    }
}
