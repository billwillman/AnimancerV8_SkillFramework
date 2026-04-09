using UnityEngine;
using Animancer;

namespace UnityTimeline
{
    /// <summary>
    /// 继承 RedirectRootMotionToTransform，额外暴露位置和旋转的补偿变量。
    /// 在 UnityTimelineTree 中 JumpToTime 后调用 SetCompensationPosition/Rotation 来修正位置跳变。
    /// </summary>
    [AddComponentMenu("TimelineSkill/Timeline Redirect Root Motion")]
    public class TimelineRedirectRootMotion : RedirectRootMotionToTransform
    {
        [SerializeField]
        [Tooltip("位置补偿值，设置后会直接覆盖 Target.position")]
        private Vector3 m_CompensationPosition;

        [SerializeField]
        [Tooltip("旋转补偿值（欧拉角），设置后会直接覆盖 Target.rotation")]
        private Vector3 m_CompensationRotationEuler;

        /// <summary>补偿剩余帧数（Seek 后前两帧的 delta 无效，需忽略）</summary>
        private int m_CompensationFrames;

        /// <summary>是否处于补偿模式。</summary>
        public bool CompensationEnabled => m_CompensationFrames > 0;

        /// <summary>获取当前补偿位置</summary>
        public Vector3 CompensationPosition => m_CompensationPosition;

        /// <summary>获取当前补偿旋转（欧拉角）</summary>
        public Vector3 CompensationRotationEuler => m_CompensationRotationEuler;

        /// <summary>默认补偿帧数，Seek 后前 N 帧的 delta 无效需忽略</summary>
        private const int kDefaultCompensationFrames = 2;

        /// <summary>
        /// 设置位置补偿值，后续两帧 OnAnimatorMove 会忽略 delta 并锁定位置。
        /// </summary>
        public void SetCompensationPosition(Vector3 position)
        {
            m_CompensationPosition = position;
            m_CompensationFrames = kDefaultCompensationFrames;
        }

        /// <summary>
        /// 设置旋转补偿值（欧拉角），后续两帧 OnAnimatorMove 会忽略 delta 并锁定旋转。
        /// </summary>
        public void SetCompensationRotation(Vector3 eulerAngles)
        {
            m_CompensationRotationEuler = eulerAngles;
            m_CompensationFrames = kDefaultCompensationFrames;
        }

        /// <summary>
        /// 同时设置位置和旋转补偿值，后续两帧 OnAnimatorMove 会忽略 delta。
        /// </summary>
        public void SetCompensation(Vector3 position, Vector3 rotationEuler)
        {
            m_CompensationPosition = position;
            m_CompensationRotationEuler = rotationEuler;
            m_CompensationFrames = kDefaultCompensationFrames;
        }

        /// <summary>
        /// 清除补偿，立即恢复正常 RootMotion 累加模式。
        /// </summary>
        public void ClearCompensation()
        {
            m_CompensationFrames = 0;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (Animator != null && !Animator.applyRootMotion)
                Animator.applyRootMotion = true;
        }

        protected override void OnAnimatorMove()
        {
            if (!ApplyRootMotion)
                return;

            if (m_CompensationFrames > 0)
            {
                // 补偿模式：直接锁定到补偿位置，忽略本帧的 delta（Seek 后前两帧的 delta 是无效的）
                Target.position = m_CompensationPosition;
                Target.rotation = Quaternion.Euler(m_CompensationRotationEuler);
                
                // 递减计数器，两帧后自动恢复正常累加模式
                m_CompensationFrames--;
            }
            else
            {
                // 正常模式：累加位移和旋转，与父类一致
                Target.position += Animator.deltaPosition;
                Target.rotation *= Animator.deltaRotation;
            }
        }
    }
}
