using UnityEngine;

namespace WaterBlob
{
    [RequireComponent(typeof(Collider2D))]
    public class WaterPlatform2D : MonoBehaviour
    {
        [Header("Water Gain")]
        [Tooltip("Amount of water to add to the player when they touch this platform.")]
        [SerializeField] private float waterAmount = 0.25f;

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

            target.AddWater(waterAmount);
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
