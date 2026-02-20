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

        [Header("Bullet Visual")]
        [Tooltip("If true, the bullet will automatically get a water-blob visual matching the character.")]
        [SerializeField] private bool giveBloblVisualToBullet = true;

        [Header("Target Filtering")]
        [Tooltip("Layers the bullet CAN hit and explode on")]
        [SerializeField] private LayerMask bulletAttackableLayers = ~0;

        [Tooltip("Layers the bullet will completely ignore / pass through")]
        [SerializeField] private LayerMask bulletIgnoredLayers = 0;

        private float cooldownTimer;

        private void Update()
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            if (ReadShootPressedThisFrame())
            {
                ShootHorizontal();
            }
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
            if (cooldownTimer > 0f || bulletPrefab == null || firePoint == null)
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

            // --- Pass layer settings & impact prefab to bullet ---
            bullet bulletScript = bulletObj.GetComponent<bullet>();
            if (bulletScript == null)
            {
                bulletScript = bulletObj.AddComponent<bullet>();
            }
            // Use reflection-free approach: set public/serialized fields via helper
            SetBulletSettings(bulletScript);

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
        /// Passes attack/ignore layer masks and impact prefab to the bullet.
        /// </summary>
        private void SetBulletSettings(bullet b)
        {
            // Access serialized fields through the public lifeTime, but layer masks 
            // and impact effect are private [SerializeField]. We use a small helper approach:
            // The bullet exposes a Setup method for runtime assignment.
            b.lifeTime = bulletLifetime;
            b.RuntimeSetup(bulletAttackableLayers, bulletIgnoredLayers, impactEffectPrefab);
        }

        /// <summary>
        /// Disables physics collisions between the bullet and all ignored layers.
        /// </summary>
        private void ApplyIgnoredLayerCollisions(GameObject bulletObj)
        {
            if (bulletIgnoredLayers.value == 0) return;

            int bulletLayer = bulletObj.layer;
            for (int i = 0; i < 32; i++)
            {
                if ((bulletIgnoredLayers.value & (1 << i)) != 0)
                {
                    Physics2D.IgnoreLayerCollision(bulletLayer, i, true);
                }
            }
        }
    }
}
