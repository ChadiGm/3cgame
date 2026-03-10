using UnityEngine;

namespace WaterBlob
{
    [RequireComponent(typeof(OneDropWaterResource2D))]
    public class PlayerHurtbox : MonoBehaviour
    {
        [Tooltip("Enemy layers that can hurt the player on contact and vaporize on touch.")]
        [SerializeField] private LayerMask touchEnemyLayers = 0;
        
        [Tooltip("Prefab for damage feedback on player touch hit.")]
        [SerializeField] private GameObject playerDamageEffectPrefab;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.25f, 0.25f, 0.9f);
        
        [Tooltip("Prefab for enemy vaporization on touch.")]
        [SerializeField] private GameObject vaporizationEffectPrefab;

        private OneDropWaterResource2D waterResource;

        private void Awake()
        {
            waterResource = GetComponent<OneDropWaterResource2D>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.gameObject, collision.rigidbody);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other.gameObject, other.attachedRigidbody);
        }

        private void HandleHit(GameObject touchedObj, Rigidbody2D rb)
        {
            if (touchedObj == null || (waterResource != null && waterResource.IsInvulnerable)) return;
            if (!IsLayerInMask(touchedObj.layer, touchEnemyLayers)) return;

            GameObject enemyRoot = rb != null ? rb.gameObject : touchedObj;
            SpawnEnemyTouchVaporization(enemyRoot.transform.position);
            
            IDamageable damageable = enemyRoot.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(1);
            }
            else
            {
                Debug.LogWarning($"[PlayerHurtbox] Touched object {enemyRoot.name} on enemy layer {LayerMask.LayerToName(touchedObj.layer)} but it lacks IDamageable. No damage applied to enemy.", enemyRoot);
            }

            if (waterResource != null)
            {
                waterResource.TakeDamage(waterResource.damageCost);
            }

            SpawnPlayerDamageEffect(transform.position);
        }

        private bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void SpawnEnemyTouchVaporization(Vector3 position)
        {
            if (vaporizationEffectPrefab != null)
            {
                Instantiate(vaporizationEffectPrefab, position, Quaternion.identity);
                return;
            }

            GameObject fx = new GameObject("EnemyTouchVapor_Runtime");
            fx.transform.position = position;
            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.startColor = new Color(0.85f, 0.98f, 1f, 0.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            Destroy(fx, 1f);
        }

        private void SpawnPlayerDamageEffect(Vector3 position)
        {
            if (playerDamageEffectPrefab != null)
            {
                Instantiate(playerDamageEffectPrefab, position, Quaternion.identity);
                return;
            }

            GameObject fx = new GameObject("OneDropDamage_Runtime");
            fx.transform.position = position;
            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startColor = damageFlashColor;
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            Destroy(fx, 0.8f);
        }
    }
}
