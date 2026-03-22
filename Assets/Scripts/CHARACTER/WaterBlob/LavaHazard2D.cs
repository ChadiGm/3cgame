using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class LavaHazard2D : MonoBehaviour
    {
        [SerializeField] private bool requirePlayerTag = true;
        [SerializeField] private string playerTag = "Player";

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryKill(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision != null)
            {
                TryKill(collision.collider);
            }
        }

        private void EnsureTriggerCollider()
        {
            Collider2D collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.isTrigger = true;
            }
        }

        private void TryKill(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            OneDropWaterResource2D resource = other.GetComponentInParent<OneDropWaterResource2D>();
            if (resource == null && other.attachedRigidbody != null)
            {
                resource = other.attachedRigidbody.GetComponentInParent<OneDropWaterResource2D>();
            }

            if (resource == null)
            {
                return;
            }

            if (requirePlayerTag)
            {
                if (string.IsNullOrEmpty(playerTag) || !resource.CompareTag(playerTag))
                {
                    return;
                }
            }

            resource.DepleteAllWater();
        }
    }
}
