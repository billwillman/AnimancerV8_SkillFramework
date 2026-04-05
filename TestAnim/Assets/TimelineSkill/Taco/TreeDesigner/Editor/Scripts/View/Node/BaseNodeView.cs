using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using Taco;

namespace TreeDesigner.Editor
{
    public class BaseNodeView : Node, IGroupable
    {
        public const string DefaultVisualTreeGUID = "5eec7eeaaa8d8374181513f90c706047";
        public const string StyleSheetGUID = "f24502238ee8ac5478af96e8894528ee";
        static Regex s_ReplaceNodeIndexPropertyPath = new Regex(@"(^m_Nodes.Array.data\[)(\d+)(\])");

        protected BaseNode m_Node;
        public BaseNode Node => m_Node;

        protected BaseTreeWindow m_TreeWindow;
        protected VisualElement m_NodeBorder;
        protected VisualElement m_SelectionBorder;
        protected VisualElement m_Top;
        protected NodePortContainerView m_InputPortContainer;
        protected NodePortContainerView m_OutputPortContainer;
        protected NodePanelView m_NodePanel;
        protected NodeInputFieldContainerView m_NodeInputFieldContainer;
        public NodeInputFieldContainerView NodeInputFieldContainer => m_NodeInputFieldContainer;

        protected NodeGroupView m_NodeGroupView;
        public NodeGroupView NodeGroupView { get => m_NodeGroupView; set => m_NodeGroupView = value; }

        protected StackNodeView m_StackNodeView;
        public StackNodeView StackNodeView
        {
            get => m_StackNodeView;
            set
            {
                m_StackNodeView = value;

                RemoveFromClassList("stacked");
                if (m_StackNodeView != null)
                    AddToClassList("stacked");
            }
        }

        bool execute;

        public Dictionary<string, BasePortView> InputPorts => m_InputPortContainer.PortViewMap;
        public Dictionary<string, BasePortView> OutputPorts => m_OutputPortContainer.PortViewMap;
        public Dictionary<string, PropertyPortView> InputPropertyPorts => m_InputPortContainer.PropertyPortViewMap;
        public Dictionary<string, PropertyPortView> OutputPropertyPorts => m_OutputPortContainer.PropertyPortViewMap;

        public BaseTreeView TreeView => m_TreeWindow.TreeView;

