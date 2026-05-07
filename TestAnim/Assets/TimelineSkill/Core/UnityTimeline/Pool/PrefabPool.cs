using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTimeline
{
    /// <summary>
    /// Prefab 对象池单例管理器。
    /// 使用 InstanceID 注册表方式管理池中实例，无需额外挂载组件。
    /// 每种 Prefab 在 Hierarchy 中有独立的 "XXX Root" 子节点存放回收实例。
    /// 同时维护 Active 字典，通过分配的 Name（格式: {PrefabName}_{InstanceID}）管理激活实例。
    /// </summary>
    public class PrefabPool : MonoBehaviour
    {
        private static PrefabPool s_Instance;
        private static bool s_IsQuitting;

        public static PrefabPool Instance
        {
            get
            {
                if (s_IsQuitting) return null;
                if (s_Instance == null)
                {
                    var go = new GameObject("[PrefabPool]");
                    DontDestroyOnLoad(go);
                    s_Instance = go.AddComponent<PrefabPool>();
                }
                return s_Instance;
            }
        }

        private struct PoolData
        {
            public GameObject Prefab;
            public Transform Root;
            public Queue<GameObject> Inactive;
            public int MaxSize;
            public bool PersistAcrossScenes;
        }

        // Key: prefab.GetInstanceID()
        private Dictionary<int, PoolData> m_Pools = new Dictionary<int, PoolData>();

        // Key: instance.GetInstanceID() → Value: prefab.GetInstanceID()
        private Dictionary<int, int> m_InstanceToPrefabMap = new Dictionary<int, int>();

        // Key: 分配的 Name (格式: {PrefabName}_{InstanceID}) → Value: 激活中的 GameObject
        private Dictionary<string, GameObject> m_ActiveInstances = new Dictionary<string, GameObject>();

        // Key: instance.GetInstanceID() → Value: 分配的 Name
        private Dictionary<int, string> m_InstanceIdToName = new Dictionary<int, string>();

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (s_Instance == this)
                s_Instance = null;
        }

        private void OnApplicationQuit()
        {
            s_IsQuitting = true;
        }

        /// <summary>
        /// 从池中取出或新建实例，返回分配的 Name。
        /// Name 格式: {Prefab原始名}_{InstanceID}
        /// </summary>
        /// <param name="prefab">原始 Prefab</param>
        /// <param name="parent">挂载的父节点</param>
        /// <param name="localPos">本地坐标偏移</param>
        /// <param name="localEuler">本地旋转偏移</param>
        /// <param name="maxPoolSize">该 Prefab 池的上限</param>
        /// <param name="persistAcrossScenes">场景切换时是否保留池</param>
        /// <returns>分配的 Name，可通过此 Name 查找或回收实例</returns>
        public string Spawn(GameObject prefab, Transform parent, Vector3 localPos, Vector3 localEuler,
            int maxPoolSize = 10, bool persistAcrossScenes = false)
        {
            if (prefab == null) return null;

            int prefabId = prefab.GetInstanceID();
            EnsurePoolExists(prefab, prefabId, maxPoolSize, persistAcrossScenes);

            var pool = m_Pools[prefabId];
            GameObject instance;

            if (pool.Inactive.Count > 0)
            {
                instance = pool.Inactive.Dequeue();
                if (instance == null)
                {
                    instance = CreateInstance(prefab, prefabId);
                }
            }
            else
            {
                instance = CreateInstance(prefab, prefabId);
            }

            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPos;
            instance.transform.localEulerAngles = localEuler;
            instance.SetActive(true);

            // 分配 Name 并注册到 Active 字典
            int instanceId = instance.GetInstanceID();
            string assignedName = $"{prefab.name}_{instanceId}";
            instance.name = assignedName;
            m_ActiveInstances[assignedName] = instance;
            m_InstanceIdToName[instanceId] = assignedName;

            return assignedName;
        }

        /// <summary>
        /// 通过分配的 Name 获取激活中的实例。
        /// </summary>
        public GameObject GetActiveInstance(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            m_ActiveInstances.TryGetValue(name, out var instance);
            return instance;
        }

        /// <summary>
        /// 通过分配的 Name 尝试回收实例。
        /// </summary>
        /// <param name="name">Spawn 时返回的 Name</param>
        /// <returns>true = 已回池或已销毁处理，false = 不属于池</returns>
        public bool TryRecycleByName(string name)
        {
            if (s_IsQuitting || string.IsNullOrEmpty(name)) return false;

            if (!m_ActiveInstances.TryGetValue(name, out var instance))
                return false;

            return TryRecycleInternal(instance);
        }

        /// <summary>
        /// 尝试将实例回收到池中（通过 GameObject 引用）。
        /// </summary>
        /// <param name="instance">要回收的 GameObject</param>
        /// <returns>true = 已回池或已销毁处理，false = 不属于池（调用方应自行 Destroy）</returns>
        public bool TryRecycle(GameObject instance)
        {
            if (s_IsQuitting || instance == null) return false;

            int instanceId = instance.GetInstanceID();
            if (!m_InstanceToPrefabMap.ContainsKey(instanceId))
                return false;

            return TryRecycleInternal(instance);
        }

        /// <summary>
        /// 内部回收逻辑。
        /// </summary>
        private bool TryRecycleInternal(GameObject instance)
        {
            if (instance == null) return false;

            int instanceId = instance.GetInstanceID();

            if (!m_InstanceToPrefabMap.TryGetValue(instanceId, out int prefabId))
                return false;

            // 从 Active 字典移除
            if (m_InstanceIdToName.TryGetValue(instanceId, out string name))
            {
                m_ActiveInstances.Remove(name);
                m_InstanceIdToName.Remove(instanceId);
            }

            if (!m_Pools.TryGetValue(prefabId, out var pool))
            {
                m_InstanceToPrefabMap.Remove(instanceId);
                return false;
            }

            // 池满则直接销毁
            if (pool.Inactive.Count >= pool.MaxSize)
            {
                m_InstanceToPrefabMap.Remove(instanceId);
                Object.Destroy(instance);
                return true;
            }

            // 回池
            instance.SetActive(false);
            instance.transform.SetParent(pool.Root, false);
            pool.Inactive.Enqueue(instance);

            return true;
        }

        /// <summary>
        /// 确保指定 Prefab 的池已创建。
        /// </summary>
        private void EnsurePoolExists(GameObject prefab, int prefabId, int maxPoolSize, bool persistAcrossScenes)
        {
            if (m_Pools.ContainsKey(prefabId)) return;

            var rootGo = new GameObject($"{prefab.name} Root");
            rootGo.transform.SetParent(transform, false);

            var poolData = new PoolData
            {
                Prefab = prefab,
                Root = rootGo.transform,
                Inactive = new Queue<GameObject>(),
                MaxSize = maxPoolSize,
                PersistAcrossScenes = persistAcrossScenes
            };

            m_Pools[prefabId] = poolData;
        }

        /// <summary>
        /// 创建新实例并注册到 InstanceID 映射表。
        /// </summary>
        private GameObject CreateInstance(GameObject prefab, int prefabId)
        {
            var instance = Instantiate(prefab);
            int instanceId = instance.GetInstanceID();
            m_InstanceToPrefabMap[instanceId] = prefabId;
            return instance;
        }

        /// <summary>
        /// 场景卸载时清理非持久池。
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            if (s_IsQuitting) return;

            var poolsToRemove = new List<int>();

            foreach (var kvp in m_Pools)
            {
                if (kvp.Value.PersistAcrossScenes) continue;

                var pool = kvp.Value;

                while (pool.Inactive.Count > 0)
                {
                    var go = pool.Inactive.Dequeue();
                    if (go != null)
                    {
                        int goId = go.GetInstanceID();
                        m_InstanceToPrefabMap.Remove(goId);
                        if (m_InstanceIdToName.TryGetValue(goId, out string n))
                        {
                            m_ActiveInstances.Remove(n);
                            m_InstanceIdToName.Remove(goId);
                        }
                        Object.Destroy(go);
                    }
                }

                if (pool.Root != null)
                    Object.Destroy(pool.Root.gameObject);

                poolsToRemove.Add(kvp.Key);
            }

            // 清理属于非持久池的激活实例注册
            var instancesToRemove = new List<int>();
            foreach (var kvp in m_InstanceToPrefabMap)
            {
                if (poolsToRemove.Contains(kvp.Value))
                    instancesToRemove.Add(kvp.Key);
            }
            foreach (var id in instancesToRemove)
            {
                m_InstanceToPrefabMap.Remove(id);
                if (m_InstanceIdToName.TryGetValue(id, out string n))
                {
                    m_ActiveInstances.Remove(n);
                    m_InstanceIdToName.Remove(id);
                }
            }

            foreach (var id in poolsToRemove)
                m_Pools.Remove(id);
        }
    }
}
