using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class UnityTimelinePlayableBehaviour : PlayableBehaviour
{
    [System.NonSerialized]
    public UnityTimeline.UnityTimelineTree RuntimeTree = null;
    [System.NonSerialized]
    public bool IsRunTreeAsset = false;
    [System.NonSerialized]
    public UnityTimeline.IDirectorController Controller = null;
    public void ApplyLocalRuntimeTreeController(GameObject owner) {
        if (owner != null) {
            if (Controller == null) {
                var comp1 = owner.GetComponent<PlayableDirector>();
                if (comp1 != null)
                    Controller = new UnityTimeline.PlayableDirectorController(comp1);
            }
            ApplyLocalRuntimeTreeController();
        }
    }

    public void ApplyLocalRuntimeTreeController(Animancer.PlayableAssetState state) {
        if (state == null || Controller != null)
            return;
        Controller = new UnityTimeline.PlayableAssetStateController(state);
        ApplyLocalRuntimeTreeController();
    }

    public void ApplyLocalRuntimeTreeController() {
        if (RuntimeTree != null && RuntimeTree.DirectorController == null && Controller != null) {
            RuntimeTree.SetDirectorController(Controller);
        }
    }

    public override void OnGraphStart(Playable playable) {
        Debug.LogWarning("OnGraphStart");

        /*
        SpawnRuntimeTree(RuntimeTree);
        ApplyLocalRuntimeTreeController();
        */
        if (RuntimeTree != null) {
            RuntimeTree.ResetTree();
            RuntimeTree.Running = false;
            ApplyLocalRuntimeTreeController();
        }
    }

    public override void OnGraphStop(Playable playable) {
        Debug.LogWarning("OnGraphStop");
    }

    public void SpawnRuntimeTree(UnityTimeline.UnityTimelineTree timelineTree, GameObject owner) {
        var temp = this.RuntimeTree;
        
        this.RuntimeTree = GameObject.Instantiate(timelineTree);
        this.RuntimeTree.OnSpawn();
        this.RuntimeTree.InitTree(this);

        if (owner != null)
            this.RuntimeTree.AbilityLinker = owner.GetComponentInChildren<AnimancerAbilityLinker>();

        if (IsRunTreeAsset) {
            DestroyRuntimeTree(ref temp);
        }
        IsRunTreeAsset = true;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
        if (RuntimeTree != null) {
            if (RuntimeTree.DirectorController == null) {
                if (Controller != null) {
                    RuntimeTree.SetDirectorController(Controller);
                } else
                if (playerData != null) {
                    PlayableDirector director = playerData as PlayableDirector;
                    if (director != null) {
                        RuntimeTree.SetDirectorController(new UnityTimeline.PlayableDirectorController(director));
                    }
                }
            }
            if (RuntimeTree.DirectorController != null) {
                if (!RuntimeTree.Running)
                    CallTreeEnable();
                RuntimeTree.UpdateTree(info.deltaTime);
            }
        }
    }

    void ResetTree() {
        ResetTree(RuntimeTree);
    }

    static void ResetTree(UnityTimeline.UnityTimelineTree RuntimeTree) {
        if (RuntimeTree != null) {
            RuntimeTree.ResetTree();
            RuntimeTree.Running = false;
        }
    }

    static void DestroyRuntimeTree(ref UnityTimeline.UnityTimelineTree RuntimeTree) {
        if (RuntimeTree != null) {
            ResetTree(RuntimeTree);

            RuntimeTree.DisposeTree();
            if (Application.isPlaying)
                GameObject.Destroy(RuntimeTree);
            else
                GameObject.DestroyImmediate(RuntimeTree);
            RuntimeTree = null;
        }
    }

    public void DestroyRuntimeTree(bool isCallCallBack = false) {
        if (RuntimeTree != null) {
            if (isCallCallBack)
                RuntimeTree.OnTreeDestroy();
            DestroyRuntimeTree(ref RuntimeTree);
        }
    }

    void CallTreeEnable() {
        if (RuntimeTree != null) {
            ApplyLocalRuntimeTreeController();
            if (RuntimeTree.DirectorController != null) {
                Debug.LogWarning("OnTreeEnable");
                RuntimeTree.OnTreeEnable();
            }
        }
    }

    // 打断
    void CallTreeInterrpt() {
        if (RuntimeTree != null) {
            ApplyLocalRuntimeTreeController();
            if (RuntimeTree.DirectorController != null) {
                Debug.LogWarning("OnInterrpt");
                RuntimeTree.OnTreeInterrupt();
            }
        }
    }

    void CallTreeDisable() {
        if (RuntimeTree != null) {
            ApplyLocalRuntimeTreeController();
            if (RuntimeTree.DirectorController != null) {
                Debug.LogWarning("OnTreeDisable");
                RuntimeTree.OnTreeDisable();
            }
        }
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info) {
        /*
        if (!RuntimeTree.Running) {
            CallTreeEnable();
        }
        */
    }

    public override void OnBehaviourPause(Playable playable, FrameData info) {
        if (RuntimeTree.Running) {
            bool isInterrupted = info.effectiveWeight > float.Epsilon;
            if (isInterrupted)
                CallTreeInterrpt();
            else
                CallTreeDisable();

            ResetTree();
        }
    }

    public override void OnPlayableDestroy(Playable playable) {
        DestroyRuntimeTree(true);
        Controller = null;
    }
}
