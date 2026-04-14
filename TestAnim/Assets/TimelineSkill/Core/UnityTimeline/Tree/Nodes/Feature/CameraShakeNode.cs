#if PACKAGE_CINEMACHINE
using UnityEngine;
using TreeDesigner;
using Cinemachine;

namespace UnityTimeline
{
    [NodeName("CameraShake")]
    [NodePath("UnityTimeline/Action/CameraShake")]
    public class CameraShakeNode : UnityTimelineActionNode
    {
        [SerializeField, ShowInPanel]
        CinemachineImpulseDefinition.ImpulseTypes m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;

        [SerializeField, ShowInPanel]
        CinemachineImpulseDefinition.ImpulseShapes m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;

        [SerializeField, PropertyPort(PortDirection.Input, "Duration")]
        FloatPropertyPort m_Duration = new FloatPropertyPort() { Value = 0.1f };

        [SerializeField, PropertyPort(PortDirection.Input, "Velocity")]
        Vector3PropertyPort m_Velocity = new Vector3PropertyPort();

        protected override void DoAction()
        {
            if (AbilityLinker == null)
                return;

            var impulseDefinition = new CinemachineImpulseDefinition
            {
                m_ImpulseChannel = 1,
                m_ImpulseShape = m_ImpulseShape,
                m_ImpulseDuration = m_Duration.Value,
                m_ImpulseType = m_ImpulseType,
                m_DissipationDistance = 100,
                m_DissipationRate = 0.25f,
                m_PropagationSpeed = 343
            };

            impulseDefinition.CreateEvent(AbilityLinker.transform.position, m_Velocity.Value);
        }
    }
}
#endif
