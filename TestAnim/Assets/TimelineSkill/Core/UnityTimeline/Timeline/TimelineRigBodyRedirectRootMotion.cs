using UnityEngine;
using Animancer;

namespace UnityTimeline
{
    /// <summary>
    /// 继承 RedirectRootMotionToRigidbody，额外暴露位置和旋转的补偿变量。
    /// 在 UnityTimelineTree 中 JumpToTime 后调用 SetCompensationPosition/Rotation 来修正位置跳变。
    /// 与 TimelineRedirectRootMotion 功能一致，但通过 Rigidbody.MovePosition/MoveRotation 应用位移，
    /// 适用于需要物理交互的角色。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 基类 <see cref="RedirectRootMotionToRigidbody"/> 继承自 <see cref="RedirectRootMotion{T}"/>（T = Rigidbody），
    /// 其 <see cref="RedirectRootMotion.Position"/> / <see cref="RedirectRootMotion.Rotation"/> 属性的 setter
    /// 已通过 <see cref="Rigidbody.MovePosition"/> / <see cref="Rigidbody.MoveRotation"/> 实现。
    /// 本类 override <see cref="RedirectRootMotion.OnAnimatorMove"/> 时统一通过这些属性操作，
    /// 确保与基类行为语义一致。
    /// </para>
    /// </remarks>
    [AddComponentMenu("TimelineSkill/Timeline RigBody Redirect Root Motion")]
    public class TimelineRigBodyRedirectRootMotion : RedirectRootMotionToRigidbody
    {
        [SerializeField]
        [Tooltip("位置补偿值，设置后会通过 Rigidbody.MovePosition 锁定到该位置")]
        private Vector3 m_CompensationPosition;

        [SerializeField]
        [Tooltip("旋转补偿值（欧拉角），设置后会通过 Rigidbody.MoveRotation 锁定到该旋转")]
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

        /// <summary>
        /// 通过基类的 <see cref="RedirectRootMotion.Position"/> / <see cref="RedirectRootMotion.Rotation"/> 属性
        /// 应用 RootMotion。这些属性在 <see cref="RedirectRootMotionToRigidbody"/> 中已实现为
        /// <see cref="Rigidbody.MovePosition"/> / <see cref="Rigidbody.MoveRotation"/>，
        /// 因此无需直接调用 Target.MovePosition/MoveRotation。
        /// </summary>
        protected override void OnAnimatorMove()
        {
            if (!ApplyRootMotion)
                return;

            if (m_CompensationFrames > 0)
            {
                // 补偿模式：通过属性 setter（内部调用 MovePosition/MoveRotation）锁定到补偿值，
                // 忽略本帧的 delta（Seek 后前两帧的 delta 是无效的）
                Position = m_CompensationPosition;
                Rotation = Quaternion.Euler(m_CompensationRotationEuler);

                // 递减计数器，两帧后自动恢复正常累加模式
                m_CompensationFrames--;
            }
            else
            {
                // 正常模式：与基类 RedirectRootMotion.OnAnimatorMove() 行为一致，
                // 通过属性累加位移和旋转（setter 内部使用 MovePosition/MoveRotation）
                Position += Animator.deltaPosition;
                Rotation *= Animator.deltaRotation;
            }
        }
    }
}



