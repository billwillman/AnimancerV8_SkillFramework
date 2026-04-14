using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("CreateAfterImage")]
    [NodePath("UnityTimeline/Action/CreateAfterImage")]
    public class CreateAfterImageNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "MeshName")]
        StringPropertyPort m_MeshName = new StringPropertyPort();

        [SerializeField, ShowInPanel]
        Material m_Material;

        [SerializeField, PropertyPort(PortDirection.Input, "LayerMask")]
        IntPropertyPort m_LayerMask = new IntPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "SubMeshIndex")]
        IntListPropertyPort m_SubMeshIndex = new IntListPropertyPort();

        protected override void DoAction()
        {
            if (AbilityLinker == null)
                return;

            var controller = AbilityLinker.GetComponent<AfterImageController>();
            if (controller != null && m_Material != null)
            {
                int[] subMeshArray = m_SubMeshIndex.Value != null ? m_SubMeshIndex.Value.ToArray() : new int[0];
                var afterImage = controller.CreateAfterImage(m_MeshName.Value, m_Material, m_LayerMask.Value, subMeshArray);
                afterImage?.Start();
            }
        }
    }
}
