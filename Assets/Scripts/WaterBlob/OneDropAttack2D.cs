using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class OneDropAttack2D : MonoBehaviour
    {
        [Header("Shooting")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject gunSprite;
        [SerializeField, Min(0f)] private float bulletSpeed = 20f;
        [SerializeField, Min(0f)] private float shootCooldown = 0.12f;
        [SerializeField, Min(0f)] private float bulletLifetime = 4f;

        [Header("Effects")]
        [Tooltip("Prefab for the muzzle / shoot effect. If null, a default one is created.")]
        [SerializeField] private GameObject muzzleEffectPrefab;

        [Tooltip("Prefab for the impact / splash effect. Passed to the bullet. If null, bullet creates a default at runtime.")]
        [SerializeField] private GameObject impactEffectPrefab;
        [Tooltip("Prefab for enemy vaporization. If null, a default vapor effect is created at runtime.")]
        [SerializeField] private GameObject vaporizationEffectPrefab;

        [Header("Bullet Visual")]
        [Tooltip("If true, the bullet will automatically get a water-blob visual matching the character.")]
        [SerializeField] private bool giveBloblVisualToBullet = true;

        [Header("Target Filtering")]
        [Tooltip("Layers that bullets can destroy (recommended: Enemy only).")]
        [SerializeField] private LayerMask bulletAttackableLayers = 0;

        [Tooltip("Layers that bullets can hit but never destroy (recommended: Wall, Ground, Platform).")]
        [SerializeField] private LayerMask bulletNonDestructibleLayers = 0;

        [Tooltip("Layers the bullet will completely ignore / pass through")]
        [SerializeField] private LayerMask bulletIgnoredLayers = 0;

        [Header("Enemy Touch")]
        [Tooltip("Enemy layers that can hurt the player on contact and vaporize on touch.")]
        [SerializeField] private LayerMask touchEnemyLayers = 0;
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0f)] private float touchInvulnerabilityTime = 0.75f;
        [Tooltip("Optional prefab for damage feedback on player touch hit.")]
        [SerializeField] private GameObject playerDamageEffectPrefab;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.25f, 0.25f, 0.9f);

        private float cooldownTimer;
        private float touchInvulnerabilityTimer;
        private bool hasLoggedMissingRefs;
        private int currentHealth;
        private OneDropWaterResource2D waterResource;

        private void Awake()
        {
            ResolveWaterResource();
        }

        private void Start()
        {
            currentHealth = Mathf.Max(1, maxHealth);
            ResolveWaterResource();
        }

        private void Update()
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            touchInvulnerabilityTimer = Mathf.Max(0f, touchInvulnerabilityTimer - Time.deltaTime);
            if (ReadShootPressedThisFrame())
            {
                ShootHorizontal();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.gameObject == null)
            {
                return;
            }

            TryHandleEnemyTouch(collision.gameObject, collision.rigidbody);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || other.gameObject == null)
            {
                return;
            }

            TryHandleEnemyTouch(other.gameObject, other.attachedRigidbody);
        }

        private bool ReadShootPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && (gamepad.rightShoulder.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame))
            {
                return true;
            }

            return false;
#else
            return Input.GetKeyDown(KeyCode.J);
