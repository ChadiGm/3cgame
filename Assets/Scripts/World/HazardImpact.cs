using UnityEngine;

namespace WaterBlob
{
    /// <summary>
    /// Component for environmental hazards like lava platforms.
    /// Causes continuous water loss / damage when the player stays in contact.
    /// </summary>
    public class HazardImpact : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Water loss per second while touching the hazard.")]
        [SerializeField] private float damagePerSecond = 20f;
        
        [Tooltip("Knockback force applied away from the hazard center.")]
        [SerializeField] private float knockbackForce = 5f;

        [Header("Visuals & Feedback")]
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField] private float effectInterval = 0.5f;
        
        private float effectTimer;

        private void OnCollisionStay2D(Collision2D collision)
        {
            ProcessHazard(collision.gameObject, collision.contacts[0].point);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ProcessHazard(other.gameObject, other.transform.position);
        }

        private void ProcessHazard(GameObject target, Vector2 contactPoint)
        {
            // Only affect the player
            if (!target.CompareTag("Player"))
            {
                // Fallback for untagged blobs during scene setup
                if (target.name != "WaterBlobPlayer") return;
            }

            IWaterReceiver water = target.GetComponent<IWaterReceiver>();
            
            if (water != null)
            {
                if (water.IsInvulnerable) return;

                // Apply continuous damage (bypassing I-frames for lava)
                water.TakeWaterDamage(damagePerSecond * Time.deltaTime, true);

                // Periodic effects (hissing, steam, etc)
                effectTimer -= Time.deltaTime;
                if (effectTimer <= 0)
                {
                    effectTimer = effectInterval;
                    SpawnEffect(contactPoint);
                    Debug.Log($"[HazardImpact] Sizzling {target.name}! Water level: {water.NormalizedAmount:P1}");
                }

                // Apply slight knockback to the actual moving physics body
                Rigidbody2D rb = target.GetComponentInParent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 dir = (Vector2)target.transform.position - contactPoint;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
                    rb.AddForce(dir.normalized * knockbackForce, ForceMode2D.Force);
                }
            }
        }

        private void SpawnEffect(Vector2 position)
        {
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, position, Quaternion.identity);
            }
        }
    }
}
