using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    /// <summary>
    /// 设置 Animator 对象世界坐标位置和旋转的 Action 节点。
    /// </summary>
    [NodeName("SetWorldTransform")]
    [NodePath("UnityTimeline/Action/SetWorldTransform")]
    public class SetWorldTransformNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "WorldPosition")]
        Vector3PropertyPort m_WorldPosition = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "WorldRotation")]
        Vector3PropertyPort m_WorldRotation = new Vector3PropertyPort();

        protected override void DoAction()
        {
            if (Controller?.IsValid == true)
            {
                Controller.SetWorldPosition(m_WorldPosition.Value);
                Controller.SetWorldRotation(m_WorldRotation.Value);
            }
        }
    }
}
