using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Animancer;
using UnityEngine.Timeline;

public static class AnimancerUnityTimelineExtend
{
    private static void ApplyPlayableAssetState(PlayableAssetState state, bool bindSignal = false) {
        if (state == null || state.Graph == null || state.Graph.Component == null)
            return;
        /*
        var graph = state.Root.Graph;
        for (int i = 0; i < graph.GetOutputCountByType<ScriptPlayableOutput>(); ++i) {
            var PlayableOutput = graph.GetOutputByType<ScriptPlayableOutput>(i);
            var refObj = PlayableOutput.GetReferenceObject();
            if (refObj is UnityTimelinePlayableTrack) {
                //PlayableOutput.SetUserData(state);
                var sourcePlayable = PlayableOutput.GetSourcePlayable();
            }
        }
        */
        var gameObj = state.Graph.Component.gameObject;
        if (gameObj == null)
            return;
        var tempPlayableBehaviour = UnityTimelineTreeTempPlayableBehaviourMgr.GetInstance().GetTempPlayableBehaviour(gameObj);
        if (tempPlayableBehaviour != null)
        {
            for (int index = 0; index < tempPlayableBehaviour.PlayableBehaviourCount; ++index)
            {
                var behaviour = tempPlayableBehaviour.GetBehaviour(index);
                if (behaviour != null)
                {
                    behaviour.ApplyLocalRuntimeTreeController(state);
                }
            }
            tempPlayableBehaviour.Clear();
        }

        // 按需注册 Signal Receiver
        if (bindSignal)
            RegisterSignalReceiverForAnimancer(state, gameObj);
    }

    /// <summary>
    /// 在 PlayableAssetState 初始化完成后，将 TimelineEventLuaReceiver 注册到 PlayableGraph 的通知输出中。
    /// 解决通过 Animacer 播放 Timeline 时 Signal Receiver 不响应的问题。
    /// </summary>
    private static void RegisterSignalReceiverForAnimancer(PlayableAssetState state, GameObject gameObject)
    {
        if (state == null || !state.IsValid() || state.Graph == null)
            return;

        var signalReceiver = gameObject.GetComponentInChildren<SOC.GamePlay.TimelineEventLuaReceiver>();
        if (signalReceiver == null)
            return;

        var graph = state.Graph.PlayableGraph;
        var outputCount = graph.GetOutputCount();
        for (int i = 0; i < outputCount; i++)
        {
            var output = graph.GetOutput(i);
            if (output.IsPlayableOutputOfType<ScriptPlayableOutput>())
            {
                var scriptOutput = (ScriptPlayableOutput)output;
                scriptOutput.RemoveNotificationReceiver<ScriptPlayableOutput>(signalReceiver);
                scriptOutput.AddNotificationReceiver(signalReceiver);
            }
        }
    }

    public static AnimancerState PlayTimeline(this AnimancerComponent component, ITransition asset, float fadeDuration, FadeMode mode = default, bool bindSignal = false) {
        if (asset == null)
            return null;
        AnimancerState temp;
        if (component.States.TryGet(asset, out temp)) {
            return component.Play(temp, fadeDuration, mode);
        }
        PlayableAssetState state = component.Play(asset, fadeDuration, mode) as PlayableAssetState;
        ApplyPlayableAssetState(state, bindSignal);
        return state;
    }

    public static AnimancerState PlayTimeline(this AnimancerComponent component, ITransition asset, bool bindSignal = false) {
        if (asset == null)
            return null;
        AnimancerState temp;
        if (component.States.TryGet(asset, out temp)) {
            return component.Play(temp, asset.FadeDuration, asset.FadeMode);
        }
        PlayableAssetState state = component.Play(asset) as PlayableAssetState;
        ApplyPlayableAssetState(state, bindSignal);
        return state;
    }
}