        public BaseNodeView(BaseNode node, BaseTreeWindow treeWindow) : this(node, treeWindow, AssetDatabase.GUIDToAssetPath(DefaultVisualTreeGUID))
        {
        }
        public BaseNodeView(BaseNode node, BaseTreeWindow treeWindow, string path) : base(path)
        {
            m_Node = node;
            m_TreeWindow = treeWindow;
            m_NodeBorder = this.Q("node-border");
            m_SelectionBorder = this.Q("node-selection-border");
            m_Top = this.Q("top");

            m_InputPortContainer = inputContainer as NodePortContainerView;
            m_InputPortContainer.Init(m_Node, this);

            m_OutputPortContainer = outputContainer as NodePortContainerView;
            m_OutputPortContainer.Init(m_Node, this);

            m_NodePanel = this.Q<NodePanelView>();
            m_NodePanel.Init(m_Node, this);

            m_NodeInputFieldContainer = this.Q<NodeInputFieldContainerView>();
            m_NodeInputFieldContainer.Init(m_Node, this);

            //NodeNameAttribute nodeName = m_Node.GetAttribute<NodeNameAttribute>();
            //if(nodeName != null)
            //    title = nodeName.Name;
            title = NodeName();

            NodeColorAttribute nodeColor = m_Node.GetAttribute<NodeColorAttribute>();
            if (nodeColor != null)
                titleContainer.style.backgroundColor = nodeColor.Color / 255f;

            RefreshCapabilities();

            viewDataKey = m_Node.GUID;
            expanded = m_Node.Expanded;
            style.left = m_Node.Position.x;
            style.top = m_Node.Position.y;

            GeneratePorts();
            GeneratePropertyPorts();
            Refresh();
            SortPropertyPorts();
            RefreshNodeExpandedState();

            //inputContainer.RegisterCallback<MouseEnterEvent>((e) =>
            //{
            //    if(TreeView.Drag)
            //        Debug.Log($"Enter {title}");
            //});
            //inputContainer.RegisterCallback<MouseLeaveEvent>((e) =>
            //{
            //    if (TreeView.Drag)
            //        Debug.Log($"Leave {title}");
            //});

            this.Q("panel-button").AddManipulator(new Clickable(ToggleShowPanel));

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_Node.OnNodeChanged = () =>
            {
                RefreshPropertyPorts();
                Refresh();
                RefreshNodeExpandedState();
                SortPropertyPorts();
            };

            if(m_Node is RunnableNode runnableNode)
            {
                runnableNode.OnUpdateCallback = Update;
                runnableNode.OnStartCallback = () => execute = true;
                runnableNode.OnResetCallback = () => execute = false;
            }
            schedule.Execute(Update);
        }


        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is BaseNodeView bnv)
            {
                //if (bnv.Node is StartAbilityNode san)
                //{
                //    evt.menu.AppendAction("Open Ability Window", (s) =>
                //   {
                //       OpenAbilityWindow(san);
                //   });
                //}
                if (TreeView.selection.Contains(this))
                {
                    evt.menu.AppendAction("Select Node Script", (s) =>
                    {
                        SelectNodeScript();
                    });
                    evt.menu.AppendAction("Select NodeView Script", (s) =>
                    {
                        SelectNodeViewScript();
                    });
                    evt.menu.AppendAction("Open Node Script", (s) =>
                    {
                        OpenNodeScript();
                    });
                    evt.menu.AppendAction("Open NodeView Script", (s) =>
                    {
                        OpenNodeViewScript();
                    });
                    evt.menu.AppendSeparator();
                }

                List<BaseNodeView> canShowNodeViews = new List<BaseNodeView>();
                List<BaseNodeView> canHideNodeViews = new List<BaseNodeView>();
                foreach (var element in TreeView.selection)
                {
                    if (element is BaseNodeView nodeView)
                    {
                        if (nodeView.CanShowPanel())
                        {
                            if (nodeView.Node.ShowPanel)
                                canHideNodeViews.Add(nodeView);
                            else
                                canShowNodeViews.Add(nodeView);
                        }
                    }
                }
                evt.menu.AppendAction("Show Panel", delegate
                {
                    canShowNodeViews.ForEach(i => i.ToggleShowPanel());
                }, (DropdownMenuAction a) => canShowNodeViews.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Hide Panel", delegate
                {
                    canHideNodeViews.ForEach(i => i.ToggleShowPanel());
                }, (DropdownMenuAction a) => canHideNodeViews.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendSeparator();
            }
        }

