using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityTimelineTreeTempPlayableBehaviourMgr : SingetonMono<UnityTimelineTreeTempPlayableBehaviourMgr>
{
    private Dictionary<int, UnityTimelineTreeTempPlayableBehaviour> TempMap = new Dictionary<int, UnityTimelineTreeTempPlayableBehaviour>();

    public void Register(UnityTimelineTreeTempPlayableBehaviour tempBehaviour) {
        if (tempBehaviour == null)
            return;
        int gameObjectInstanceID = tempBehaviour.CachedGameObject.GetInstanceID();
        TempMap[gameObjectInstanceID] = tempBehaviour;
    }

    public void UnRegister(UnityTimelineTreeTempPlayableBehaviour tempBehaviour) {
        if (tempBehaviour == null)
            return;
        int gameObjectInstanceID = tempBehaviour.CachedGameObject.GetInstanceID();
        UnityTimelineTreeTempPlayableBehaviour r;
        if (TempMap.TryGetValue(gameObjectInstanceID, out r) && r == tempBehaviour)
            TempMap.Remove(gameObjectInstanceID);
    }

    public UnityTimelineTreeTempPlayableBehaviour GetTempPlayableBehaviour(GameObject gameObject) {
        if (gameObject == null || TempMap == null)
            return null;
        int gameObjectInstanceID = gameObject.GetInstanceID();
        UnityTimelineTreeTempPlayableBehaviour ret;
        if (TempMap.TryGetValue(gameObjectInstanceID, out ret))
            return ret;
        ret = gameObject.GetComponent<UnityTimelineTreeTempPlayableBehaviour>();
        if (ret != null) {
            TempMap[gameObjectInstanceID] = ret;
        }
        return ret;
    }
}
