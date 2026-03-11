using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class CheckpointManager2D : MonoBehaviour
    {
        public static CheckpointManager2D Instance { get; private set; }

        [Header("Defaults")]
        [SerializeField] private Transform defaultSpawnPoint;
        [SerializeField] private bool usePlayerStartIfNoDefault = true;

        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;
        private Vector3 lastCheckpointPosition;
        private Quaternion lastCheckpointRotation;
        private bool hasInitialSpawn;
        private bool hasCheckpoint;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public static CheckpointManager2D EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject go = new GameObject("CheckpointManager");
            Instance = go.AddComponent<CheckpointManager2D>();
            return Instance;
        }

        public void RegisterPlayer(Transform player)
        {
            if (hasInitialSpawn)
            {
                return;
            }

            if (defaultSpawnPoint != null)
            {
                initialSpawnPosition = defaultSpawnPoint.position;
                initialSpawnRotation = defaultSpawnPoint.rotation;
                hasInitialSpawn = true;
                return;
            }

            if (usePlayerStartIfNoDefault && player != null)
            {
                initialSpawnPosition = player.position;
                initialSpawnRotation = player.rotation;
                hasInitialSpawn = true;
            }
        }

        public void SetDefaultSpawnPoint(Transform spawnPoint)
        {
            if (spawnPoint == null)
            {
                return;
            }

            defaultSpawnPoint = spawnPoint;
            initialSpawnPosition = spawnPoint.position;
            initialSpawnRotation = spawnPoint.rotation;
            hasInitialSpawn = true;
            if (!hasCheckpoint)
            {
                lastCheckpointPosition = initialSpawnPosition;
                lastCheckpointRotation = initialSpawnRotation;
                hasCheckpoint = true;
            }
        }

        public void ActivateCheckpoint(Transform checkpoint)
        {
            if (checkpoint == null)
            {
                return;
            }

            lastCheckpointPosition = checkpoint.position;
            lastCheckpointRotation = checkpoint.rotation;
            hasCheckpoint = true;
        }

        public bool TryGetRespawn(out Vector3 position, out Quaternion rotation)
        {
            if (hasCheckpoint)
            {
                position = lastCheckpointPosition;
                rotation = lastCheckpointRotation;
                return true;
            }

            if (hasInitialSpawn)
            {
                position = initialSpawnPosition;
                rotation = initialSpawnRotation;
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }
    }
}
