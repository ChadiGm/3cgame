using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class RotatingObstacle25D : MonoBehaviour
    {
        [Header("3D Visual")]
        [SerializeField] private Transform visualRoot3D;
        [SerializeField] private Vector3 localAxis = Vector3.forward;
        [SerializeField] private float rotationSpeedDeg = 180f;
        [SerializeField] private bool useUnscaledTime;

        [Header("2D Gameplay Proxy")]
        [SerializeField] private RotatingObstacle2D gameplayProxy2D;
        [SerializeField] private World3DToGameplay2DProxy proxySync;
        [SerializeField] private float gameplayPlaneZ;

        private void Awake()
        {
            ResolveReferences();
            ConfigureProxy();
            proxySync?.SyncImmediate();
        }

        private void OnValidate()
        {
            ResolveReferences();
            ConfigureProxy();
        }

        private void Update()
        {
            if (visualRoot3D == null)
            {
                return;
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            visualRoot3D.Rotate(localAxis.normalized, rotationSpeedDeg * dt, Space.Self);
        }

        private void ResolveReferences()
        {
            visualRoot3D ??= transform;
            gameplayProxy2D ??= GetComponentInChildren<RotatingObstacle2D>();

            if (gameplayProxy2D != null)
            {
                proxySync ??= gameplayProxy2D.GetComponent<World3DToGameplay2DProxy>();
                if (proxySync == null)
                {
                    proxySync = gameplayProxy2D.gameObject.AddComponent<World3DToGameplay2DProxy>();
                }
            }
        }

        private void ConfigureProxy()
        {
            if (proxySync == null)
            {
                return;
            }

            proxySync.Configure(visualRoot3D, gameplayPlaneZ, constrainToPlane: true);
            gameplayProxy2D?.SetExternallyDriven(true);
        }
    }
}
