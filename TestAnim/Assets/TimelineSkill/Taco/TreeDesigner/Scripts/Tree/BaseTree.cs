using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [TreeWindow("OpenBaseTreeWindow")]
    [AcceptableNodePaths("Base")]
    public partial class BaseTree : ScriptableObject
    {
        [SerializeReference]
        protected List<BaseNode> m_Nodes = new List<BaseNode>();
        public List<BaseNode> Nodes => m_Nodes;

        [SerializeField]
        protected List<BaseEdge> m_Edges = new List<BaseEdge>();
        public List<BaseEdge> Edges => m_Edges;

        [SerializeField]
        protected List<PropertyEdge> m_PropertyEdges = new List<PropertyEdge>();
        public List<PropertyEdge> PropertyEdges => m_PropertyEdges;

        [SerializeReference]
        protected List<BaseExposedProperty> m_ExposedProperties = new List<BaseExposedProperty>();
        public List<BaseExposedProperty> ExposedProperties => m_ExposedProperties;

        [NonSerialized]
        protected Dictionary<string, BaseNode> m_GUIDNodeMap = new Dictionary<string, BaseNode>();
        public Dictionary<string, BaseNode> GUIDNodeMap => m_GUIDNodeMap;
        
        [NonSerialized]
        protected Dictionary<string, BaseEdge> m_GUIDEdgeMap = new Dictionary<string, BaseEdge>();
        public Dictionary<string, BaseEdge> GUIDEdgeMap => m_GUIDEdgeMap;

        [NonSerialized]
        protected Dictionary<string, PropertyEdge> m_GUIDPropertyEdgeMap = new Dictionary<string, PropertyEdge>();
        public Dictionary<string, PropertyEdge> GUIDPropertyEdgeMap => m_GUIDPropertyEdgeMap;

        [NonSerialized]
        protected Dictionary<string, BaseExposedProperty> m_GUIDExposedPropertyMap = new Dictionary<string, BaseExposedProperty>();
        public Dictionary<string, BaseExposedProperty> GUIDExposedPropertyMap => m_GUIDExposedPropertyMap;

        [NonSerialized]
        protected Dictionary<string,BaseExposedProperty> m_NameExposedPropertyMap = new Dictionary<string, BaseExposedProperty>();
        [NonSerialized]
        protected Dictionary<BaseExposedProperty, object> m_ExposedPropertyOriginalValueMap = new Dictionary<BaseExposedProperty, object>();

        public int ID { get; private set; }
        public bool IsValid { get; private set; }
        public object User { get; private set; }

        // ── Blackboard 运行时引用 ──

        [NonSerialized]
        protected BlackboardContext m_CurrentContext;
        /// <summary>当前绑定的 per-Tree BlackboardContext（BeginContext 时设置）</summary>
        public BlackboardContext CurrentContext => m_CurrentContext;

        [NonSerialized]
        protected CommonBlackboard m_CurrentBlackboard;
        /// <summary>当前绑定的 CommonBlackboard 组件引用</summary>
        public CommonBlackboard CurrentBlackboard => m_CurrentBlackboard;

        public virtual void InitTree(object user)
        {
            ID = GetInstanceID();
            IsValid = true;
            User = user;

            m_GUIDNodeMap.Clear();
            m_GUIDEdgeMap.Clear();
            m_GUIDPropertyEdgeMap.Clear();
            m_GUIDExposedPropertyMap.Clear();
            m_NameExposedPropertyMap.Clear();

            m_Nodes.ForEach(i => 
            {
                m_GUIDNodeMap.Add(i.GUID, i);
                i.BeforeInit();
            });
            m_Edges.ForEach(i => m_GUIDEdgeMap.Add(i.GUID, i));
            m_PropertyEdges.ForEach(i => m_GUIDPropertyEdgeMap.Add(i.GUID, i));
            m_ExposedProperties.ForEach(i => 
            {
                m_GUIDExposedPropertyMap.Add(i.GUID, i);
                m_NameExposedPropertyMap.Add(i.Name, i);
            });

            m_Edges.ForEach(i => i.Init(this));
            m_PropertyEdges.ForEach(i => i.Init(this));
            m_Nodes.ForEach(i => i.Init(this));
            m_Nodes.ForEach(i => i.AfterInit());
            m_ExposedProperties.ForEach(i => i.Init(this));

        }
        public virtual void DisposeTree()
        {
            m_Nodes.ForEach(i => i.Dispose());
            m_Edges.ForEach(i => i.Dispose());
            m_PropertyEdges.ForEach(i => i.Dispose());
            m_ExposedProperties.ForEach(i => i.Dispose());
            
            m_GUIDNodeMap.Clear();
            m_GUIDEdgeMap.Clear();
            m_GUIDPropertyEdgeMap.Clear();
            m_GUIDExposedPropertyMap.Clear();

            IsValid = false;
            User = null;
        }

        public virtual void OnSpawn()
        {
            m_ExposedPropertyOriginalValueMap.Clear();
            m_ExposedProperties.ForEach(i => m_ExposedPropertyOriginalValueMap.Add(i, i.GetValue()));
            m_Nodes.ForEach(i => i.OnSpawn());
        }
        public virtual void OnUnspawn()
        {
            m_ExposedProperties.ForEach(i => i.SetValue(m_ExposedPropertyOriginalValueMap[i]));
            m_Nodes.ForEach(i => i.OnUnspawn());
        }

        public BaseExposedProperty GetExposedProperty(string name)
        {
            // 1. per-tree 运行时 EP（克隆 + 注入）
            if (m_CurrentContext != null
                && m_CurrentContext.EPMap.TryGetValue(name, out var ctxEP))
                return ctxEP;
            // 2. CommonBlackboard 全局变量
            if (m_CurrentBlackboard != null)
            {
                var gv = m_CurrentBlackboard.GetVariable(name);
                if (gv != null) return gv;
            }
            // 3. SO 模板默认值
            if (m_NameExposedPropertyMap.TryGetValue(name, out var ep))
                return ep;
            return null;
        }
        public T GetExposedProperty<T>(string name) where T : BaseExposedProperty
        {
            return GetExposedProperty(name) as T;
        }

        // ── Blackboard 绑定 ──

        /// <summary>
        /// 将 BlackboardContext 绑定到 Tree 及其所有节点。
        /// 由 CommonBlackboard.BindTree 调用（BeginContext）。
        /// </summary>
        public void BindBlackboardContext(BlackboardContext context, CommonBlackboard blackboard)
        {
            m_CurrentContext = context;
            m_CurrentBlackboard = blackboard;

            // 遍历所有节点，注入对应的 NodeBlackboardData
            foreach (var node in m_Nodes)
            {
                var nodeData = context.GetNodeData(node.GUID);
                if (nodeData != null)
                    node.BindBlackboard(nodeData);
            }
        }

        /// <summary>
        /// 解绑 BlackboardContext。
        /// 由 CommonBlackboard.UnbindTree 调用（EndContext）。
        /// </summary>
        public void UnbindBlackboardContext()
        {
            foreach (var node in m_Nodes)
                node.UnbindBlackboard();

            m_CurrentContext = null;
            m_CurrentBlackboard = null;
        }
    }
}