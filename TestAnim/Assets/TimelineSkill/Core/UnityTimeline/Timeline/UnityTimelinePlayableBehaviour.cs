using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using TreeDesigner;

[Serializable]
public class UnityTimelinePlayableBehaviour : PlayableBehaviour
{
    [System.NonSerialized]
    public UnityTimeline.UnityTimelineTree RuntimeTree = null;
    [System.NonSerialized]
    public bool IsRunTreeAsset = false;
    [System.NonSerialized]
    public UnityTimeline.IDirectorController Controller = null;

    // ── BlackboardContext 管理 ──
    [System.NonSerialized]
    private BlackboardContext m_Context = null;
    [System.NonSerialized]
    private CommonBlackboard m_Blackboard = null;

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

        if (RuntimeTree != null) {
            BeginContext();
            RuntimeTree.ResetTree();
            RuntimeTree.Running = false;
            ApplyLocalRuntimeTreeController();
            EndContext();
        }
    }

    public override void OnGraphStop(Playable playable) {
        Debug.LogWarning("OnGraphStop");
    }

    /// <summary>
    /// 克隆 SO 并创建运行时树 + 独立的 BlackboardContext。
    /// </summary>
    public void SpawnRuntimeTree(UnityTimeline.UnityTimelineTree timelineTree, GameObject owner) {
        // 先销毁旧 tree（此时 m_Context 还对应旧 tree，Context 匹配）
        if (IsRunTreeAsset && RuntimeTree != null) {
            DestroyOldTree(RuntimeTree);
        }

        // 克隆 SO
        this.RuntimeTree = GameObject.Instantiate(timelineTree);

        // 先注入 AbilityLinker（在 InitTree 之前，修复 OnAbilityEventNode 时序问题）
        if (owner != null)
            this.RuntimeTree.AbilityLinker = owner.GetComponentInChildren<AnimancerAbilityLinker>();

        this.RuntimeTree.OnSpawn();
        this.RuntimeTree.InitTree(this);

        // 获取 CommonBlackboard 并创建独立的 BlackboardContext
        if (owner != null) {
            m_Blackboard = owner.GetComponent<CommonBlackboard>();
            if (m_Blackboard == null)
                m_Blackboard = owner.GetComponentInChildren<CommonBlackboard>();
        }
        if (m_Blackboard != null) {
            m_Context = m_Blackboard.CreateContextForTree(this.RuntimeTree);
        }

        IsRunTreeAsset = true;
    }

    // ── BeginContext / EndContext ──

    void BeginContext() {
        if (RuntimeTree != null && m_Context != null) {
            RuntimeTree.BindBlackboardContext(m_Context, m_Blackboard);
        }
    }

    void EndContext() {
        if (RuntimeTree != null) {
            RuntimeTree.UnbindBlackboardContext();
        }
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
                BeginContext();
                if (!RuntimeTree.Running)
                    CallTreeEnable();
                RuntimeTree.UpdateTree(info.deltaTime);
                EndContext();
            }
        }
    }

    void ResetTree() {
        ResetTree(RuntimeTree);
    }

    void ResetTree(UnityTimeline.UnityTimelineTree tree) {
        if (tree != null) {
            tree.ResetTree();
            tree.Running = false;
        }
    }

    /// <summary>
    /// 在 SpawnRuntimeTree 中销毁旧的克隆体（此时 m_Context 仍对应旧 tree）。
    /// </summary>
    void DestroyOldTree(UnityTimeline.UnityTimelineTree oldTree) {
        if (oldTree == null) return;

        BeginContext();
        ResetTree(oldTree);
        oldTree.DisposeTree();
        EndContext();

        if (Application.isPlaying)
            GameObject.Destroy(oldTree);
        else
            GameObject.DestroyImmediate(oldTree);

        m_Context = null;
    }

    /// <summary>
    /// 销毁当前 RuntimeTree（外部调用，如 OnPlayableDestroy）。
    /// </summary>
    public void DestroyRuntimeTree(bool isCallCallBack = false) {
        if (RuntimeTree != null) {
            BeginContext();
            if (isCallCallBack)
                RuntimeTree.OnTreeDestroy();
            ResetTree(RuntimeTree);
            RuntimeTree.DisposeTree();
            EndContext();

            if (Application.isPlaying)
                GameObject.Destroy(RuntimeTree);
            else
                GameObject.DestroyImmediate(RuntimeTree);
            RuntimeTree = null;
        }
        m_Context = null;
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
        if (RuntimeTree == null) return;

        BeginContext();
        if (RuntimeTree.Running) {
            bool isInterrupted = info.effectiveWeight > float.Epsilon;
            if (isInterrupted)
                CallTreeInterrpt();
            else
                CallTreeDisable();

            ResetTree();
        }
        EndContext();
    }

    public override void OnPlayableDestroy(Playable playable) {
        DestroyRuntimeTree(true);
        Controller = null;
        m_Blackboard = null;
    }
}
