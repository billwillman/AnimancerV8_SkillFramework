using System;
using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    public enum HitboxShape { Sphere, Box }

    [NodeName("HitboxDetection")]
    [NodePath("UnityTimeline/Action/HitboxDetection")]
    public class HitboxDetectionNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Shape")]
        IntPropertyPort m_Shape = new IntPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "Radius")]
        FloatPropertyPort m_Radius = new FloatPropertyPort() { Value = 1f };

        [SerializeField, PropertyPort(PortDirection.Input, "HalfExtents")]
        Vector3PropertyPort m_HalfExtents = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "Offset")]
        Vector3PropertyPort m_Offset = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "LayerMask")]
        IntPropertyPort m_LayerMask = new IntPropertyPort() { Value = -1 };

        [SerializeField, PropertyPort(PortDirection.Output, "HitCount"), ReadOnly]
        IntPropertyPort m_HitCount = new IntPropertyPort();

        [NonSerialized] private Collider[] m_HitBuffer = new Collider[32];
        public Collider[] HitBuffer => m_HitBuffer;
        public int LastHitCount { get; private set; }

        protected override void DoAction()
        {
            if (AbilityLinker == null)
                return;

            if (m_HitBuffer == null)
                m_HitBuffer = new Collider[32];

            Vector3 center = AbilityLinker.transform.position + AbilityLinker.transform.TransformDirection(m_Offset.Value);
            int hitCount = 0;

            if ((HitboxShape)m_Shape.Value == HitboxShape.Sphere)
            {
                hitCount = Physics.OverlapSphereNonAlloc(center, m_Radius.Value, m_HitBuffer, m_LayerMask.Value);
            }
            else
            {
                hitCount = Physics.OverlapBoxNonAlloc(center, m_HalfExtents.Value, m_HitBuffer, AbilityLinker.transform.rotation, m_LayerMask.Value);
            }

            LastHitCount = hitCount;
            m_HitCount.Value = hitCount;
        }
    }
}
