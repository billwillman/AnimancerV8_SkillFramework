using UnityEngine;
using TreeDesigner;
using ECM2;

namespace UnityTimeline
{
    [NodeName("AddForce")]
    [NodePath("UnityTimeline/Action/AddForce")]
    public class AddForceNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Force")]
        Vector3PropertyPort m_Force = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "ForceMode")]
        IntPropertyPort m_ForceMode = new IntPropertyPort();

        protected override void DoAction()
        {
            var character = AbilityLinker?.GetComponent<Character>();
            if (character != null)
            {
                character.AddForce(m_Force.Value, (ForceMode)m_ForceMode.Value);
            }
        }
    }
}
