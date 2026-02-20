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

            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

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

            Destroy(bulletObj, bulletLifetime);
        }
    }
}
