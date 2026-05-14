using System.Collections.Generic;

namespace TreeDesigner
{
    /// <summary>
    /// 每个节点在 BlackboardContext 中的 per-instance 运行时数据。
    /// 通过节点 GUID 从 BlackboardContext.NodeDataMap 获取。
    /// 所有运行时可变数据统一存储于此，节点本身不持有任何 [NonSerialized] 临时变量。
    /// </summary>
    public class NodeBlackboardData
    {
        /// <summary>节点执行状态（RunnableNode 专用）</summary>
        public State State;

        /// <summary>
        /// 对应节点 Input PropertyPort 的 EP 字典（Key = PropertyPort.Name）。
        /// RegisterTree 时根据 PropertyPort 类型自动创建对应的 ExposedProperty。
        /// </summary>
        public Dictionary<string, BaseExposedProperty> InputProperties
            = new Dictionary<string, BaseExposedProperty>();

        /// <summary>
        /// 对应节点 Output PropertyPort 的 EP 字典（Key = PropertyPort.Name）。
        /// </summary>
        public Dictionary<string, BaseExposedProperty> OutputProperties
            = new Dictionary<string, BaseExposedProperty>();

        /// <summary>
        /// 额外运行时变量 EP 字典（Key = 变量名，由节点 OnRegisterRuntimeProperties 声明）。
        /// 例如 SequenceNode 的 "CurrentIndex"、PlayAnimancerTimelineNode 的 "Completed"/"IsFailure" 等。
        /// </summary>
        public Dictionary<string, BaseExposedProperty> RuntimeProperties
            = new Dictionary<string, BaseExposedProperty>();

        /// <summary>
        /// 泛型辅助：获取指定 key 的 RuntimeProperty 强类型引用。
        /// 用法：m_NodeData.GetRuntime&lt;int&gt;("CurrentIndex").Value = 0;
        /// </summary>
        public BaseExposedProperty<T> GetRuntime<T>(string key)
            => (BaseExposedProperty<T>)RuntimeProperties[key];

        /// <summary>泛型辅助：获取指定 key 的 InputProperty 强类型引用。</summary>
        public BaseExposedProperty<T> GetInput<T>(string key)
            => (BaseExposedProperty<T>)InputProperties[key];

        /// <summary>泛型辅助：获取指定 key 的 OutputProperty 强类型引用。</summary>
        public BaseExposedProperty<T> GetOutput<T>(string key)
            => (BaseExposedProperty<T>)OutputProperties[key];
    }
}
