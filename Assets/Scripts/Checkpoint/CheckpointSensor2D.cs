using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class CheckpointSensor2D : MonoBehaviour
    {
        [SerializeField] private bool useTriggers = true;
        [SerializeField] private bool useCollisions = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (useTriggers)
            {
                TryActivate(other);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (useTriggers)
            {
                TryActivate(other);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (useCollisions && collision != null)
            {
                TryActivate(collision.collider);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (useCollisions && collision != null)
            {
                TryActivate(collision.collider);
            }
        }

        private static void TryActivate(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            Checkpoint2D checkpoint = other.GetComponent<Checkpoint2D>();
            if (checkpoint == null)
            {
                checkpoint = other.GetComponentInParent<Checkpoint2D>();
            }

            if (checkpoint == null)
            {
                return;
            }

            CheckpointManager2D manager = CheckpointManager2D.EnsureInstance();
            if (manager == null)
            {
                return;
            }

            manager.ActivateCheckpoint(checkpoint.transform);
        }
    }
}
