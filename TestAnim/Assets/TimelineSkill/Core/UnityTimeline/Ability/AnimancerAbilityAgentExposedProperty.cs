using System;
using UnityEngine;
using TreeDesigner;

namespace TreeDesigner
{
    [Serializable]
    [PropertyColor(255, 180, 100)]
    public class AnimancerAbilityAgentExposedProperty : BaseExposedProperty<AnimancerAbilityAgent>
    {
        public AnimancerAbilityAgentExposedProperty() { }

#if UNITY_EDITOR
        /// <summary>
        /// 运行时注入的 Agent 的 InstanceID，用于 Inspector 调试显示
        /// </summary>
        public int InstanceID => m_Value != null ? m_Value.GetHashCode() : 0;
#endif
    }
}
