using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class UnityTimelinePlayableClip : PlayableAsset, ITimelineClipAsset
{
   public UnityTimelinePlayableBehaviour template = new UnityTimelinePlayableBehaviour ();

    // public ExposedReference<UnityTimeline.UnityTimelineTree> timelineTree;
    public UnityTimeline.UnityTimelineTree timelineTree;
    public string PrivateName = ""; // 私有名字

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
       var playable = ScriptPlayable<UnityTimelinePlayableBehaviour>.Create (graph, template);
      // var playable = ScriptPlayable<UnityTimelinePlayableBehaviour>.Create(graph, 1);
        UnityTimelinePlayableBehaviour clone = playable.GetBehaviour ();
        clone.DestroyRuntimeTree();
        if (timelineTree != null) {
            clone.ApplyLocalRuntimeTreeController(owner);
           // clone.RuntimeTree = timelineTree;
          //  clone.IsRunTreeAsset = true;
            clone.SpawnRuntimeTree(timelineTree, owner);

            // 注册进去
            UnityTimelineTreeTempPlayableBehaviour tempBehaviour = UnityTimelineTreeTempPlayableBehaviourMgr.GetInstance().GetTempPlayableBehaviour(owner);
            if (tempBehaviour == null)
                tempBehaviour = owner.AddComponent<UnityTimelineTreeTempPlayableBehaviour>();
            if (tempBehaviour != null) {
                tempBehaviour.RegisterBehaviour(clone);
            }
            //-----------------------------------------
        } else
            clone.RuntimeTree = null;
        return playable;
    }
}