        public virtual void Update()
        {
            if (m_Node is RunnableNode runnableNode && execute)
            {
                switch (runnableNode.State)
                {
                    case State.None:
                        m_Top.style.backgroundColor = m_LastColor = new Color(0, 0, 0, 0);
                        AddToClassList("nodeState-None");
                        RemoveFromClassList("nodeState-Running");
                        RemoveFromClassList("nodeState-Success");
                        RemoveFromClassList("nodeState-Failure");
                        break;
                    case State.Running:
                        m_Top.style.backgroundColor = m_LastColor = m_RunningColor;
                        AddToClassList("nodeState-Running");
                        RemoveFromClassList("nodeState-None");
                        RemoveFromClassList("nodeState-Success");
                        RemoveFromClassList("nodeState-Failure");
                        break;
                    case State.Success:
                        m_Top.style.backgroundColor = m_LastColor = m_SuccessColor;
                        AddToClassList("nodeState-Success");
                        RemoveFromClassList("nodeState-None");
                        RemoveFromClassList("nodeState-Running");
                        RemoveFromClassList("nodeState-Failure");
                        break;
                    case State.Failure:
                        m_Top.style.backgroundColor = m_LastColor = m_FailureColor;
                        AddToClassList("nodeState-Failure");
                        RemoveFromClassList("nodeState-None");
                        RemoveFromClassList("nodeState-Running");
                        RemoveFromClassList("nodeState-Success");
                        break;
                    default:
                        break;
                }
                // m_Top.style.backgroundColor = m_LastColor = m_RunningColor;
                m_AnimationFrame = m_AnimationDuration;
            }
            else
            {
                m_Top.style.backgroundColor = m_LastColor = new Color(0, 0, 0, 0);
                AddToClassList("nodeState-None");
                RemoveFromClassList("nodeState-Running");
                RemoveFromClassList("nodeState-Success");
                RemoveFromClassList("nodeState-Failure");
            }
            // if (m_LastState != m_Node.State)
            // {
            //     switch (m_Node.State)
            //     {
            //         case State.None:
            //             m_Top.style.backgroundColor = m_LastColor = new Color(0, 0, 0, 0);
            //             break;
            //         case State.Running:
            //             m_Top.style.backgroundColor = m_LastColor = m_RunningColor;
            //             break;
            //         case State.Success:
            //             m_Top.style.backgroundColor = m_LastColor = m_SuccessColor;
            //             break;
            //         case State.Failure:
            //             m_Top.style.backgroundColor = m_LastColor = m_FailureColor;
            //             break;
            //         default:
            //             break;
            //     }
            //     m_AnimationFrame = m_AnimationDuration;
            //     m_LastState = m_Node.State;
            // }


            //RemoveFromClassList("nodeState-None");
            //RemoveFromClassList("nodeState-Running");
            //RemoveFromClassList("nodeState-Success");
            //RemoveFromClassList("nodeState-Failure");

            //if (m_Node is RunnableNode runnableNode)
            //{
            //    switch (runnableNode.State)
            //    {
            //        case State.None:
            //            AddToClassList("nodeState-None");
            //            RemoveFromClassList("nodeState-Running");
            //            RemoveFromClassList("nodeState-Success");
            //            RemoveFromClassList("nodeState-Failure");
            //            break;
            //        case State.Running:
            //            AddToClassList("nodeState-Running");
            //            RemoveFromClassList("nodeState-None");
            //            RemoveFromClassList("nodeState-Success");
            //            RemoveFromClassList("nodeState-Failure");
            //            break;
            //        case State.Success:
            //            AddToClassList("nodeState-Success");
            //            RemoveFromClassList("nodeState-None");
            //            RemoveFromClassList("nodeState-Running");
            //            RemoveFromClassList("nodeState-Failure");
            //            break;
            //        case State.Failure:
            //            AddToClassList("nodeState-Failure");
            //            RemoveFromClassList("nodeState-None");
            //            RemoveFromClassList("nodeState-Running");
            //            RemoveFromClassList("nodeState-Success");
            //            break;
            //        default:
            //            break;
            //    }
            //}

            m_InputPortContainer.Update();
            m_OutputPortContainer.Update();
        }

        int m_AnimationDuration = 60;
        Color m_RunningColor = new Color(242, 210, 63, 255) / 255;
        Color m_SuccessColor = new Color(65, 172, 66, 255) / 255;
        Color m_FailureColor = new Color(234, 65, 76, 255) / 255;

        int m_AnimationFrame;
        State m_LastState;
        Color m_LastColor;
        public virtual void Animation()
        {
            if (m_AnimationFrame > 0)
            {
                m_Top.style.backgroundColor = Color.Lerp(m_LastColor, new Color(0, 0, 0, 0), (float)(m_AnimationDuration - m_AnimationFrame) / m_AnimationDuration);
                m_AnimationFrame--;
            }
        }

