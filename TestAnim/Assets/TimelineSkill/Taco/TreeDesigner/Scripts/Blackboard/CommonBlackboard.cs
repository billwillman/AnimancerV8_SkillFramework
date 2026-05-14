using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    /// <summary>
    /// 通用 Blackboard 组件，挂在 GameObject 上，作为该实例所有 Tree 的统一运行时数据中心。
    /// 
    /// 三层数据：
    /// 1. 全局变量（Inspector 编辑）：m_Variables，外部脚本可通过 GetVariable/SetVariable 全局访问
    /// 2. per-Tree 运行时数据：m_Contexts，每个 Tree 有独立的 BlackboardContext
    /// 3. per-Node 数据：NodeBlackboardData（State + Input/Output/Runtime EP），存在 BlackboardContext 中
    /// 
    /// 支持动态 RegisterTree/UnregisterTree，对应 Ability 的 Add/Remove 生命周期。
    /// </summary>
    public class CommonBlackboard : MonoBehaviour
    {
        // ── 全局变量（Inspector 编辑，实例级）──
        [SerializeReference]
        private List<BaseExposedProperty> m_Variables = new List<BaseExposedProperty>();

        public IReadOnlyList<BaseExposedProperty> Variables => m_Variables;

        private Dictionary<string, BaseExposedProperty> m_VariableMap
            = new Dictionary<string, BaseExposedProperty>();

        // ── per-Tree 运行时数据 ──
        private Dictionary<BaseTree, BlackboardContext> m_Contexts
            = new Dictionary<BaseTree, BlackboardContext>();

        // ────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildVariableMap();
        }

        /// <summary>构建全局变量名称查找字典</summary>
        public void BuildVariableMap()
        {
            m_VariableMap.Clear();
            foreach (var v in m_Variables)
            {
                if (v != null && !string.IsNullOrEmpty(v.Name))
                    m_VariableMap[v.Name] = v;
            }
        }

        // ── 全局变量访问接口 ──

        public BaseExposedProperty GetVariable(string name)
        {
            m_VariableMap.TryGetValue(name, out var ep);
            return ep;
        }

        public T GetVariable<T>(string name) where T : BaseExposedProperty
            => GetVariable(name) as T;

        public void SetVariable(string name, object value)
        {
            if (m_VariableMap.TryGetValue(name, out var ep))
                ep.SetValue(value);
        }

        // ── per-Tree 数据管理 ──

        /// <summary>
        /// 注册一棵 Tree，创建其独立的 BlackboardContext。
        /// 自动遍历节点 PropertyPort 创建 EP + 克隆 SO ExposedProperties。
        /// 返回后调用方可向 ctx.EPMap 注入额外上下文 EP。
        /// </summary>
        public BlackboardContext RegisterTree(BaseTree tree)
        {
            if (m_Contexts.TryGetValue(tree, out var existing))
            {
                Debug.LogWarning($"[CommonBlackboard] Tree '{tree.name}' already registered.");
                return existing;
            }

            var ctx = new BlackboardContext { Owner = this };

            // 1. 为每个节点创建 NodeBlackboardData
            foreach (var node in tree.Nodes)
            {
                var nodeData = CreateNodeData(node);
                ctx.NodeDataMap[node.GUID] = nodeData;
            }

            // 2. 克隆 SO ExposedProperties 到 EPMap
            foreach (var ep in tree.ExposedProperties)
            {
                if (ep != null && !string.IsNullOrEmpty(ep.Name))
                    ctx.EPMap[ep.Name] = CloneExposedProperty(ep);
            }

            m_Contexts[tree] = ctx;
            return ctx;
        }

        /// <summary>注销一棵 Tree，删除其 BlackboardContext</summary>
        public void UnregisterTree(BaseTree tree)
        {
            m_Contexts.Remove(tree);
        }

        /// <summary>获取指定 Tree 的 BlackboardContext</summary>
        public BlackboardContext GetContext(BaseTree tree)
        {
            m_Contexts.TryGetValue(tree, out var ctx);
            return ctx;
        }

        /// <summary>将 Tree 的 Context 绑定到 Tree 上（BeginContext 调用）</summary>
        public void BindTree(BaseTree tree)
        {
            if (m_Contexts.TryGetValue(tree, out var ctx))
                tree.BindBlackboardContext(ctx, this);
        }

        /// <summary>解绑 Tree 的 Context（EndContext 调用）</summary>
        public void UnbindTree(BaseTree tree)
        {
            tree.UnbindBlackboardContext();
        }

        // ── NodeBlackboardData 创建 ──

        /// <summary>为单个节点创建 NodeBlackboardData，遍历其 PropertyPort 创建 EP</summary>
        private NodeBlackboardData CreateNodeData(BaseNode node)
        {
            var data = new NodeBlackboardData { State = State.None };

            foreach (var kv in node.PropertyPortMap)
            {
                var port = kv.Value;
                var ep = CreateEPForPort(port);
                if (ep == null) continue;

                ep.Name = port.Name;

                if (port.Direction == PortDirection.Input)
                {
                    ep.SetValue(port.GetValue());
                    data.InputProperties[port.Name] = ep;
                }
                else
                {
                    data.OutputProperties[port.Name] = ep;
                }
            }

            // 调用节点的运行时属性注册
            if (node is RunnableNode rn)
            {
                rn.OnRegisterRuntimeProperties(data.RuntimeProperties);
            }

            return data;
        }

        // ── EP 工厂方法 ──

        /// <summary>
        /// 根据 PropertyPort 的值类型创建对应的 ExposedProperty。
        /// 支持所有标准类型，非标准类型返回 null。
        /// </summary>
        public static BaseExposedProperty CreateEPForPort(PropertyPort port)
        {
            var valueType = port.ValueType;
            if (valueType == null) return null;

            if (valueType == typeof(bool))   return new BoolExposedProperty();
            if (valueType == typeof(int))    return new IntExposedProperty();
            if (valueType == typeof(float))  return new FloatExposedProperty();
            if (valueType == typeof(string)) return new StringExposedProperty();
            if (valueType == typeof(Vector2)) return new Vector2ExposedProperty();
            if (valueType == typeof(Vector3)) return new Vector3ExposedProperty();

            // 非标准类型：尝试通过反射创建泛型 EP
            // 如果已有对应的 ExposedProperty 子类（如 AnimancerStateExposedProperty），
            // 调用方可在 RegisterTree 后手动替换
            return null;
        }

        /// <summary>克隆单个 ExposedProperty</summary>
        public static BaseExposedProperty CloneExposedProperty(BaseExposedProperty source)
        {
            var clone = (BaseExposedProperty)Activator.CreateInstance(source.GetType());
            clone.Name = source.Name;
            clone.GUID = source.GUID;
            clone.SetValue(source.GetValue());
            return clone;
        }
    }
}
