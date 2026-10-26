using System;
using UnityEngine;

namespace Vtcong.Pool
{
    /// <summary>
    /// Khai bao pool trong Inspector: tao pool + prewarm luc scene load, tuy chon xoa pool khi scene unload.
    /// Pool van song qua scene (nam trong [FastPoolManager]) neu khong bat clearOnDestroy.
    /// </summary>
    public sealed class FastPoolPreloader : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            public GameObject prefab;
            [Min(0)] public int prewarmCount;
            [Tooltip("0 la khong gioi han.")]
            [Min(0)] public int capacity;
            public bool keepParentOnReturn;
        }

        [SerializeField] private Entry[] pools = Array.Empty<Entry>();
        [SerializeField] private bool clearOnDestroy;

        [Tooltip("So object huy moi frame khi clearOnDestroy. 0 la huy het trong 1 frame.")]
        [SerializeField, Min(0)] private int destroyPerFrame = 20;

        // awake (khong phai start) de pool co dung capacity truoc khi script khac GetPool trong Start.
        private void Awake()
        {
            for (int i = 0; i < pools.Length; i++)
            {
                Entry entry = pools[i];
                if (entry == null || entry.prefab == null)
                {
                    Debug.LogWarning($"[FastPoolPreloader] '{name}' pools[{i}] chua gan prefab.", this);
                    continue;
                }

                FastPool.CreatePool(entry.prefab, entry.prewarmCount, null, entry.capacity, entry.keepParentOnReturn);
            }
        }

        private void OnDestroy()
        {
            if (!clearOnDestroy) return;

            for (int i = 0; i < pools.Length; i++)
            {
                Entry entry = pools[i];
                if (entry == null || entry.prefab == null) continue;

                if (destroyPerFrame > 0) FastPool.ClearPoolSlowly(entry.prefab, destroyPerFrame);
                else FastPool.ClearPool(entry.prefab);
            }
        }
    }
}
