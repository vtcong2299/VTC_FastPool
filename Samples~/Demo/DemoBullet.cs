using UnityEngine;

namespace Vtcong.Pool.Samples
{
    /// <summary>
    /// Vien dan demo: bay theo huong duoc set luc spawn, tu tra ve pool sau <see cref="lifeTime"/> giay.
    /// Implement <see cref="IPoolable"/> nen se nhan OnSpawn/OnReturn tu FastPool.
    /// </summary>
    public class DemoBullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 6f;
        [SerializeField] private float lifeTime = 2f;

        private Vector3 _direction;
        private float _timer;

        public static int SpawnCallbacks;
        public static int ReturnCallbacks;

        public void Launch(Vector3 direction)
        {
            _direction = direction.normalized;
        }

        // Duoc FastPool goi sau khi object da active va transform da duoc set.
        public void OnSpawn()
        {
            _timer = 0f;
            _direction = Vector3.up;
            SpawnCallbacks++;
        }

        // Duoc FastPool goi truoc khi object bi tat.
        public void OnReturn()
        {
            ReturnCallbacks++;
        }

        private void Update()
        {
            transform.position += _direction * (speed * Time.deltaTime);

            _timer += Time.deltaTime;
            if (_timer < lifeTime) return;

            // Tu tra ve pool. FastPool se canh bao neu object da duoc tra roi.
            FastPool.ReturnPool(gameObject);
        }
    }
}
