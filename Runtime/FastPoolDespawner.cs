using System.Collections;
using UnityEngine;

namespace Vtcong.Pool
{
    /// <summary>
    /// Tu ReturnPool object khi het lifetime, hoac khi moi particle / audio duoc chon da chay xong
    /// (dieu kien nao den truoc). Chi chay voi object lay tu VtcFastPool.GetPool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FastPoolDespawner : MonoBehaviour, IPoolable
    {
        [Tooltip("Giay. <= 0 la khong gioi han. Dung lam chot an toan cho particle/audio loop.")]
        [SerializeField] private float lifetime;

        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool waitForParticles;
        [SerializeField] private bool waitForAudio;

        [Tooltip("Clear + Play lai moi ParticleSystem moi lan spawn.")]
        [SerializeField] private bool restartParticlesOnSpawn;

        private ParticleSystem[] _particles;
        private AudioSource[] _audios;
        private Coroutine _routine;

        private bool HasWaitCondition => waitForParticles || waitForAudio;

        private void Awake()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _audios = GetComponentsInChildren<AudioSource>(true);

            if (waitForParticles && _particles.Length == 0)
                Debug.LogWarning($"[FastPoolDespawner] '{name}' bat waitForParticles nhung khong co ParticleSystem.");

            if (waitForAudio && _audios.Length == 0)
                Debug.LogWarning($"[FastPoolDespawner] '{name}' bat waitForAudio nhung khong co AudioSource.");

            if (lifetime <= 0f && !HasWaitCondition)
                Debug.LogWarning($"[FastPoolDespawner] '{name}' khong co dieu kien nao, se khong bao gio tu tra ve.");
        }

        public void OnSpawn()
        {
            if (restartParticlesOnSpawn)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    _particles[i].Clear(false);
                    _particles[i].Play(false);
                }
            }

            if (lifetime > 0f || HasWaitCondition) _routine = StartCoroutine(DespawnRoutine());
        }

        public void OnReturn()
        {
            if (_routine == null) return;

            StopCoroutine(_routine);
            _routine = null;
        }

        private IEnumerator DespawnRoutine()
        {
            float elapsed = 0f;

            while (true)
            {
                // cho 1 frame truoc khi kiem tra: OnSpawn cua component khac co the moi goi Play().
                yield return null;

                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (lifetime > 0f && elapsed >= lifetime) break;
                if (HasWaitCondition && !IsWaitingParticles() && !IsWaitingAudio()) break;
            }

            _routine = null;
            VtcFastPool.ReturnPool(gameObject);
        }

        private bool IsWaitingParticles()
        {
            if (!waitForParticles) return false;

            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i].IsAlive(false)) return true;
            }

            return false;
        }

        private bool IsWaitingAudio()
        {
            if (!waitForAudio) return false;

            for (int i = 0; i < _audios.Length; i++)
            {
                if (_audios[i].isPlaying) return true;
            }

            return false;
        }
    }
}
