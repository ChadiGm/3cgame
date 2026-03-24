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

        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 1.2f, -10f);
        [SerializeField, Min(0f)] private float smoothTime = 0.13f;
        [SerializeField] private bool clampX = true;
        [SerializeField] private bool clampY = false;
        [SerializeField] private Vector2 xLimits = new(-13f, 34f);
        [SerializeField] private Vector2 yLimits = new(-3.5f, 8f);

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

            baseRotation = transform.rotation;
            noiseSeedX = Random.value * 100f + 11.3f;
            noiseSeedY = Random.value * 100f + 47.9f;
            noiseSeedR = Random.value * 100f + 83.7f;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            if (clampX)
            {
                desired.x = Mathf.Clamp(desired.x, xLimits.x, xLimits.y);
            }

            if (clampY)
            {
                desired.y = Mathf.Clamp(desired.y, yLimits.x, yLimits.y);
            }

            if (!hasFollowPosition)
            {
                followPosition = desired;
                hasFollowPosition = true;
            }

            followPosition = Vector3.SmoothDamp(followPosition, desired, ref velocity, smoothTime);
            ApplyEarthquake(followPosition);
        }

        private void ApplyEarthquake(Vector3 basePosition)
        {
            float proximity = GetZoneProximity(target.position.x, out bool nowInside);
            HandleZoneState(nowInside);

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
        }
    }
}
