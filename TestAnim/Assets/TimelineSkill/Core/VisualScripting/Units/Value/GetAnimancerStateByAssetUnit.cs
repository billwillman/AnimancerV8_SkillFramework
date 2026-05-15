using UnityEngine;
using Unity.VisualScripting;
using Animancer;

/// <summary>
/// 通过 Asset 的 Name 在 AnimancerComponent.States 中查找运行时 AnimancerState。
/// 无 ControlInput：getter 每次现场遍历 States 字典按 Key 的 name 匹配，
/// 不依赖 flow.SetValue 缓存，可跨任意 Flow 边界（OnEnter / OnUpdate / 异步回调）独立工作。
/// </summary>
[UnitTitle("Get Animancer State By Asset")]
[UnitCategory("AnimancerLinkNodes/Value")]
public class GetAnimancerStateByAssetUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ValueInput Asset;
    [DoNotSerialize] public ValueOutput AnimancerStateOut;

    protected override void Definition()
    {
        Asset = ValueInput<TransitionAssetBase>("Asset", null);
        AnimancerStateOut = ValueOutput<AnimancerState>("AnimancerState", GetState);
    }

    private AnimancerState GetState(Flow flow)
    {
        var animancer = GetAnimancer(flow);
        if (animancer == null || animancer.States == null) return null;

        var asset = flow.GetValue<TransitionAssetBase>(Asset);
        if (asset == null) return null;

        AnimancerState ret;
        if (!animancer.States.TryGet(asset, out ret))
            ret = null;
        return ret;
    }
}
