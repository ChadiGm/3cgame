using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class LavaBackgroundFX2D : MonoBehaviour
    {
        [Header("Auto References")]
        [SerializeField] private Transform player;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private Transform mainCamera;

        [Header("Parallax")]
        [SerializeField, Min(0f)] private float parallaxX = 0.12f;
        [SerializeField, Min(0f)] private float parallaxY = 0.08f;
        [SerializeField, Min(0f)] private float followLerp = 4f;
        [SerializeField, Min(0f)] private float speedLookAhead = 0.45f;
        [SerializeField, Min(0f)] private float jumpInfluenceY = 0.35f;
        [SerializeField, Min(0.01f)] private float maxSpeedReference = 14f;

        [Header("Reactive Visual")]
        [SerializeField, Min(0f)] private float pulseAmount = 0.025f;
        [SerializeField, Min(0f)] private float pulseSpeed = 1.6f;
        [SerializeField, Min(0f)] private float colorLerp = 4f;
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color fastMoveColor = new(1f, 0.93f, 0.86f, 1f);
        [SerializeField] private Color airborneColor = new(0.88f, 0.95f, 1f, 1f);

        private SpriteRenderer sr;
        private Vector3 basePos;
        private Vector3 baseScale;
        private Vector3 cameraStartPos;
        private float nextRefResolveTime;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            basePos = transform.position;
            baseScale = transform.localScale;

            TryResolveReferences();
            if (mainCamera != null)
            {
                cameraStartPos = mainCamera.position;
            }

            sr.color = baseColor;
        }

        private void LateUpdate()
        {
            TryResolveReferences();
            if (playerBody == null || mainCamera == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            Vector2 vel = playerBody.linearVelocity;
            float speed01 = Mathf.Clamp01(Mathf.Abs(vel.x) / maxSpeedReference);
            float air01 = Mathf.Clamp01(Mathf.Abs(vel.y) / maxSpeedReference);

            Vector3 camDelta = mainCamera.position - cameraStartPos;
            float lookAheadX = Mathf.Sign(vel.x) * speedLookAhead * speed01;
            float jumpOffsetY = Mathf.Clamp(vel.y / maxSpeedReference, -1f, 1f) * jumpInfluenceY;

            Vector3 targetPos = basePos + new Vector3(camDelta.x * parallaxX + lookAheadX, camDelta.y * parallaxY + jumpOffsetY, 0f);
            transform.position = Vector3.Lerp(transform.position, targetPos, followLerp * dt);

            float wave = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            float pulse = pulseAmount * (0.35f + speed01 * 0.65f) * wave;
            Vector3 targetScale = baseScale * (1f + pulse);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, followLerp * dt);

            Color targetColor = Color.Lerp(baseColor, fastMoveColor, speed01);
            targetColor = Color.Lerp(targetColor, airborneColor, air01 * 0.6f);
            sr.color = Color.Lerp(sr.color, targetColor, colorLerp * dt);
        }

        private void TryResolveReferences()
        {
            if (Time.time < nextRefResolveTime)
            {
                return;
            }

            if (player == null)
            {
                GameObject playerGo = GameObject.Find("WaterBlobPlayer");
                if (playerGo != null)
                {
                    player = playerGo.transform;
                }
            }

            if (playerBody == null && player != null)
            {
                playerBody = player.GetComponent<Rigidbody2D>();
            }

            if (mainCamera == null && Camera.main != null)
            {
                mainCamera = Camera.main.transform;
            }

            if (player != null && playerBody != null && mainCamera != null)
            {
                nextRefResolveTime = float.PositiveInfinity;
            }
            else
            {
                nextRefResolveTime = Time.time + 0.5f;
            }
        }
    }
}
