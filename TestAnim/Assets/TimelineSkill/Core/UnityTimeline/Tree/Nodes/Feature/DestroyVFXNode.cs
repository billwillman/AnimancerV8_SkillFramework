using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("DestroyVFX")]
    [NodePath("UnityTimeline/Action/DestroyVFX")]
    public class DestroyVFXNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Instance")]
        objectPropertyPort m_Instance = new objectPropertyPort();

        protected override void DoAction()
        {
            if (m_Instance.Value is GameObject go && go != null)
            {
                UnityEngine.Object.Destroy(go);
            }
        }
    }
}
