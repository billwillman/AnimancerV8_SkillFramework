using System;
using System.Collections.Generic;
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

        public override void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties)
        {
            properties["HitBuffer"] = new StringExposedProperty { Name = "HitBuffer" };
            properties["LastHitCount"] = new IntExposedProperty { Name = "LastHitCount" };
        }

        public Collider[] HitBuffer
        {
            get
            {
                if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("HitBuffer", out var ep))
                {
                    var buffer = ep.GetValue() as Collider[];
                    if (buffer == null)
                    {
                        buffer = new Collider[32];
                        ep.SetValue(buffer);
                    }
                    return buffer;
                }
                return null;
            }
        }

        public int LastHitCount
        {
            get
            {
                if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("LastHitCount", out var ep))
                    return ep is BaseExposedProperty<int> typed ? typed.Value : 0;
                return 0;
            }
            private set
            {
                if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("LastHitCount", out var ep)
                    && ep is BaseExposedProperty<int> typed)
                    typed.Value = value;
            }
        }

        protected override void DoAction()
        {
            if (AbilityLinker == null)
                return;

            var hitBuffer = HitBuffer;
            if (hitBuffer == null)
                return;

            Vector3 center = AbilityLinker.transform.position + AbilityLinker.transform.TransformDirection(m_Offset.Value);
            int hitCount = 0;

            if ((HitboxShape)m_Shape.Value == HitboxShape.Sphere)
            {
                hitCount = Physics.OverlapSphereNonAlloc(center, m_Radius.Value, hitBuffer, m_LayerMask.Value);
            }
            else
            {
                hitCount = Physics.OverlapBoxNonAlloc(center, m_HalfExtents.Value, hitBuffer, AbilityLinker.transform.rotation, m_LayerMask.Value);
            }

            LastHitCount = hitCount;
            m_HitCount.Value = hitCount;
        }
    }
}
