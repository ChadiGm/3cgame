using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WaterBlobCharacter2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class WaterBlobMovementVfx2D : MonoBehaviour
    {
        [Header("Particle References (Optional)")]
        [SerializeField] private ParticleSystem trailParticles;
        [SerializeField] private ParticleSystem moveDropletsParticles;
        [SerializeField] private ParticleSystem dashPopParticles;
        [SerializeField] private ParticleSystem jumpParticles;
        [SerializeField] private ParticleSystem slideScrapeParticles;
        [SerializeField] private ParticleSystem crouchPulseParticles;
        [SerializeField] private ParticleSystem landingSplashParticles;
        [SerializeField] private TrailRenderer movementTrail;

        [Header("Movement Trail Particles")]
        [SerializeField] private float minTrailSpeed = 1.2f;
        [SerializeField] private float maxTrailSpeed = 9f;
        [SerializeField] private float trailRateAtMaxSpeed = 40f;

        [Header("Movement Trail Renderer")]
        [SerializeField] private float minTrailRendererSpeed = 1f;
        [SerializeField] private float trailRendererTimeAtMaxSpeed = 0.2f;
        [SerializeField] private float trailRendererWidth = 0.34f;

        [Header("Move Droplets")]
        [SerializeField] private float minDropletSpeed = 3.25f;
        [SerializeField] private float dropletIntervalAtMaxSpeed = 0.05f;
        [SerializeField] private float dropletIntervalAtMinSpeed = 0.16f;
        [SerializeField] private int dropletsPerBurst = 3;

        [Header("Jump And Landing")]
        [SerializeField] private float jumpVelocityThreshold = 3.6f;
        [SerializeField] private float landingImpactThreshold = 3.8f;
        [SerializeField] private int jumpBurstCount = 14;
        [SerializeField] private int landingBurstCount = 18;

        [Header("State Bursts")]
        [SerializeField] private int dashBurstCount = 24;
        [SerializeField] private int slideEnterBurstCount = 14;
        [SerializeField] private int crouchEnterBurstCount = 10;

        private WaterBlobCharacter2D blob;
        private Rigidbody2D coreBody;
        private CircleCollider2D coreCollider;

        private float dropletTimer;
        private float lastVerticalVelocity;
        private bool wasGrounded;
        private bool wasDashing;
        private bool wasSliding;
        private bool wasCrouching;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private Material runtimeVfxMaterial;

        private void Awake()
        {
            blob = GetComponent<WaterBlobCharacter2D>();
            coreBody = GetComponent<Rigidbody2D>();
            coreCollider = GetComponent<CircleCollider2D>();

            EnsureParticleSystems();
            EnsureTrailRenderer();
            wasGrounded = IsGrounded();
            wasDashing = blob != null && blob.IsDashing;
            wasSliding = blob != null && blob.IsSliding;
            wasCrouching = blob != null && blob.IsCrouching;
            lastVerticalVelocity = coreBody != null ? coreBody.linearVelocity.y : 0f;
        }

        private void FixedUpdate()
        {
            if (blob == null || coreBody == null)
            {
                return;
            }

            bool grounded = IsGrounded();
            float speed = Mathf.Abs(coreBody.linearVelocity.x);
            float verticalVelocity = coreBody.linearVelocity.y;
            bool isSliding = blob.IsSliding;
            bool isCrouching = blob.IsCrouching;

            UpdateTrailEmission(speed, grounded);
            UpdateTrailRenderer(speed, grounded);
            UpdateMoveDroplets(speed, grounded);
            UpdateSlideScrape(speed, grounded, isSliding);
            HandleDashPop();
            HandleSlideEnter();
            HandleCrouchEnter();
            HandleJumpBurst(grounded, verticalVelocity);
            HandleLandingBurst(grounded);

            wasGrounded = grounded;
            lastVerticalVelocity = verticalVelocity;
            wasSliding = isSliding;
            wasCrouching = isCrouching;
        }

        private void UpdateTrailEmission(float speed, bool grounded)
        {
            if (trailParticles == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = trailParticles.emission;
            if (!grounded || speed < minTrailSpeed)
            {
                emission.rateOverTime = 0f;
                return;
            }

            float t = Mathf.InverseLerp(minTrailSpeed, Mathf.Max(minTrailSpeed + 0.01f, maxTrailSpeed), speed);
            emission.rateOverTime = Mathf.Lerp(8f, trailRateAtMaxSpeed, t);
        }

        private void UpdateTrailRenderer(float speed, bool grounded)
        {
            if (movementTrail == null)
            {
                return;
            }

            bool visible = grounded && speed >= minTrailRendererSpeed;
            movementTrail.emitting = visible;
            if (!visible)
            {
                return;
            }

            float t = Mathf.InverseLerp(minTrailRendererSpeed, Mathf.Max(minTrailRendererSpeed + 0.01f, maxTrailSpeed), speed);
            movementTrail.time = Mathf.Lerp(0.06f, trailRendererTimeAtMaxSpeed, t);
            float width = Mathf.Lerp(trailRendererWidth * 0.5f, trailRendererWidth, t);
            movementTrail.startWidth = width;
            movementTrail.endWidth = width * 0.18f;
        }

        private void UpdateMoveDroplets(float speed, bool grounded)
        {
            if (moveDropletsParticles == null)
            {
                return;
            }

            if (!grounded || speed < minDropletSpeed)
            {
                dropletTimer = 0f;
                return;
            }

            float t = Mathf.InverseLerp(minDropletSpeed, Mathf.Max(minDropletSpeed + 0.01f, maxTrailSpeed), speed);
            float interval = Mathf.Lerp(dropletIntervalAtMinSpeed, dropletIntervalAtMaxSpeed, t);
            dropletTimer += Time.fixedDeltaTime;
            if (dropletTimer < interval)
            {
                return;
            }

            dropletTimer = 0f;
            moveDropletsParticles.Emit(Mathf.Max(1, dropletsPerBurst));
        }

        private void UpdateSlideScrape(float speed, bool grounded, bool isSliding)
        {
            if (slideScrapeParticles == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = slideScrapeParticles.emission;
            if (!grounded || !isSliding)
            {
                emission.rateOverTime = 0f;
                return;
            }

            float t = Mathf.InverseLerp(0f, Mathf.Max(0.01f, maxTrailSpeed), speed);
            emission.rateOverTime = Mathf.Lerp(14f, 55f, t);
        }

        private void HandleDashPop()
        {
            if (dashPopParticles == null || blob == null)
            {
                return;
            }

            bool isDashing = blob.IsDashing;
            if (!wasDashing && isDashing)
            {
                dashPopParticles.Emit(Mathf.Max(1, dashBurstCount));
            }

            wasDashing = isDashing;
        }

        private void HandleSlideEnter()
        {
            if (slideScrapeParticles == null || blob == null)
            {
                return;
            }

            if (!wasSliding && blob.IsSliding)
            {
                slideScrapeParticles.Emit(Mathf.Max(1, slideEnterBurstCount));
            }
        }

        private void HandleCrouchEnter()
        {
            if (crouchPulseParticles == null || blob == null)
            {
                return;
            }

            if (!wasCrouching && blob.IsCrouching)
            {
                crouchPulseParticles.Emit(Mathf.Max(1, crouchEnterBurstCount));
            }
        }

        private void HandleJumpBurst(bool grounded, float verticalVelocity)
        {
            if (jumpParticles == null)
            {
                return;
            }

            bool jumpedThisFrame = wasGrounded && !grounded && verticalVelocity >= jumpVelocityThreshold;
            if (jumpedThisFrame)
            {
                jumpParticles.Emit(Mathf.Max(1, jumpBurstCount));
            }
        }

        private void HandleLandingBurst(bool grounded)
        {
            if (landingSplashParticles == null)
            {
                return;
            }

            bool landedThisFrame = !wasGrounded && grounded;
            float impactSpeed = -lastVerticalVelocity;
            if (landedThisFrame && impactSpeed >= landingImpactThreshold)
            {
                int burst = Mathf.RoundToInt(Mathf.Lerp(landingBurstCount * 0.6f, landingBurstCount * 1.6f, Mathf.InverseLerp(landingImpactThreshold, landingImpactThreshold * 2f, impactSpeed)));
                landingSplashParticles.Emit(Mathf.Max(1, burst));
            }
        }

        private bool IsGrounded()
        {
            if (coreCollider == null || blob == null)
            {
                return false;
            }

            return coreCollider.IsTouchingLayers(blob.groundMask);
        }

        private void EnsureParticleSystems()
        {
            trailParticles ??= CreateDefaultParticles(
                "__TrailParticles",
                lifetime: 0.24f,
                startSpeed: 0.75f,
                startSize: 0.09f,
                color: new Color(0.55f, 0.85f, 1f, 0.55f),
                radius: 0.36f,
                velocityOverLifetime: new Vector2(-0.6f, 0.15f));

            moveDropletsParticles ??= CreateDefaultParticles(
                "__MoveDroplets",
                lifetime: 0.42f,
                startSpeed: 1.7f,
                startSize: 0.11f,
                color: new Color(0.45f, 0.75f, 1f, 0.78f),
                radius: 0.48f,
                velocityOverLifetime: new Vector2(0f, -0.35f));

            dashPopParticles ??= CreateDefaultParticles(
                "__DashPop",
                lifetime: 0.3f,
                startSpeed: 3.2f,
                startSize: 0.13f,
                color: new Color(0.75f, 0.95f, 1f, 0.82f),
                radius: 0.2f,
                velocityOverLifetime: Vector2.zero);

            jumpParticles ??= CreateDefaultParticles(
                "__JumpBurst",
                lifetime: 0.35f,
                startSpeed: 2.4f,
                startSize: 0.12f,
                color: new Color(0.65f, 0.9f, 1f, 0.7f),
                radius: 0.32f,
                velocityOverLifetime: new Vector2(0f, 0.2f));

            slideScrapeParticles ??= CreateDefaultParticles(
                "__SlideScrape",
                lifetime: 0.18f,
                startSpeed: 1.25f,
                startSize: 0.08f,
                color: new Color(0.75f, 0.92f, 1f, 0.55f),
                radius: 0.46f,
                velocityOverLifetime: new Vector2(0f, 0.05f));

            crouchPulseParticles ??= CreateDefaultParticles(
                "__CrouchPulse",
                lifetime: 0.2f,
                startSpeed: 1.4f,
                startSize: 0.1f,
                color: new Color(0.5f, 0.85f, 1f, 0.62f),
                radius: 0.35f,
                velocityOverLifetime: Vector2.zero);

            landingSplashParticles ??= CreateDefaultParticles(
                "__LandingSplash",
                lifetime: 0.34f,
                startSpeed: 2.2f,
                startSize: 0.11f,
                color: new Color(0.62f, 0.9f, 1f, 0.8f),
                radius: 0.52f,
                velocityOverLifetime: new Vector2(0f, -0.2f));
        }

        private ParticleSystem CreateDefaultParticles(
            string childName,
            float lifetime,
            float startSpeed,
            float startSize,
            Color color,
            float radius,
            Vector2 velocityOverLifetime)
        {
            Transform existing = transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName);
            if (existing == null)
            {
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }

            ParticleSystem particles = go.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                particles = go.AddComponent<ParticleSystem>();
            }

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.startColor = color;
            main.maxParticles = 200;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = velocityOverLifetime.x;
            velocity.y = velocityOverLifetime.y;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(color.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sharedMaterial = GetOrCreateRuntimeVfxMaterial();

            return particles;
        }

        private void EnsureTrailRenderer()
        {
            if (movementTrail == null)
            {
                Transform existing = transform.Find("__MovementTrail");
                GameObject go = existing != null ? existing.gameObject : new GameObject("__MovementTrail");
                if (existing == null)
                {
                    go.transform.SetParent(transform);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                }

                movementTrail = go.GetComponent<TrailRenderer>();
                if (movementTrail == null)
                {
                    movementTrail = go.AddComponent<TrailRenderer>();
                }
            }

            movementTrail.sharedMaterial = GetOrCreateRuntimeVfxMaterial();
            movementTrail.time = 0.15f;
            movementTrail.minVertexDistance = 0.04f;
            movementTrail.numCapVertices = 4;
            movementTrail.alignment = LineAlignment.TransformZ;
            movementTrail.textureMode = LineTextureMode.Stretch;
            movementTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            movementTrail.receiveShadows = false;
            movementTrail.emitting = false;
            movementTrail.startWidth = trailRendererWidth;
            movementTrail.endWidth = trailRendererWidth * 0.16f;

            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.55f, 0.88f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.38f, 0.72f, 1f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.36f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            movementTrail.colorGradient = gradient;
        }

        private Material GetOrCreateRuntimeVfxMaterial()
        {
            if (runtimeVfxMaterial != null)
            {
                return runtimeVfxMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            runtimeVfxMaterial = new Material(shader)
            {
                name = "WaterBlobVfxRuntime"
            };
            runtimeVfxMaterial.hideFlags = HideFlags.DontSave;

            Color tint = new(0.66f, 0.9f, 1f, 0.7f);
            if (runtimeVfxMaterial.HasProperty(BaseColorId))
            {
                runtimeVfxMaterial.SetColor(BaseColorId, tint);
            }

            if (runtimeVfxMaterial.HasProperty(ColorId))
            {
                runtimeVfxMaterial.SetColor(ColorId, tint);
            }

            return runtimeVfxMaterial;
        }

        private void OnDestroy()
        {
            if (runtimeVfxMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeVfxMaterial);
            }
            else
            {
                DestroyImmediate(runtimeVfxMaterial);
            }
        }
    }
}
