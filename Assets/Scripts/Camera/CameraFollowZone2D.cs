using System.Collections.Generic;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class CameraFollowZone2D : MonoBehaviour
    {
        private enum ZoneDetectionMode
        {
            TriggerCollider = 0,
            TwoPositions = 1
        }

        private enum TwoPointAxis
        {
            X = 0,
            Y = 1
        }

        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private ZoneDetectionMode detectionMode = ZoneDetectionMode.TriggerCollider;
        [SerializeField] private bool affectOnlyPlayer = true;
        [SerializeField] private string playerTag = "Player";
        [Header("Two Positions Mode")]
        [SerializeField] private Transform enterPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private TwoPointAxis twoPointAxis = TwoPointAxis.X;
        [SerializeField] private CameraFollow2D.ZoneOverrideSettings zoneSettings = new();

        private readonly HashSet<int> insideTargets = new();
        private Transform trackedTarget;
        private bool twoPointZoneActive;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && detectionMode == ZoneDetectionMode.TriggerCollider)
            {
                col.isTrigger = true;
            }

            ResolveCameraFollow();
            ResolveTrackedTarget();
        }

        private void Awake()
        {
            ResolveCameraFollow();
            ResolveTrackedTarget();
        }

        private void Update()
        {
            if (detectionMode != ZoneDetectionMode.TwoPositions)
            {
                return;
            }

            UpdateTwoPositionZone();
        }

        private void OnDisable()
        {
            insideTargets.Clear();
            twoPointZoneActive = false;
            if (cameraFollow != null)
            {
                cameraFollow.ClearZoneOverride(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (detectionMode != ZoneDetectionMode.TriggerCollider)
            {
                return;
            }

            if (!IsValidTarget(other))
            {
                return;
            }

            int id = ResolveTargetId(other);
            if (!insideTargets.Add(id))
            {
                return;
            }

            if (cameraFollow != null)
            {
                cameraFollow.ApplyZoneOverride(this, zoneSettings);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (detectionMode != ZoneDetectionMode.TriggerCollider)
            {
                return;
            }

            if (!IsValidTarget(other))
            {
                return;
            }

            int id = ResolveTargetId(other);
            insideTargets.Remove(id);
            if (insideTargets.Count > 0)
            {
                return;
            }

            if (cameraFollow != null)
            {
                cameraFollow.ClearZoneOverride(this);
            }
        }

        private bool IsValidTarget(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (!affectOnlyPlayer)
            {
                return true;
            }

            if (other.GetComponentInParent<OneDropController2D>() != null)
            {
                return true;
            }

            return !string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag);
        }

        private bool IsValidTarget(Transform other)
        {
            if (other == null)
            {
                return false;
            }

            if (!affectOnlyPlayer)
            {
                return true;
            }

            if (other.GetComponentInParent<OneDropController2D>() != null)
            {
                return true;
            }

            return !string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag);
        }

        private int ResolveTargetId(Collider2D other)
        {
            if (other == null)
            {
                return 0;
            }

            Transform root = other.attachedRigidbody != null ? other.attachedRigidbody.transform.root : other.transform.root;
            return root != null ? root.GetInstanceID() : other.GetInstanceID();
        }

        private void UpdateTwoPositionZone()
        {
            if (cameraFollow == null)
            {
                ResolveCameraFollow();
            }

            if (trackedTarget == null)
            {
                ResolveTrackedTarget();
            }

            if (trackedTarget == null || enterPoint == null || exitPoint == null)
            {
                if (twoPointZoneActive && cameraFollow != null)
                {
                    twoPointZoneActive = false;
                    cameraFollow.ClearZoneOverride(this);
                }

                return;
            }

            float targetValue = AxisValue(trackedTarget.position);
            float enterValue = AxisValue(enterPoint.position);
            float exitValue = AxisValue(exitPoint.position);
            float min = Mathf.Min(enterValue, exitValue);
            float max = Mathf.Max(enterValue, exitValue);
            bool inside = targetValue >= min && targetValue <= max;

            if (inside && !twoPointZoneActive)
            {
                twoPointZoneActive = true;
                cameraFollow?.ApplyZoneOverride(this, zoneSettings);
            }
            else if (!inside && twoPointZoneActive)
            {
                twoPointZoneActive = false;
                cameraFollow?.ClearZoneOverride(this);
            }
        }

        private float AxisValue(Vector3 value)
        {
            return twoPointAxis == TwoPointAxis.X ? value.x : value.y;
        }

        private void ResolveCameraFollow()
        {
            if (cameraFollow != null)
            {
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraFollow = mainCam.GetComponent<CameraFollow2D>();
                if (cameraFollow != null)
                {
                    return;
                }
            }

            cameraFollow = FindFirstObjectByType<CameraFollow2D>();
        }

        private void ResolveTrackedTarget()
        {
            if (trackedTarget != null && IsValidTarget(trackedTarget))
            {
                return;
            }

            trackedTarget = null;

            if (affectOnlyPlayer)
            {
                OneDropController2D player = FindFirstObjectByType<OneDropController2D>();
                if (player != null)
                {
                    trackedTarget = player.transform;
                    return;
                }
            }

            if (!string.IsNullOrEmpty(playerTag))
            {
                GameObject tagged = GameObject.FindGameObjectWithTag(playerTag);
                if (tagged != null)
                {
                    trackedTarget = tagged.transform;
                }
            }
        }

        private void OnValidate()
        {
            if (detectionMode != ZoneDetectionMode.TriggerCollider)
            {
                return;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
    }
}
