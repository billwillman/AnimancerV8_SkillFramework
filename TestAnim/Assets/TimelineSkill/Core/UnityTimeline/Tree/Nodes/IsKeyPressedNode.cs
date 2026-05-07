using UnityEngine;
using UnityEngine.InputSystem;
using TreeDesigner;

namespace UnityTimeline
{
    /// <summary>
    /// 检测指定 InputAction 是否被按下（IsPressed），输出 bool 值。
    /// </summary>
    [NodeName("InputKeyCondition")]
    [NodePath("UnityTimeline/Value/InputKeyCondition")]
    public class InputKeyConditionNode : UnityTimelineValueNode
    {
        [SerializeField, ShowInPanel, Tooltip("要检测的 Input Action（从 Input Action Asset 拖入）")]
        InputActionReference m_Action;

        [SerializeField, PropertyPort(PortDirection.Output, "Success"), TreeDesigner.ReadOnly]
        BoolPropertyPort m_IsPressed = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            var action = m_Action?.action;
            m_IsPressed.Value = action != null && action.IsPressed();
        }
    }
}