        public virtual void Refresh()
        {
            m_NodePanel.Refresh();
            m_NodeInputFieldContainer.Refresh();
            RefreshShowPanelState();
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void SyncSerializedPropertyPathes()
        {
            //int nodeIndex = m_Node.GameplayTagContainerOwner.Nodes.FindIndex(i => i == m_Node);
            //if (nodeIndex == -1)
            //    return;

            //var nodeIndexString = nodeIndex.ToString();
            //foreach (var propertyField in this.Query<PropertyField>().ToList())
            //{
            //    propertyField.Unbind();
            //    propertyField.bindingPath = s_ReplaceNodeIndexPropertyPath.Replace(propertyField.bindingPath, m => m.Groups[1].Value + nodeIndexString + m.Groups[3].Value);
            //    propertyField.BindProperty(m_Node.GetSerializedTree());
            //}

            m_NodePanel.Refresh();
            m_NodeInputFieldContainer.Refresh();
        }

        public virtual void OnMoved(Vector2 position)
        {
            if (m_Node.Position != position)
            {
                m_Node.ApplyModify("Move Node", () =>
                {
                    m_Node.Position = position;
                    m_Node.OnMoved();
                    m_NodeGroupView?.OnMoved();
                });
            }
        }
        public virtual void OnInputPortConnected(BasePortView portView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnInputPortDisconnected(BasePortView portView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnOutputPortConnected(BasePortView portView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnOutputPortDisconnected(BasePortView portView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnInputPropertyPortConnected(PropertyPortView inputPropertyPortView)
        {
            m_NodePanel.SetPropertyPortFieldEnable(inputPropertyPortView.Name, false);
            m_NodeInputFieldContainer.SetPropertyPortFieldEnable(inputPropertyPortView.Name, false);
            schedule.Execute(RefreshCollapseButton);

            PropertyPortOnLinkedAttribute propertyPortOnLinkedAttribute = m_Node.GetFieldAttribute<PropertyPortOnLinkedAttribute>(inputPropertyPortView.Name);
            if (propertyPortOnLinkedAttribute != null)
            {
                MethodInfo methodInfo = m_Node.GetMethod(propertyPortOnLinkedAttribute.CallbackName);
                if (methodInfo != null)
                    methodInfo.Invoke(m_Node, null);
            }
        }
        public virtual void OnInputPropertyPortDisconnected(PropertyPortView inputPropertyPortView)
        {
            m_NodePanel.SetPropertyPortFieldEnable(inputPropertyPortView.Name, true);
            m_NodeInputFieldContainer.SetPropertyPortFieldEnable(inputPropertyPortView.Name, true);
            schedule.Execute(RefreshCollapseButton);

            PropertyPortOnUnlinkedAttribute propertyPortOnLinkedAttribute = m_Node.GetFieldAttribute<PropertyPortOnUnlinkedAttribute>(inputPropertyPortView.Name);
            if (propertyPortOnLinkedAttribute != null)
            {
                MethodInfo methodInfo = m_Node.GetMethod(propertyPortOnLinkedAttribute.CallbackName);
                if (methodInfo != null)
                    methodInfo.Invoke(m_Node, null);
            }
        }
        public virtual void OnOutputPropertyPortConnected(PropertyPortView outputPropertyPortView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnOutputPropertyPortDisconnected(PropertyPortView outputPropertyPortView)
        {
            schedule.Execute(RefreshCollapseButton);
        }

        public override bool expanded
        {
            get => base.expanded;
            set
            {
                base.expanded = value;
                RefreshNodeExpandedState();
            }
        }
        protected override void ToggleCollapse()
        {
            if (CanCollapsed())
            {
                base.ToggleCollapse();
                m_Node.ApplyModify("SetExpandedState Node", () =>
                {
                    m_Node.Expanded = expanded;
                });

                if (m_StackNodeView == null)
                    BringToFront();
            }
        }
        protected virtual bool CanCollapsed()
        {
            //List<Port> inputPorts = inputContainer.Query<Port>().ToList();
            //List<Port> outputPorts = outputContainer.Query<Port>().ToList();
            //foreach (var item in inputPorts)
            //{
            //    if (!item.connected)
            //        return true;
            //}
            //foreach (var item in outputPorts)
            //{
            //    if (!item.connected)
            //        return true;
            //}
            //return false;
            return true;
        }
        protected virtual void RefreshCollapseButton()
        {
            bool flag = false;
            List<Port> list = inputContainer.Query<Port>().ToList();
            List<Port> list2 = outputContainer.Query<Port>().ToList();
            foreach (Port item in list)
            {
                if (!item.connected)
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                foreach (Port item2 in list2)
                {
                    if (!item2.connected)
                    {
                        flag = true;
                        break;
                    }
                }
            }

            if (m_CollapseButton != null)
            {
                if (flag)
                {
                    m_CollapseButton.SetEnabled(!m_CollapseButton.enabledSelf);
                    m_CollapseButton.SetEnabled(true);
                }
                else
                {
                    m_CollapseButton.SetEnabled(!m_CollapseButton.enabledSelf);
                    m_CollapseButton.SetEnabled(false);
                }
            }
        }
        public virtual void RefreshNodeExpandedState()
        {
            RefreshExpandedState();
            m_NodeInputFieldContainer.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            m_NodePanel.style.top = this.Query<BasePortView>().ToList().Count > 0 ? 26 : 0;
        }
        public virtual void RefreshCapabilities()
        {
            capabilities = (Capabilities)m_Node.Capabilities;
        }

        protected virtual void ToggleShowPanel()
        {
            m_Node.ApplyModify("Switch Node Panel", () =>
            {
                m_Node.ShowPanel = !m_Node.ShowPanel;
                RefreshShowPanelState();
            });

            if (m_StackNodeView == null)
                BringToFront();
        }
        protected virtual bool CanShowPanel()
        {
            return m_NodePanel.PropertyCount > 0;
        }
        public virtual void RefreshShowPanelState()
        {
            if (CanShowPanel())
            {
                this.Q("title-label").style.borderRightWidth = 1;
                this.Q("panel-button-container").style.visibility = Visibility.Visible;
            }
            else
            {
                this.Q("title-label").style.borderRightWidth = 0;
                this.Q("panel-button-container").style.visibility = Visibility.Hidden;
            }

            RemoveFromClassList("showPanel");
            RemoveFromClassList("hidePanel");
            if (m_Node.ShowPanel && CanShowPanel())
            {
                m_NodePanel.style.visibility = Visibility.Visible;
                AddToClassList("showPanel");
            }
            else
            {
                m_NodePanel.style.visibility = Visibility.Hidden;
                AddToClassList("hidePanel");
            }
        }

        protected virtual void GeneratePorts()
        {
            foreach (var inputAttribute in m_Node.GetAttributes<InputAttribute>())
            {
                m_InputPortContainer.AddPort(inputAttribute.Name, Direction.Input, Port.Capacity.Single);
            }
            foreach (var outputAttribute in m_Node.GetAttributes<OutputAttribute>())
            {
                m_OutputPortContainer.AddPort(outputAttribute.Name, Direction.Output, (Port.Capacity)outputAttribute.Capacity);
            }
        }
        protected virtual void GeneratePropertyPorts()
        {
            foreach (var fieldInfo in m_Node.GetAllFields())
            {
                if (!m_Node.IsShow(fieldInfo.Name))
                    continue;

                var propertyPortAttributes = fieldInfo.GetCustomAttributes<PropertyPortAttribute>();
                if (propertyPortAttributes.Count() > 0)
                {
                    PropertyPortAttribute propertyPortAttribute = propertyPortAttributes.ElementAt(0);
                    PropertyPort propertyPort = fieldInfo.GetValue(m_Node) as PropertyPort;
                    switch (propertyPortAttribute.Direction)
                    {
                        case PortDirection.Input:
                            m_InputPortContainer.AddPropertyPort(propertyPort, propertyPortAttribute.Name, Port.Capacity.Single);
                            break;
                        case PortDirection.Output:
                            m_OutputPortContainer.AddPropertyPort(propertyPort, propertyPortAttribute.Name, Port.Capacity.Multi);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        protected virtual void RefreshPropertyPorts()
        {
            foreach (var fieldInfo in m_Node.GetAllFields())
            {
                var propertyPortAttributes = fieldInfo.GetCustomAttributes<PropertyPortAttribute>();
                if (propertyPortAttributes.Count() > 0)
                {
                    PropertyPortAttribute propertyPortAttribute = propertyPortAttributes.ElementAt(0);
                    PropertyPort propertyPort = fieldInfo.GetValue(m_Node) as PropertyPort;
                    switch (propertyPortAttribute.Direction)
                    {
                        case PortDirection.Input:
                            if (m_Node.IsShow(fieldInfo.Name) && !InputPropertyPorts.ContainsKey(fieldInfo.Name))
                                m_InputPortContainer.AddPropertyPort(propertyPort, propertyPortAttribute.Name, Port.Capacity.Single);
                            else if (!m_Node.IsShow(fieldInfo.Name) && InputPropertyPorts.ContainsKey(fieldInfo.Name))
                                m_InputPortContainer.RemovePropertyPort(propertyPort);
                            break;
                        case PortDirection.Output:
                            if (m_Node.IsShow(fieldInfo.Name) && !OutputPropertyPorts.ContainsKey(fieldInfo.Name))
                                m_OutputPortContainer.AddPropertyPort(propertyPort, propertyPortAttribute.Name, Port.Capacity.Multi);
                            else if (!m_Node.IsShow(fieldInfo.Name) && OutputPropertyPorts.ContainsKey(fieldInfo.Name))
                                m_OutputPortContainer.RemovePropertyPort(propertyPort);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        protected virtual void SortPropertyPorts()
        {
            m_InputPortContainer.Sort();
            m_OutputPortContainer.Sort();
        }
        protected virtual void OnGeometryChanged(GeometryChangedEvent geometryChangedEvent)
        {

        }

        string NodeName()
        {
            NodeNameAttribute nodeNameAttribute = m_Node.GetAttribute<NodeNameAttribute>();
            if (nodeNameAttribute != null)
            {
                MethodInfo methodInfo = m_Node.GetMethod(nodeNameAttribute.Name);
                if (methodInfo != null)
                    return (string)methodInfo.Invoke(m_Node, null);
                else
                    return nodeNameAttribute.Name;
            }
            return m_Node.GetType().Name;
        }

        void SelectNodeScript()
        {
            var scriptInfo = TreeDesignerUtility.GetNodeScript(m_Node.GetType());
            if (scriptInfo != null)
                Selection.activeObject = scriptInfo.Mono;
        }
        void OpenNodeScript()
        {
            var scriptInfo = TreeDesignerUtility.GetNodeScript(m_Node.GetType());
            if (scriptInfo != null)
                AssetDatabase.OpenAsset(scriptInfo.Mono.GetInstanceID(), scriptInfo.LineNumber, scriptInfo.ColumnNumber);
        }
        void SelectNodeViewScript()
        {
            NodeViewAttribute nodeViewAttribute = m_Node.GetAttribute<NodeViewAttribute>();
            if (nodeViewAttribute != null)
            {
                var script = TreeDesignerUtility.GetNodeViewScript(TreeDesignerUtility.GetNodeViewType(nodeViewAttribute.NodeViewTypeName));
                if (script != null)
                    Selection.activeObject = script;
            }
            else
            {
                var script = TreeDesignerUtility.GetNodeViewScript(typeof(BaseNodeView));
                if (script != null)
                    Selection.activeObject = script;
            }
        }
        void OpenNodeViewScript()
        {
            NodeViewAttribute nodeViewAttribute = m_Node.GetAttribute<NodeViewAttribute>();
            if (nodeViewAttribute != null)
            {
                var script = TreeDesignerUtility.GetNodeViewScript(TreeDesignerUtility.GetNodeViewType(nodeViewAttribute.NodeViewTypeName));
                if (script != null)
                    AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
            }
            else
            {
                var script = TreeDesignerUtility.GetNodeViewScript(typeof(BaseNodeView));
                if (script != null)
                    AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
            }
        }

        //void OpenAbilityWindow(StartAbilityNode san)
        //{
        //    string abilityName = san.AbilityName;
        //    string[] guids = AssetDatabase.FindAssets($"t:Ability {abilityName}", new string[] { "Assets/Datas/Ability" });
        //    if (guids.Length != 1)
        //    {
        //        Debug.LogError($"no this Ability! t:Ability name:{abilityName}");
        //        return;
        //    }
        //    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        //    BaseTree bt = AssetDatabase.LoadAssetAtPath<BaseTree>(path);
        //    bt?.OpenTree();
        //}
    }
}