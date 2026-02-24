using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class Platform25DAuthoring : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot3D;
        [SerializeField] private Transform gameplayProxy2D;
        [SerializeField] private World3DToGameplay2DProxy proxySync;

        [Header("Plane")]
        [SerializeField] private float gameplayPlaneZ = 0f;

        public Transform VisualRoot3D => visualRoot3D != null ? visualRoot3D : transform;
        public Transform GameplayProxy2D => gameplayProxy2D;

        private void Awake()
        {
            EnsureProxySync();
            SyncNow();
        }

        private void OnValidate()
        {
            EnsureProxySync();
        }

        [ContextMenu("Sync Proxy Now")]
        public void SyncNow()
        {
            if (proxySync == null)
            {
                return;
            }

            proxySync.Configure(VisualRoot3D, gameplayPlaneZ, constrainToPlane: true);
            proxySync.SyncImmediate();
        }

        private void EnsureProxySync()
        {
            if (gameplayProxy2D == null)
            {
                return;
            }

            if (proxySync == null)
            {
                proxySync = gameplayProxy2D.GetComponent<World3DToGameplay2DProxy>();
            }

            if (proxySync == null)
            {
                proxySync = gameplayProxy2D.gameObject.AddComponent<World3DToGameplay2DProxy>();
            }
        }
    }
}
