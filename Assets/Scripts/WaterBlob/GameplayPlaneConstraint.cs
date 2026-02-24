using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class GameplayPlaneConstraint : MonoBehaviour
    {
        [SerializeField] private bool lockToGameplayPlane = true;
        [SerializeField] private GameplayPlaneConfig gameplayPlane = new()
        {
            planeZ = 0f,
            syncMode = GameplayPlaneSyncMode.Damped,
            dampedSharpness = 18f
        };

        public bool LockToGameplayPlane => lockToGameplayPlane;
        public float GameplayPlaneZ => gameplayPlane.planeZ;

        private void LateUpdate()
        {
            if (!lockToGameplayPlane)
            {
                return;
            }

            transform.position = GetConstrainedPosition(transform.position, Time.deltaTime);
        }

        public void Configure(bool shouldLock, float planeZ, float dampedSharpness = 18f, GameplayPlaneSyncMode syncMode = GameplayPlaneSyncMode.Damped)
        {
            lockToGameplayPlane = shouldLock;
            gameplayPlane.planeZ = planeZ;
            gameplayPlane.dampedSharpness = Mathf.Max(0f, dampedSharpness);
            gameplayPlane.syncMode = syncMode;
        }

        public Vector3 GetConstrainedPosition(Vector3 currentPosition, float dt)
        {
            if (!lockToGameplayPlane)
            {
                return currentPosition;
            }

            if (gameplayPlane.syncMode == GameplayPlaneSyncMode.HardSnap)
            {
                currentPosition.z = gameplayPlane.planeZ;
                return currentPosition;
            }

            float t = 1f - Mathf.Exp(-Mathf.Max(0.0001f, gameplayPlane.dampedSharpness) * Mathf.Max(0f, dt));
            currentPosition.z = Mathf.Lerp(currentPosition.z, gameplayPlane.planeZ, t);
            return currentPosition;
        }
    }
}
