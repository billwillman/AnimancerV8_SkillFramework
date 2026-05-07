using System;
using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [Serializable]
    [NodeName("GetPoolInstance")]
    [NodePath("UnityTimeline/Value/GetPoolInstance")]
    public class GetPoolInstanceNode : UnityTimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Name")]
        StringPropertyPort m_Name = new StringPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Instance"), ReadOnly]
        objectPropertyPort m_Instance = new objectPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Instance.Value = string.IsNullOrEmpty(m_Name.Value) ? null : PrefabPool.Instance.GetActiveInstance(m_Name.Value);
        }
    }
}
