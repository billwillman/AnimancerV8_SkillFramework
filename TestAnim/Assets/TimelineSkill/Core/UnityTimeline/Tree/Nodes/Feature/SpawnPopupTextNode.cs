using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("SpawnPopupText")]
    [NodePath("UnityTimeline/Action/SpawnPopupText")]
    public class SpawnPopupTextNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Number")]
        FloatPropertyPort m_Number = new FloatPropertyPort() { Value = 100f };

        [SerializeField, PropertyPort(PortDirection.Input, "Position")]
        Vector3PropertyPort m_Position = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "Velocity")]
        Vector2PropertyPort m_Velocity = new Vector2PropertyPort();

        protected override void DoAction()
        {
            if (AbilityLinker == null)
                return;

            if (PopupTextManager.Instance != null)
            {
                PopupTextManager.Instance.SpawnPopup(m_Number.Value, m_Position.Value, AbilityLinker.transform, m_Velocity.Value);
            }
        }
    }
}
