using System.Collections.Generic;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class OneDropRefillZone2D : MonoBehaviour
    {
        [Header("Refill")]
        [SerializeField, Range(0f, 1f)] private float refillPercentPerSecond = 0.2f;
        [SerializeField] private bool affectOnlyPlayerTag = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool useUnscaledTime;

        [Header("VFX")]
        [SerializeField] private GameObject touchVfxPrefab;
        [SerializeField] private GameObject whileRefillingVfxPrefab;
        [SerializeField] private Vector3 vfxOffset = new(0f, 0.2f, 0f);
        [SerializeField] private bool parentWhileVfxToPlayer = true;
        [SerializeField] private bool useFallbackTouchVfx = true;

        private readonly Dictionary<OneDropWaterResource2D, int> overlapCounts = new();
        private readonly Dictionary<OneDropWaterResource2D, GameObject> activeLoopVfx = new();
        private readonly List<OneDropWaterResource2D> staleResources = new();

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void OnValidate()
        {
            refillPercentPerSecond = Mathf.Clamp01(refillPercentPerSecond);
            EnsureTriggerCollider();
        }

        private void Update()
        {
            if (overlapCounts.Count == 0 || refillPercentPerSecond <= 0f)
            {
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            staleResources.Clear();
            foreach (KeyValuePair<OneDropWaterResource2D, int> pair in overlapCounts)
            {
                OneDropWaterResource2D resource = pair.Key;
                if (resource == null || pair.Value <= 0)
                {
                    staleResources.Add(resource);
                    continue;
                }

                resource.RestoreWaterPercent(refillPercentPerSecond * deltaTime);
            }

            for (int i = 0; i < staleResources.Count; i++)
            {
                RemoveTrackedResource(staleResources[i]);
            }
        }

        private void OnDisable()
        {
            ClearTrackedResources();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            OneDropWaterResource2D resource = ResolveResource(other);
            if (resource == null)
            {
                return;
            }

            overlapCounts.TryGetValue(resource, out int currentCount);
            currentCount++;
            overlapCounts[resource] = currentCount;
            if (currentCount > 1)
            {
                return;
            }

            Vector3 touchPosition = other.ClosestPoint(transform.position);
            SpawnTouchVfx(touchPosition);
            SpawnLoopVfx(resource);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OneDropWaterResource2D resource = ResolveResource(other);
            if (resource == null || !overlapCounts.TryGetValue(resource, out int currentCount))
            {
                return;
            }

            currentCount--;
            if (currentCount <= 0)
            {
                RemoveTrackedResource(resource);
                return;
            }

            overlapCounts[resource] = currentCount;
        }

        private OneDropWaterResource2D ResolveResource(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            OneDropWaterResource2D resource = other.GetComponentInParent<OneDropWaterResource2D>();
            if (resource == null)
            {
                return null;
            }

            if (affectOnlyPlayerTag && !resource.CompareTag(playerTag))
            {
                return null;
            }

            return resource;
        }

        private void SpawnTouchVfx(Vector3 worldPosition)
        {
            if (touchVfxPrefab != null)
            {
                Instantiate(touchVfxPrefab, worldPosition + vfxOffset, Quaternion.identity);
                return;
            }

            if (useFallbackTouchVfx)
            {
                SpawnFallbackTouchVfx(worldPosition + vfxOffset);
            }
        }

        private void SpawnLoopVfx(OneDropWaterResource2D resource)
        {
            if (whileRefillingVfxPrefab == null || resource == null || activeLoopVfx.ContainsKey(resource))
            {
                return;
            }

            Transform parent = parentWhileVfxToPlayer ? resource.transform : null;
            Vector3 spawnPosition = resource.transform.position + vfxOffset;
            GameObject instance = Instantiate(whileRefillingVfxPrefab, spawnPosition, Quaternion.identity, parent);
            activeLoopVfx[resource] = instance;
        }

        private void RemoveTrackedResource(OneDropWaterResource2D resource)
        {
            overlapCounts.Remove(resource);
            if (resource != null && activeLoopVfx.TryGetValue(resource, out GameObject loopVfx))
            {
                if (loopVfx != null)
                {
                    Destroy(loopVfx);
                }

                activeLoopVfx.Remove(resource);
            }
        }

        private void ClearTrackedResources()
        {
            foreach (KeyValuePair<OneDropWaterResource2D, GameObject> pair in activeLoopVfx)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            activeLoopVfx.Clear();
            overlapCounts.Clear();
            staleResources.Clear();
        }

        private void EnsureTriggerCollider()
        {
            Collider2D zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private static void SpawnFallbackTouchVfx(Vector3 position)
        {
            GameObject fx = new("OneDropRefillTouchFx_Runtime");
            fx.transform.position = position;

            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.44f, 0.9f, 1f, 0.9f),
                new Color(0.17f, 0.65f, 1f, 0.72f)
            );
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.28f;

            ps.Play();
            Object.Destroy(fx, 1.2f);
        }
    }
}
