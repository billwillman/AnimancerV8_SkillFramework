using System;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityTimeline
{
    /// <summary>
    /// PlayableDirector 的封装实现类。
    /// 内部持有 PlayableDirector 实例，实现 IDirectorController 接口。
    /// </summary>
    public class PlayableDirectorController : IDirectorController
    {
        private PlayableDirector m_Director;
        private Animator m_Animator;
        private Animator CachedAnimator
        {
            get
            {
                if (m_Animator == null)
                    m_Animator = m_Director?.GetComponent<Animator>();
                return m_Animator;
            }
        }

        public PlayableDirectorController(PlayableDirector director)
        {
            m_Director = director;
        }

        public void Play()
        {
            m_Director?.Play();
        }

        public void Pause()
        {
            m_Director?.Pause();
        }

        public void Stop()
        {
            m_Director?.Stop();
        }

        public double time
        {
            get => m_Director != null ? m_Director.time : 0;
            set { if (m_Director != null) m_Director.time = value; }
        }

        public DirectorState state
        {
            get
            {
                if (m_Director == null)
                    return DirectorState.Stopped;

                switch (m_Director.state)
                {
                    case PlayState.Playing:
                        return DirectorState.Playing;
                    case PlayState.Paused:
                        return DirectorState.Paused;
                    default:
                        return DirectorState.Stopped;
                }
            }
        }

        public bool IsValid => m_Director != null;

        public void SetSpeed(double speed)
        {
            if (m_Director != null && m_Director.playableGraph.IsValid())
            {
                var rootPlayable = m_Director.playableGraph.GetRootPlayable(0);
                rootPlayable.SetSpeed(speed);
            }
        }

        public void SetTrackEnabled(int trackIndex, bool enabled)
        {
            if (m_Director == null || !m_Director.playableGraph.IsValid())
                return;

            var rootPlayable = m_Director.playableGraph.GetRootPlayable(0);
            if (trackIndex < 0 || trackIndex >= rootPlayable.GetInputCount())
                return;

            rootPlayable.SetInputWeight(trackIndex, enabled ? 1f : 0f);
        }

        public bool IsTrackEnabled(int trackIndex)
        {
            if (m_Director == null || !m_Director.playableGraph.IsValid())
                return false;

            var rootPlayable = m_Director.playableGraph.GetRootPlayable(0);
            if (trackIndex < 0 || trackIndex >= rootPlayable.GetInputCount())
                return false;

            return rootPlayable.GetInputWeight(trackIndex) > 0f;
        }

        public void SetRootMotionEnabled(bool enable)
        {
            if (CachedAnimator != null)
                CachedAnimator.applyRootMotion = enable;
        }

        public Vector3 GetRootMotionDeltaPosition()
        {
            if (CachedAnimator != null)
                return CachedAnimator.deltaPosition;
            return Vector3.zero;
        }

        public Vector3 GetRootMotionDeltaRotation()
        {
            if (CachedAnimator != null)
                return CachedAnimator.deltaRotation.eulerAngles;
            return Vector3.zero;
        }

        public Vector3 GetWorldPosition()
        {
            if (m_Director != null)
                return m_Director.transform.position;
            return Vector3.zero;
        }

        public Vector3 GetWorldRotation()
        {
            if (m_Director != null)
                return m_Director.transform.rotation.eulerAngles;
            return Vector3.zero;
        }

        public void SetWorldPosition(Vector3 position)
        {
            if (m_Director != null)
                m_Director.transform.position = position;
        }

        public void SetWorldRotation(Vector3 eulerAngles)
        {
            if (m_Director != null)
                m_Director.transform.rotation = Quaternion.Euler(eulerAngles);
        }

        /// <summary>
        /// Seek 到指定时间点，并自动使用 TimelineRedirectRootMotion 补偿 RootMotion 跳变。
        /// </summary>
        public void Seek(double time)
        {
            if (m_Director == null) return;

            // 1. 记录当前位置/旋转作为补偿基线
            var redirect = m_Director.GetComponent<TimelineRedirectRootMotion>();
            if (redirect != null && redirect.Target != null)
            {
                var pos = redirect.Target.position;
                var rotEuler = redirect.Target.rotation.eulerAngles;
                redirect.SetCompensation(pos, rotEuler);
            }

            // 2. 执行 Seek
            m_Director.time = time;
        }
    }
}
