using UnityEngine;

namespace WaterBlob
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX Library")]
        [SerializeField] private AudioClip shootClip;
        [SerializeField] private AudioClip chargeShootClip;
        [SerializeField] private AudioClip damageClip;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip deathClip;

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<AudioTriggerEvent>(OnAudioTriggerEvent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AudioTriggerEvent>(OnAudioTriggerEvent);
        }

        private void OnAudioTriggerEvent(AudioTriggerEvent evt)
        {
            switch (evt.EventType)
            {
                case AudioEventType.Shoot: PlayShoot(); break;
                case AudioEventType.ChargeShoot: PlayChargeShoot(); break;
                case AudioEventType.Damage: PlayDamage(); break;
                case AudioEventType.Jump: PlayJump(); break;
                case AudioEventType.Death: PlayDeath(); break;
            }
        }

        public void PlayShoot() => PlaySFX(shootClip);
        public void PlayChargeShoot() => PlaySFX(chargeShootClip);
        public void PlayDamage() => PlaySFX(damageClip);
        public void PlayJump() => PlaySFX(jumpClip);
        public void PlayDeath() => PlaySFX(deathClip);

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                // Fallback debug log if sound is missing
                Debug.Log($"[AudioManager] Played SFX: {(clip != null ? clip.name : "Null Clip")}");
            }
        }
    }
}
