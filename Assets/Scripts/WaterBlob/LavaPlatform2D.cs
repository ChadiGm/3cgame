using UnityEngine;

namespace WaterBlob
{
    [RequireComponent(typeof(Collider2D))]
    public class LavaPlatform2D : MonoBehaviour
    {
        [Header("Lava Effects")]
        [Tooltip("Amount of water to subtract when the player touches this platform.")]
        [SerializeField] private float waterDrain = 0.25f;
        [Tooltip("Immediate multiplier applied to the player's velocity when touching lava.")]
        [Range(0f, 1f)]
        [SerializeField] private float speedMultiplier = 0.5f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other.attachedRigidbody, other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.rigidbody, collision.collider);
        }

        private void HandleCollision(Rigidbody2D rb, Collider2D hitCollider)
        {
            if (!TryResolveWaterLevelTarget(rb, hitCollider, out WaterBlobWaterLevel2D target))
            {
                return;
            }

            Rigidbody2D velocityBody = rb;
            Rigidbody2D ownerBody = target.GetComponent<Rigidbody2D>();
            if (ownerBody != null)
            {
                velocityBody = ownerBody;
            }

            if (velocityBody != null)
            {
                velocityBody.linearVelocity *= speedMultiplier;
            }

            target.RemoveWater(waterDrain);
        }

        private static bool TryResolveWaterLevelTarget(Rigidbody2D rb, Collider2D hitCollider, out WaterBlobWaterLevel2D target)
        {
            target = null;
            if (rb != null)
            {
                target = rb.GetComponent<WaterBlobWaterLevel2D>();
                if (target == null)
                {
                    target = rb.GetComponentInParent<WaterBlobWaterLevel2D>();
                }
            }

            if (target == null && hitCollider != null)
            {
                target = hitCollider.GetComponent<WaterBlobWaterLevel2D>();
                if (target == null)
                {
                    target = hitCollider.GetComponentInParent<WaterBlobWaterLevel2D>();
                }
            }

            return target != null;
        }

        public void SetExternallyDriven(bool value)
        {
            // This platform doesn't have autonomous movement, but the method is needed
            // for API consistency with other platform types when wrapped in 25D proxies.
        }
    }
}
