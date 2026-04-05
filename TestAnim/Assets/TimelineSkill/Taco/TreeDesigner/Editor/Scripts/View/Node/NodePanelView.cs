using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using Taco;
using Taco.Editor;

namespace TreeDesigner.Editor
{
    public class NodePanelView : GraphElement
    {
        public new class UxmlFactory : UxmlFactory<NodePanelView, UxmlTraits> { }

        protected BaseNode m_Node;
        protected BaseNodeView m_NodeView;
        protected VisualElement m_Container;
        protected Dictionary<string, VisualElement> m_FieldMap = new Dictionary<string, VisualElement>();
        protected Dictionary<FieldInfo, object> m_ValueMap = new Dictionary<FieldInfo, object>();

        protected bool m_Expanded;

        public int PropertyCount => m_Container.childCount;

        public NodePanelView()
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>("VisualTree/NodePanel");
            template.CloneTree(this);
            AddToClassList("nodePanel");
            m_Container = this.Q("container");
            this.AddManipulator(new Clickable(() => 
            {
                m_Expanded = !m_Expanded;
                foreach (var fieldPair in m_FieldMap)
                {
                    if (fieldPair.Value.Q<Foldout>() is Foldout foldout)
                        foldout.SetValueWithoutNotify(m_Expanded);
                }
            }));
            this.AddManipulator(new DragLineManipulator(DragLineDirection.Right,(f) => 
            {
                style.width = Mathf.Max(style.width.value.value + f.x, style.minWidth.value.value);
            }));

            style.width = 0;
        }


