using UnityEngine;

namespace Vtcong.Pool.Samples
{
    /// <summary>
    /// Demo chay duoc ngay. Mo scene "FastPoolDemo" roi bam Play,
    /// hoac gan script nay vao 1 GameObject rong va de trong <see cref="bulletPrefab"/>
    /// (khi do demo tu dung prefab luc runtime, khong can asset nao).
    /// </summary>
    public class FastPoolDemo : MonoBehaviour
    {
        [Header("Pool")]
        [Tooltip("De trong thi demo tu dung mot prefab don gian luc runtime.")]
        [SerializeField] private GameObject bulletPrefab;

        [SerializeField] private int prewarmCount = 20;

        [Tooltip("So object toi da giu san trong pool. 0 = khong gioi han.")]
        [SerializeField] private int capacity = 0;

        [Header("Spawn")]
        [SerializeField] private bool autoSpawn = true;
        [SerializeField] private float spawnInterval = 0.1f;
        [SerializeField] private float spawnRadius = 1.5f;

        private GameObject _bulletPrefab;
        private Transform _prefabHolder;
        private float _timer;
        private float _angle;

        private void Start()
        {
            // Static counter khong tu reset khi bat "Disable Domain Reload".
            DemoBullet.SpawnCallbacks = 0;
            DemoBullet.ReturnCallbacks = 0;

            // "== null" thay vi "??": UnityEngine.Object co operator== rieng cho object da bi huy.
            _bulletPrefab = bulletPrefab == null ? BuildRuntimePrefab() : bulletPrefab;

            // Tao pool + prewarm truoc de tranh giat khi ban vien dau tien.
            FastPool.CreatePool(_bulletPrefab, prewarmCount, capacity: capacity);
        }

        private void OnDestroy()
        {
            // Don dep: huy ca object dang bay lan object dang nam trong pool.
            FastPool.ClearPool(_bulletPrefab);

            if (_prefabHolder != null) Destroy(_prefabHolder.gameObject);
        }

        private void Update()
        {
            if (!autoSpawn) return;

            _timer += Time.deltaTime;
            if (_timer < spawnInterval) return;

            _timer = 0f;
            SpawnOne();
        }

        private void SpawnOne()
        {
            _angle += 22.5f;

            Vector3 direction = Quaternion.Euler(0f, 0f, _angle) * Vector3.up;
            Vector3 position = transform.position + direction * spawnRadius;

            // Lay theo component: FastPool tu tra object ve pool neu prefab thieu component.
            DemoBullet bullet = FastPool.GetPool<DemoBullet>(_bulletPrefab, position, Quaternion.identity);
            if (bullet == null) return;

            bullet.Launch(direction);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 260, 250), GUI.skin.box);
            GUILayout.Label("FastPool Demo");

            GUILayout.Label($"Trong pool : {FastPool.GetPoolCount(_bulletPrefab)}");
            GUILayout.Label($"Dang dung  : {FastPool.GetActiveCount(_bulletPrefab)}");
            GUILayout.Label($"OnSpawn    : {DemoBullet.SpawnCallbacks}");
            GUILayout.Label($"OnReturn   : {DemoBullet.ReturnCallbacks}");

            autoSpawn = GUILayout.Toggle(autoSpawn, " Auto spawn");

            if (GUILayout.Button("Spawn 10"))
            {
                for (int i = 0; i < 10; i++) SpawnOne();
            }

            if (GUILayout.Button("Return All")) FastPool.ReturnAll(_bulletPrefab);
            if (GUILayout.Button("Prewarm +20")) FastPool.Prewarm(_bulletPrefab, 20);
            if (GUILayout.Button("Clear Pool")) FastPool.ClearPool(_bulletPrefab);

            GUILayout.EndArea();
        }

        /// <summary>
        /// Dung mot "prefab" ngay trong runtime. FastPool nhan bat ky GameObject nao lam prefab nguon.
        /// Scale co chu y dat khac 1 de kiem chung FastPool giu dung scale goc.
        /// </summary>
        private GameObject BuildRuntimePrefab()
        {
            _prefabHolder = new GameObject("[Demo Prefab Source]").transform;
            _prefabHolder.gameObject.SetActive(false);

            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.name = "DemoBullet";
            prefab.transform.SetParent(_prefabHolder, false);
            prefab.transform.localScale = Vector3.one * 0.3f;

            // DestroyImmediate: Destroy() bi tri hoan den cuoi frame nen prewarm ngay sau do
            // van se copy collider sang cac ban sao.
            DestroyImmediate(prefab.GetComponent<Collider>());
            prefab.AddComponent<DemoBullet>();

            // CreatePrimitive gan material mac dinh cua Built-in RP -> hong (mau hong) tren URP/HDRP.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("HDRP/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null) prefab.GetComponent<Renderer>().sharedMaterial = new Material(shader);

            return prefab;
        }
    }
}
