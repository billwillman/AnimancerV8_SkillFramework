using Animancer;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityTimeline
{
    /// <summary>
    /// 基于 Animancer 的 PlayableAssetState 实现的 IDirectorController。
    /// 内部持有 PlayableAssetState 实例，将 IDirectorController 接口映射到 AnimancerState 的 API。
    /// </summary>
    public class PlayableAssetStateController : IDirectorController
    {
        private readonly PlayableAssetState m_State;

        public PlayableAssetStateController(PlayableAssetState state)
        {
            m_State = state;
        }

        public void Play()
        {
            if (m_State != null)
            {
                m_State.IsPlaying = true;
               // m_State.Weight = 1;
            }
        }

        public void Pause()
        {
            if (m_State != null)
                m_State.IsPlaying = false;
        }

        public void Stop()
        {
            if (m_State != null)
                m_State.Stop();
        }

        public double time
        {
            get => m_State != null ? m_State.TimeD : 0;
            set { if (m_State != null) m_State.TimeD = value; }
        }

        public DirectorState state
        {
            get
            {
                if (m_State == null || !m_State.IsValid() || m_State.IsStopped)
                    return DirectorState.Stopped;

                if (m_State.IsPlaying)
                    return DirectorState.Playing;

                return DirectorState.Paused;
            }
        }

        public bool IsValid => m_State != null && m_State.IsValid();

        public void SetSpeed(double speed)
        {
            if (m_State != null)
                m_State.Speed = (float)speed;
        }

        public void SetTrackEnabled(int trackIndex, bool enabled)
        {
            if (m_State == null || !m_State.IsValid())
                return;

            var playable = m_State.Playable;
            if (trackIndex < 0 || trackIndex >= playable.GetInputCount())
                return;

            playable.SetInputWeight(trackIndex, enabled ? 1f : 0f);
        }

        public bool IsTrackEnabled(int trackIndex)
        {
            if (m_State == null || !m_State.IsValid())
                return false;

            var playable = m_State.Playable;
            if (trackIndex < 0 || trackIndex >= playable.GetInputCount())
                return false;

            return playable.GetInputWeight(trackIndex) > 0f;
        }

        public void SetRootMotionEnabled(bool enable)
        {
            if (m_State == null || !m_State.IsValid())
                return;
            var animator = m_State.Layer.Graph?.Component?.Animator;
            if (animator != null)
                animator.applyRootMotion = enable;
        }

        public Vector3 GetRootMotionDeltaPosition()
        {
            if (m_State == null || !m_State.IsValid())
                return Vector3.zero;
            var animator = m_State.Layer.Graph?.Component?.Animator;
            if (animator != null)
                return animator.deltaPosition;
            return Vector3.zero;
        }

        public Vector3 GetRootMotionDeltaRotation()
        {
            if (m_State == null || !m_State.IsValid())
                return Vector3.zero;
            var animator = m_State.Layer.Graph?.Component?.Animator;
            if (animator != null)
                return animator.deltaRotation.eulerAngles;
            return Vector3.zero;
        }

        public Vector3 GetWorldPosition()
        {
            if (m_State == null || !m_State.IsValid())
                return Vector3.zero;
            var animator = m_State.Layer.Graph?.Component?.Animator;
            if (animator != null)
                return animator.transform.position;
            return Vector3.zero;
        }

        public Vector3 GetWorldRotation()
        {
            if (m_State == null || !m_State.IsValid())
                return Vector3.zero;
            var animator = m_State.Layer.Graph?.Component?.Animator;
            if (animator != null)
                return animator.transform.rotation.eulerAngles;
            return Vector3.zero;
        }
    }
}
