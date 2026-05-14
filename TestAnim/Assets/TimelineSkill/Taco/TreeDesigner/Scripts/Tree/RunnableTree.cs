using System;

namespace TreeDesigner
{
    public abstract partial class RunnableTree : BaseTree
    {
        /// <summary>树是否正在运行，强制走 BlackboardContext。</summary>
        public bool Running
        {
            get => m_CurrentContext?.TreeRunning ?? false;
            set { if (m_CurrentContext != null) m_CurrentContext.TreeRunning = value; }
        }

        /// <summary>树级执行状态，强制走 BlackboardContext。</summary>
        public State State
        {
            get => m_CurrentContext?.TreeState ?? State.None;
            set { if (m_CurrentContext != null) m_CurrentContext.TreeState = value; }
        }

        public float DeltaTime { get; private set; }

        public Action OnStopCallback;

        public override void DisposeTree()
        {
            OnStop();
            base.DisposeTree();
        }

        public virtual State UpdateTree(float deltaTime)
        {
            DeltaTime = deltaTime;

            if (!Running && State == State.None)
            {
                OnStart();
            }
            if (Running && State == State.Running)
            {
                State = OnUpdate();
            }
            if (Running && State == State.Success || State == State.Failure)
            {
                OnStop();
            }
            return State;
        }

        public virtual void ResetTree()
        {
            State = State.None;
            OnReset();
        }

        public abstract void OnStart();
        public abstract State OnUpdate();
        public abstract void OnStop();
        public abstract void OnReset();
    }
}
