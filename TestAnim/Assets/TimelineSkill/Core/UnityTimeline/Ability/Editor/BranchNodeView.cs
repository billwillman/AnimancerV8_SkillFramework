using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using TreeDesigner.Editor;

namespace UnityTimeline.Editor
{
    /// <summary>
    /// BranchNode 的自定义 NodeView
    /// 在输出端口上显示 "True" / "False" 标签，方便区分分支
    /// </summary>
    public class BranchNodeView : BaseNodeView
    {
        private Dictionary<string, Label> m_OutputPortLabels = new Dictionary<string, Label>();

        public BranchNodeView(TreeDesigner.BaseNode node, BaseTreeWindow treeWindow) : base(node, treeWindow)
        {
        }

        protected override void GeneratePorts()
        {
            base.GeneratePorts();

            // 为输出端口添加标签显示 True/False
            foreach (var port in m_OutputPortContainer.PortViewMap)
            {
                var label = new Label(port.Key);
                label.style.fontSize = 10;
                label.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                label.style.marginRight = 4;
                label.tooltip = port.Key == "True"
                    ? "当 Condition 为 True 时执行此分支"
                    : "当 Condition 为 False 时执行此分支";

                // 将 Label 插入到端口前面（右侧显示）
                int index = m_OutputPortContainer.IndexOf(port.Value);
                m_OutputPortContainer.Insert(index, label);
                m_OutputPortLabels[port.Key] = label;
            }
        }
    }
}
