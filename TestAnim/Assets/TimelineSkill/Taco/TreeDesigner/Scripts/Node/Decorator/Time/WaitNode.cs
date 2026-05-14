using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Wait")]
    [NodePath("Base/Decorator/Time/Wait")]
    public class WaitNode : DecoratorNode
    {
        public enum WaitType { Time, Frame }

        [SerializeField, EnumMenu("WaitType", "OnNodeChangedCallback")]
        WaitType m_WaitType;
        [SerializeField, PropertyPort(PortDirection.Input, "Time"), ShowIf("m_WaitType", WaitType.Time)]
        FloatPropertyPort m_Time = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Frame"), ShowIf("m_WaitType", WaitType.Frame)]
        IntPropertyPort m_Frame = new IntPropertyPort();

        public override void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties)
        {
            properties["CurrentTime"] = new FloatExposedProperty { Name = "CurrentTime" };
            properties["CurrentFrame"] = new IntExposedProperty { Name = "CurrentFrame" };
        }

        protected override void OnStart()
        {
            base.OnStart();
            NodeData.GetRuntime<float>("CurrentTime").Value = 0;
            NodeData.GetRuntime<int>("CurrentFrame").Value = 0;
        }

        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            if (m_WaitType == WaitType.Time && NodeData.GetRuntime<float>("CurrentTime").Value < m_Time.Value)
            {
                NodeData.GetRuntime<float>("CurrentTime").Value += UnityEngine.Time.deltaTime;
                return State.Running;
            }
            else if (m_WaitType == WaitType.Frame && NodeData.GetRuntime<int>("CurrentFrame").Value < m_Frame.Value)
            {
                NodeData.GetRuntime<int>("CurrentFrame").Value++;
                return State.Running;
            }
            return m_Child?.UpdateNode() ?? State.Success;
        }
    }
}
