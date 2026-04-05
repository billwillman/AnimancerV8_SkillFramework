using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace TreeDesigner.Editor
{
    public class BaseEdgeView : Edge
    {
        protected BaseEdge m_Edge;
        public BaseEdge Edge { get => m_Edge; set => m_Edge = value; }

        public BasePortView StartPortView => output as BasePortView;
        public BasePortView EndPortView => input as BasePortView;

        public BaseNodeView StartNodeView => StartPortView.NodeView;
        public BaseNodeView EndNodeView => EndPortView.NodeView;
    }
}