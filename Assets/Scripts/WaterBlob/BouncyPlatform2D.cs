using UnityEngine;

namespace WaterBlob
{
    [RequireComponent(typeof(Collider2D))]
    public class BouncyPlatform2D : MonoBehaviour
    {
        [SerializeField] private float upwardVelocity = 16f;
        [SerializeField] private float lateralDamp = 0.92f;
        [SerializeField] private float minIncomingDownwardVelocity = -0.1f;
        [Header("Loop Motion")]
        [SerializeField] private float verticalAmplitude = 1f;
        [SerializeField] private float verticalFrequency = 0.5f;
        [SerializeField] private float phaseOffsetDegrees;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool externallyDriven;

        private Rigidbody2D platformBody;
        private Vector2 startPosition;

        private void Awake()
        {
            platformBody = GetComponent<Rigidbody2D>();
            startPosition = platformBody != null ? platformBody.position : transform.position;
        }

        private void FixedUpdate()
        {
            if (externallyDriven)
            {
                return;
            }

            float time = useUnscaledTime ? Time.fixedUnscaledTime : Time.fixedTime;
            float phase = phaseOffsetDegrees * Mathf.Deg2Rad;
            float offsetY = Mathf.Sin((time * Mathf.PI * 2f * Mathf.Max(0f, verticalFrequency)) + phase) * Mathf.Max(0f, verticalAmplitude);

            Vector2 target = new(startPosition.x, startPosition.y + offsetY);
            if (platformBody != null && platformBody.bodyType == RigidbodyType2D.Kinematic)
            {
                platformBody.MovePosition(target);
                return;
            }

            transform.position = target;
        }

        public void SetExternallyDriven(bool value)
        {
            externallyDriven = value;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Bounce(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Bounce(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Bounce(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            Bounce(collision);
        }

        private void Bounce(Collision2D collision)
        {
            if (collision == null)
            {
                return;
            }

            Rigidbody2D rb = collision.rigidbody;
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            if (!HasUpwardFacingContact(collision))
            {
                return;
            }

            if (rb.linearVelocity.y > minIncomingDownwardVelocity)
            {
                return;
            }

            ApplyBounce(rb);
        }

        private void Bounce(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            if (rb.linearVelocity.y > minIncomingDownwardVelocity)
            {
                return;
            }

            ApplyBounce(rb);
        }

        private void ApplyBounce(Rigidbody2D rb)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x *= Mathf.Clamp01(lateralDamp);
            velocity.y = Mathf.Max(velocity.y, upwardVelocity);
            rb.linearVelocity = velocity;
        }

        private static bool HasUpwardFacingContact(Collision2D collision)
        {
            ContactPoint2D[] contacts = collision.contacts;
            for (int i = 0; i < contacts.Length; i++)
            {
                if (contacts[i].normal.y > 0.35f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
