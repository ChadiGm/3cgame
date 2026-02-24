using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class BouncyPlatform25D : MonoBehaviour
    {
        [Header("3D Visual Root")]
        [SerializeField] private Transform visualRoot3D;

        [Header("Gameplay Proxy")]
        [SerializeField] private BouncyPlatform2D gameplayProxy2D;
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

        private void ResolveReferences()
        {
            visualRoot3D ??= transform;
            gameplayProxy2D ??= GetComponentInChildren<BouncyPlatform2D>();

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
