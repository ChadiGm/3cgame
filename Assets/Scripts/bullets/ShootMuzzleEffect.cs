using UnityEngine;

/// <summary>
/// A quick muzzle / shoot particle burst that plays when the player fires.
/// Spawns small water droplets ejecting forward from the fire point.
/// Destroys itself automatically after playing.
/// </summary>
public class ShootMuzzleEffect : MonoBehaviour
{
    [Header("Muzzle Settings")]
    [SerializeField, Min(1)] private int burstCount = 6;
    [SerializeField, Min(0.05f)] private float lifetime = 0.25f;
    [SerializeField, Min(0.01f)] private float startSize = 0.12f;
    [SerializeField, Min(0.1f)] private float startSpeed = 5f;
    [SerializeField] private Color particleColor = new Color(0.4f, 0.85f, 1f, 0.8f);
    [SerializeField, Min(0f)] private float spreadAngle = 25f;
    [SerializeField] private int sortingOrder = 14;

    private void Awake()
    {
        CreateMuzzleParticles();
        Destroy(gameObject, lifetime + 0.1f);
    }

    private void CreateMuzzleParticles()
    {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = lifetime;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startColor = particleColor;
        main.gravityModifier = 0f;
        main.maxParticles = burstCount + 2;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emission: single burst
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, (short)burstCount)
        });

        // Shape: cone pointing forward
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spreadAngle;
        shape.radius = 0.02f;

        // Size over lifetime: shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime: fade out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(particleColor, 0f),
                new GradientColorKey(particleColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(particleColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Renderer
        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
            Shader shader = FindBestShader();
            if (shader != null)
            {
                Material mat = new Material(shader) { name = "MuzzleParticle" };
                mat.color = Color.white;
                renderer.sharedMaterial = mat;
            }
        }

        ps.Play();
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
