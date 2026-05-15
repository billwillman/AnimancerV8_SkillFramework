using System;
using System.Collections.Generic;
using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("OnAbilityEvent")]
    [NodePath("UnityTimeline/Action/OnAbilityEvent")]
    public class OnAbilityEventNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "EventKey")]
        StringPropertyPort m_EventKey = new StringPropertyPort();

        public override void OnRegisterRuntimeProperties(Dictionary<string, BaseExposedProperty> properties)
        {
            properties["ScopedKey"] = new StringExposedProperty { Name = "ScopedKey" };
            properties["Callback"] = new StringExposedProperty { Name = "Callback" };
            properties["Registered"] = new BoolExposedProperty { Name = "Registered" };
        }

        private string GetScopedKey()
        {
            if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("ScopedKey", out var ep))
                return ep.GetValue() as string;
            return null;
        }

        private void SetScopedKey(string value)
        {
            if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("ScopedKey", out var ep))
                ep.SetValue(value);
        }

        private Action GetCallback()
        {
            if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("Callback", out var ep))
                return ep.GetValue() as Action;
            return null;
        }

        private void SetCallback(Action value)
        {
            if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("Callback", out var ep))
                ep.SetValue(value);
        }

        private bool GetRegistered()
        {
            if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("Registered", out var ep)
                && ep is BaseExposedProperty<bool> typed)
                return typed.Value;
            return false;
        }

        private void SetRegistered(bool value)
        {
            if (NodeData != null && NodeData.RuntimeProperties.TryGetValue("Registered", out var ep)
                && ep is BaseExposedProperty<bool> typed)
                typed.Value = value;
        }

        /// <summary>
        /// 延迟注册事件：在首次被驱动时注册（此时 AbilityLinker 和 Context 都已可用）。
        /// </summary>
        private void EnsureEventRegistered()
        {
            if (GetRegistered()) return;

            if (AbilityLinker == null || string.IsNullOrEmpty(m_EventKey.Value))
                return;

            string scopedKey = m_EventKey.Value;
            Action callback = OnEventTriggered;

            SetScopedKey(scopedKey);
            SetCallback(callback);
            SetRegistered(true);

            EventDispatch.Instance.AddEvent(scopedKey, callback);
        }

        public override void Init(BaseTree tree)
        {
            base.Init(tree);
            // 不再在 Init 中注册事件（此时 AbilityLinker 可能未注入、Context 可能未绑定）
        }

        public override void AfterInit()
        {
            base.AfterInit();
            // 尝试注册：如果 SetupRuntimeTree 中先注入了 AbilityLinker 再 InitTree，
            // 且 Context 在 InitTree 之后才创建，则 AfterInit 时 NodeData 仍为 null。
            // 实际注册会在首次 DoAction 时完成。
        }

        protected override void DoAction()
        {
            EnsureEventRegistered();
        }

        private void OnEventTriggered()
        {
            UpdateNode();
        }

        public override void Dispose()
        {
            var scopedKey = GetScopedKey();
            var callback = GetCallback();
            if (!string.IsNullOrEmpty(scopedKey) && callback != null)
            {
                EventDispatch.Instance.RemoveEvent(scopedKey, callback);
                SetScopedKey(null);
                SetCallback(null);
                SetRegistered(false);
            }
            base.Dispose();
        }
    }
}
