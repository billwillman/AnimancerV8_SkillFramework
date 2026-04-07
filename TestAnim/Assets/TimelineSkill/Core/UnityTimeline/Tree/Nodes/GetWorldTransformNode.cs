using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    /// <summary>
    /// 获取 Animator 对象的世界坐标位置和旋转的 Value 节点。
    /// </summary>
    [NodeName("GetWorldTransform")]
    [NodePath("UnityTimeline/Value/GetWorldTransform")]
    public class GetWorldTransformNode : UnityTimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "WorldPosition"), TreeDesigner.ReadOnly]
        Vector3PropertyPort m_WorldPosition = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "WorldRotation"), TreeDesigner.ReadOnly]
        Vector3PropertyPort m_WorldRotation = new Vector3PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            if (Controller?.IsValid != true)
            {
                m_WorldPosition.Value = Vector3.zero;
                m_WorldRotation.Value = Vector3.zero;
                return;
            }
            m_WorldPosition.Value = Controller.GetWorldPosition();
            m_WorldRotation.Value = Controller.GetWorldRotation();
        }
    }
}
