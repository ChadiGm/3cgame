using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("Shooting")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject gunSprite;
        [SerializeField, Min(0f)] private float bulletSpeed = 20f;
        [SerializeField, Min(0f)] private float shootCooldown = 0.12f;
        [SerializeField, Min(0f)] private float bulletLifetime = 4f;
        [SerializeField, Min(0)] private int initialPoolSize = 8;

        [Header("Charged Shot")]
        [SerializeField, Min(0f)] private float chargeTimeThreshold = 0.8f;
        [SerializeField, Min(0f)] private float chargedBulletSpeed = 30f;
        [SerializeField, Min(1f)] private float chargedBulletScale = 2.5f;
        [SerializeField] private Color chargeFlashColor = Color.white;

        [Header("Effects")]
        [Tooltip("Prefab for the muzzle / shoot effect. If null, a default one is created.")]
        [SerializeField] private GameObject muzzleEffectPrefab;

        [Tooltip("Prefab for the impact / splash effect. Passed to the bullet.")]
        [SerializeField] private GameObject impactEffectPrefab;
        [Tooltip("Prefab for enemy vaporization. Passed to the bullet.")]
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

        private float cooldownTimer;
        private float chargeTimer;
        private bool isCharging;
        private bool hasLoggedMissingRefs;
        private Vector3 originalGunScale = Vector3.one;
        private OneDropWaterResource2D waterResource;
        private WaterBlobInput2D inputSource;
        private readonly Queue<BulletRuntimeHandler> bulletPool = new();
        private Transform bulletPoolRoot;
        private Vector3 baseBulletScale = Vector3.one;

        private void Awake()
        {
            if (gunSprite != null) originalGunScale = gunSprite.transform.localScale;
            ResolveWaterResource();
            inputSource = GetComponent<WaterBlobInput2D>();
            if (bulletPrefab != null)
            {
                baseBulletScale = bulletPrefab.transform.localScale;
            }
        }

        private void Start()
        {
            ResolveWaterResource();
            WarmBulletPool();
        }

        private void Update()
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            HandleShootInput();
        }

        private void HandleShootInput()
        {
            bool isHeld = ReadShootHeld();

            if (isHeld)
            {
                if (!isCharging && cooldownTimer <= 0f)
                {
                    isCharging = true;
                    chargeTimer = 0f;
                }

                if (isCharging)
                {
                    chargeTimer += Time.deltaTime;
                    UpdateChargeFeedback();
                }
            }
            else
            {
                if (isCharging)
                {
                    if (chargeTimer >= chargeTimeThreshold)
                    {
                        ShootCharged();
                    }
                    else
                    {
                        ShootHorizontal();
                    }
                    isCharging = false;
                    chargeTimer = 0f;
                    ResetChargeFeedback();
                }
            }
        }

        private bool ReadShootHeld()
        {
            if (inputSource != null)
            {
                return inputSource.ShootHeld;
            }

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)) return true;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) return true;

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && (gamepad.rightShoulder.isPressed || gamepad.buttonWest.isPressed)) return true;

            return false;
