using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class YAxisFollower2D : MonoBehaviour
    {
        public enum FollowMode
        {
            AbsoluteTargetY,
            RelativeDeltaFromStart,
            ConstantUpSpeed
        }

        [Header("Follow Target")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private float yOffset = 0f;
        [SerializeField] private bool onlyFollowUpwards = true;
        [SerializeField] private bool forceUpwardOnly = true;
        [SerializeField] private FollowMode followMode = FollowMode.RelativeDeltaFromStart;
        [SerializeField, Min(0f)] private float constantUpSpeed = 2f;
        [SerializeField] private bool keepRisingWhenTargetStops = true;
        [SerializeField] private bool autoReacquireTarget = true;
        [SerializeField, Min(0.05f)] private float reacquireInterval = 0.5f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float followSpeed = 6f;
        [SerializeField, Min(0f)] private float returnSpeed = 6f;
        [SerializeField] private bool useRigidbody = true;
        [SerializeField] private bool debugLogs = false;

        private Vector3 initialPosition;
        private Rigidbody2D rb;
        private bool isFollowing;

        // FIX: maxFollowY now tracks the TARGET ceiling, not the object's current Y.
        // This lets followSpeed control how fast the object actually moves toward that ceiling.
        private float maxTargetY;

        private float followStartTargetY;
        private float followStartObjectY;
        private float followStartFixedTime;
        private float lastReacquireTime = -999f;

        public bool IsFollowing => isFollowing;

        private void Awake()
        {
            initialPosition = transform.position;
            rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (isFollowing && followTarget != null)
            {
                float targetY = followTarget.position.y + yOffset;
                float desiredY;

                if (followMode == FollowMode.ConstantUpSpeed)
                {
                    desiredY = transform.position.y + constantUpSpeed * Time.fixedDeltaTime;
                }
                else if (forceUpwardOnly)
                {
                    float delta = Mathf.Max(0f, targetY - followStartTargetY);
                    desiredY = followStartObjectY + delta;
                }
                else
                {
                    if (followMode == FollowMode.RelativeDeltaFromStart)
                    {
                        float delta = targetY - followStartTargetY;
                        if (onlyFollowUpwards) delta = Mathf.Max(0f, delta);
                        desiredY = followStartObjectY + delta;
                    }
                    else
                    {
                        desiredY = targetY;
                        if (onlyFollowUpwards)
                            desiredY = Mathf.Max(desiredY, followStartObjectY);
                    }
                }

                if (keepRisingWhenTargetStops && followMode != FollowMode.ConstantUpSpeed)
                {
                    float elapsed = Mathf.Max(0f, Time.fixedTime - followStartFixedTime);
                    float independentY = followStartObjectY + constantUpSpeed * elapsed;
                    if (independentY > desiredY) desiredY = independentY;
                }

                // FIX: advance the TARGET ceiling, not the object's position directly.
                if (onlyFollowUpwards || forceUpwardOnly)
                {
                    if (desiredY > maxTargetY)
                        maxTargetY = desiredY;

                    desiredY = maxTargetY;
                }

                // Now smoothly move the object toward the target Y using followSpeed.
                Vector3 desired = new Vector3(initialPosition.x, desiredY, initialPosition.z);
                MoveToward(desired, followSpeed);
                return;
            }

            if (isFollowing && autoReacquireTarget)
                TryReacquireTarget();

            MoveToward(initialPosition, returnSpeed);
        }

        public void BeginFollow(Transform target)
        {
            if (target == null) return;

            followTarget = target;
            isFollowing = true;
            followStartTargetY = target.position.y + yOffset;
            followStartObjectY = transform.position.y;
            followStartFixedTime = Time.fixedTime;
            maxTargetY = followStartObjectY; // FIX: initialise target ceiling to current Y

            if (debugLogs)
                Debug.Log($"YAxisFollower2D: BeginFollow -> {target.name}", this);
        }

        public void StopFollowAndReturn()
        {
            isFollowing = false;
            followTarget = null;
            if (debugLogs)
                Debug.Log("YAxisFollower2D: StopFollow", this);
        }

        private void TryReacquireTarget()
        {
            if (Time.time - lastReacquireTime < reacquireInterval) return;

            lastReacquireTime = Time.time;
            Transform found = ResolveFallbackTarget();
            if (found != null) BeginFollow(found);
        }

        private static Transform ResolveFallbackTarget()
        {
            OneDropController2D controller = FindObjectOfType<OneDropController2D>();
            if (controller != null) return controller.transform;

            OneDropWaterResource2D water = FindObjectOfType<OneDropWaterResource2D>();
            if (water != null) return water.transform;

            GameObject tagged = GameObject.FindWithTag("Player");
            if (tagged != null) return tagged.transform;

            return null;
        }

        private void MoveToward(Vector3 desired, float speed)
        {
            if (speed <= 0f) return;

            float step = speed * Time.fixedDeltaTime;
            Vector3 next = Vector3.MoveTowards(transform.position, desired, step);
            SetPosition(next);
        }

        private void SetPosition(Vector3 position)
        {
            if (useRigidbody && rb != null)
                rb.MovePosition(position);
            else
                transform.position = position;
        }
    }
}