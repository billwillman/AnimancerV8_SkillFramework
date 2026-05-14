using System;
using System.Collections.Generic;

namespace TreeDesigner 
{
    [NodeName("Sequence")]
    [NodePath("Base/Composite/Sequence")]
    public class SequenceNode : CompositeNode
    {
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
            var currentIndex = m_NodeData.GetRuntime<int>("CurrentIndex");
            if (m_Parent.State != State.Running || currentIndex.Value >= m_Children.Count)
                return State.None;

            State childState = m_Children[currentIndex.Value].UpdateNode();
            switch (childState)
            {
                case State.Running:
                    return State.Running;
                case State.Success:
                    currentIndex.Value++;
                    if (currentIndex.Value < m_Children.Count)
                        return OnUpdate();
                    else
                        return State.Success;
                case State.Failure:
                    return State.Failure;          
            }
            return State.None;
        }
    }
}
