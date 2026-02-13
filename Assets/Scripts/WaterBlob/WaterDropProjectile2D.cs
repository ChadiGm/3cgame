using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class WaterDropProjectile2D : MonoBehaviour
    {
        [Min(0f)] public float lifeTime = 1.5f;
        [Min(0f)] public float normalImpactImpulse = 2.5f;
        [Min(0f)] public float chargedImpactImpulse = 6f;

        private Rigidbody2D rb;
        private bool isCharged;
        private bool initialized;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Destroy(gameObject, lifeTime);
        }

        public void Initialize(Vector2 direction, float speed, bool charged)
        {
            isCharged = charged;
            initialized = true;
            rb.linearVelocity = direction.normalized * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized || other == null || other.isTrigger)
            {
                return;
            }

            Rigidbody2D otherBody = other.attachedRigidbody;
            if (otherBody != null)
            {
                Vector2 pushDir = rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized : Vector2.right;
                float impulse = isCharged ? chargedImpactImpulse : normalImpactImpulse;
                otherBody.AddForce(pushDir * impulse, ForceMode2D.Impulse);
            }

            Destroy(gameObject);
        }
    }
}
