using UnityEngine;

namespace UnityTimeline
{
    /// <summary>
    /// PlayableDirector 控制接口。
    /// 封装 PlayableDirector 的常用操作，供行为树节点使用。
    /// </summary>
    public interface IDirectorController
    {
        void Play();
        void Pause();
        void Stop();
        double time { get; set; }
        DirectorState state { get; }
        bool IsValid { get; }
        void SetSpeed(double speed);
        void SetTrackEnabled(int trackIndex, bool enabled);
        bool IsTrackEnabled(int trackIndex);
        void SetRootMotionEnabled(bool enable);
        Vector3 GetRootMotionDeltaPosition();
        Vector3 GetRootMotionDeltaRotation();
        Vector3 GetWorldPosition();
        Vector3 GetWorldRotation();
        void SetWorldPosition(Vector3 position);
        void SetWorldRotation(Vector3 eulerAngles);
    }
}
