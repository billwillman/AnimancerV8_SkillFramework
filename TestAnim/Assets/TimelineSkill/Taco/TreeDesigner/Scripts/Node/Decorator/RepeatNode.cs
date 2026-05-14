using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Repeat")]
    [NodePath("Base/Decorator/Repeat")]
    public class RepeatNode : DecoratorNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Int")]
        IntPropertyPort m_Count = new IntPropertyPort();

        public override void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties)
        {
            properties["CurrentIndex"] = new IntExposedProperty { Name = "CurrentIndex" };
        }

        protected override void OnStart()
        {
            base.OnStart();
            m_NodeData.GetRuntime<int>("CurrentIndex").Value = 0;
        }

        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            var currentIndex = m_NodeData.GetRuntime<int>("CurrentIndex");
            State childState = m_Child.UpdateNode();
            if (childState == State.Running)
                return State.Running;
            else
            {
                currentIndex.Value++;
                if (currentIndex.Value < m_Count.Value)
                    return OnUpdate();
                else
                    return State.Success;
            }
        }
    }
}
