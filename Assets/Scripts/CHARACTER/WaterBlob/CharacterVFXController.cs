using System.Collections;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class CharacterVFXController : MonoBehaviour
    {
        [Header("Effect Prefabs (Assign in Inspector)")]
        public GameObject moveEffectPrefab;
        public GameObject jumpStartEffectPrefab;
        public GameObject landingEffectPrefab;
        public GameObject slideEffectPrefab;

        [Header("Follow Effect Offsets")]
        public Vector3 moveEffectOffset = Vector3.zero;
        public Vector3 slideEffectOffset = Vector3.zero;

        [Header("Horizontal Side Placement")]
        [Tooltip("If enabled, move effect is placed on left/right side based on facing direction.")]
        public bool moveEffectUseFacingSide = true;
        [Min(0f)] public float moveEffectSideDistance = 0.55f;
        [Tooltip("If enabled, slide effect is placed on left/right side based on facing direction.")]
        public bool slideEffectUseFacingSide = true;
        [Min(0f)] public float slideEffectSideDistance = 0.55f;

        [Header("Ground Detection")]
        public LayerMask groundMask = ~0;
        [Min(0f)] public float groundProbeUpOffset = 1f;
        [Min(0.01f)] public float groundRayDistance = 6f;
        public float fallbackGroundY = 0f;

        [Header("Follow Effect Stop Behavior")]
        [Tooltip("If enabled, follow effects stop emitting and finish naturally instead of turning off instantly.")]
        public bool smoothStopFollowEffects = true;
        [Tooltip("Maximum wait time before force-disabling a stopped follow effect. Set to 0 to wait indefinitely.")]
        [Min(0f)] public float followEffectStopTimeout = 3f;

        [Header("Optional Spawn Cleanup")]
        [Tooltip("Set to 0 to keep spawned effects alive (if prefab handles its own cleanup).")]
        [Min(0f)] public float spawnedEffectLifetime = 2f;

        private sealed class FollowFxRuntime
        {
            public GameObject instance;
            public ParticleSystem[] particles;
            public Coroutine stopRoutine;
        }

        private readonly FollowFxRuntime moveFx = new();
        private readonly FollowFxRuntime slideFx = new();

        private void Awake()
        {
            moveFx.instance = CreateFollowEffect(moveEffectPrefab, moveEffectOffset);
            slideFx.instance = CreateFollowEffect(slideEffectPrefab, slideEffectOffset);
            moveFx.particles = GetFollowParticles(moveFx.instance);
            slideFx.particles = GetFollowParticles(slideFx.instance);
        }

        private void LateUpdate()
        {
            UpdateFollowEffectTransform(moveFx.instance, moveEffectOffset, moveEffectUseFacingSide, moveEffectSideDistance);
            UpdateFollowEffectTransform(slideFx.instance, slideEffectOffset, slideEffectUseFacingSide, slideEffectSideDistance);
        }

        private void OnDestroy()
        {
            CleanupFollowFx(moveFx);
            CleanupFollowFx(slideFx);
        }

        public void OnMoveStart()
        {
            SetFollowEffectActive(moveFx, true);
        }

        public void OnMoveStop()
        {
            SetFollowEffectActive(moveFx, false);
        }

        public void OnJump()
        {
            SpawnGroundEffect(jumpStartEffectPrefab);
        }

        public void OnLand()
        {
            SpawnGroundEffect(landingEffectPrefab);
        }

        public void OnSlideStart()
        {
            SetFollowEffectActive(slideFx, true);
        }

        public void OnSlideStop()
        {
            SetFollowEffectActive(slideFx, false);
        }

        private GameObject CreateFollowEffect(GameObject prefab, Vector3 offset)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(prefab, transform);
            instance.transform.localPosition = offset;
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(false);
            return instance;
        }

        private void UpdateFollowEffectTransform(GameObject effectInstance, Vector3 offset, bool useFacingSide, float sideDistance)
        {
            if (effectInstance == null)
            {
                return;
            }

            Vector3 targetOffset = offset;
            if (useFacingSide)
            {
                float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
                float horizontalDistance = sideDistance > 0f ? sideDistance : Mathf.Abs(offset.x);
                targetOffset.x = Mathf.Abs(horizontalDistance) * facingSign;
            }

            effectInstance.transform.localPosition = targetOffset;
        }

        private ParticleSystem[] GetFollowParticles(GameObject effectInstance)
        {
            if (effectInstance == null)
            {
                return null;
            }

            return effectInstance.GetComponentsInChildren<ParticleSystem>(true);
        }

        private void CleanupFollowFx(FollowFxRuntime fx)
        {
            if (fx == null)
            {
                return;
            }

            if (fx.stopRoutine != null)
            {
                StopCoroutine(fx.stopRoutine);
                fx.stopRoutine = null;
            }

            if (fx.instance != null)
            {
                Destroy(fx.instance);
            }
        }

        private void SetFollowEffectActive(FollowFxRuntime fx, bool active)
        {
            if (fx == null || fx.instance == null)
            {
                return;
            }

            if (active)
            {
                if (fx.stopRoutine != null)
                {
                    StopCoroutine(fx.stopRoutine);
                    fx.stopRoutine = null;
                }

                if (!fx.instance.activeSelf)
                {
                    fx.instance.SetActive(true);
                }

                PlayParticles(fx.particles);
                return;
            }

            if (!smoothStopFollowEffects || fx.particles == null || fx.particles.Length == 0)
            {
                if (fx.instance.activeSelf)
                {
                    fx.instance.SetActive(false);
                }

                return;
            }

            if (!fx.instance.activeSelf)
            {
                return;
            }

            StopParticlesEmission(fx.particles);
            if (fx.stopRoutine != null)
            {
                StopCoroutine(fx.stopRoutine);
            }

            fx.stopRoutine = StartCoroutine(DisableAfterParticlesFinish(fx));
        }

        private static void PlayParticles(ParticleSystem[] particles)
        {
            if (particles == null)
            {
                return;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem ps = particles[i];
                if (ps != null && !ps.isPlaying)
                {
                    ps.Play(true);
                }
            }
        }

        private static void StopParticlesEmission(ParticleSystem[] particles)
        {
            if (particles == null)
            {
                return;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem ps = particles[i];
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private static bool AreParticlesAlive(ParticleSystem[] particles)
        {
            if (particles == null)
            {
                return false;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem ps = particles[i];
                if (ps != null && ps.IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator DisableAfterParticlesFinish(FollowFxRuntime fx)
        {
            float elapsed = 0f;
            bool useTimeout = followEffectStopTimeout > 0f;
            while (AreParticlesAlive(fx.particles) && (!useTimeout || elapsed < followEffectStopTimeout))
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (fx.instance != null)
            {
                fx.instance.SetActive(false);
            }

            fx.stopRoutine = null;
        }

        private void SpawnGroundEffect(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            Vector3 spawnPosition = GetGroundPosition();
            GameObject spawnedEffect = Instantiate(prefab, spawnPosition, Quaternion.identity);

            if (spawnedEffectLifetime > 0f)
            {
                Destroy(spawnedEffect, spawnedEffectLifetime);
            }
        }

        private Vector3 GetGroundPosition()
        {
            Vector3 characterPosition = transform.position;
            Vector2 rayOrigin = new Vector2(characterPosition.x, characterPosition.y + groundProbeUpOffset);
            float rayDistance = groundProbeUpOffset + groundRayDistance;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayDistance, groundMask);

            if (hit.collider != null)
            {
                return new Vector3(characterPosition.x, hit.point.y, characterPosition.z);
            }

            return new Vector3(characterPosition.x, fallbackGroundY, characterPosition.z);
        }
    }
}
