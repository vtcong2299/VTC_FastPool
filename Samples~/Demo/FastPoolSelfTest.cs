using System.Collections;
using UnityEngine;

namespace Vtcong.Pool.Samples
{
    /// <summary>
    /// Kiem chung hanh vi cua FastPool, tap trung vao dung cac lo hong da duoc va.
    /// Gan vao 1 GameObject rong va bam Play, ket qua in ra Console.
    /// Luu y: mot vai Warning "[FastPool] ..." la KET QUA MONG DOI cua test, khong phai loi.
    /// </summary>
    public class FastPoolSelfTest : MonoBehaviour
    {
        private class Probe : MonoBehaviour, IPoolable
        {
            public int SpawnCount;
            public int ReturnCount;

            public void OnSpawn() => SpawnCount++;
            public void OnReturn() => ReturnCount++;
        }

        private class ChildProbe : MonoBehaviour, IPoolable
        {
            public static int SpawnCount;

            public void OnSpawn() => SpawnCount++;
            public void OnReturn() { }
        }

        private int _passed;
        private int _failed;
        private Transform _sources;

        private IEnumerator Start()
        {
            _sources = new GameObject("[SelfTest Sources]").transform;
            _sources.gameObject.SetActive(false);

            yield return TestScaleAndTransform();
            yield return TestReuseAndCallbacks();
            yield return TestChildPoolable();
            yield return TestCapacity();
            yield return TestReturnAll();
            yield return TestDoubleReturn();
            yield return TestCustomParentNotDestroyed();
            yield return TestPoolIdNotRecycled();
            yield return TestKeepParentOnReturn();
            yield return TestClearPoolSlowly();
            yield return TestDespawnerLifetime();
            yield return TestGetPoolInfos();

            Debug.Log(_failed == 0
                ? $"<color=#22aa22><b>[FastPool SelfTest] PASS {_passed}/{_passed + _failed}</b></color>"
                : $"<color=#cc2222><b>[FastPool SelfTest] FAIL {_failed}/{_passed + _failed}</b></color>");

            FastPool.ClearAll();
            if (_sources != null) Destroy(_sources.gameObject);
        }

        // ---------------------------------------------------------
        // TESTS
        // ---------------------------------------------------------