        public void Init(BaseNode node, BaseNodeView nodeView)
        {
            m_Node = node;
            m_NodeView = nodeView;
        }
        public void Refresh()
        {
            ClearFields();
            foreach (var fieldInfo in m_Node.GetAllFields())
            {
                string fieldName = fieldInfo.Name;

                if (!m_Node.IsShow(fieldName))
                    continue;

                var propertyPortAttributes = fieldInfo.GetCustomAttributes<PropertyPortAttribute>();
                var variablePropertyPortAttributes = fieldInfo.GetCustomAttributes<VariablePropertyPortAttribute>();
                var showInPanelAttributes = fieldInfo.GetCustomAttributes<ShowInPanelAttribute>();
                var enumMenuAttributes = fieldInfo.GetCustomAttributes<EnumMenuAttribute>();
                var toggleAttributes = fieldInfo.GetCustomAttributes<ToggleAttribute>();

                if (propertyPortAttributes.Count() > 0)
                {
                    PropertyPortAttribute propertyPortAttribute = propertyPortAttributes.ElementAt(0);
                    PropertyPort propertyPort = fieldInfo.GetValue(m_Node) as PropertyPort;

                    if (propertyPort.GetType() == typeof(PropertyPort))
                    {

                    }
                    else
                    {
                        SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty(fieldName).FindPropertyRelative("m_Value");
                        if(serializedProperty != null)
                        {
                            PropertyField propertyField = AddPropertyPortField(serializedProperty, propertyPort, propertyPortAttribute.Name);

                            var onValueChangedAttributes = fieldInfo.GetCustomAttributes<OnValueChangedAttribute>();
                            if (onValueChangedAttributes.Count() > 0 && onValueChangedAttributes.ElementAt(0) is OnValueChangedAttribute onValueChangedAttribute)
                            {
                                FieldInfo valueFieldInfo = propertyPort.GetField("m_Value");
                                m_ValueMap.Add(valueFieldInfo, valueFieldInfo.GetValue(propertyPort));

                                propertyField.RegisterValueChangeCallback(i =>
                                {
                                    if (!valueFieldInfo.GetValue(propertyPort).Equals(m_ValueMap[valueFieldInfo]))
                                    {
                                        m_ValueMap[valueFieldInfo] = valueFieldInfo.GetValue(propertyPort);
                                        MethodInfo methodInfo = m_Node.GetMethod(onValueChangedAttribute.CallbackName);
                                        methodInfo?.Invoke(m_Node, null);
                                    }
                                });
                            }
                        }
                    }
                    if (propertyPort.Direction == PortDirection.Input)
                        SetPropertyPortFieldEnable(propertyPort.Name, !m_Node.IsConnected(propertyPort.Name));
                    else
                        SetPropertyPortFieldEnable(propertyPort.Name, true);
                }
                else if (variablePropertyPortAttributes.Count() > 0)
                {
                    VariablePropertyPortAttribute variablePropertyPortAttribute = variablePropertyPortAttributes.ElementAt(0);                   
                    PropertyPort propertyPort = fieldInfo.GetValue(m_Node) as PropertyPort;

                    if (propertyPort.GetType() == typeof(PropertyPort))
                    {

                    }
                    else
                    {
                        SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty(fieldName).FindPropertyRelative("m_Value");
                        if (serializedProperty != null)
                        {
                            PropertyField propertyField = AddPropertyPortField(serializedProperty, propertyPort, variablePropertyPortAttribute.Name);

                            var onValueChangedAttributes = fieldInfo.GetCustomAttributes<OnValueChangedAttribute>();
                            if (onValueChangedAttributes.Count() > 0 && onValueChangedAttributes.ElementAt(0) is OnValueChangedAttribute onValueChangedAttribute)
                            {
                                FieldInfo valueFieldInfo = propertyPort.GetField("m_Value");
                                m_ValueMap.Add(valueFieldInfo, valueFieldInfo.GetValue(propertyPort));

                                propertyField.RegisterValueChangeCallback(i =>
                                {
                                    if (!valueFieldInfo.GetValue(propertyPort).Equals(m_ValueMap[valueFieldInfo]))
                                    {
                                        m_ValueMap[valueFieldInfo] = valueFieldInfo.GetValue(propertyPort);
                                        MethodInfo methodInfo = m_Node.GetMethod(onValueChangedAttribute.CallbackName);
                                        methodInfo?.Invoke(m_Node, null);
                                    }
                                });
                            }
                        }
                    }
                    if (propertyPort.Direction == PortDirection.Input)
                        SetPropertyPortFieldEnable(propertyPort.Name, !m_Node.IsConnected(propertyPort.Name));
                    else
                        SetPropertyPortFieldEnable(propertyPort.Name, true);
                }
                else if (showInPanelAttributes.Count() > 0)
                {
                    ShowInPanelAttribute showInPanelAttribute = showInPanelAttributes.ElementAt(0);
                    PropertyField propertyField = AddBaseField(fieldName, showInPanelAttribute.Label);

                    var onValueChangedAttributes = fieldInfo.GetCustomAttributes<OnValueChangedAttribute>();
                    if (onValueChangedAttributes.Count() > 0 && onValueChangedAttributes.ElementAt(0) is OnValueChangedAttribute onValueChangedAttribute)
                    {
                        m_ValueMap.Add(fieldInfo, fieldInfo.GetValue(m_Node));

                        propertyField.RegisterValueChangeCallback(i =>
                        {
                            if (!fieldInfo.GetValue(m_Node).Equals(m_ValueMap[fieldInfo]))
                            {
                                m_ValueMap[fieldInfo] = fieldInfo.GetValue(m_Node);
                                MethodInfo methodInfo = m_Node.GetMethod(onValueChangedAttribute.CallbackName);
                                methodInfo?.Invoke(m_Node, null);
                            }
                        });
                    }
                }
                else if (enumMenuAttributes.Count() > 0)
                {
                    EnumMenuAttribute enumMenuAttribute = enumMenuAttributes.ElementAt(0);
                    EnumMenuView enumMenuView = new EnumMenuView();
                    enumMenuView.Init(fieldInfo.GetValue(m_Node), enumMenuAttribute.Label, (o) =>
                    {
                        m_Node.ApplyModify("ChangeNodeValue", () =>
                        {
                            fieldInfo.SetValue(m_Node, o);
                            MethodInfo methodInfo = m_Node.GetMethod(enumMenuAttribute.CallbackName);
                            methodInfo?.Invoke(m_Node, null);
                        });
                    });
                    AddField(fieldName, enumMenuView);
                }
                else if (toggleAttributes.Count() > 0)
                {
                    ToggleAttribute toggleAttribute = toggleAttributes.ElementAt(0);
                    Toggle toggle = new Toggle(toggleAttribute.Label);
                    toggle.style.height = 21;
                    toggle.style.marginTop = toggle.style.marginBottom = 2;
                    toggle.labelElement.style.minWidth = 50;
                    toggle.labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
                    toggle.value = (bool)fieldInfo.GetValue(m_Node);
                    toggle.RegisterValueChangedCallback((i) =>
                    {
                        m_Node.ApplyModify("ChangeNodeValue", () =>
                        {
                            fieldInfo.SetValue(m_Node, i.newValue);
                            MethodInfo methodInfo = m_Node.GetMethod(toggleAttribute.CallbackName);
                            methodInfo.Invoke(m_Node, null);
                        });
                    });
                    AddField(fieldName, toggle);
                }
            }
        }
        public void Rebind()
        {
            foreach (var item in m_FieldMap)
            {
                if(item.Value is PropertyField propertyField)
                {
                    propertyField.Unbind();
                    SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty(item.Key);
                    if (m_Node.GetField(item.Key).GetValue(m_Node) is PropertyPort)
                        serializedProperty = serializedProperty.FindPropertyRelative("m_Value");
                    propertyField.bindingPath = serializedProperty.propertyPath;
                    propertyField.Bind(m_Node.GetSerializedTree());
                }
            }
        }

        public PropertyField AddBaseField(string fieldName, string labelName)
        {
            SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty(fieldName);
            PropertyField field = new PropertyField(serializedProperty, labelName);
            field.Bind(serializedProperty.serializedObject);
            AddField(fieldName, field);
            field.SetEnabled(!m_Node.IsReadOnly(fieldName));
            return field;
        }
        public PropertyField AddPropertyPortField(SerializedProperty serializedProperty, PropertyPort propertyPort, string labelName)
        {
            PropertyField field = new PropertyField(serializedProperty, labelName);
            field.Bind(serializedProperty.serializedObject);
            AddField(propertyPort.Name, field);
            return field;
        }
        public void SetPropertyPortFieldEnable(string name, bool enable)
        {
            if (m_FieldMap.TryGetValue(name, out VisualElement field))
            {
                if (enable && (m_Node.GetNodeSerializedProperty(name) == null || !m_Node.IsReadOnly(name)))
                    field.SetEnabled(true);
                else
                    field.SetEnabled(false);
            }
        }

        public void AddField(string name, VisualElement field)
        {
            m_Container.Add(field);
            m_FieldMap.Add(name, field);
            field.name = "nodePanelField";
            field.SetEnabled(true);
        }
        void ClearFields()
        {
            m_Container.Clear();
            m_FieldMap.Clear();
            m_ValueMap.Clear();
        }
    }
}