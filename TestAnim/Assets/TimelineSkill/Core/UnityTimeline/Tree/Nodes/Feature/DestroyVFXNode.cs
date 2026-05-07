using UnityEngine;
using TreeDesigner;

namespace UnityTimeline
{
    [NodeName("DestroyVFX")]
    [NodePath("UnityTimeline/Action/DestroyVFX")]
    public class DestroyVFXNode : UnityTimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Instance")]
        objectPropertyPort m_Instance = new objectPropertyPort();

        protected override void DoAction()
        {
            var value = m_Instance.Value;

            // 支持 string (Name) 模式：通过 PrefabPool 分配的 Name 回收
            if (value is string instanceName)
            {
                var pool = PrefabPool.Instance;
                if (pool != null && pool.TryRecycleByName(instanceName))
                    return;

                // 如果通过 Name 找到了实例但无法回池，直接销毁
                var go = pool?.GetActiveInstance(instanceName);
                if (go != null)
                    Object.Destroy(go);
                return;
            }

            // 支持 GameObject 模式：直接引用回收
            if (value is GameObject gameObject && gameObject != null)
            {
                var pool = PrefabPool.Instance;
                if (pool == null || !pool.TryRecycle(gameObject))
                    Object.Destroy(gameObject);
            }
        }
    }
}
