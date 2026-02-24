using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class World3DToGameplay2DProxy : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private Transform source;
        [SerializeField] private bool autoFindFromParent = true;

        [Header("Proxy Plane")]
        [SerializeField] private bool lockToGameplayPlane = true;
        [SerializeField] private GameplayPlaneConfig gameplayPlane = new()
        {
            planeZ = 0f,
            syncMode = GameplayPlaneSyncMode.HardSnap,
            dampedSharpness = 20f
        };

        [Header("Sync")]
        [SerializeField] private bool syncInFixedUpdate = true;
        [SerializeField] private bool syncRotationAroundZ = true;
        [SerializeField] private bool syncScaleXY = false;

        private void Awake()
        {
            TryAutoResolveSource();
            SyncImmediate();
        }

        private void FixedUpdate()
        {
            if (syncInFixedUpdate)
            {
                Sync(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (!syncInFixedUpdate)
            {
                Sync(Time.deltaTime);
            }
        }

        [ContextMenu("Sync Immediate")]
        public void SyncImmediate()
        {
            Sync(0f, forceImmediate: true);
        }

        public void Configure(Transform sourceTransform, float planeZ, bool constrainToPlane = true)
        {
            source = sourceTransform;
            gameplayPlane.planeZ = planeZ;
            lockToGameplayPlane = constrainToPlane;
            SyncImmediate();
        }

        private void Sync(float dt, bool forceImmediate = false)
        {
            if (source == null)
            {
                TryAutoResolveSource();
                if (source == null)
                {
                    return;
                }
            }

            Vector3 pos = source.position;
            if (lockToGameplayPlane)
            {
                if (forceImmediate || gameplayPlane.syncMode == GameplayPlaneSyncMode.HardSnap)
                {
                    pos.z = gameplayPlane.planeZ;
                }
                else
                {
                    float t = 1f - Mathf.Exp(-Mathf.Max(0.0001f, gameplayPlane.dampedSharpness) * Mathf.Max(0f, dt));
                    pos.z = Mathf.Lerp(transform.position.z, gameplayPlane.planeZ, t);
                }
            }

            transform.position = pos;

            if (syncRotationAroundZ)
            {
                Vector3 euler = transform.eulerAngles;
                euler.z = source.eulerAngles.z;
                transform.eulerAngles = euler;
            }

            if (syncScaleXY)
            {
                Vector3 scale = source.lossyScale;
                transform.localScale = new Vector3(scale.x, scale.y, transform.localScale.z);
            }
        }

        private void TryAutoResolveSource()
        {
            if (source != null || !autoFindFromParent)
            {
                return;
            }

            if (transform.parent != null)
            {
                source = transform.parent;
            }
        }
    }
}
