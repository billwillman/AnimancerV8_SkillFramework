using System.Collections.Generic;

namespace TreeDesigner
{
    /// <summary>
    /// per-Tree 运行时数据包。每个 Tree 实例（如一个角色上的某个 AnimancerAbility）
    /// 拥有独立的 BlackboardContext。由 CommonBlackboard.RegisterTree 创建。
    /// </summary>
    public class BlackboardContext
    {
        /// <summary>
        /// per-Node 数据（通过节点 GUID 查找）。
        /// 每个节点拥有 State + Input/Output/Runtime EP 数组。
        /// </summary>
        public Dictionary<string, NodeBlackboardData> NodeDataMap
            = new Dictionary<string, NodeBlackboardData>();

        /// <summary>
        /// Tree 级 EP 变量表（SO ExposedProperty 克隆 + 运行时注入的上下文 EP）。
        /// 按名称查找，如 "Active"、"Duration"、"Agent"、"AnimancerComponent" 等。
        /// </summary>
        public Dictionary<string, BaseExposedProperty> EPMap
            = new Dictionary<string, BaseExposedProperty>();

        /// <summary>树级执行状态</summary>
        public State TreeState;

        /// <summary>树是否正在运行</summary>
        public bool TreeRunning;

        /// <summary>所属的 CommonBlackboard 组件引用（可为 null）</summary>
        public CommonBlackboard Owner;

        /// <summary>获取指定节点的 BlackboardData，不存在返回 null</summary>
        public NodeBlackboardData GetNodeData(string guid)
        {
            NodeDataMap.TryGetValue(guid, out var data);
            return data;
        }

        /// <summary>重置所有节点状态和树级状态（不清除 EPMap）</summary>
        public void Reset()
        {
            foreach (var kv in NodeDataMap)
                kv.Value.State = State.None;
            TreeState = State.None;
            TreeRunning = false;
        }
    }
}