#else
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetMouseButton(0);
#endif
        }

        private void ShootHorizontal()
        {
            if (cooldownTimer > 0f) return;

            if (bulletPrefab == null || firePoint == null)
            {
                if (!hasLoggedMissingRefs)
                {
                    Debug.LogError("[PlayerCombatController] Missing bulletPrefab or firePoint reference.", this);
                    hasLoggedMissingRefs = true;
                }
                return;
            }

            hasLoggedMissingRefs = false;
            if (waterResource != null && !waterResource.ConsumeShoot()) return;
            
            cooldownTimer = shootCooldown;

            float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
            Vector3 shootDirection = facingSign >= 0f ? Vector3.right : Vector3.left;

            if (gunSprite != null)
            {
                gunSprite.transform.rotation = Quaternion.Euler(0f, 0f, facingSign >= 0f ? 0f : 180f);
            }

            SpawnMuzzleEffect(facingSign);
            EventBus.Publish(new AudioTriggerEvent(AudioEventType.Shoot));

            GameObject bulletObj = AcquireBullet(1f);
            bulletObj.transform.SetPositionAndRotation(firePoint.position, Quaternion.identity);
            Rigidbody2D bulletRb2D = bulletObj.GetComponent<Rigidbody2D>();
            if (bulletRb2D != null)
            {
                bulletRb2D.linearVelocity = Vector2.zero;
                bulletRb2D.angularVelocity = 0f;
                bulletRb2D.linearVelocity = (Vector2)(shootDirection * bulletSpeed);
                bulletRb2D.gravityScale = 0f;
            }
        }

        private void ShootCharged()
        {
            if (cooldownTimer > 0f) return;

            if (bulletPrefab == null || firePoint == null) return;

            // Charged shots consume 2 water (assuming normal shot is 1, handled in ConsumeShoot())
            if (waterResource != null && !waterResource.Consume(2f)) return;

            cooldownTimer = shootCooldown * 2f; // Longer cooldown for charged shot

            float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
            Vector3 shootDirection = facingSign >= 0f ? Vector3.right : Vector3.left;

            SpawnMuzzleEffect(facingSign);
            EventBus.Publish(new AudioTriggerEvent(AudioEventType.ChargeShoot));

            GameObject bulletObj = AcquireBullet(chargedBulletScale);
            bulletObj.transform.SetPositionAndRotation(firePoint.position, Quaternion.identity);
            Rigidbody2D bulletRb2D = bulletObj.GetComponent<Rigidbody2D>();
            if (bulletRb2D != null)
            {
                bulletRb2D.linearVelocity = Vector2.zero;
                bulletRb2D.angularVelocity = 0f;
                bulletRb2D.linearVelocity = (Vector2)(shootDirection * chargedBulletSpeed);
                bulletRb2D.gravityScale = 0f;
            }
        }

        private void UpdateChargeFeedback()
        {
            // Visual feedback: simple pulsing of the gun sprite or something similar
            if (gunSprite != null)
            {
                float pulse = 1f + Mathf.PingPong(Time.time * 10f, 0.2f);
                float chargeProgress = Mathf.Clamp01(chargeTimer / chargeTimeThreshold);
                gunSprite.transform.localScale = originalGunScale * (pulse + chargeProgress * 0.5f);
            }
        }

        private void ResetChargeFeedback()
        {
            if (gunSprite != null)
            {
                gunSprite.transform.localScale = originalGunScale;
            }
        }

        private void SpawnMuzzleEffect(float facingSign)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, facingSign >= 0f ? 0f : 180f);

            if (muzzleEffectPrefab != null)
            {
                Instantiate(muzzleEffectPrefab, firePoint.position, rotation);
            }
            else
            {
                GameObject fx = new GameObject("MuzzleEffect_Runtime");
                fx.transform.position = firePoint.position;
                fx.transform.rotation = rotation;
                fx.AddComponent<ShootMuzzleEffect>();
            }
        }

        private void EnsureBlobVisual(GameObject bulletObj)
        {
            if (bulletObj.GetComponent<BulletWaterBlobVisual>() != null) return;

            if (bulletObj.GetComponent<MeshFilter>() == null) bulletObj.AddComponent<MeshFilter>();
            if (bulletObj.GetComponent<MeshRenderer>() == null) bulletObj.AddComponent<MeshRenderer>();

            bulletObj.AddComponent<BulletWaterBlobVisual>();
        }

        private void ConfigureBulletRuntime(GameObject bulletObj)
        {
            if (bulletObj == null) return;

            BulletRuntimeHandler handler = bulletObj.GetComponent<BulletRuntimeHandler>();
            if (handler == null) handler = bulletObj.AddComponent<BulletRuntimeHandler>();

            handler.Configure(
                bulletAttackableLayers,
                bulletNonDestructibleLayers,
                bulletIgnoredLayers,
                impactEffectPrefab,
                vaporizationEffectPrefab,
                ReturnBulletToPool
            );
        }

        private void WarmBulletPool()
        {
            if (bulletPrefab == null || initialPoolSize <= 0)
            {
                return;
            }

            for (int i = bulletPool.Count; i < initialPoolSize; i++)
            {
                BulletRuntimeHandler handler = CreatePooledBulletInstance();
                ReturnBulletToPool(handler);
            }
        }

        private GameObject AcquireBullet(float scaleMultiplier)
        {
            BulletRuntimeHandler handler = null;
            while (bulletPool.Count > 0 && handler == null)
            {
                handler = bulletPool.Dequeue();
            }

            if (handler == null)
            {
                handler = CreatePooledBulletInstance();
            }

            GameObject bulletObj = handler.gameObject;
            bulletObj.SetActive(true);
            bulletObj.transform.SetParent(null, true);
            bulletObj.transform.localScale = baseBulletScale * scaleMultiplier;
            handler.Activate(bulletLifetime);
            return bulletObj;
        }

        private BulletRuntimeHandler CreatePooledBulletInstance()
        {
            GameObject bulletObj = Instantiate(bulletPrefab);
            bulletObj.name = bulletPrefab.name + "_Pooled";

            if (giveBloblVisualToBullet)
            {
                EnsureBlobVisual(bulletObj);
            }

            ConfigureBulletRuntime(bulletObj);
            BulletRuntimeHandler handler = bulletObj.GetComponent<BulletRuntimeHandler>();
            handler.ResetPhysicsState();
            bulletObj.SetActive(false);
            return handler;
        }

        private void ReturnBulletToPool(BulletRuntimeHandler handler)
        {
            if (handler == null)
            {
                return;
            }

            if (bulletPoolRoot == null)
            {
                GameObject poolRootObject = new GameObject("BulletPool_Runtime");
                bulletPoolRoot = poolRootObject.transform;
            }

            handler.ResetPhysicsState();
            GameObject bulletObj = handler.gameObject;
            bulletObj.transform.SetParent(bulletPoolRoot, false);
            bulletObj.SetActive(false);
            bulletPool.Enqueue(handler);
        }


        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void ResolveWaterResource()
        {
            if (waterResource != null) return;

            waterResource = GetComponent<OneDropWaterResource2D>();
            if (waterResource == null)
            {
                waterResource = GetComponentInParent<OneDropWaterResource2D>();
            }
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

        private System.Action<BulletRuntimeHandler> releaseToPool;
        private float lifeTimer;

        public void Configure(LayerMask attackable, LayerMask nonDestructible, LayerMask ignored, GameObject impactPrefab, GameObject vaporizationPrefab, System.Action<BulletRuntimeHandler> releaseAction)
        {
            attackableLayers = attackable;
            nonDestructibleLayers = nonDestructible;
            ignoredLayers = ignored;
            impactEffectPrefab = impactPrefab;
            vaporizationEffectPrefab = vaporizationPrefab;
            releaseToPool = releaseAction;
            
            Collider2D coll = GetComponent<Collider2D>();
            if (coll != null) coll.isTrigger = true;

            initialized = true;
        }

        public void Activate(float lifetimeSeconds)
        {
            lifeTime = Mathf.Max(0.01f, lifetimeSeconds);
            lifeTimer = lifeTime;
            initialized = true;
        }

        public void ResetPhysicsState()
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                ReleaseSelf();
            }
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (!initialized || collider == null || collider.gameObject == null) return;

            HandleHit(
                collider.gameObject.layer,
                collider.attachedRigidbody != null ? collider.attachedRigidbody.gameObject : collider.gameObject,
                collider.ClosestPoint(transform.position),
                Vector2.up
            );
        }

        private void HandleHit(int otherLayer, GameObject target, Vector3 contactPoint, Vector3 contactNormal)
        {
            if (target == null)
            {
                ReleaseSelf();
                return;
            }

            if (target.CompareTag("Player")) return;
            if (IsLayerInMask(otherLayer, ignoredLayers)) return;

            SpawnImpactEffect(contactPoint, contactNormal);

            bool canBeDestroyed = IsLayerInMask(otherLayer, attackableLayers) && !IsLayerInMask(otherLayer, nonDestructibleLayers);
            if (canBeDestroyed)
            {
                SpawnVaporizationEffect(target.transform.position);
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(1);
                }
                else
                {
                    Debug.LogWarning($"[PlayerCombatController] Hit object {target.name} on attackable layer {LayerMask.LayerToName(otherLayer)} but it lacks IDamageable. No damage applied.", target);
                }
            }

            ReleaseSelf();
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
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.4f;
            main.startColor = new Color(0.8f, 0.95f, 1f, 0.95f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            Destroy(fx, 1.2f);
        }

        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void ReleaseSelf()
        {
            initialized = false;
            lifeTimer = 0f;
            if (releaseToPool != null)
            {
                releaseToPool(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
