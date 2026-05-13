using System;
using System.Collections.Generic;
using UnityEngine;
using TreeDesigner;

namespace UnityTimeline.Blackboard
{
    /// <summary>
    /// 运行时存储引擎。双层字典 Dict&lt;slotKey, Dict&lt;name, object&gt;&gt;。
    ///
    /// 特殊槽位:
    ///   - slotKey=0 (GLOBAL_SLOT_KEY): 公共数据区域，所有消费者共享
    ///   - slotKey&gt;0: 每个消费者的隔离槽位
    ///
    /// 能力:
    ///   - AllocateSlot / ReleaseSlot: 消费者生命周期管理
    ///   - AllocateGlobalSlot: 初始化公共区域
    ///   - AddEntry / RemoveEntry: 运行时动态增删变量（对任何槽位有效）
    ///   - UnregisterAll: 批量清理（可选项是否包含 Global Slot）
    ///   - ClearAll: 兜底全清
    /// </summary>
    public class BlackboardStore
    {
        /// <summary>公共区域的固定 slotKey</summary>
        public const int GLOBAL_SLOT_KEY = 0;

        readonly Dictionary<int, Dictionary<string, object>> m_Slots = new();
        readonly Dictionary<int, int> m_SlotToOwner = new();          // slotKey → ownerInstanceId
        readonly Dictionary<int, SlotMetadata> m_SlotMeta = new();    // slotKey → 元数据（调试用）

        readonly List<BlackboardEntry> m_GlobalTemplateEntries;      // 全局 Schema 模板（注册 consumer 时复制用）

        [Serializable]
        public struct SlotMetadata
        {
            public int OwnerInstanceId;
            public string TreeName;
            public string TreeType;
            public bool IsGlobal;  // 是否是公共区域
        }

        public BlackboardStore(List<BlackboardEntry> globalTemplateEntries)
        {
            m_GlobalTemplateEntries = globalTemplateEntries ?? new List<BlackboardEntry>();
        }

        // ═══════════════════════════════════════════════════════
        //  SlotKey 计算
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 计算指定树的 slotKey。
        /// AnimancerAbility 以 (OwnerInstanceID + Asset GUID) 为维度（因 SO 共享）。
        /// </summary>
        public static int ComputeSlotKey(int ownerInstanceId, BaseTree tree)
        {
            if (tree == null) return -1;
            // AnimancerAbility 等 SO 资产：以 Asset 实例 ID 隔离
            int assetHash = tree.GetInstanceID();
            return (ownerInstanceId * 31) ^ assetHash;
        }

        // ═══════════════════════════════════════════════════════
        //  公共区域 (Global Slot)
        // ═══════════════════════════════════════════════════════

