using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [NodeName("Parallel")]
    [NodePath("Base/Composite/Parallel")]
    public class ParallelNode : CompositeNode
    {
        public enum ParallelType { JumpComplete, UpdateAll }

        [SerializeField, ShowInPanel("ParallelType")]
        ParallelType m_ParallelType;

        public override void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties)
        {
            // 占位 EP，实际存 List<BaseNode> 引用
            properties["CompletedChildren"] = new StringExposedProperty { Name = "CompletedChildren" };
        }

        private List<BaseNode> GetCompletedChildren()
        {
            var obj = m_NodeData.RuntimeProperties["CompletedChildren"].GetValue();
            if (obj is List<BaseNode> list) return list;
            var newList = new List<BaseNode>();
            m_NodeData.RuntimeProperties["CompletedChildren"].SetValue(newList);
            return newList;
        }

        protected override void OnStart()
        {
            base.OnStart();
            GetCompletedChildren().Clear();
        }

        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            var completedChildren = GetCompletedChildren();
            bool running = false;

            foreach (var child in m_Children)
            {
                if (m_ParallelType == ParallelType.JumpComplete && completedChildren.Contains(child))
                    continue;

                State childState = child.UpdateNode();
                if ((childState == State.Success || childState == State.Failure) && 
                    m_ParallelType == ParallelType.JumpComplete)
                    completedChildren.Add(child);

                if (childState == State.Running)
                    running = true;
            }

            return running ? State.Running : State.Success;
        }
    }
}
