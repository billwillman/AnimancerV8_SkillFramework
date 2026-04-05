using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace TreeDesigner.Editor
{
    public class BaseTreeWindow : EditorWindow
    {
        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;
        
        protected BaseTreeView m_TreeView;
        public BaseTreeView TreeView => m_TreeView;

        protected VisualElement m_LeftPanel;
        protected VisualElement m_RightPanel;
        protected Label m_TreeTitle;
        protected BaseTreeInspectorView m_TreeInspectorView;
        protected List<BaseTree> m_OpenedTrees = new List<BaseTree>();

        protected virtual Type m_TreeViewType => typeof(BaseTreeView);
        protected virtual Type m_TreeInspectorViewType => typeof(BaseTreeInspectorView);

        public Action OnClosedCallback;
        public Action OnFocusCallback;
        public Action OnLostFocusCallback;

        protected bool m_Docking;
        public bool Docking => m_Docking;

        public virtual void CreateGUI()
        {
            m_Tree = null;

            VisualElement root = rootVisualElement;
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeWindow");
            visualTree.CloneTree(root);

            m_LeftPanel = root.Q("left-panel");
            m_RightPanel = root.Q("right-panel");

            m_TreeView = Activator.CreateInstance(m_TreeViewType) as BaseTreeView;
            m_TreeView.Init(this);
            m_TreeView.name = "tree-view";
            m_RightPanel.Add(m_TreeView);

            m_TreeTitle = new Label();
            m_TreeTitle.name = "tree-title";
            m_RightPanel.Add(m_TreeTitle);

            m_TreeInspectorView = Activator.CreateInstance(m_TreeInspectorViewType) as BaseTreeInspectorView;
            m_TreeInspectorView.name = "tree-inspector";
            m_LeftPanel.Add(m_TreeInspectorView);

            Undo.undoRedoPerformed += OnUndoRedo;
            OnClosedCallback?.Invoke();
        }

        public virtual void OnFocus()
        {
            TreeWindowUtility.SelectTree(m_Tree);
            OnFocusCallback?.Invoke();

            //if (Application.isPlaying && m_Tree)
            //{
            //    foreach (var propertyField in rootVisualElement.Query<PropertyField>().ToList())
            //    {
            //        propertyField.Bind(m_Tree.GetSerializedTree());
            //    }
            //}
        }
        public virtual void OnLostFocus()
        {
            OnLostFocusCallback?.Invoke();

            //if (Application.isPlaying && m_Tree)
            //{
            //    foreach (var propertyField in rootVisualElement.Query<PropertyField>().ToList())
            //    {
            //        propertyField.Unbind();
            //    }
            //}
        }
        public virtual void OnDisable()
        {
            m_TreeView?.ClearView();
            m_TreeInspectorView?.ClearView();

            m_OpenedTrees.ForEach(i => 
            {
                Undo.ClearUndo(i);

                if (i.User == null)
                    i.DisposeTree();
            });
            m_OpenedTrees.Clear();

            Undo.undoRedoPerformed -= OnUndoRedo;
            TreeWindowUtility.OnWindowClosed(this);
            
            OnClosedCallback?.Invoke();
            OnClosedCallback = null;
        }
        public virtual void Update()
        {
            if (m_Tree)
            {
                if (!Application.isPlaying)
                {
                    m_TreeTitle.text = m_Tree.name;
                    //m_TreeView.NodeViews.ForEach(i => i.Update());
                }
                m_TreeView.NodeViews.ForEach(i => i.Animation());
            }
            else if(m_Tree == null && !m_TreeView.Empty)
            {
                m_TreeView.ClearView();
                m_TreeInspectorView.ClearView();
            }

            if (EditorApplication.isCompiling)
                Close();
        }
        public void SelectTree(BaseTree tree)
        {
            if (tree && tree != m_Tree)
            {
                //m_Tree?.Dispose();
                m_Tree = tree;
                m_TreeTitle.text = m_Tree.name;

                if (!m_OpenedTrees.Contains(tree))
                    m_OpenedTrees.Add(tree);
                if (m_Tree.Refresh())
                    EditorUtility.SetDirty(m_Tree);
                if (m_Tree.CheckInit())
                    EditorUtility.SetDirty(m_Tree);
                m_TreeView.PopulateView(m_Tree);
                m_TreeInspectorView.PopulateView(m_Tree);
                TreeWindowUtility.SelectTree(m_Tree);
            }
        }
        void OnUndoRedo()
        {
            if (m_Tree)
            {
                //m_Tree.DisposeTree();

                m_Tree.GetNewSerializedTree();
                if (m_Tree.Refresh())
                    EditorUtility.SetDirty(m_Tree);
                if (m_Tree.CheckInit())
                    EditorUtility.SetDirty(m_Tree);
                m_TreeView.PopulateView(m_Tree);
                m_TreeInspectorView.PopulateView(m_Tree);
            }
        }

        void OnBeforeRemovedAsTab()
        {
            m_Docking = true;
        }
        void OnAddedAsTab()
        {
            m_Docking = false;
        }
    }
}