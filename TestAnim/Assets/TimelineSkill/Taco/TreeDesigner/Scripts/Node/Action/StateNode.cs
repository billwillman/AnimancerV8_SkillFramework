using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("State")]
    [NodePath("Base/Action/State")]
    public class StateNode : ActionNode
    {
        [SerializeField, ShowInPanel("ReturnState")]
        State m_ReturnState;

        public override State ReturnState => m_ReturnState;

        protected override void DoAction() { }
    }
}