        /// <summary>Scale goc cua prefab phai duoc giu, khong bi ep ve Vector3.one.</summary>
        private IEnumerator TestScaleAndTransform()
        {
            GameObject prefab = CreateSource("Scaled", new Vector3(2f, 3f, 4f));

            GameObject spawned = FastPool.GetPool(prefab, new Vector3(5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            Check("Scale giu nguyen sau GetPool", spawned.transform.localScale == new Vector3(2f, 3f, 4f));
            Check("Position dung sau GetPool", spawned.transform.position == new Vector3(5f, 0f, 0f));

            FastPool.ReturnPool(spawned);
            Check("Scale giu nguyen sau ReturnPool", spawned.transform.localScale == new Vector3(2f, 3f, 4f));

            FastPool.ClearPool(prefab);
            yield return null;
        }

        /// <summary>Object phai duoc tai su dung va callback IPoolable phai chay dung so lan.</summary>
        private IEnumerator TestReuseAndCallbacks()
        {
            GameObject prefab = CreateSource("Reuse", Vector3.one);
            prefab.AddComponent<Probe>();

            FastPool.CreatePool(prefab, 3);
            Check("Prewarm tao dung so luong", FastPool.GetPoolCount(prefab) == 3);

            GameObject first = FastPool.GetPool(prefab);
            Check("Active count = 1", FastPool.GetActiveCount(prefab) == 1);
            Check("Pool count = 2", FastPool.GetPoolCount(prefab) == 2);

            int id = first.GetInstanceID();
            FastPool.ReturnPool(first);
            Check("Active count ve 0", FastPool.GetActiveCount(prefab) == 0);

            GameObject second = FastPool.GetPool(prefab);
            Check("Tai su dung dung instance", second.GetInstanceID() == id);

            Probe probe = second.GetComponent<Probe>();
            Check("OnSpawn goi 2 lan", probe.SpawnCount == 2);
            Check("OnReturn goi 1 lan", probe.ReturnCount == 1);

            FastPool.ClearPool(prefab);
            yield return null;
        }

        /// <summary>IPoolable nam o object con cung phai nhan callback.</summary>
        private IEnumerator TestChildPoolable()
        {
            GameObject prefab = CreateSource("WithChild", Vector3.one);

            GameObject child = new("Child");
            child.transform.SetParent(prefab.transform, false);
            child.AddComponent<ChildProbe>();

            ChildProbe.SpawnCount = 0;
            GameObject spawned = FastPool.GetPool(prefab);
            Check("IPoolable o child nhan OnSpawn", ChildProbe.SpawnCount == 1);

            FastPool.ReturnPool(spawned);
            FastPool.ClearPool(prefab);
            yield return null;
        }

        /// <summary>Capacity gioi han so object giu san; phan thua bi huy thay vi don vo han.</summary>
        private IEnumerator TestCapacity()
        {
            GameObject prefab = CreateSource("Capped", Vector3.one);
            FastPool.CreatePool(prefab, capacity: 2);

            GameObject a = FastPool.GetPool(prefab);
            GameObject b = FastPool.GetPool(prefab);
            GameObject c = FastPool.GetPool(prefab);

            FastPool.ReturnPool(a);
            FastPool.ReturnPool(b);
            FastPool.ReturnPool(c);

            Check("Pool khong vuot capacity", FastPool.GetPoolCount(prefab) == 2);
            Check("Active ve 0 sau khi tra het", FastPool.GetActiveCount(prefab) == 0);

            yield return null; // cho Destroy() cuoi frame
            Check("Object vuot capacity bi huy", c == null);

            FastPool.ClearPool(prefab);
            yield return null;
        }

        /// <summary>ReturnAll dua moi object dang dung ve pool.</summary>
        private IEnumerator TestReturnAll()
        {
            GameObject prefab = CreateSource("ReturnAll", Vector3.one);

            for (int i = 0; i < 5; i++) FastPool.GetPool(prefab);
            Check("Spawn 5 object", FastPool.GetActiveCount(prefab) == 5);

            FastPool.ReturnAll(prefab);
            Check("ReturnAll -> active = 0", FastPool.GetActiveCount(prefab) == 0);
            Check("ReturnAll -> pool = 5", FastPool.GetPoolCount(prefab) == 5);

            FastPool.ClearPool(prefab);
            yield return null;
        }

        /// <summary>Tra 2 lan khong duoc lam object bi day vao pool 2 lan.</summary>
        private IEnumerator TestDoubleReturn()
        {
            GameObject prefab = CreateSource("DoubleReturn", Vector3.one);

            GameObject spawned = FastPool.GetPool(prefab);
            FastPool.ReturnPool(spawned);

            Debug.Log("[SelfTest] 1 warning ngay duoi day la mong doi:");
            FastPool.ReturnPool(spawned);

            Check("Tra 2 lan khong nhan doi object trong pool", FastPool.GetPoolCount(prefab) == 1);

            FastPool.ClearPool(prefab);
            yield return null;
        }

        /// <summary>ClearPool khong duoc huy parent do nguoi dung truyen vao.</summary>
        private IEnumerator TestCustomParentNotDestroyed()
        {
            GameObject prefab = CreateSource("CustomParent", Vector3.one);

            GameObject customParent = new("[Shared Container]");
            GameObject sibling = new("Sibling khong thuoc pool");
            sibling.transform.SetParent(customParent.transform, false);

            FastPool.CreatePool(prefab, 2, customParent.transform);
            FastPool.ClearPool(prefab);

            yield return null;
            Check("ClearPool khong huy parent cua user", customParent != null);
            Check("ClearPool khong huy object khac trong parent", sibling != null);

            Destroy(customParent);
            yield return null;
        }

        /// <summary>
        /// Sau ClearAll, pool moi khong duoc cap lai id cu -> object cu khong the bi
        /// tra nham vao pool cua mot prefab khac.
        /// </summary>
        private IEnumerator TestPoolIdNotRecycled()
        {
            GameObject prefabA = CreateSource("PrefabA", Vector3.one);
            GameObject prefabB = CreateSource("PrefabB", Vector3.one);

            GameObject fromA = FastPool.GetPool(prefabA);
            FastPool.ClearAll();

            FastPool.CreatePool(prefabB);

            Debug.Log("[SelfTest] 1 warning ngay duoi day la mong doi:");
            FastPool.ReturnPool(fromA); // object cua pool cu, khong duoc chui vao pool cua B

            Check("Object cu khong lot vao pool moi", FastPool.GetPoolCount(prefabB) == 0);

            GameObject fromB = FastPool.GetPool(prefabB);
            Check("Pool B tra ve dung prefab B", fromB != null && fromB.name.StartsWith("PrefabB"));

            FastPool.ClearAll();
            yield return null;
        }

        /// <summary>keepParentOnReturn: ReturnPool khong doi parent; mac dinh van doi parent nhu cu.</summary>
        private IEnumerator TestKeepParentOnReturn()
        {
            GameObject keepPrefab = CreateSource("KeepParent", Vector3.one);
            GameObject movePrefab = CreateSource("MoveParent", Vector3.one);
            Transform container = new GameObject("[UI Container]").transform;

            FastPool.CreatePool(keepPrefab, keepParentOnReturn: true);

            GameObject kept = FastPool.GetPool(keepPrefab, container);
            FastPool.ReturnPool(kept);
            Check("keepParentOnReturn giu parent", kept.transform.parent == container);
            Check("keepParentOnReturn van an object", !kept.activeSelf);
            Check("keepParentOnReturn van vao pool", FastPool.GetPoolCount(keepPrefab) == 1);
            Check("keepParentOnReturn tai su dung", FastPool.GetPool(keepPrefab, container) == kept);

            GameObject moved = FastPool.GetPool(movePrefab, container);
            FastPool.ReturnPool(moved);
            Check("Mac dinh van doi parent ve pool", moved.transform.parent != container);

            FastPool.ClearAll();
            Destroy(container.gameObject);
            yield return null;
        }

        /// <summary>ClearPoolSlowly go pool ngay, huy object idle dan qua nhieu frame.</summary>
        private IEnumerator TestClearPoolSlowly()
        {
            GameObject prefab = CreateSource("Slow", Vector3.one);
            FastPool.CreatePool(prefab, 10);

            GameObject probe = FastPool.GetPool(prefab);
            FastPool.ReturnPool(probe);

            FastPool.ClearPoolSlowly(prefab, 3);
            Check("ClearPoolSlowly go pool ngay", !FastPool.HasPool(prefab));

            yield return null;
            int alive = CountAlive(prefab.name);
            Check("ClearPoolSlowly chua huy het sau 1 frame", alive > 0 && alive < 10);

            for (int i = 0; i < 6; i++) yield return null;
            Check("ClearPoolSlowly huy het sau vai frame", CountAlive(prefab.name) == 0);
        }

        /// <summary>FastPoolDespawner tra object ve pool sau lifetime.</summary>
        private IEnumerator TestDespawnerLifetime()
        {
            GameObject prefab = CreateSource("Despawner", Vector3.one);
            FastPoolDespawner despawner = prefab.AddComponent<FastPoolDespawner>();
            SetPrivate(despawner, "lifetime", 0.2f);

            FastPool.GetPool(prefab);
            Check("Despawner: dang active", FastPool.GetActiveCount(prefab) == 1);

            yield return new WaitForSeconds(0.4f);
            Check("Despawner: tu tra ve sau lifetime", FastPool.GetActiveCount(prefab) == 0);
            Check("Despawner: nam trong pool", FastPool.GetPoolCount(prefab) == 1);

            FastPool.ClearPool(prefab);
            yield return null;
        }

        /// <summary>GetPoolInfos phan anh dung so lieu pool.</summary>
        private IEnumerator TestGetPoolInfos()
        {
            GameObject prefab = CreateSource("Infos", Vector3.one);
            FastPool.CreatePool(prefab, 2, capacity: 5);
            FastPool.GetPool(prefab);

            var infos = new System.Collections.Generic.List<FastPoolInfo>();
            FastPool.GetPoolInfos(infos);

            FastPoolInfo info = infos.Find(x => x.Prefab == prefab);
            Check("GetPoolInfos co pool", info.Prefab == prefab);
            Check("GetPoolInfos dung so lieu", info.IdleCount == 1 && info.ActiveCount == 1 && info.Capacity == 5);

            FastPool.ClearAll();
            yield return null;
        }

        // ---------------------------------------------------------
        // HELPERS

        private static int CountAlive(string prefabName)
        {
            int count = 0;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.scene.IsValid() && go.name.StartsWith(prefabName + "(Clone)")) count++;
            }

            return count;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType()
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }
        // ---------------------------------------------------------

        private GameObject CreateSource(string sourceName, Vector3 scale)
        {
            GameObject source = new(sourceName);
            source.transform.SetParent(_sources, false);
            source.transform.localScale = scale;
            return source;
        }

        private void Check(string label, bool condition)
        {
            if (condition)
            {
                _passed++;
                Debug.Log($"<color=#22aa22>PASS</color> - {label}");
                return;
            }

            _failed++;
            Debug.LogError($"FAIL - {label}");
        }
    }
}
