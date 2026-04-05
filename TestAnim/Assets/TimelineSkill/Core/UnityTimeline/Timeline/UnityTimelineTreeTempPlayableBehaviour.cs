using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityTimelineTreeTempPlayableBehaviour : Utils.CachedMonoBehaviour
{
    private List<UnityTimelinePlayableBehaviour> ClonePlayableBehaviour = null;

    private void Awake() {
        UnityTimelineTreeTempPlayableBehaviourMgr.GetInstance().Register(this);
    }

    private void OnDestroy() {
        if (UnityTimelineTreeTempPlayableBehaviourMgr.IsDestroy)
            return;
        UnityTimelineTreeTempPlayableBehaviourMgr.GetInstance().UnRegister(this);
    }

    public void RegisterBehaviour(UnityTimelinePlayableBehaviour behaviour) {
        if (behaviour == null)
            return;
        if (ClonePlayableBehaviour == null)
            ClonePlayableBehaviour = new List<UnityTimelinePlayableBehaviour>();
        ClonePlayableBehaviour.Add(behaviour);
    }

    public void Clear() {
        if (ClonePlayableBehaviour != null)
            ClonePlayableBehaviour.Clear();
    }

    public void UnregisterBehaviour(UnityTimelinePlayableBehaviour behaviour) {
        if (behaviour == null || ClonePlayableBehaviour == null)
            return;
        ClonePlayableBehaviour.Remove(behaviour);
    }

    public int PlayableBehaviourCount {
        get {
            if (ClonePlayableBehaviour != null)
                return ClonePlayableBehaviour.Count;
            return 0;
        }
    }

    public UnityTimelinePlayableBehaviour GetBehaviour(int index) {
        if (ClonePlayableBehaviour == null || index < 0 || index >= ClonePlayableBehaviour.Count)
            return null;
        UnityTimelinePlayableBehaviour ret = ClonePlayableBehaviour[index];
        return ret;
    }
}
