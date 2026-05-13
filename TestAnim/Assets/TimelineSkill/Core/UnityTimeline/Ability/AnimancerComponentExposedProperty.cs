using System;
using UnityEngine;
using Animancer;
using TreeDesigner;

namespace TreeDesigner
{
    [Serializable]
    [PropertyColor(100, 180, 255)]
    public class AnimancerComponentExposedProperty : BaseExposedProperty<AnimancerComponent>
    {
        public AnimancerComponentExposedProperty() { }

        /// <summary>
        /// 运行时注入的组件的 InstanceID，用于 Inspector 调试显示
        /// </summary>
#if UNITY_EDITOR
        public int InstanceID => m_Value != null ? m_Value.GetInstanceID() : 0;
#endif
    }
}
