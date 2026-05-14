using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    public class TreeRunner : MonoBehaviour
    {
        [SerializeField]
        protected RunnableTree m_Tree;
        [SerializeField]
        protected bool m_Loop;
        [SerializeField, Min(1)]
        protected float m_LoopInterval;

        bool m_Running;
        float m_CDTime;
        CommonBlackboard m_Blackboard;

        void Update()
        {
            if (!m_Tree || !m_Running)
                return;

            if (!m_Tree.Running && m_Loop)
            {
                if (m_CDTime >= m_LoopInterval)
                {
                    m_CDTime = 0;
                    m_Tree.ResetTree();
                    m_Tree.UpdateTree(Time.deltaTime);
                }
                else
                {
                    m_CDTime += Time.deltaTime;
                }
            }
            else
            {
                m_Tree.UpdateTree(Time.deltaTime);
            }
        }

        [ContextMenu("CloneTree")]
        void CloneTree()
        {
            m_Tree = Instantiate(m_Tree);
            m_Tree.OnSpawn();
            SetupBlackboard();
        }

        [ContextMenu("InitTree")]
        void InitTree()
        {
            m_Tree?.InitTree(this);
            SetupBlackboard();
        }

        [ContextMenu("DisposeTree")]
        void Dispose()
        {
            if (m_Blackboard != null && m_Tree != null)
                m_Blackboard.UnregisterTree(m_Tree);
            m_Tree?.DisposeTree();
        }

        [ContextMenu("UpdateTree")]
        void UpdateTree()
        {
            m_Tree?.UpdateTree(0);
            m_Running = true;
        }

        [ContextMenu("ResetTree")]
        void ResetTree()
        {
            m_Tree?.ResetTree();
            m_Tree.Running = false;
            m_Running = false;
        }

        [ContextMenu("PauseTree")]
        void PauseTree()
        {
            m_Running = false;
        }

        [ContextMenu("ResumeTree")]
        void ResumeTree()
        {
            m_Running = true;
        }

        /// <summary>
        /// 初始化 CommonBlackboard 并注册 Tree。
        /// </summary>
        void SetupBlackboard()
        {
            if (m_Tree == null) return;

            m_Blackboard = GetComponent<CommonBlackboard>();
            if (m_Blackboard == null)
                m_Blackboard = gameObject.AddComponent<CommonBlackboard>();

            var ctx = m_Blackboard.RegisterTree(m_Tree);
            m_Blackboard.BindTree(m_Tree);
        }
    }
}
