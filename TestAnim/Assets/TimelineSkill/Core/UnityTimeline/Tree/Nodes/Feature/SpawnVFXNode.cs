using System;
using System.Collections.Generic;
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

        public override void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties)
        {
            properties["Instance"] = new StringExposedProperty { Name = "Instance" };
        }

        private GameObject GetInstance()
        {
            return NodeData?.RuntimeProperties.TryGetValue("Instance", out var ep) == true
                ? ep.GetValue() as GameObject : null;
        }

        private void SetInstance(GameObject value)
        {
            if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("Instance", out var ep))
                ep.SetValue(value);
        }

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

            var instance = UnityEngine.Object.Instantiate(m_Prefab, socketTransform, false);
            instance.transform.localPosition = m_PositionOffset.Value;
            instance.transform.localEulerAngles = m_RotationOffset.Value;
            SetInstance(instance);
            m_InstancePort.Value = instance;
        }

        public override void Dispose()
        {
            var instance = GetInstance();
            if (instance != null)
            {
                UnityEngine.Object.Destroy(instance);
                SetInstance(null);
            }
            base.Dispose();
        }
    }
}
