using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using Taco;

namespace TreeDesigner.Editor
{
    public class VariablePropertyNodeView : BaseNodeView
    {
        public VariablePropertyNodeView(BaseNode node, BaseTreeWindow treeWindow) : base(node, treeWindow, AssetDatabase.GUIDToAssetPath(DefaultVisualTreeGUID))
        {
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshVirablePropertyPorts();
        }
        public override void SyncSerializedPropertyPathes()
        {
            base.SyncSerializedPropertyPathes();

        }

        public override void OnInputPropertyPortConnected(PropertyPortView inputPropertyPortView)
        {
            base.OnInputPropertyPortConnected(inputPropertyPortView);
            m_Node.GetNewSerializedTree();
            Refresh();
        }
        public override void OnInputPropertyPortDisconnected(PropertyPortView inputPropertyPortView)
        {
            base.OnInputPropertyPortDisconnected(inputPropertyPortView);
            m_Node.GetNewSerializedTree();
            Refresh();
        }
        public override void OnOutputPropertyPortConnected(PropertyPortView outputPropertyPortView)
        {
            base.OnOutputPropertyPortConnected(outputPropertyPortView);
            m_Node.GetNewSerializedTree();
            Refresh();
        }
        public override void OnOutputPropertyPortDisconnected(PropertyPortView outputPropertyPortView)
        {
            base.OnOutputPropertyPortDisconnected(outputPropertyPortView);
            m_Node.GetNewSerializedTree();
            Refresh();
        }

        protected override void GeneratePropertyPorts()
        {
            base.GeneratePropertyPorts();
            foreach (var fieldInfo in m_Node.GetAllFields())
            {
                if (!m_Node.IsShow(fieldInfo.Name))
                    continue;

                var variablePropertyPortAttributes = fieldInfo.GetCustomAttributes<VariablePropertyPortAttribute>();
                if (variablePropertyPortAttributes.Count() > 0)
                {
                    VariablePropertyPortAttribute variablePropertyPortAttribute = variablePropertyPortAttributes.ElementAt(0);
                    PropertyPort propertyPort = fieldInfo.GetValue(m_Node) as PropertyPort;
                    switch (variablePropertyPortAttribute.Direction)
                    {
                        case PortDirection.Input:
                            m_InputPortContainer.AddVariablePropertyPort(propertyPort, variablePropertyPortAttribute.Name, variablePropertyPortAttribute.AcceptableTypesMethodName, variablePropertyPortAttribute.AcceptableTypes, Port.Capacity.Single);
                            break;
                        case PortDirection.Output:
                            m_OutputPortContainer.AddVariablePropertyPort(propertyPort, variablePropertyPortAttribute.Name, variablePropertyPortAttribute.AcceptableTypesMethodName, variablePropertyPortAttribute.AcceptableTypes, Port.Capacity.Multi);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        protected override void RefreshPropertyPorts()
        {
            base.RefreshPropertyPorts();
            foreach (var fieldInfo in m_Node.GetAllFields())
            {
                var variablePropertyPortAttributes = fieldInfo.GetCustomAttributes<VariablePropertyPortAttribute>();
                if (variablePropertyPortAttributes.Count() > 0)
                {
                    VariablePropertyPortAttribute variablePropertyPortAttribute = variablePropertyPortAttributes.ElementAt(0);
                    PropertyPort propertyPort = fieldInfo.GetValue(m_Node) as PropertyPort;
                    switch (variablePropertyPortAttribute.Direction)
                    {
                        case PortDirection.Input:
                            if (m_Node.IsShow(fieldInfo.Name) && !InputPropertyPorts.ContainsKey(fieldInfo.Name))
                                m_InputPortContainer.AddPropertyPort(propertyPort, variablePropertyPortAttribute.Name, Port.Capacity.Single);
                            else if (!m_Node.IsShow(fieldInfo.Name) && InputPropertyPorts.ContainsKey(fieldInfo.Name))
                                m_InputPortContainer.RemovePropertyPort(propertyPort);
                            break;
                        case PortDirection.Output:
                            if (m_Node.IsShow(fieldInfo.Name) && !OutputPropertyPorts.ContainsKey(fieldInfo.Name))
                                m_OutputPortContainer.AddPropertyPort(propertyPort, variablePropertyPortAttribute.Name, Port.Capacity.Multi);
                            else if (!m_Node.IsShow(fieldInfo.Name) && OutputPropertyPorts.ContainsKey(fieldInfo.Name))
                                m_OutputPortContainer.RemovePropertyPort(propertyPort);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        protected virtual void RefreshVirablePropertyPorts()
        {
            foreach (var item in InputPropertyPorts)
            {
                if (item.Value is VariablePropertyPortView variablePropertyPortView)
                {
                    variablePropertyPortView.SetPropertyPort(m_Node.PropertyPortMap[item.Key]);
                }
            }
            foreach (var item in OutputPropertyPorts)
            {
                if (item.Value is VariablePropertyPortView variablePropertyPortView)
                {
                    variablePropertyPortView.SetPropertyPort(m_Node.PropertyPortMap[item.Key]);
                }
            }
        }
    }
}