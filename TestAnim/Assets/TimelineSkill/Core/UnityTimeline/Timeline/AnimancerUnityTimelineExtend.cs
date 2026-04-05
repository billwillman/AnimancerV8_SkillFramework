using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Animancer;
using UnityEngine.Timeline;

public static class AnimancerUnityTimelineExtend
{
    private static void ApplyPlayableAssetState(PlayableAssetState state) {
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
    }

    public static AnimancerState PlayTimeline(this AnimancerComponent component, ITransition asset, float fadeDuration, FadeMode mode = default) { 
        if (asset == null)
            return null;
        AnimancerState temp;
        if (component.States.TryGet(asset, out temp)) {
            return component.Play(temp, fadeDuration, mode);
        }
        PlayableAssetState state = component.Play(asset, fadeDuration, mode) as PlayableAssetState;
        ApplyPlayableAssetState(state);
        return state;
    }

    public static AnimancerState PlayTimeline(this AnimancerComponent component, ITransition asset) {
        if (asset == null)
            return null;
        AnimancerState temp;
        if (component.States.TryGet(asset, out temp)) {
            return component.Play(temp, asset.FadeDuration, asset.FadeMode);
        }
        PlayableAssetState state = component.Play(asset) as PlayableAssetState;
        ApplyPlayableAssetState(state);
        return state;
    }
}