#endif
        }

        private void ShootHorizontal()
        {
            if (cooldownTimer > 0f)
            {
                return;
            }

            if (bulletPrefab == null || firePoint == null)
            {
                if (!hasLoggedMissingRefs)
                {
                    Debug.LogError("[OneDropAttack2D] Missing bulletPrefab or firePoint reference.", this);
                    hasLoggedMissingRefs = true;
                }
                return;
            }

            hasLoggedMissingRefs = false;
            if (waterResource != null && !waterResource.ConsumeShoot())
            {
                return;
            }
            cooldownTimer = shootCooldown;

            float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
            Vector3 shootDirection = facingSign >= 0f ? Vector3.right : Vector3.left;

            if (gunSprite != null)
            {
                gunSprite.transform.rotation = Quaternion.Euler(0f, 0f, facingSign >= 0f ? 0f : 180f);
            }

            // --- Muzzle / Shoot Effect ---
            SpawnMuzzleEffect(facingSign);

            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            // --- Water-blob visual on bullet ---
            if (giveBloblVisualToBullet)
            {
                EnsureBlobVisual(bulletObj);
            }

            // --- Configure runtime bullet behavior from this attack script ---
            ConfigureBulletRuntime(bulletObj);

            Rigidbody2D bulletRb2D = bulletObj.GetComponent<Rigidbody2D>();
            if (bulletRb2D != null)
            {
                bulletRb2D.linearVelocity = (Vector2)(shootDirection * bulletSpeed);
                bulletRb2D.gravityScale = 0f;
            }
            else
            {
                Rigidbody bulletRb3D = bulletObj.GetComponent<Rigidbody>();
                if (bulletRb3D != null)
                {
                    bulletRb3D.useGravity = false;
                    bulletRb3D.linearVelocity = shootDirection * bulletSpeed;
                    bulletRb3D.constraints = RigidbodyConstraints.FreezePositionY
                                           | RigidbodyConstraints.FreezePositionZ
                                           | RigidbodyConstraints.FreezeRotation;
                }
            }

            // Set ignored layers physics collision on the bullet
            ApplyIgnoredLayerCollisions(bulletObj);

            Destroy(bulletObj, bulletLifetime);
        }

        /// <summary>
        /// Spawns the muzzle/shoot effect at the fire point, oriented in the shoot direction.
        /// </summary>
        private void SpawnMuzzleEffect(float facingSign)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, facingSign >= 0f ? 0f : 180f);

            if (muzzleEffectPrefab != null)
            {
                Instantiate(muzzleEffectPrefab, firePoint.position, rotation);
            }
            else
            {
                // Auto-create a default muzzle effect
                GameObject fx = new GameObject("MuzzleEffect_Runtime");
                fx.transform.position = firePoint.position;
                fx.transform.rotation = rotation;
                fx.AddComponent<ShootMuzzleEffect>();
            }
        }

        /// <summary>
        /// Ensures the bullet has the BulletWaterBlobVisual + required components.
        /// </summary>
        private void EnsureBlobVisual(GameObject bulletObj)
        {
            if (bulletObj.GetComponent<BulletWaterBlobVisual>() != null)
            {
                return;
            }

            // Add MeshFilter and MeshRenderer if needed
            if (bulletObj.GetComponent<MeshFilter>() == null)
            {
                bulletObj.AddComponent<MeshFilter>();
            }
            if (bulletObj.GetComponent<MeshRenderer>() == null)
            {
                bulletObj.AddComponent<MeshRenderer>();
            }

            bulletObj.AddComponent<BulletWaterBlobVisual>();
        }

        /// <summary>
        /// Adds runtime hit behavior to the bullet (attackable + ignored layers, effects, lifetime).
        /// </summary>
        private void ConfigureBulletRuntime(GameObject bulletObj)
        {
            if (bulletObj == null)
            {
                return;
            }

            BulletRuntimeHandler handler = bulletObj.GetComponent<BulletRuntimeHandler>();
            if (handler == null)
            {
                handler = bulletObj.AddComponent<BulletRuntimeHandler>();
            }

            handler.Setup(
                bulletAttackableLayers,
                bulletNonDestructibleLayers,
                bulletIgnoredLayers,
                impactEffectPrefab,
                vaporizationEffectPrefab,
                bulletLifetime
            );
        }

        /// <summary>
        /// Disables collisions only for this bullet against objects on ignored layers.
        /// Avoids changing global layer collision rules at runtime.
        /// </summary>
        private void ApplyIgnoredLayerCollisions(GameObject bulletObj)
        {
            if (bulletObj == null || bulletIgnoredLayers.value == 0)
            {
                return;
            }

            Collider2D[] bulletColliders2D = bulletObj.GetComponentsInChildren<Collider2D>(true);
            if (bulletColliders2D.Length > 0)
            {
                Collider2D[] sceneColliders2D = FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < sceneColliders2D.Length; i++)
                {
                    Collider2D other = sceneColliders2D[i];
                    if (other == null || !other.enabled || !other.gameObject.activeInHierarchy || other.transform.IsChildOf(bulletObj.transform))
                    {
                        continue;
                    }

                    if (!IsLayerInMask(other.gameObject.layer, bulletIgnoredLayers))
                    {
                        continue;
                    }

                    for (int j = 0; j < bulletColliders2D.Length; j++)
                    {
                        Collider2D bulletCollider = bulletColliders2D[j];
                        if (bulletCollider != null && bulletCollider.enabled && bulletCollider.gameObject.activeInHierarchy)
                        {
                            Physics2D.IgnoreCollision(bulletCollider, other, true);
                        }
                    }
                }
            }

            Collider[] bulletColliders3D = bulletObj.GetComponentsInChildren<Collider>(true);
            if (bulletColliders3D.Length > 0)
            {
                Collider[] sceneColliders3D = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < sceneColliders3D.Length; i++)
                {
                    Collider other = sceneColliders3D[i];
                    if (other == null || !other.enabled || !other.gameObject.activeInHierarchy || other.transform.IsChildOf(bulletObj.transform))
                    {
                        continue;
                    }

                    if (!IsLayerInMask(other.gameObject.layer, bulletIgnoredLayers))
                    {
                        continue;
                    }

                    for (int j = 0; j < bulletColliders3D.Length; j++)
                    {
                        Collider bulletCollider = bulletColliders3D[j];
                        if (bulletCollider != null && bulletCollider.enabled && bulletCollider.gameObject.activeInHierarchy)
                        {
                            Physics.IgnoreCollision(bulletCollider, other, true);
                        }
                    }
                }
            }
        }

        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void TryHandleEnemyTouch(GameObject touchedObject, Rigidbody2D touchedBody)
        {
            if (touchInvulnerabilityTimer > 0f || touchedObject == null)
            {
                return;
            }

            if (!IsLayerInMask(touchedObject.layer, touchEnemyLayers))
            {
                return;
            }

            GameObject enemyRoot = touchedBody != null ? touchedBody.gameObject : touchedObject;
            SpawnEnemyTouchVaporization(enemyRoot.transform.position);
            Destroy(enemyRoot);
            ApplyTouchDamage();
        }

        private void ApplyTouchDamage()
        {
            touchInvulnerabilityTimer = touchInvulnerabilityTime;
            currentHealth = Mathf.Max(0, currentHealth - 1);
            if (waterResource != null)
            {
                waterResource.ConsumeDamage();
            }
            SpawnPlayerDamageEffect(transform.position);

            if (currentHealth <= 0)
            {
                currentHealth = Mathf.Max(1, maxHealth);
                Debug.Log("[OneDropAttack2D] Player reached 0 HP from enemy touch.");
            }
        }

        private void ResolveWaterResource()
        {
            if (waterResource != null)
            {
                return;
            }

            waterResource = GetComponent<OneDropWaterResource2D>();
            if (waterResource == null)
            {
                waterResource = GetComponentInParent<OneDropWaterResource2D>();
            }
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
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.85f, 0.98f, 1f, 0.95f),
                new Color(0.45f, 0.75f, 1f, 0.6f)
            );
            main.maxParticles = 90;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

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
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = damageFlashColor;
            main.maxParticles = 45;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.16f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(damageFlashColor, 0f),
                    new GradientColorKey(damageFlashColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(damageFlashColor.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(g);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            Destroy(fx, 0.8f);
        }
    }

    internal class BulletRuntimeHandler : MonoBehaviour
    {
        private LayerMask attackableLayers;
        private LayerMask nonDestructibleLayers;
        private LayerMask ignoredLayers;
        private GameObject impactEffectPrefab;
        private GameObject vaporizationEffectPrefab;
        private float lifeTime;
        private bool initialized;

        public void Setup(
            LayerMask attackable,
            LayerMask nonDestructible,
            LayerMask ignored,
            GameObject impactPrefab,
            GameObject vaporizationPrefab,
            float lifetimeSeconds
        )
        {
            attackableLayers = attackable;
            nonDestructibleLayers = nonDestructible;
            ignoredLayers = ignored;
            impactEffectPrefab = impactPrefab;
            vaporizationEffectPrefab = vaporizationPrefab;
            lifeTime = Mathf.Max(0.01f, lifetimeSeconds);
            initialized = true;

            Destroy(gameObject, lifeTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!initialized || collision == null || collision.gameObject == null)
            {
                return;
            }

            HandleHit(
                collision.gameObject.layer,
                collision.rigidbody != null ? collision.rigidbody.gameObject : collision.gameObject,
                collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position,
                collision.contactCount > 0 ? collision.GetContact(0).normal : Vector2.up
            );
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!initialized || collision == null || collision.gameObject == null)
            {
                return;
            }

            HandleHit(
                collision.gameObject.layer,
                collision.rigidbody != null ? collision.rigidbody.gameObject : collision.gameObject,
                collision.contactCount > 0 ? collision.GetContact(0).point : transform.position,
                collision.contactCount > 0 ? collision.GetContact(0).normal : Vector3.up
            );
        }

        private void HandleHit(int otherLayer, GameObject target, Vector3 contactPoint, Vector3 contactNormal)
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            if (IsLayerInMask(otherLayer, ignoredLayers))
            {
                return;
            }

            SpawnImpactEffect(contactPoint, contactNormal);

            bool canBeDestroyed = IsLayerInMask(otherLayer, attackableLayers) && !IsLayerInMask(otherLayer, nonDestructibleLayers);
            if (canBeDestroyed)
            {
                SpawnVaporizationEffect(target.transform.position);
                Destroy(target);
            }

            Destroy(gameObject);
        }

        private void SpawnImpactEffect(Vector3 position, Vector3 normal)
        {
            Vector3 safeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Quaternion rotation = Quaternion.LookRotation(Vector3.forward, safeNormal);

            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, position, rotation);
                return;
            }

            GameObject fx = new GameObject("WaterSplash_Runtime");
            fx.transform.position = position;
            fx.transform.rotation = rotation;
            fx.AddComponent<WaterSplashEffect>();
        }

        private void SpawnVaporizationEffect(Vector3 position)
        {
            if (vaporizationEffectPrefab != null)
            {
                Instantiate(vaporizationEffectPrefab, position, Quaternion.identity);
                return;
            }

            GameObject fx = new GameObject("EnemyVaporize_Runtime");
            fx.transform.position = position;
            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.8f, 0.95f, 1f, 0.95f),
                new Color(0.5f, 0.8f, 1f, 0.65f)
            );
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 48) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.85f, 0.98f, 1f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.75f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.35f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.8f),
                    new Keyframe(0.5f, 1.15f),
                    new Keyframe(1f, 0.1f)
                )
            );

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
            Destroy(fx, 1.2f);
        }

        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }
    }
}
