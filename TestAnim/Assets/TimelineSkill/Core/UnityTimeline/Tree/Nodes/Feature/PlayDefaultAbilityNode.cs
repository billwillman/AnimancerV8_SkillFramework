using UnityEngine;
using TreeDesigner;

/// <summary>
/// 播放 AnimancerAbilityLinker 上配置的 DefaultAbility。
/// 通过 AnimancerAbility.Runner.DefaultAbilityName 获取名称，无需 GetComponent。
/// </summary>
[NodeName("PlayDefaultAbility")]
[NodePath("AnimancerAbility/Action/PlayDefaultAbility")]
public class PlayDefaultAbilityNode : AnimancerAbilityActionNode
{
    [SerializeField, PropertyPort(PortDirection.Output, "Success"), ReadOnly]
    BoolPropertyPort m_Success = new BoolPropertyPort();

    protected override void DoAction()
    {
        m_Success.Value = false;

        var runner = AnimancerAbility?.Runner;
        if (runner != null && !string.IsNullOrEmpty(runner.DefaultAbilityName))
        {
            m_Success.Value = runner.TryStartAbility(runner.DefaultAbilityName);
        }
    }
}
