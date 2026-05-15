using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SpawnPrefabPool")]
    [NodePath("UnityTimeline/Action/SpawnPrefabPool")]
    public class SpawnPrefabPoolNode : UnityTimelineActionNode
    {
        [SerializeField, ShowInPanel]
        GameObject m_Prefab;

        [SerializeField, ShowInPanel]
        int m_MaxPoolSize = 10;

        [SerializeField, ShowInPanel]
        bool m_PersistAcrossScenes = false;

        [SerializeField, PropertyPort(PortDirection.Input, "SocketName")]
        StringPropertyPort m_SocketName = new StringPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "PositionOffset")]
        Vector3PropertyPort m_PositionOffset = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "RotationOffset")]
        Vector3PropertyPort m_RotationOffset = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "InstanceName"), ReadOnly]
        StringPropertyPort m_InstanceNamePort = new StringPropertyPort();

        protected override void DoAction()
        {
            if (AbilityLinker == null || m_Prefab == null)
                return;

            Transform socketTransform = AbilityLinker.transform;
            if (!string.IsNullOrEmpty(m_SocketName.Value))
            {
                var childTransforms = AbilityLinker.gameObject.GetComponentsInChildren<Transform>();
                foreach (var child in childTransforms)
                {
                    if (child.name == m_SocketName.Value)
                    {
                        socketTransform = child;
                        break;
                    }
                }
            }

            var pool = PrefabPool.Instance;
            if (pool == null) return;

            string assignedName = pool.Spawn(
                m_Prefab,
                socketTransform,
                m_PositionOffset.Value,
                m_RotationOffset.Value,
                m_MaxPoolSize,
                m_PersistAcrossScenes
            );

            m_InstanceNamePort.Value = assignedName;
        }

        public override void Dispose()
        {
            m_InstanceNamePort.Value = null;
            base.Dispose();
        }
    }
}
