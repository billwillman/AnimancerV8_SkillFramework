using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public abstract class RunnableNode : BaseNode
    {
        [NonSerialized]
        protected State m_State;
        public State State { get => m_State; set => m_State = value; }

        public Action OnUpdateCallback;
        public Action OnStartCallback;
        public Action OnResetCallback;

        public virtual State UpdateNode()
        {
            if (m_State != State.Running)
            {
                OnStart();
            }
            if (m_State == State.Running)
            {
                m_State = OnUpdate();
            }
            if (m_State == State.Success || m_State == State.Failure)
            {
                OnStop();
            }
            OnUpdateCallback?.Invoke();
            return m_State;
        }
        public virtual void ResetNode()
        {
            m_State = State.None;
            OnReset();
            OnUpdateCallback?.Invoke();
        }

        protected virtual void OnStart()
        {
            m_State = State.Running;
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

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_State = State.None;
        }

        /// <summary>
        /// 该节点是否拥有需要跨帧保存的自定义状态字段。
        /// 返回 false（默认）时 SaveNodeStates 完全跳过字典分配，消除每帧 GC 压力。
        /// override SaveContextState/RestoreContextState 的子类必须同时 override 此属性为 true。
        /// </summary>
        public virtual bool HasContextState => false;

        /// <summary>
        /// EndContext 时调用，子类 override 将自定义 [NonSerialized] 状态字段保存到 store。
        /// 仅在 HasContextState == true 时才会被调用。
        /// </summary>
        public virtual void SaveContextState(Dictionary<string, object> store) { }

        /// <summary>
        /// BeginContext 时调用，子类 override 从 store 恢复自定义 [NonSerialized] 状态字段。
        /// 仅在 NodeSnapshot.Custom != null 时才会被调用。
        /// </summary>
        public virtual void RestoreContextState(Dictionary<string, object> store) { }
    }
}