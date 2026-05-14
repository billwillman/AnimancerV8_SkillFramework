using Unity.VisualScripting;
using Animancer;

/// <summary>
/// 按 Key 获取 AnimancerState
/// </summary>
[UnitTitle("Get Animancer State")]
[UnitCategory("AnimancerLinkNodes/Value")]
public class GetAnimancerStateUnit : VSAbilityUnitBase
{
    [DoNotSerialize] public ValueInput Key;
    [DoNotSerialize] public ValueOutput AnimancerStateOut;

    protected override void Definition()
    {
        Key = ValueInput<string>("Key", "");
        AnimancerStateOut = ValueOutput<AnimancerState>("AnimancerState", GetState);
    }

    private AnimancerState GetState(Flow flow)
    {
        var animancer = GetAnimancer(flow);
        if (animancer == null) return null;

        var key = flow.GetValue<string>(Key);
        if (string.IsNullOrEmpty(key)) return null;

        if (animancer.States.TryGet(key, out AnimancerState state))
            return state;
        return null;
    }
}
