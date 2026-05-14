using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("SmoothDamp")]
    [NodePath("Base/Value/SmoothDamp")]
    public class SmoothDampNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Current")]
        FloatPropertyPort m_Current = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Target")]
        FloatPropertyPort m_Target = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "SmoothTime")]
        FloatPropertyPort m_SmoothTime = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Result"), ReadOnly]
        FloatPropertyPort m_Result = new FloatPropertyPort();

        public override void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties)
        {
            properties["CurrentVelocity"] = new FloatExposedProperty { Name = "CurrentVelocity" };
        }

        protected override void OutputValue()
        {
            base.OutputValue();
            var nodeData = NodeData;
            if (nodeData == null)
            {
                m_Result.Value = m_Current.Value;
                return;
            }
            var velocityEP = nodeData.GetRuntime<float>("CurrentVelocity");
            float velocity = velocityEP.Value;
            m_Result.Value = Mathf.SmoothDamp(m_Current.Value, m_Target.Value, ref velocity, m_SmoothTime.Value);
            velocityEP.Value = velocity;
        }
    }
}