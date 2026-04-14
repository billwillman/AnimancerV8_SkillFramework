using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("PlayAudio")]
    [NodePath("UnityTimeline/Action/PlayAudio")]
    public class PlayAudioNode : UnityTimelineActionNode
    {
        [SerializeField, ShowInPanel]
        AudioClip m_Clip;

        [SerializeField, PropertyPort(PortDirection.Input, "Volume")]
        FloatPropertyPort m_Volume = new FloatPropertyPort() { Value = 1f };

        [SerializeField, PropertyPort(PortDirection.Input, "Speed")]
        FloatPropertyPort m_Speed = new FloatPropertyPort() { Value = 1f };

        [SerializeField, PropertyPort(PortDirection.Input, "StartTime")]
        FloatPropertyPort m_StartTime = new FloatPropertyPort();

        protected override void DoAction()
        {
            if (m_Clip == null)
                return;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(m_Clip, m_Volume.Value, m_Speed.Value, m_StartTime.Value);
            }
        }
    }
}
