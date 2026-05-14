using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Taco;

namespace TreeDesigner
{
    [Serializable]
    public abstract partial class BaseNode
    {
        [SerializeField]
        protected string m_GUID;
        public string GUID { get => m_GUID; set => m_GUID = value; }

        [NonSerialized]
        protected BaseTree m_Owner;
        public BaseTree Owner { get => m_Owner; set => m_Owner = value; }

        [NonSerialized]
        protected Dictionary<string, PropertyPort> m_PropertyPortMap = new Dictionary<string, PropertyPort>();
        public Dictionary<string, PropertyPort> PropertyPortMap => m_PropertyPortMap;
        
        [NonSerialized]
        protected List<BaseNode> m_InputPropertyNodes = new List<BaseNode>();
        public List<BaseNode> InputPropertyNodes => m_InputPropertyNodes;

        public virtual void BeforeInit()
        {
            m_PropertyPortMap.Clear();
            foreach (var fieldInfo in this.GetAllFields())
            {
                if (fieldInfo.GetValue(this) is PropertyPort propertyPort)
                {
                    if (m_PropertyPortMap.ContainsKey(propertyPort.Name))
                    {

                    }
                    else
                        m_PropertyPortMap.Add(propertyPort.Name, propertyPort);
                }
                else if (fieldInfo.GetValue(this) is List<PropertyPort> propertyPorts)
                    propertyPorts.ForEach(i => m_PropertyPortMap.Add(i.Name, i));
            }
        }
        public virtual void Init(BaseTree tree)
        {
            m_Owner = tree;
            foreach (var propertyPair in m_PropertyPortMap)
            {
                propertyPair.Value.Init(this);
            }
        }
        public virtual void AfterInit()
        {
            m_InputPropertyNodes.Clear();
            foreach (var propertyPortPair in m_PropertyPortMap)
            {
                if (propertyPortPair.Value.Direction == PortDirection.Input &&
                    propertyPortPair.Value.SourcePort != null &&
                    !m_InputPropertyNodes.Contains(propertyPortPair.Value.SourcePort.Owner))
                    m_InputPropertyNodes.Add(propertyPortPair.Value.SourcePort.Owner);
            }
        }
        public virtual void Dispose()
        {
            m_Owner = null;
            foreach (var propertyPair in m_PropertyPortMap)
            {
                propertyPair.Value.Dispose();
            }
            m_PropertyPortMap.Clear();
            m_InputPropertyNodes.Clear();
        }

        protected virtual void InputValue()
        {
            m_InputPropertyNodes.ForEach(i => i.OutputValue());
            foreach (var propertyPortPair in m_PropertyPortMap)
            {
                if (propertyPortPair.Value.Direction == PortDirection.Input)
                    propertyPortPair.Value.GetSourceValue();
            }
        }
        protected virtual void OutputValue(){ }
        public void OutputValueImperatively()
        {
            OutputValue();
        }

        public virtual void OnBeforeSerialize() { }
        public virtual void OnAfterDeserialize() 
        {
            m_Owner = null;
            //m_State = State.None;
            m_PropertyPortMap.Clear();
            m_InputPropertyNodes.Clear();

            foreach (var fieldInfo in this.GetAllFields())
            {
                if (fieldInfo.GetValue(this) is PropertyPort propertyPort)
                {
                    propertyPort.OnAfterDeserialize();
                }
            }
        }
        public virtual void OnSpawn() { }
        public virtual void OnUnspawn() { }

        public virtual bool IsConnected(string name)
        {
            return m_PropertyPortMap.TryGetValue(name, out PropertyPort propertyPort) && (!string.IsNullOrEmpty(propertyPort.InputEdgeGUID) || propertyPort.OutputEdgeGUIDs.Count > 0);
        }

        // ── Blackboard 绑定 ──

        /// <summary>
        /// 绑定 Blackboard：将 NodeBlackboardData 中的 EP 引用注入到对应的 PropertyPort。
        /// 由 BaseTree.BindBlackboardContext 遍历调用。
        /// </summary>
        public virtual void BindBlackboard(NodeBlackboardData nodeData)
        {
            foreach (var kv in m_PropertyPortMap)
            {
                var port = kv.Value;
                BaseExposedProperty ep = null;

                if (port.Direction == PortDirection.Input)
                    nodeData.InputProperties.TryGetValue(port.Name, out ep);
                else
                    nodeData.OutputProperties.TryGetValue(port.Name, out ep);

                if (ep != null)
                    port.BindBlackboardEP(ep);
            }
        }

        /// <summary>
        /// 解绑 Blackboard：清除所有 PropertyPort 的 EP 引用。
        /// </summary>
        public virtual void UnbindBlackboard()
        {
            foreach (var kv in m_PropertyPortMap)
                kv.Value.UnbindBlackboardEP();
        }

        public static implicit operator bool(BaseNode exists) => exists != null;
    }
}