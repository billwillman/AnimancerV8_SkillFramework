using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public abstract class RunnableNode : BaseNode
    {
        // ── Blackboard 节点数据（通过 CurrentContext 实时查找）──

        /// <summary>
        /// 节点运行时数据，通过 CurrentContext 实时查找。
        /// 在 Context 绑定期间（OnStart/OnUpdate/DoAction 等）始终有效。
        /// </summary>
        public NodeBlackboardData NodeData
            => m_Owner?.CurrentContext?.GetNodeData(m_GUID);

        /// <summary>
        /// 节点执行状态，强制通过 NodeBlackboardData 读写。
        /// </summary>
        public State State
        {
            get => NodeData?.State ?? State.None;
            set { if (NodeData != null) NodeData.State = value; }
        }

        // ── Callbacks ──

        public Action OnUpdateCallback;
        public Action OnStartCallback;
        public Action OnResetCallback;

        // ── 生命周期 ──

        public virtual State UpdateNode()
        {
            if (State != State.Running)
            {
                OnStart();
            }
            if (State == State.Running)
            {
                State = OnUpdate();
            }
            if (State == State.Success || State == State.Failure)
            {
                OnStop();
            }
            OnUpdateCallback?.Invoke();
            return State;
        }

        public virtual void ResetNode()
        {
            State = State.None;
            OnReset();
            OnUpdateCallback?.Invoke();
        }

        protected virtual void OnStart()
        {
            State = State.Running;
            InputValue();
            OnStartCallback?.Invoke();
        }

        protected virtual State OnUpdate()
        {
            return State.None;
        }

        protected virtual void OnStop()
        {
        }

        protected virtual void OnReset()
        {
            OnResetCallback?.Invoke();
        }

        // ── Blackboard 绑定 ──

        public override void BindBlackboard(NodeBlackboardData nodeData)
        {
            base.BindBlackboard(nodeData);
        }

        public override void UnbindBlackboard()
        {
            base.UnbindBlackboard();
        }

        /// <summary>
        /// 子类 override 此方法声明额外的运行时 EP（Key=变量名, Value=EP 实例）。
        /// RegisterTree 时调用，创建的 EP 会放入 NodeBlackboardData.RuntimeProperties 字典。
        /// </summary>
        public virtual void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties) { }
    }
}
