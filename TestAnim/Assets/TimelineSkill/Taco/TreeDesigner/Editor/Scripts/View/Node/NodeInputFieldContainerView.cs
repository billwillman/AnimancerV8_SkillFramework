using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Taco;

namespace TreeDesigner.Editor
{
    public class NodeInputFieldContainerView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<NodeInputFieldContainerView, UxmlTraits> { }

        protected BaseNode m_Node;
        protected BaseNodeView m_NodeView;
        protected Dictionary<string, VisualElement> m_FieldContainerMap = new Dictionary<string, VisualElement>();

        public NodeInputFieldContainerView()
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>("VisualTree/NodeInputFieldContainer");
            template.CloneTree(this);
            AddToClassList("nodeInputFieldContainer");
        }

        public void Init(BaseNode node, BaseNodeView nodeView)
        {
            m_Node = node;
            m_NodeView = nodeView;
            style.display = m_Node.Expanded ? DisplayStyle.Flex : DisplayStyle.None;
        }
        public void Refresh()
        {
            style.top = m_NodeView.InputPorts.Count > 0 ? 53 : 28;
            Clear();
            m_FieldContainerMap.Clear();
            foreach (var fieldInfo in m_Node.GetAllFields())
            {
                string fieldName = fieldInfo.Name;
                if (m_NodeView.InputPropertyPorts.ContainsKey(fieldName))
                {
                    var propertyPortAttributes = fieldInfo.GetCustomAttributes<PropertyPortAttribute>();
                    if (propertyPortAttributes.Count() > 0)
                    {
                        PropertyPortAttribute propertyPortAttribute = propertyPortAttributes.ElementAt(0);
                        PropertyPort propertyPort = fieldInfo.GetValue(m_Node) as PropertyPort;

                        if (propertyPortAttribute.Direction == PortDirection.Input)
                        {
                            if (propertyPort.GetType() == typeof(PropertyPort))
                            {
                                AddEmptyField(fieldName);
                            }
                            else if (propertyPort.ValueType.IsSubClassOfRawGeneric(typeof(List<>)))
                            {
                                AddEmptyField(fieldName);
                            }
                            else
                            {
                                SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty(fieldName).FindPropertyRelative("m_Value");
                                if (serializedProperty != null)
                                    AddPropertyPortField(serializedProperty, propertyPort);
                                else
                                    AddEmptyField(fieldName);
                            }
                        }
                        SetPropertyPortFieldEnable(fieldName, !m_Node.IsConnected(fieldName) && !m_Node.IsReadOnly(fieldName));
                    }

                    var variablePropertyPortAttributes = fieldInfo.GetCustomAttributes<VariablePropertyPortAttribute>();
                    if (variablePropertyPortAttributes.Count() > 0)
                    {
                        VariablePropertyPortAttribute variablePropertyPortAttribute = variablePropertyPortAttributes.ElementAt(0);
                        PropertyPort propertyPort = fieldInfo.GetValue(m_Node) as PropertyPort;

                        if (variablePropertyPortAttribute.Direction == PortDirection.Input)
                        {
                            if (propertyPort.GetType() == typeof(PropertyPort))
                            {
                                AddEmptyField(fieldName);
                            }
                            else if (propertyPort.ValueType.IsSubClassOfRawGeneric(typeof(List<>)))
                            {
                                AddEmptyField(fieldName);
                            }
                            else
                            {
                                SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty(fieldName).FindPropertyRelative("m_Value");
                                if (serializedProperty != null)
                                    AddPropertyPortField(serializedProperty, propertyPort);
                            }
                            SetPropertyPortFieldEnable(fieldName, !m_Node.IsConnected(fieldName) && !m_Node.IsReadOnly(fieldName));
                        }
                    }
                }
            }
            Sort();
        }
        public void Rebind()
        {
            foreach (var item in m_FieldContainerMap)
            {
                if (item.Value.Q<PropertyField>() is PropertyField propertyField)
                {
                    propertyField.Unbind();
                    SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty(item.Key).FindPropertyRelative("m_Value");
                    propertyField.bindingPath = serializedProperty.propertyPath;
                    propertyField.Bind(m_Node.GetSerializedTree());
                }
            }
        }
        public void Sort()
        {
            foreach (var fieldPair in m_FieldContainerMap)
            {
                Remove(fieldPair.Value);
            }
            m_FieldContainerMap = m_FieldContainerMap.OrderBy(i => m_NodeView.InputPropertyPorts.Keys.ToList().IndexOf(i.Key)).ToDictionary(i => i.Key, i => i.Value);
            foreach (var fieldPair in m_FieldContainerMap)
            {
                Add(fieldPair.Value);
            }
        }
        public void AddEmptyField(string fieldName)
        {
            VisualElement container = new VisualElement();
            container.name = "propertyFieldContainer";
            container.pickingMode = PickingMode.Ignore;
            container.style.width = 0;
            Add(container);
            m_FieldContainerMap.Add(fieldName, container);
        }
        public void AddPropertyPortField(SerializedProperty serializedProperty, PropertyPort propertyPort)
        {
            VisualElement container = new VisualElement();
            container.name = "propertyFieldContainer";
            container.pickingMode = PickingMode.Ignore;
            container.RegisterCallback<MouseDownEvent>((e) => e.StopPropagation());

            PropertyField propertyField = new PropertyField(serializedProperty, string.Empty);
            propertyField.Bind(serializedProperty.serializedObject);
            propertyField.style.borderTopColor = propertyField.style.borderBottomColor = propertyField.style.borderLeftColor = propertyField.style.borderRightColor = propertyPort.Color();
            container.Add(propertyField);

            Add(container);
            m_FieldContainerMap.Add(propertyPort.Name, container);
        }
        public void SetPropertyPortFieldEnable(string name, bool enable)
        {
            if (m_FieldContainerMap.TryGetValue(name, out VisualElement propertyField))
            {
                if (enable)
                {
                    if (propertyField.childCount > 0)
                        propertyField.Children().ElementAt(0).style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (propertyField.childCount > 0)
                        propertyField.Children().ElementAt(0).style.display = DisplayStyle.None;
                }
            }
        }
    }
}