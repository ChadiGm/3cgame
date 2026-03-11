using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint2D : MonoBehaviour
    {
        [SerializeField] private bool isDefaultSpawn = false;
        [SerializeField] private bool activateOnce = true;
        [SerializeField] private bool requirePlayerController = true;

        private bool activated;
        private bool registeredDefault;

        private void Awake()
        {
            TryRegisterDefault();
        }

        private void OnEnable()
        {
            TryRegisterDefault();
        }

        private void Start()
        {
            TryRegisterDefault();
        }

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryActivate(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryActivate(other);
        }

        private void TryRegisterDefault()
        {
            if (!isDefaultSpawn || registeredDefault)
            {
                return;
            }

            CheckpointManager2D manager = CheckpointManager2D.EnsureInstance();
            if (manager == null)
            {
                return;
            }

            manager.SetDefaultSpawnPoint(transform);
            registeredDefault = true;
        }

        private void TryActivate(Collider2D other)
        {
            TryRegisterDefault();
            if (activated && activateOnce)
            {
                return;
            }

            if (!IsPlayer(other))
            {
                return;
            }

            CheckpointManager2D manager = CheckpointManager2D.Instance;
            if (manager == null)
            {
                return;
            }

            manager.ActivateCheckpoint(transform);
            activated = true;
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (requirePlayerController)
            {
                return other.GetComponentInParent<OneDropController2D>() != null;
            }

            return other.GetComponentInParent<OneDropWaterResource2D>() != null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = activated ? new Color(0.2f, 1f, 0.4f, 0.85f) : new Color(1f, 0.8f, 0.2f, 0.85f);
            Gizmos.DrawSphere(transform.position, 0.25f);
        }
    }
}
