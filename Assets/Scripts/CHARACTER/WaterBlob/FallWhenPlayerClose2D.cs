using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class FallWhenPlayerClose2D : MonoBehaviour, IPlayerRespawnResettable
    {
        [Header("Target")]
        [SerializeField] private Transform player;
        [SerializeField] private string playerTag = "Player";

        [Header("Trigger")]
        [SerializeField, Min(0.01f)] private float triggerDistance = 3f;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField, Min(0f)] private float fallDelay = 0f;
        [Tooltip("If player starts already inside range, wait until they exit then re-enter.")]
        [SerializeField] private bool ignoreIfPlayerStartsInside = true;
        [Tooltip("If true, player must remain in range during delay.")]
        [SerializeField] private bool requireStayInRangeForDelay = true;

        [Header("Fall Setup")]
        [Tooltip("Keep object locked at start, then release it only when triggered.")]
        [SerializeField] private bool holdInPlaceUntilTriggered = true;
        [SerializeField] private bool makeDynamicOnTrigger = true;
        [SerializeField] private bool enableGravityOnTrigger = true;
        [SerializeField] private Vector2 startVelocity2D = Vector2.zero;
        [SerializeField] private Vector3 startVelocity3D = Vector3.zero;

        [Header("Debug")]
        [SerializeField] private bool drawTriggerGizmo = true;
        [SerializeField] private Color gizmoColor = new(1f, 0.7f, 0.1f, 0.4f);

        private Rigidbody2D rb2D;
        private Rigidbody rb3D;
        private bool isTriggered;
        private bool delayActive;
        private float delayTimer;
        private bool startStateInitialized;
        private bool playerWasInsideRange;
        private float nextResolveTime;
        private RigidbodyType2D initialBodyType2D;
        private float initialGravityScale2D;
        private bool initialIsKinematic3D;
        private bool initialUseGravity3D;

        private void Awake()
        {
            rb2D = GetComponent<Rigidbody2D>();
            rb3D = GetComponent<Rigidbody>();
            CacheInitialPhysicsState();
            ApplyHoldStateIfNeeded();
            TryResolvePlayer(true);
        }

        private void Update()
        {
            if (isTriggered && triggerOnce)
            {
                return;
            }

            TryResolvePlayer(false);
            if (player == null)
            {
                return;
            }

            float maxDistance = triggerDistance;
            float sqrDistance = (player.position - transform.position).sqrMagnitude;
            bool isInsideRange = sqrDistance <= maxDistance * maxDistance;

            if (!startStateInitialized)
            {
                startStateInitialized = true;
                playerWasInsideRange = isInsideRange;
                if (ignoreIfPlayerStartsInside && isInsideRange)
                {
                    return;
                }
            }

            if (ignoreIfPlayerStartsInside && playerWasInsideRange && isInsideRange)
            {
                return;
            }

            if (ignoreIfPlayerStartsInside && playerWasInsideRange && !isInsideRange)
            {
                playerWasInsideRange = false;
                return;
            }

            if (isInsideRange)
            {
                if (fallDelay <= 0f)
                {
                    TriggerFall();
                }
                else
                {
                    if (!delayActive)
                    {
                        delayActive = true;
                        delayTimer = fallDelay;
                    }
                    else
                    {
                        delayTimer -= Time.deltaTime;
                        if (delayTimer <= 0f)
                        {
                            delayActive = false;
                            TriggerFall();
                        }
                    }
                }
                return;
            }

            if (requireStayInRangeForDelay && delayActive)
            {
                delayActive = false;
                delayTimer = 0f;
            }
        }

        private void TriggerFall()
        {
            if (isTriggered && triggerOnce)
            {
                return;
            }

            isTriggered = true;

            if (rb2D != null)
            {
                if (makeDynamicOnTrigger)
                {
                    rb2D.bodyType = RigidbodyType2D.Dynamic;
                }
                else
                {
                    rb2D.bodyType = initialBodyType2D;
                }

                if (enableGravityOnTrigger)
                {
                    rb2D.gravityScale = Mathf.Approximately(initialGravityScale2D, 0f) ? 1f : initialGravityScale2D;
                }
                else
                {
                    rb2D.gravityScale = 0f;
                }

                rb2D.linearVelocity = startVelocity2D;
                return;
            }

            if (rb3D != null)
            {
                if (makeDynamicOnTrigger)
                {
                    rb3D.isKinematic = false;
                }
                else
                {
                    rb3D.isKinematic = initialIsKinematic3D;
                }

                if (enableGravityOnTrigger)
                {
                    rb3D.useGravity = initialUseGravity3D || !initialIsKinematic3D;
                }
                else
                {
                    rb3D.useGravity = false;
                }

                rb3D.linearVelocity = startVelocity3D;
            }
        }

        private void CacheInitialPhysicsState()
        {
            if (rb2D != null)
            {
                initialBodyType2D = rb2D.bodyType;
                initialGravityScale2D = rb2D.gravityScale;
            }

            if (rb3D != null)
            {
                initialIsKinematic3D = rb3D.isKinematic;
                initialUseGravity3D = rb3D.useGravity;
            }
        }

        private void ApplyHoldStateIfNeeded()
        {
            if (!holdInPlaceUntilTriggered)
            {
                return;
            }

            if (rb2D != null)
            {
                rb2D.linearVelocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
                rb2D.gravityScale = 0f;
                rb2D.bodyType = RigidbodyType2D.Kinematic;
            }

            if (rb3D != null)
            {
                rb3D.linearVelocity = Vector3.zero;
                rb3D.angularVelocity = Vector3.zero;
                rb3D.useGravity = false;
                rb3D.isKinematic = true;
            }
        }

        private void TryResolvePlayer(bool immediate)
        {
            if (player != null)
            {
                return;
            }

            if (!immediate && Time.time < nextResolveTime)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(playerTag))
            {
                GameObject tagged = GameObject.FindGameObjectWithTag(playerTag);
                if (tagged != null)
                {
                    player = tagged.transform;
                }
            }

            if (player == null)
            {
                GameObject named = GameObject.Find("WaterBlobPlayer");
                if (named != null)
                {
                    player = named.transform;
                }
            }

            nextResolveTime = Time.time + 0.5f;
        }

        private void OnValidate()
        {
            triggerDistance = Mathf.Max(0.01f, triggerDistance);
            fallDelay = Mathf.Max(0f, fallDelay);
        }

        public void ResetForPlayerRespawn()
        {
            isTriggered = false;
            delayActive = false;
            delayTimer = 0f;
            startStateInitialized = false;
            playerWasInsideRange = false;
            nextResolveTime = 0f;

            if (rb2D != null)
            {
                rb2D.linearVelocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
                rb2D.bodyType = initialBodyType2D;
                rb2D.gravityScale = initialGravityScale2D;
            }

            if (rb3D != null)
            {
                rb3D.linearVelocity = Vector3.zero;
                rb3D.angularVelocity = Vector3.zero;
                rb3D.isKinematic = initialIsKinematic3D;
                rb3D.useGravity = initialUseGravity3D;
            }

            ApplyHoldStateIfNeeded();
            TryResolvePlayer(true);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawTriggerGizmo)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, triggerDistance);
        }
    }
}
