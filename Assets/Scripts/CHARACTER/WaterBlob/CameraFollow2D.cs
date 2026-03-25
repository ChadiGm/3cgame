using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class CameraFollow2D : MonoBehaviour
    {
#pragma warning disable CS0649
        [System.Serializable]
        private class EarthquakeZone
        {
            public Transform start;
            public Transform end;
        }
#pragma warning restore CS0649

        public enum CameraViewMode
        {
            KeepCurrent = 0,
            Landscape = 1,
            Portrait = 2,
            CustomRect = 3
        }

        [System.Serializable]
        public class ZoneOverrideSettings
        {
            [Tooltip("Higher priority wins when zones overlap.")]
            public int priority = 0;

            [Header("Follow")]
            public bool overrideFollowTarget;
            public bool followTarget = true;

            [Header("Position")]
            public bool overrideOffset;
            public Vector3 offset = new(0f, 1.2f, -10f);
            public bool overrideSmoothTime;
            [Min(0f)] public float smoothTime = 0.13f;

            [Header("Clamp")]
            public bool overrideClampX;
            public bool clampX = true;
            public Vector2 xLimits = new(-13f, 34f);
            public bool overrideClampY;
            public bool clampY = false;
            public Vector2 yLimits = new(-3.5f, 8f);

            [Header("Camera Lens")]
            public bool overrideOrthographicSize;
            [Min(0.01f)] public float orthographicSize = 5f;
            public bool overrideViewMode;
            public CameraViewMode viewMode = CameraViewMode.KeepCurrent;
            public Rect customViewportRect = new(0f, 0f, 1f, 1f);
        }

        private sealed class ActiveZoneOverride
        {
            public Object source;
            public ZoneOverrideSettings settings;
            public int sequence;
        }

        [SerializeField] private Transform target;
        [SerializeField] private bool followTarget = true;
        [SerializeField] private Vector3 offset = new(0f, 1.2f, -10f);
        [SerializeField, Min(0f)] private float smoothTime = 0.13f;
        [SerializeField] private bool clampX = true;
        [SerializeField] private bool clampY = false;
        [SerializeField] private Vector2 xLimits = new(-13f, 34f);
        [SerializeField] private Vector2 yLimits = new(-3.5f, 8f);
        [SerializeField] private CameraViewMode defaultViewMode = CameraViewMode.KeepCurrent;
        [SerializeField] private Rect defaultCustomViewportRect = new(0f, 0f, 1f, 1f);

        [Header("Earthquake Zone")]
        [SerializeField] private bool enableEarthquake = true;
        [Tooltip("Assign start of zone (X axis).")]
        [SerializeField] private Transform earthquakeZoneStart;
        [Tooltip("Assign end of zone (X axis).")]
        [SerializeField] private Transform earthquakeZoneEnd;
        [Tooltip("Add more zones here (Zone 2, Zone 3, ...).")]
        [SerializeField] private List<EarthquakeZone> additionalZones = new();
        [SerializeField] private bool useDistanceFalloff = true;

        [Header("Earthquake Shake")]
        [SerializeField, Min(0f)] private float shakeIntensity = 0.45f;
        [SerializeField, Min(0f)] private float shakeFrequency = 3f;
        [SerializeField, Min(0.01f)] private float damping = 6f;
        [SerializeField] private bool affectRotation = true;
        [SerializeField, Min(0f)] private float rotationIntensity = 1.6f;

        [Header("Optional Rumble")]
        [SerializeField] private bool enableRumble = true;
        [SerializeField, Min(0f)] private float rumbleIntensity = 0.12f;
        [SerializeField, Min(0f)] private float rumbleFrequency = 0.85f;

        [Header("Earthquake SFX")]
        [SerializeField] private bool enableEarthquakeSfx = true;
        [SerializeField] private AudioSource earthquakeSfxSource;
        [SerializeField] private AudioClip earthquakeSfxClip;
        [SerializeField, Range(0f, 1f)] private float earthquakeSfxMaxVolume = 0.65f;
        [SerializeField, Min(0f)] private float earthquakeSfxFadeIn = 0.18f;
        [SerializeField, Min(0f)] private float earthquakeSfxFadeOut = 0.28f;
        [SerializeField] private bool earthquakeSfxScaleWithProximity = true;

        [Header("Earthquake Events")]
        [SerializeField] private UnityEvent onEnterEarthquakeZone;
        [SerializeField] private UnityEvent onExitEarthquakeZone;

        private Vector3 velocity;
        private Vector3 followPosition;
        private bool hasFollowPosition;
        private float currentShakeStrength;
        private bool isInsideEarthquakeZone;
        private Quaternion baseRotation;
        private float noiseSeedX;
        private float noiseSeedY;
        private float noiseSeedR;
        private float rumblePhase;
        private float earthquakeSfxCurrentVolume;

        private bool currentFollowTarget;
        private Vector3 currentOffset;
        private float currentSmoothTime;
        private bool currentClampX;
        private bool currentClampY;
        private Vector2 currentXLimits;
        private Vector2 currentYLimits;
        private float currentOrthographicSize;
        private CameraViewMode currentViewMode;
        private Rect currentCustomViewportRect;

        private Camera cachedCamera;
        private float baseOrthographicSize;
        private Rect baseCameraRect;

        private readonly List<ActiveZoneOverride> activeZoneOverrides = new();
        private int zoneOverrideSequence;

        private void Awake()
        {
            if (target == null)
            {
                GameObject player = GameObject.Find("WaterBlobPlayer");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            cachedCamera = GetComponent<Camera>();
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            if (cachedCamera != null)
            {
                baseOrthographicSize = cachedCamera.orthographicSize;
                baseCameraRect = cachedCamera.rect;
            }

            baseRotation = transform.rotation;
            noiseSeedX = Random.value * 100f + 11.3f;
            noiseSeedY = Random.value * 100f + 47.9f;
            noiseSeedR = Random.value * 100f + 83.7f;
            EnsureEarthquakeSfxSource();

            RebuildRuntimeSettings();
        }

        private void OnDisable()
        {
            StopEarthquakeSfxImmediate();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (!currentFollowTarget)
            {
                if (!hasFollowPosition)
                {
                    followPosition = transform.position;
                    hasFollowPosition = true;
                }

                ApplyEarthquake(followPosition);
                return;
            }

            Vector3 desired = target.position + currentOffset;
            if (currentClampX)
            {
                desired.x = Mathf.Clamp(desired.x, currentXLimits.x, currentXLimits.y);
            }

            if (currentClampY)
            {
                desired.y = Mathf.Clamp(desired.y, currentYLimits.x, currentYLimits.y);
            }

            if (!hasFollowPosition)
            {
                followPosition = desired;
                hasFollowPosition = true;
            }

            followPosition = Vector3.SmoothDamp(followPosition, desired, ref velocity, currentSmoothTime);
            ApplyEarthquake(followPosition);
        }

        public void ApplyZoneOverride(Object source, ZoneOverrideSettings settings)
        {
            if (source == null || settings == null)
            {
                return;
            }

            int index = FindZoneOverrideIndex(source);
            if (index >= 0)
            {
                activeZoneOverrides[index].settings = settings;
                activeZoneOverrides[index].sequence = ++zoneOverrideSequence;
            }
            else
            {
                activeZoneOverrides.Add(new ActiveZoneOverride
                {
                    source = source,
                    settings = settings,
                    sequence = ++zoneOverrideSequence
                });
            }

            RebuildRuntimeSettings();
        }

        public void ClearZoneOverride(Object source)
        {
            if (source == null)
            {
                return;
            }

            int index = FindZoneOverrideIndex(source);
            if (index < 0)
            {
                return;
            }

            activeZoneOverrides.RemoveAt(index);
            RebuildRuntimeSettings();
        }

        private int FindZoneOverrideIndex(Object source)
        {
            for (int i = 0; i < activeZoneOverrides.Count; i++)
            {
                ActiveZoneOverride active = activeZoneOverrides[i];
                if (active != null && active.source == source)
                {
                    return i;
                }
            }

            return -1;
        }

        private ActiveZoneOverride ResolveTopOverride()
        {
            ActiveZoneOverride best = null;
            for (int i = 0; i < activeZoneOverrides.Count; i++)
            {
                ActiveZoneOverride candidate = activeZoneOverrides[i];
                if (candidate == null || candidate.settings == null)
                {
                    continue;
                }

                if (best == null)
                {
                    best = candidate;
                    continue;
                }

                int candidatePriority = candidate.settings.priority;
                int bestPriority = best.settings.priority;
                if (candidatePriority > bestPriority)
                {
                    best = candidate;
                    continue;
                }

                if (candidatePriority == bestPriority && candidate.sequence > best.sequence)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private void RebuildRuntimeSettings()
        {
            currentFollowTarget = followTarget;
            currentOffset = offset;
            currentSmoothTime = Mathf.Max(0f, smoothTime);
            currentClampX = clampX;
            currentClampY = clampY;
            currentXLimits = xLimits;
            currentYLimits = yLimits;
            currentOrthographicSize = baseOrthographicSize;
            currentViewMode = defaultViewMode;
            currentCustomViewportRect = defaultCustomViewportRect;

            ActiveZoneOverride active = ResolveTopOverride();
            ZoneOverrideSettings settings = active != null ? active.settings : null;
            if (settings != null)
            {
                if (settings.overrideFollowTarget) currentFollowTarget = settings.followTarget;
                if (settings.overrideOffset) currentOffset = settings.offset;
                if (settings.overrideSmoothTime) currentSmoothTime = Mathf.Max(0f, settings.smoothTime);
                if (settings.overrideClampX)
                {
                    currentClampX = settings.clampX;
                    currentXLimits = settings.xLimits;
                }

                if (settings.overrideClampY)
                {
                    currentClampY = settings.clampY;
                    currentYLimits = settings.yLimits;
                }

                if (settings.overrideOrthographicSize) currentOrthographicSize = Mathf.Max(0.01f, settings.orthographicSize);
                if (settings.overrideViewMode)
                {
                    currentViewMode = settings.viewMode;
                    currentCustomViewportRect = settings.customViewportRect;
                }
            }

            ApplyCameraLensSettings();

            followPosition = transform.position;
            hasFollowPosition = true;
            velocity = Vector3.zero;
        }

        private void ApplyCameraLensSettings()
        {
            if (cachedCamera == null)
            {
                return;
            }

            if (currentOrthographicSize > 0f)
            {
                cachedCamera.orthographicSize = currentOrthographicSize;
            }

            switch (currentViewMode)
            {
                case CameraViewMode.Landscape:
                    cachedCamera.rect = baseCameraRect;
                    break;

                case CameraViewMode.Portrait:
                {
                    const float portraitWidth = 9f / 16f;
                    float x = (1f - portraitWidth) * 0.5f;
                    cachedCamera.rect = new Rect(x, 0f, portraitWidth, 1f);
                    break;
                }

                case CameraViewMode.CustomRect:
                {
                    Rect r = currentCustomViewportRect;
                    r.x = Mathf.Clamp01(r.x);
                    r.y = Mathf.Clamp01(r.y);
                    r.width = Mathf.Clamp(r.width, 0.01f, 1f - r.x);
                    r.height = Mathf.Clamp(r.height, 0.01f, 1f - r.y);
                    cachedCamera.rect = r;
                    break;
                }

                default:
                    cachedCamera.rect = baseCameraRect;
                    break;
            }
        }

        private void ApplyEarthquake(Vector3 basePosition)
        {
            float proximity = GetZoneProximity(target.position.x, out bool nowInside);
            HandleZoneState(nowInside);
            UpdateEarthquakeSfx(proximity);

            if (!enableEarthquake || proximity <= 0f)
            {
                currentShakeStrength = Mathf.Lerp(currentShakeStrength, 0f, 1f - Mathf.Exp(-damping * Time.deltaTime));
            }
            else
            {
                float targetShake = shakeIntensity * proximity;
                float blend = 1f - Mathf.Exp(-damping * Time.deltaTime);
                currentShakeStrength = Mathf.Lerp(currentShakeStrength, targetShake, blend);
            }

            float time = Time.time * shakeFrequency;
            float noiseX = (Mathf.PerlinNoise(noiseSeedX, time) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(noiseSeedY, time + 13.37f) - 0.5f) * 2f;
            Vector3 shakeOffset = new(noiseX, noiseY, 0f);
            shakeOffset *= currentShakeStrength;

            if (enableRumble && currentShakeStrength > 0f)
            {
                rumblePhase += Time.deltaTime * rumbleFrequency * Mathf.PI * 2f;
                float rumble = Mathf.Sin(rumblePhase) * rumbleIntensity * currentShakeStrength;
                shakeOffset += new Vector3(0f, rumble, 0f);
            }

            transform.position = basePosition + shakeOffset;

            if (affectRotation && currentShakeStrength > 0.0001f)
            {
                float rotNoise = (Mathf.PerlinNoise(noiseSeedR, time + 29.73f) - 0.5f) * 2f;
                float zAngle = rotNoise * rotationIntensity * currentShakeStrength;
                transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, zAngle);
            }
            else
            {
                transform.rotation = baseRotation;
            }
        }

        private void EnsureEarthquakeSfxSource()
        {
            if (earthquakeSfxSource != null)
            {
                return;
            }

            earthquakeSfxSource = GetComponent<AudioSource>();
            if (earthquakeSfxSource == null)
            {
                earthquakeSfxSource = gameObject.AddComponent<AudioSource>();
            }

            earthquakeSfxSource.playOnAwake = false;
            earthquakeSfxSource.loop = true;
        }

        private void UpdateEarthquakeSfx(float proximity)
        {
            if (!enableEarthquakeSfx || earthquakeSfxSource == null || earthquakeSfxClip == null)
            {
                StopEarthquakeSfxImmediate();
                return;
            }

            bool active = enableEarthquake && proximity > 0.0001f;
            float proximityScale = earthquakeSfxScaleWithProximity ? Mathf.Clamp01(proximity) : 1f;
            float scaledSfxMaxVolume = OneDropAudioSettings2D.ApplySfx(earthquakeSfxMaxVolume);
            float targetVolume = active ? scaledSfxMaxVolume * proximityScale : 0f;
            float fadeDuration = targetVolume > earthquakeSfxCurrentVolume ? earthquakeSfxFadeIn : earthquakeSfxFadeOut;
            float step = fadeDuration > 0f
                ? Mathf.Max(0.0001f, scaledSfxMaxVolume) * (Time.unscaledDeltaTime / fadeDuration)
                : Mathf.Max(0.0001f, scaledSfxMaxVolume);

            if (active && !earthquakeSfxSource.isPlaying)
            {
                earthquakeSfxSource.clip = earthquakeSfxClip;
                earthquakeSfxSource.loop = true;
                earthquakeSfxSource.playOnAwake = false;
                earthquakeSfxSource.volume = 0f;
                earthquakeSfxCurrentVolume = 0f;
                earthquakeSfxSource.Play();
            }

            earthquakeSfxCurrentVolume = Mathf.MoveTowards(earthquakeSfxCurrentVolume, targetVolume, step);
            earthquakeSfxSource.volume = earthquakeSfxCurrentVolume;

            if (!active && earthquakeSfxSource.isPlaying && earthquakeSfxCurrentVolume <= 0.0005f)
            {
                earthquakeSfxSource.Stop();
            }
        }

        private void StopEarthquakeSfxImmediate()
        {
            earthquakeSfxCurrentVolume = 0f;
            if (earthquakeSfxSource == null)
            {
                return;
            }

            earthquakeSfxSource.volume = 0f;
            if (earthquakeSfxSource.isPlaying)
            {
                earthquakeSfxSource.Stop();
            }
        }

        private float GetZoneProximity(float playerX, out bool isInside)
        {
            isInside = false;
            if (!enableEarthquake)
            {
                return 0f;
            }

            float bestProximity = 0f;
            bool insideAny = false;

            // Legacy single zone (kept for backward compatibility with existing scene setup)
            EvaluateZone(earthquakeZoneStart, earthquakeZoneEnd, playerX, ref bestProximity, ref insideAny);

            if (additionalZones != null)
            {
                for (int i = 0; i < additionalZones.Count; i++)
                {
                    EarthquakeZone zone = additionalZones[i];
                    EvaluateZone(zone.start, zone.end, playerX, ref bestProximity, ref insideAny);
                }
            }

            isInside = insideAny;
            return bestProximity;
        }

        private void EvaluateZone(Transform start, Transform end, float playerX, ref float bestProximity, ref bool insideAny)
        {
            if (start == null || end == null)
            {
                return;
            }

            float startX = start.position.x;
            float endX = end.position.x;
            float minX = Mathf.Min(startX, endX);
            float maxX = Mathf.Max(startX, endX);
            if (Mathf.Approximately(minX, maxX))
            {
                return;
            }

            bool insideThisZone = playerX >= minX && playerX <= maxX;
            if (!insideThisZone)
            {
                return;
            }

            insideAny = true;

            if (!useDistanceFalloff)
            {
                bestProximity = 1f;
                return;
            }

            float centerX = (minX + maxX) * 0.5f;
            float halfWidth = Mathf.Max(0.0001f, (maxX - minX) * 0.5f);
            float proximity = 1f - Mathf.Clamp01(Mathf.Abs(playerX - centerX) / halfWidth);
            if (proximity > bestProximity)
            {
                bestProximity = proximity;
            }
        }

        private void HandleZoneState(bool nowInside)
        {
            if (nowInside == isInsideEarthquakeZone)
            {
                return;
            }

            isInsideEarthquakeZone = nowInside;
            if (isInsideEarthquakeZone)
            {
                onEnterEarthquakeZone?.Invoke();
            }
            else
            {
                onExitEarthquakeZone?.Invoke();
            }
        }

        private void OnValidate()
        {
            smoothTime = Mathf.Max(0f, smoothTime);
            damping = Mathf.Max(0.01f, damping);
            shakeIntensity = Mathf.Max(0f, shakeIntensity);
            shakeFrequency = Mathf.Max(0f, shakeFrequency);
            rotationIntensity = Mathf.Max(0f, rotationIntensity);
            rumbleIntensity = Mathf.Max(0f, rumbleIntensity);
            rumbleFrequency = Mathf.Max(0f, rumbleFrequency);
            earthquakeSfxMaxVolume = Mathf.Clamp01(earthquakeSfxMaxVolume);
            earthquakeSfxFadeIn = Mathf.Max(0f, earthquakeSfxFadeIn);
            earthquakeSfxFadeOut = Mathf.Max(0f, earthquakeSfxFadeOut);

            defaultCustomViewportRect.width = Mathf.Clamp(defaultCustomViewportRect.width, 0.01f, 1f);
            defaultCustomViewportRect.height = Mathf.Clamp(defaultCustomViewportRect.height, 0.01f, 1f);
            defaultCustomViewportRect.x = Mathf.Clamp(defaultCustomViewportRect.x, 0f, 1f - defaultCustomViewportRect.width);
            defaultCustomViewportRect.y = Mathf.Clamp(defaultCustomViewportRect.y, 0f, 1f - defaultCustomViewportRect.height);
        }
    }
}
