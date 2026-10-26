using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vtcong.Pool
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnReturn();
    }

    public readonly struct FastPoolInfo
    {
        public readonly int Id;
        public readonly GameObject Prefab;
        public readonly int IdleCount;
        public readonly int ActiveCount;
        public readonly int Capacity;
        public readonly bool KeepParentOnReturn;

        public FastPoolInfo(int id, GameObject prefab, int idleCount, int activeCount, int capacity,
            bool keepParentOnReturn)
        {
            Id = id;
            Prefab = prefab;
            IdleCount = idleCount;
            ActiveCount = activeCount;
            Capacity = capacity;
            KeepParentOnReturn = keepParentOnReturn;
        }
    }

    public static class FastPool
    {
        private sealed class PoolData
        {
            public readonly int Id;
            public readonly int PrefabId;
            public readonly GameObject Prefab;
            public readonly Vector3 PrefabScale;
            public readonly int Capacity;
            public readonly bool KeepParentOnReturn;

            public Transform Parent;
            public bool OwnsParent;

            public readonly Stack<FastPoolObject> Idle = new();
            public readonly HashSet<FastPoolObject> Active = new();

            public PoolData(int id, GameObject prefab, Transform parent, bool ownsParent, int capacity,
                bool keepParentOnReturn)
            {
                Id = id;
                PrefabId = prefab.GetInstanceID();
                Prefab = prefab;
                PrefabScale = prefab.transform.localScale;
                Capacity = capacity;
                KeepParentOnReturn = keepParentOnReturn;
                Parent = parent;
                OwnsParent = ownsParent;
            }
        }

        [DisallowMultipleComponent]
        private sealed class FastPoolObject : MonoBehaviour
        {
            public int PoolId;
            public int GoId;
            public bool IsPooled;
            public IPoolable[] Poolables;

            private void OnDestroy()
            {
                Unregister(this);
            }
        }

        // chi de chay coroutine cho ClearPoolSlowly / ClearAllSlowly.
        private sealed class FastPoolRunner : MonoBehaviour
        {
        }

        // Key = prefab.GetInstanceID() -> tra cuu bang int, tranh chi phi so sanh cua UnityEngine.Object.
        private static readonly Dictionary<int, PoolData> PoolsByPrefab = new();
        private static readonly Dictionary<int, PoolData> PoolsById = new();

        // Key = gameObject.GetInstanceID() -> ReturnPool khong can GetComponent.
        private static readonly Dictionary<int, FastPoolObject> LiveObjects = new();

        private static readonly IPoolable[] EmptyPoolables = Array.Empty<IPoolable>();

        private static Transform _manager;
        private static FastPoolRunner _runner;
        private static int _nextPoolId = 1;

        // object da go khoi pool, cho ClearPoolSlowly / ClearAllSlowly huy dan.
        private static readonly Queue<GameObject> PendingDestroy = new();
        private static bool _isDestroying;
        private static FastPoolRunner _destroyHost;
        private static int _destroyPerFrame;
        private static bool _isQuitting;

        // =========================================================
        // LIFECYCLE
        // =========================================================

        /// <summary>
        /// Reset toan bo state static. Bat buoc khi bat "Enter Play Mode Options"
        /// (Disable Domain Reload) vi static se song xuyen qua cac lan Play.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            PoolsByPrefab.Clear();
            PoolsById.Clear();
            LiveObjects.Clear();

            _manager = null;
            _runner = null;
            _nextPoolId = 1;

            PendingDestroy.Clear();
            _isDestroying = false;
            _destroyHost = null;
            _isQuitting = false;

            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            _isQuitting = true;
        }

        // =========================================================
        // CREATE
        // =========================================================

        /// <param name="keepParentOnReturn">
        /// True: ReturnPool chi SetActive(false), giu nguyen parent va transform. Hop voi UI
        /// (tranh rebuild Canvas khi doi parent). Chi co tac dung khi pool duoc tao lan dau.
        /// </param>
        public static void CreatePool(GameObject prefab, int prewarmCount = 0, Transform poolParent = null,
            int capacity = 0, bool keepParentOnReturn = false)
        {
            GetOrCreatePool(prefab, poolParent, capacity, prewarmCount, keepParentOnReturn);
        }

        private static PoolData GetOrCreatePool(GameObject prefab, Transform poolParent, int capacity,
            int prewarmCount, bool keepParentOnReturn = false)
        {
            if (prefab == null)
            {
                Debug.LogError("[FastPool] Prefab is null.");
                return null;
            }

            if (_isQuitting) return null;

            if (PoolsByPrefab.TryGetValue(prefab.GetInstanceID(), out PoolData pool))
            {
                if (capacity != 0 && capacity != pool.Capacity)
                {
                    Debug.LogWarning($"[FastPool] Pool '{prefab.name}' da ton tai voi capacity {pool.Capacity}, " +
                                     $"bo qua capacity {capacity}.");
                }

                if (keepParentOnReturn && !pool.KeepParentOnReturn)
                {
                    Debug.LogWarning($"[FastPool] Pool '{prefab.name}' da ton tai, bo qua keepParentOnReturn.");
                }

                if (prewarmCount > 0) Prewarm(pool, prewarmCount);
                return pool;
            }

            // Khong dung "??=" / "?." voi UnityEngine.Object: cac toan tu nay bo qua
            // operator== da override nen mot Transform da Destroy se bi coi la khong null.
            bool ownsParent = poolParent == null;
            if (ownsParent) poolParent = CreatePoolParent(prefab);

            int poolId = _nextPoolId++;
            pool = new PoolData(poolId, prefab, poolParent, ownsParent, Mathf.Max(0, capacity),
                keepParentOnReturn);

            PoolsByPrefab.Add(pool.PrefabId, pool);
            PoolsById.Add(poolId, pool);

            if (prewarmCount > 0) Prewarm(pool, prewarmCount);
            return pool;
        }

        // =========================================================
        // GET
        // =========================================================

        public static GameObject GetPool(GameObject prefab, Transform parent = null, Transform posSpawn = null)
        {
            FastPoolObject poolObject = Take(prefab, parent);
            if (poolObject == null) return null;

            if (posSpawn != null)
            {
                Transform target = poolObject.transform;
                target.SetPositionAndRotation(posSpawn.position, posSpawn.rotation);
            }

            Activate(poolObject);
            return poolObject.gameObject;
        }

        public static GameObject GetPool(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null)
        {
            FastPoolObject poolObject = Take(prefab, parent);
            if (poolObject == null) return null;

            poolObject.transform.SetPositionAndRotation(position, rotation);

            Activate(poolObject);
            return poolObject.gameObject;
        }

        public static T GetPool<T>(GameObject prefab, Transform parent = null, Transform posSpawn = null)
            where T : Component
        {
            GameObject target = GetPool(prefab, parent, posSpawn);
            return ResolveComponent<T>(target);
        }

        public static T GetPool<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            GameObject target = GetPool(prefab, position, rotation, parent);
            return ResolveComponent<T>(target);
        }

        private static T ResolveComponent<T>(GameObject target) where T : Component
        {
            if (target == null) return null;

            if (target.TryGetComponent(out T component)) return component;

            // Tra lai pool thay vi de object mo coi (khong ai giu tham chieu de ReturnPool).
            Debug.LogError($"[FastPool] '{target.name}' khong co component {typeof(T).Name}.");
            ReturnPool(target);
            return null;
        }

        private static FastPoolObject Take(GameObject prefab, Transform parent)
        {
            if (prefab == null)
            {
                Debug.LogError("[FastPool] GetPool prefab is null.");
                return null;
            }

            if (_isQuitting) return null;

            if (!PoolsByPrefab.TryGetValue(prefab.GetInstanceID(), out PoolData pool))
            {
                pool = GetOrCreatePool(prefab, null, 0, 0);
                if (pool == null) return null;
            }

            FastPoolObject poolObject = PopIdle(pool);
            if (poolObject == null) poolObject = CreateObject(pool);
            if (poolObject == null) return null;

            poolObject.IsPooled = false;
            pool.Active.Add(poolObject);

            Transform target = poolObject.transform;
            target.SetParent(parent, false);
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = pool.PrefabScale;

            return poolObject;
        }

        private static void Activate(FastPoolObject poolObject)
        {
            poolObject.gameObject.SetActive(true);
            NotifySpawn(poolObject);
        }

        /// <summary>
        /// Bo qua cac entry da bi Destroy tu ben ngoai (vd: unload scene chua poolParent do user cung cap).
        /// </summary>
        private static FastPoolObject PopIdle(PoolData pool)
        {
            while (pool.Idle.Count > 0)
            {
                FastPoolObject poolObject = pool.Idle.Pop();
                if (poolObject != null) return poolObject;
            }

            return null;
        }

        // =========================================================
        // RETURN
        // =========================================================

        public static void ReturnPool(GameObject target)
        {
            if (target == null) return;

            if (!LiveObjects.TryGetValue(target.GetInstanceID(), out FastPoolObject poolObject) ||
                poolObject == null)
            {
                Debug.LogWarning($"[FastPool] '{target.name}' does not belong to FastPool.");
                return;
            }

            if (poolObject.IsPooled)
            {
                Debug.LogWarning($"[FastPool] '{target.name}' was already returned.");
                return;
            }

            if (!PoolsById.TryGetValue(poolObject.PoolId, out PoolData pool))
            {
                Debug.LogWarning($"[FastPool] Pool '{poolObject.PoolId}' no longer exists.");
                return;
            }

            ReturnObject(poolObject, pool);
        }

        public static void ReturnPool(Component target)
        {
            if (target == null) return;
            ReturnPool(target.gameObject);
        }

        /// <summary>Tra tat ca object dang active cua 1 prefab ve pool.</summary>
        public static void ReturnAll(GameObject prefab)
        {
            if (prefab == null) return;
            if (!PoolsByPrefab.TryGetValue(prefab.GetInstanceID(), out PoolData pool)) return;

            ReturnAll(pool);
        }

        /// <summary>Tra tat ca object dang active cua moi pool ve pool.</summary>
        public static void ReturnAll()
        {
            foreach (PoolData pool in PoolsById.Values) ReturnAll(pool);
        }

        private static void ReturnAll(PoolData pool)
        {
            if (pool.Active.Count == 0) return;

            // Snapshot bang buffer cuc bo: ReturnObject sua pool.Active, va callback OnReturn
            // cua nguoi dung co the goi de quy vao ReturnAll/ClearPool. Day la thao tac hiem
            // (chuyen man, reset) nen 1 lan cap phat la chap nhan duoc, doi lai khong co
            // buffer dung chung de bi hong khi tai vao.
            FastPoolObject[] buffer = new FastPoolObject[pool.Active.Count];
            pool.Active.CopyTo(buffer);

            // ReturnObject tu go tung phan tu khoi pool.Active. Khong Clear() ca set:
            // callback OnReturn co the spawn object moi va object do phai duoc giu trong Active.
            for (int i = 0; i < buffer.Length; i++)
            {
                FastPoolObject poolObject = buffer[i];
                if (poolObject != null && !poolObject.IsPooled) ReturnObject(poolObject, pool);
            }
        }

        private static void ReturnObject(FastPoolObject poolObject, PoolData pool)
        {
            NotifyReturn(poolObject);

            poolObject.IsPooled = true;
            pool.Active.Remove(poolObject);

            GameObject target = poolObject.gameObject;
            target.SetActive(false);

            if (pool.Capacity > 0 && pool.Idle.Count >= pool.Capacity)
            {
                Release(poolObject);
                return;
            }

            if (pool.KeepParentOnReturn)
            {
                pool.Idle.Push(poolObject);
                return;
            }

            Transform targetTransform = poolObject.transform;
            targetTransform.SetParent(EnsureParent(pool), false);
            targetTransform.localPosition = Vector3.zero;
            targetTransform.localRotation = Quaternion.identity;
            targetTransform.localScale = pool.PrefabScale;

            pool.Idle.Push(poolObject);
        }

        // =========================================================
        // PREWARM
        // =========================================================

        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;

            PoolData pool = GetOrCreatePool(prefab, null, 0, 0);
            if (pool == null) return;

            Prewarm(pool, count);
        }

        private static void Prewarm(PoolData pool, int count)
        {
            int createCount = count;
            if (pool.Capacity > 0) createCount = Mathf.Min(count, pool.Capacity - pool.Idle.Count);

            for (int i = 0; i < createCount; i++)
            {
                FastPoolObject poolObject = CreateObject(pool);
                if (poolObject == null) return;

                poolObject.IsPooled = true;
                pool.Idle.Push(poolObject);
            }
        }

        // =========================================================
        // CLEAR
        // =========================================================

        public static void ClearPool(GameObject prefab)
        {
            if (prefab == null) return;
            if (!PoolsByPrefab.TryGetValue(prefab.GetInstanceID(), out PoolData pool)) return;

            PoolsByPrefab.Remove(pool.PrefabId);
            PoolsById.Remove(pool.Id);

            DestroyPool(pool);
        }

        public static void ClearAll()
        {
            FlushPendingDestroy();

            foreach (PoolData pool in PoolsById.Values) DestroyPool(pool);

            PoolsByPrefab.Clear();
            PoolsById.Clear();
            LiveObjects.Clear();

            if (_manager != null) SafeDestroy(_manager.gameObject);
            _manager = null;

            // KHONG reset _nextPoolId: object dang active van giu PoolId cu.
            // Cap lai id se khien chung duoc tra vao nham pool khac prefab.
        }

        /// <summary>
        /// Nhu ClearPool nhung object dang nam trong pool duoc huy dan, toi da destroyPerFrame moi frame,
        /// tranh khung hinh khi don pool lon. Object dang active van bi huy ngay nhu ClearPool.
        /// Pool bi go khoi registry ngay, GetPool sau do se tao pool moi.
        /// </summary>
        public static void ClearPoolSlowly(GameObject prefab, int destroyPerFrame = 20)
        {
            if (prefab == null) return;
            if (!PoolsByPrefab.TryGetValue(prefab.GetInstanceID(), out PoolData pool)) return;

            if (!CanDestroySlowly(destroyPerFrame))
            {
                ClearPool(prefab);
                return;
            }

            PoolsByPrefab.Remove(pool.PrefabId);
            PoolsById.Remove(pool.Id);

            DetachPool(pool);
            StartDestroyingPending(destroyPerFrame);
        }

        /// <summary>Nhu ClearAll nhung huy dan qua nhieu frame. [FastPoolManager] duoc giu lai.</summary>
        public static void ClearAllSlowly(int destroyPerFrame = 20)
        {
            if (!CanDestroySlowly(destroyPerFrame))
            {
                ClearAll();
                return;
            }

            foreach (PoolData pool in PoolsById.Values) DetachPool(pool);

            PoolsByPrefab.Clear();
            PoolsById.Clear();

            StartDestroyingPending(destroyPerFrame);
        }

        private static bool CanDestroySlowly(int destroyPerFrame)
        {
            if (destroyPerFrame <= 0 || !Application.isPlaying || _isQuitting) return false;

            EnsureManager();
            return _runner != null;
        }

        private static void DetachPool(PoolData pool)
        {
            FastPoolObject[] active = new FastPoolObject[pool.Active.Count];
            pool.Active.CopyTo(active);
            pool.Active.Clear();

            for (int i = 0; i < active.Length; i++)
            {
                if (active[i] != null) Release(active[i]);
            }

            while (pool.Idle.Count > 0)
            {
                FastPoolObject poolObject = pool.Idle.Pop();
                if (poolObject == null) continue;

                Detach(poolObject);
                PendingDestroy.Enqueue(poolObject.gameObject);
            }

            // parent xep cuoi: huy parent truoc se keo theo moi object con trong cung 1 frame.
            if (pool.OwnsParent && pool.Parent != null) PendingDestroy.Enqueue(pool.Parent.gameObject);
            pool.Parent = null;
        }

        private static void StartDestroyingPending(int destroyPerFrame)
        {
            _destroyPerFrame = destroyPerFrame;

            // runner co the da bi huy cung [FastPoolManager] -> coroutine cu chet, phai chay lai.
            if (_isDestroying && _destroyHost == _runner) return;

            _destroyHost = _runner;
            _isDestroying = true;
            _runner.StartCoroutine(DestroyPendingGradually());
        }

        private static IEnumerator DestroyPendingGradually()
        {
            while (PendingDestroy.Count > 0)
            {
                for (int i = 0; i < _destroyPerFrame && PendingDestroy.Count > 0; i++)
                {
                    GameObject target = PendingDestroy.Dequeue();
                    if (target != null) Object.Destroy(target);
                }

                yield return null;
            }

            _isDestroying = false;
            _destroyHost = null;
        }

        private static void FlushPendingDestroy()
        {
            while (PendingDestroy.Count > 0) SafeDestroy(PendingDestroy.Dequeue());

            _isDestroying = false;
            _destroyHost = null;
        }

        private static void DestroyPool(PoolData pool)
        {
            // Snapshot truoc khi destroy: o Edit Mode DestroyImmediate goi OnDestroy ngay lap tuc
            // nen se sua Idle/Active ngay giua vong lap.
            List<FastPoolObject> buffer = new(pool.Idle.Count + pool.Active.Count);

            while (pool.Idle.Count > 0)
            {
                FastPoolObject poolObject = pool.Idle.Pop();
                if (poolObject != null) buffer.Add(poolObject);
            }

            foreach (FastPoolObject poolObject in pool.Active)
            {
                if (poolObject != null) buffer.Add(poolObject);
            }

            pool.Active.Clear();

            for (int i = 0; i < buffer.Count; i++) Release(buffer[i]);

            // Chi huy parent do FastPool tu tao. Parent user truyen vao co the la object
            // dung chung trong scene -> khong duoc dung toi.
            if (pool.OwnsParent && pool.Parent != null) SafeDestroy(pool.Parent.gameObject);
            pool.Parent = null;
        }

        /// <summary>Go khoi registry roi huy. PoolId = 0 de object nay khong bao gio khop pool nao khac.</summary>
        private static void Release(FastPoolObject poolObject)
        {
            Detach(poolObject);
            SafeDestroy(poolObject.gameObject);
        }

        private static void Detach(FastPoolObject poolObject)
        {
            LiveObjects.Remove(poolObject.GoId);
            poolObject.PoolId = 0;
        }

        private static void Unregister(FastPoolObject poolObject)
        {
            LiveObjects.Remove(poolObject.GoId);

            if (poolObject.PoolId != 0 && PoolsById.TryGetValue(poolObject.PoolId, out PoolData pool))
            {
                pool.Active.Remove(poolObject);
            }
        }

        // =========================================================
        // INFO
        // =========================================================

        public static bool HasPool(GameObject prefab)
        {
            return prefab != null && PoolsByPrefab.ContainsKey(prefab.GetInstanceID());
        }

        /// <summary>So object dang nam san trong pool (chua duoc dung).</summary>
        public static int GetPoolCount(GameObject prefab)
        {
            if (prefab == null) return 0;
            return PoolsByPrefab.TryGetValue(prefab.GetInstanceID(), out PoolData pool) ? pool.Idle.Count : 0;
        }

        /// <summary>So object dang duoc su dung (da GetPool, chua ReturnPool).</summary>
        public static int GetActiveCount(GameObject prefab)
        {
            if (prefab == null) return 0;
            return PoolsByPrefab.TryGetValue(prefab.GetInstanceID(), out PoolData pool) ? pool.Active.Count : 0;
        }

        /// <summary>Ghi thong tin moi pool hien co vao results (results bi Clear truoc).</summary>
        public static void GetPoolInfos(List<FastPoolInfo> results)
        {
            if (results == null) return;

            results.Clear();
            foreach (PoolData pool in PoolsById.Values)
            {
                results.Add(new FastPoolInfo(pool.Id, pool.Prefab, pool.Idle.Count, pool.Active.Count, pool.Capacity,
                    pool.KeepParentOnReturn));
            }
        }

        // =========================================================
        // INTERNAL
        // =========================================================

        private static FastPoolObject CreateObject(PoolData pool)
        {
            // Prefab nguon co the da bi huy (vd: prefab la object trong scene da unload).
            if (pool.Prefab == null)
            {
                Debug.LogError($"[FastPool] Prefab nguon cua pool '{pool.Id}' da bi huy.");
                return null;
            }

            Transform parent = EnsureParent(pool);
            if (parent == null) return null;

            GameObject target = Object.Instantiate(pool.Prefab, parent);
            target.SetActive(false);

            Transform targetTransform = target.transform;
            targetTransform.localPosition = Vector3.zero;
            targetTransform.localRotation = Quaternion.identity;
            targetTransform.localScale = pool.PrefabScale;

            FastPoolObject poolObject = target.AddComponent<FastPoolObject>();
            poolObject.PoolId = pool.Id;
            poolObject.GoId = target.GetInstanceID();
            poolObject.IsPooled = true;

            IPoolable[] poolables = target.GetComponentsInChildren<IPoolable>(true);
            poolObject.Poolables = poolables.Length > 0 ? poolables : EmptyPoolables;

            LiveObjects[poolObject.GoId] = poolObject;
            return poolObject;
        }

        private static void NotifySpawn(FastPoolObject poolObject)
        {
            IPoolable[] poolables = poolObject.Poolables;
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawn();
        }

        private static void NotifyReturn(FastPoolObject poolObject)
        {
            IPoolable[] poolables = poolObject.Poolables;
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnReturn();
        }

        /// <summary>Parent co the bi Destroy khi doi scene (neu user tu truyen vao) -> tao lai.</summary>
        private static Transform EnsureParent(PoolData pool)
        {
            if (pool.Parent != null) return pool.Parent;
            if (_isQuitting) return null;

            pool.Parent = CreatePoolParent(pool.Prefab);
            pool.OwnsParent = true;
            return pool.Parent;
        }

        private static Transform CreatePoolParent(GameObject prefab)
        {
            Transform manager = EnsureManager();

            GameObject parent = new(prefab.name);
            parent.transform.SetParent(manager, false);

            return parent.transform;
        }

        private static Transform EnsureManager()
        {
            if (_manager != null) return _manager;

            GameObject manager = new("[FastPoolManager]");
            _manager = manager.transform;

            // Song qua scene load: neu khong, toan bo object trong pool bi huy
            // trong khi dictionary van giu tham chieu chet.
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(manager);
                _runner = manager.AddComponent<FastPoolRunner>();
            }

            return _manager;
        }

        private static void SafeDestroy(Object target)
        {
            if (target == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }
    }
}
