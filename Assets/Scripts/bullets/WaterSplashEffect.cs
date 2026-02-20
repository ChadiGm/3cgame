using UnityEngine;

/// <summary>
/// Spawns a water-ball splash / explosion particle effect when instantiated.
/// Destroys itself once the particles finish playing.
/// Attach to an empty GameObject and assign it as the impact prefab on the bullet.
/// </summary>
public class WaterSplashEffect : MonoBehaviour
{
    [Header("Splash Settings")]
    [SerializeField, Min(1)] private int burstCount = 12;
    [SerializeField, Min(0.1f)] private float lifetime = 0.6f;
    [SerializeField, Min(0.01f)] private float startSize = 0.18f;
    [SerializeField, Min(0.01f)] private float endSize = 0.02f;
    [SerializeField, Min(0.1f)] private float startSpeed = 3.5f;
    [SerializeField] private Color startColor = new Color(0.25f, 0.78f, 1f, 0.85f);
    [SerializeField] private Color endColor = new Color(0.6f, 0.92f, 1f, 0f);
    [SerializeField, Min(0.1f)] private float gravityModifier = 0.8f;
    [SerializeField] private int sortingOrder = 15;

    [Header("Ring Wave (optional)")]
    [SerializeField] private bool enableRingWave = true;
    [SerializeField, Min(0.1f)] private float ringDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float ringStartRadius = 0.1f;
    [SerializeField, Min(0.05f)] private float ringEndRadius = 0.6f;
    [SerializeField] private Color ringColor = new Color(0.82f, 0.95f, 1f, 0.7f);

    private void Awake()
    {
        CreateSplashParticles();

        if (enableRingWave)
        {
            CreateRingWave();
        }

        // Destroy this effect object after everything finishes
        Destroy(gameObject, Mathf.Max(lifetime, ringDuration) + 0.1f);
    }

    private void CreateSplashParticles()
    {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = lifetime;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startColor = startColor;
        main.gravityModifier = gravityModifier;
        main.maxParticles = burstCount + 5;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        // Emission: single burst
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, (short)burstCount)
        });

        // Shape: hemisphere for splash feel
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        // Size over lifetime: shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 0.7f),
            new Keyframe(1f, endSize / startSize)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime: fade out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(startColor.a * 0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Renderer setup
        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
            Shader shader = FindBestShader();
            if (shader != null)
            {
                Material mat = new Material(shader) { name = "WaterSplashParticle" };
                mat.color = Color.white;
                renderer.sharedMaterial = mat;
            }
        }
    }

    private void CreateRingWave()
    {
        GameObject ringObj = new GameObject("RingWave");
        ringObj.transform.SetParent(transform);
        ringObj.transform.localPosition = Vector3.zero;

        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        Shader shader = FindBestShader();
        if (shader == null) return;

        Material ringMat = new Material(shader) { name = "WaterRingWave" };
        ringMat.color = ringColor;
        lr.sharedMaterial = ringMat;
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = 0.04f;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.sortingOrder = sortingOrder + 1;
        lr.startColor = ringColor;
        lr.endColor = ringColor;

        WaterRingWaveAnimator animator = ringObj.AddComponent<WaterRingWaveAnimator>();
        animator.Init(ringStartRadius, ringEndRadius, ringDuration, ringColor);
    }

    private static Shader FindBestShader()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) return shader;
        shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null) return shader;
        return Shader.Find("Unlit/Color");
    }
}

/// <summary>
/// Animates an expanding ring (circle) that fades out - the "wave" of a water splash.
/// </summary>
public class WaterRingWaveAnimator : MonoBehaviour
{
    private float startRadius;
    private float endRadius;
    private float duration;
    private Color baseColor;
    private LineRenderer lr;
    private float timer;
    private const int RingSegments = 32;

    public void Init(float startR, float endR, float dur, Color color)
    {
        startRadius = startR;
        endRadius = endR;
        duration = dur;
        baseColor = color;
    }

    private void Start()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null)
        {
            Destroy(gameObject);
            return;
        }
        lr.positionCount = RingSegments;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        float currentRadius = Mathf.Lerp(startRadius, endRadius, t);
        float alpha = Mathf.Lerp(baseColor.a, 0f, t);

        Color c = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        lr.startColor = c;
        lr.endColor = c;
        lr.widthMultiplier = Mathf.Lerp(0.05f, 0.01f, t);

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            lr.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * currentRadius,
                Mathf.Sin(angle) * currentRadius,
                0f
            ));
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
