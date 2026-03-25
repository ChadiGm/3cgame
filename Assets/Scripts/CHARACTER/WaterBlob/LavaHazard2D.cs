using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class LavaHazard2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private bool requirePlayerTag = true;
        [SerializeField] private string playerTag = "Player";

        [Header("Audio")]
        [SerializeField] private AudioSource lavaAudioSource;
        [SerializeField] private AudioClip ambientLoopClip;
        [SerializeField] private bool playAmbientLoop = true;
        [SerializeField, Range(0f, 1f)] private float ambientLoopVolume = 0.35f;
        [SerializeField] private AudioClip touchSfxClip;
        [SerializeField, Range(0f, 1f)] private float touchSfxVolume = 0.85f;
        [SerializeField, Min(0f)] private float touchSfxCooldown = 0.15f;

        private float nextTouchSfxTime;

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
            ResolveAudioSource();
            ConfigureAudioSource();
        }

        private void OnEnable()
        {
            OneDropAudioSettings2D.VolumesChanged += HandleVolumesChanged;
            TryStartAmbientLoop();
        }

        private void OnDisable()
        {
            OneDropAudioSettings2D.VolumesChanged -= HandleVolumesChanged;
            StopAmbientLoop();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryKill(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision != null)
            {
                TryKill(collision.collider);
            }
        }

        private void EnsureTriggerCollider()
        {
            Collider2D collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.isTrigger = true;
            }
        }

        private void TryKill(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            OneDropWaterResource2D resource = other.GetComponentInParent<OneDropWaterResource2D>();
            if (resource == null && other.attachedRigidbody != null)
            {
                resource = other.attachedRigidbody.GetComponentInParent<OneDropWaterResource2D>();
            }

            if (resource == null)
            {
                return;
            }

            if (requirePlayerTag)
            {
                if (string.IsNullOrEmpty(playerTag) || !resource.CompareTag(playerTag))
                {
                    return;
                }
            }

            PlayTouchSfx();
            resource.DepleteAllWater();
        }

        private void ResolveAudioSource()
        {
            if (lavaAudioSource != null)
            {
                return;
            }

            lavaAudioSource = GetComponent<AudioSource>();
            if (lavaAudioSource == null)
            {
                lavaAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void ConfigureAudioSource()
        {
            if (lavaAudioSource == null)
            {
                return;
            }

            lavaAudioSource.playOnAwake = false;
            lavaAudioSource.loop = false;
            lavaAudioSource.spatialBlend = 0f;
            lavaAudioSource.volume = OneDropAudioSettings2D.ApplySfx(ambientLoopVolume);
        }

        private void TryStartAmbientLoop()
        {
            if (!playAmbientLoop || ambientLoopClip == null || lavaAudioSource == null)
            {
                return;
            }

            lavaAudioSource.clip = ambientLoopClip;
            lavaAudioSource.loop = true;
            lavaAudioSource.volume = OneDropAudioSettings2D.ApplySfx(ambientLoopVolume);
            if (!lavaAudioSource.isPlaying)
            {
                lavaAudioSource.Play();
            }
        }

        private void StopAmbientLoop()
        {
            if (lavaAudioSource == null)
            {
                return;
            }

            if (lavaAudioSource.clip == ambientLoopClip && lavaAudioSource.isPlaying)
            {
                lavaAudioSource.Stop();
            }
        }

        private void PlayTouchSfx()
        {
            if (touchSfxClip == null || lavaAudioSource == null)
            {
                return;
            }

            if (touchSfxCooldown > 0f && Time.time < nextTouchSfxTime)
            {
                return;
            }

            nextTouchSfxTime = Time.time + touchSfxCooldown;
            lavaAudioSource.PlayOneShot(touchSfxClip, OneDropAudioSettings2D.ApplySfx(touchSfxVolume));
        }

        private void HandleVolumesChanged()
        {
            if (lavaAudioSource == null)
            {
                return;
            }

            if (lavaAudioSource.clip == ambientLoopClip && lavaAudioSource.isPlaying)
            {
                lavaAudioSource.volume = OneDropAudioSettings2D.ApplySfx(ambientLoopVolume);
            }
        }
    }
}
