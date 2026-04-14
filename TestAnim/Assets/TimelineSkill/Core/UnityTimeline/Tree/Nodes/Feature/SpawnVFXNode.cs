using System;
using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SpawnVFX")]
    [NodePath("UnityTimeline/Action/SpawnVFX")]
    public class SpawnVFXNode : UnityTimelineActionNode
    {
        [SerializeField, ShowInPanel]
        GameObject m_Prefab;

        [SerializeField, PropertyPort(PortDirection.Input, "SocketName")]
        StringPropertyPort m_SocketName = new StringPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "PositionOffset")]
        Vector3PropertyPort m_PositionOffset = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "RotationOffset")]
        Vector3PropertyPort m_RotationOffset = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Instance"), ReadOnly]
        objectPropertyPort m_InstancePort = new objectPropertyPort();

        [NonSerialized] private GameObject m_Instance;

        protected override void DoAction()
        {
            if (AbilityLinker == null || m_Prefab == null)
                return;

            Transform socketTransform = AbilityLinker.transform;
            if (!string.IsNullOrEmpty(m_SocketName.Value))
            {
                var childTransforms = AbilityLinker.GetComponentsInChildren<Transform>();
                foreach (var child in childTransforms)
                {
                    if (child.name == m_SocketName.Value)
                    {
                        socketTransform = child;
                        break;
                    }
                }
            }

            m_Instance = UnityEngine.Object.Instantiate(m_Prefab, socketTransform, false);
            m_Instance.transform.localPosition = m_PositionOffset.Value;
            m_Instance.transform.localEulerAngles = m_RotationOffset.Value;
            m_InstancePort.Value = m_Instance;
        }

        public override void Dispose()
        {
            if (m_Instance != null)
            {
                UnityEngine.Object.Destroy(m_Instance);
                m_Instance = null;
            }
            base.Dispose();
        }
    }
}