        /// <summary>初始化/重置公共数据区域。重复调用安全（幂等+覆盖）。</summary>
        public void AllocateGlobalSlot(List<BlackboardEntry> globalEntries)
        {
            // 先清除旧的（如果存在）
            Dictionary<string, object> data;
            if (m_Slots.TryGetValue(GLOBAL_SLOT_KEY, out var oldData))
            {
                oldData.Clear();
                data = oldData;
            }
            else
            {
                data = new Dictionary<string, object>();
            }

            if (globalEntries != null)
            {
                foreach (var entry in globalEntries)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Name))
                        data[entry.Name] = entry.GetTypedDefaultValue();
                }
            }

            m_Slots[GLOBAL_SLOT_KEY] = data;
            m_SlotToOwner[GLOBAL_SLOT_KEY] = 0;  // owner=0 表示公共
            m_SlotMeta[GLOBAL_SLOT_KEY] = new SlotMetadata
            {
                OwnerInstanceId = 0,
                TreeName = "[Global]",
                TreeType = "Shared",
                IsGlobal = true,
            };
        }

        /// <summary>公共区域是否存在</summary>
        public bool HasGlobalSlot => m_Slots.ContainsKey(GLOBAL_SLOT_KEY);

        // ═══════════════════════════════════════════════════════
        //  消费者注册 / 注销
        // ═══════════════════════════════════════════════════════

        /// <summary>注册消费者，分配独立隔离槽位。返回 slotKey。已注册则跳过（幂等）。</summary>
        public int AllocateSlot(int ownerInstanceId, BaseTree tree, List<BlackboardEntry> localEntries)
        {
            int slotKey = ComputeSlotKey(ownerInstanceId, tree);
            if (slotKey < 0) return -1;

            if (m_Slots.ContainsKey(slotKey))
            {
                return slotKey; // 幂等：已注册则跳过
            }

            // 合并 Schema = 全局模板 + 局部（后者覆盖同名）
            var mergedSchema = MergeSchema(m_GlobalTemplateEntries, localEntries);

            // 初始化槽位数据
            var data = new Dictionary<string, object>(mergedSchema.Count);
            foreach (var entry in mergedSchema)
            {
                data[entry.Name] = entry.GetTypedDefaultValue();
            }

            m_Slots[slotKey] = data;
            m_SlotToOwner[slotKey] = ownerInstanceId;
            m_SlotMeta[slotKey] = new SlotMetadata
            {
                OwnerInstanceId = ownerInstanceId,
                TreeName = tree.name,
                TreeType = tree.GetType().Name,
                IsGlobal = false,
            };

            return slotKey;
        }

        // ═══════════════════════════════════════════════════════
        //  三层清理机制
        // ═══════════════════════════════════════════════════════

        /// <summary>L1: 精确释放单个隔离槽位（幂等安全）。不会释放 Global Slot。</summary>
        public void ReleaseSlot(int slotKey)
        {
            if (slotKey == GLOBAL_SLOT_KEY) return; // 保护：L1 不允许直接释放 Global Slot
            InternalReleaseSlot(slotKey);
        }

        /// <summary>L2: 按 ownerInstanceId 批量释放该角色的所有隔离槽位（默认保留 Global Slot）</summary>
        /// <param name="includeGlobal">是否同时释放公共区域，默认 false</param>
        public void UnregisterAll(int ownerInstanceId, bool includeGlobal = false)
        {
            var toRemove = new List<int>();
            foreach (var kvp in m_SlotToOwner)
            {
                if (kvp.Key == GLOBAL_SLOT_KEY && !includeGlobal) continue;
                if (kvp.Value == ownerInstanceId || (includeGlobal && kvp.Key == GLOBAL_SLOT_KEY))
                    toRemove.Add(kvp.Key);
            }
            foreach (var k in toRemove) InternalReleaseSlot(k);
        }

        /// <summary>L3: 兜底释放全部（包括 Global Slot）</summary>
        public void ClearAll()
        {
            foreach (var kvp in m_Slots) kvp.Value.Clear();
            m_Slots.Clear();
            m_SlotToOwner.Clear();
            m_SlotMeta.Clear();
        }

        // ═══════════════════════════════════════════════════════
        //  ★ 运行时动态增删变量（对任意槽位有效，包括 Global Slot）★
        // ═══════════════════════════════════════════════════════

        /// <summary>向指定槽位动态添加新变量（同名覆盖值）</summary>
        public bool AddEntry(int slotKey, BlackboardEntry entry)
        {
            if (!m_Slots.ContainsKey(slotKey)) return false;
            if (string.IsNullOrWhiteSpace(entry.Name)) return false;
            m_Slots[slotKey][entry.Name] = entry.GetTypedDefaultValue();
            return true;
        }

        /// <summary>泛型便捷重载</summary>
        public bool AddEntry<T>(int slotKey, string name, T defaultValue = default)
        {
            if (!m_Slots.ContainsKey(slotKey)) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            m_Slots[slotKey][name] = defaultValue;
            return true;
        }

        /// <summary>从指定槽位删除变量（无论来源均可删除）</summary>
        public bool RemoveEntry(int slotKey, string name)
        {
            if (!m_Slots.TryGetValue(slotKey, out var data)) return false;
            return data.Remove(name);
        }

        public int AddEntries(int slotKey, IEnumerable<BlackboardEntry> entries)
        {
            if (!m_Slots.ContainsKey(slotKey)) return 0;
            int count = 0;
            foreach (var entry in entries) { if (AddEntry(slotKey, entry)) count++; }
            return count;
        }

        public IEnumerable<string> GetVariableNames(int slotKey) =>
            m_Slots.TryGetValue(slotKey, out var d) ? d.Keys : System.Array.Empty<string>();

        public int GetVariableCount(int slotKey) =>
            m_Slots.TryGetValue(slotKey, out var d) ? d.Count : 0;

        // ═══════════════════════════════════════════════════════
        //  读写 API（容错）
        // ═══════════════════════════════════════════════════════

        public T GetValue<T>(int slotKey, string name)
        {
            if (!m_Slots.TryGetValue(slotKey, out var data)) return default;
            if (!data.TryGetValue(name, out var rawVal)) return default;
            return rawVal is T typed ? typed : default;
        }

        public void SetValue<T>(int slotKey, string name, T value)
        {
            if (!m_Slots.TryGetValue(slotKey, out var data)) return;
            data[name] = value;
        }

        public object GetValueObject(int slotKey, string name)
        {
            if (!m_Slots.TryGetValue(slotKey, out var data)) return null;
            return data.TryGetValue(name, out var v) ? v : null;
        }

        public void SetValueObject(int slotKey, string name, object value)
        {
            if (!m_Slots.TryGetValue(slotKey, out var data)) return;
            data[name] = value;
        }

        public bool HasSlot(int slotKey) => m_Slots.ContainsKey(slotKey);
        public bool HasValue(int slotKey, string name) =>
            m_Slots.TryGetValue(slotKey, out var d) && d.ContainsKey(name);

        // ═══════════════════════════════════════════════════════
        //  调试 / 查询
        // ═══════════════════════════════════════════════════════

        public int RegisteredSlotCount => m_Slots.Count;
        public int ConsumerSlotCount => m_Slots.Count - (HasGlobalSlot ? 1 : 0); // 排除 Global
        public IEnumerable<int> GetAllSlotKeys() => m_Slots.Keys;
        public IEnumerable<KeyValuePair<string, object>> GetSlotData(int slotKey) =>
            m_Slots.TryGetValue(slotKey, out var d) ? d : EmptyData;
        public bool TryGetMeta(int slotKey, out SlotMetadata meta) =>
            m_SlotMeta.TryGetValue(slotKey, out meta);

        static readonly IReadOnlyDictionary<string, object> EmptyData =
            new Dictionary<string, object>();

        // ═══════════════════════════════════════════════════════
        //  内部工具
        // ═══════════════════════════════════════════════════════

        static List<BlackboardEntry> MergeSchema(List<BlackboardEntry> globalTpl, List<BlackboardEntry> local)
        {
            var result = new List<BlackboardEntry>((globalTpl?.Count ?? 0) + (local?.Count ?? 0));
            var usedNames = new HashSet<string>();
            if (globalTpl != null)
            {
                foreach (var e in globalTpl)
                {
                    if (!string.IsNullOrWhiteSpace(e.Name) && usedNames.Add(e.Name))
                        result.Add(e);
                }
            }
            if (local != null)
            {
                foreach (var e in local)
                {
                    if (!string.IsNullOrWhiteSpace(e.Name))
                    {
                        usedNames.Add(e.Name); // 允许局部覆盖全局同名
                        result.Add(e.Clone());
                    }
                }
            }
            return result;
        }

        void InternalReleaseSlot(int slotKey)
        {
            if (m_Slots.TryGetValue(slotKey, out var data)) { data.Clear(); m_Slots.Remove(slotKey); }
            m_SlotToOwner.Remove(slotKey);
            m_SlotMeta.Remove(slotKey);
        }
    }
}
