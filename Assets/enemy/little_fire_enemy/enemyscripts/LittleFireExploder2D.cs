using UnityEngine;
using WaterBlob;

[DisallowMultipleComponent]
public class LittleFireExploder2D : MonoBehaviour, IPlayerRespawnResettable
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.1f)] private float explodeDistance = 1.75f;

    [Header("Explosion VFX")]
    [SerializeField] private GameObject explosionVfxPrefab;
    [SerializeField, Min(0.05f)] private float fallbackExplosionLifetime = 1f;

    [Header("Damage Area")]
    [Tooltip("Optional area prefab. If empty, a runtime trigger area is created.")]
    [SerializeField] private GameObject damageAreaPrefab;
    [SerializeField] private GameObject damageAreaVfxPrefab;
    [SerializeField] private Vector2 areaOffset = Vector2.zero;
    [SerializeField, Min(0.1f)] private float areaRadius = 1.35f;
    [SerializeField, Min(0.01f)] private float areaDuration = 2.25f;
    [Tooltip("Manual damage amount to apply each tick while the player stays in the area.")]
    [SerializeField, Min(0.01f)] private float damageAmount = 20f;
    [SerializeField, Min(0.01f)] private float damageInterval = 0.45f;
    [SerializeField] private bool requirePlayerTagForDamage = true;
    [SerializeField] private bool damageImmediatelyOnEnter = true;

    [Header("Cleanup")]
    [SerializeField, Min(0f)] private float destroyDelay = 0f;

    private bool exploded;
    private Rigidbody2D body;
    private Collider2D[] colliders;
    private Renderer[] renderers;
    private Animator animator;
    private FireEnemyMovement movement;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        animator = GetComponent<Animator>();
        movement = GetComponent<FireEnemyMovement>();
    }

    private void Start()
    {
        ResolveTarget();
    }

    private void Update()
    {
        if (exploded)
        {
            return;
        }

        if (target == null)
        {
            ResolveTarget();
            if (target == null)
            {
                return;
            }
        }

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist <= explodeDistance * explodeDistance)
        {
            Explode();
        }
    }

    private void ResolveTarget()
    {
        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }
    }

    private void Explode()
    {
        exploded = true;
        Vector3 areaPosition = transform.position + (Vector3)areaOffset;

        SpawnExplosionVfx(transform.position);
        SpawnDamageArea(areaPosition);
        SpawnDamageAreaVfx(areaPosition);

        ResetOnPlayerRespawn2D resettable = GetComponent<ResetOnPlayerRespawn2D>();
        if (resettable != null)
        {
            resettable.Despawn();
            return;
        }

        DisableEnemyVisualAndCollisions();
        Destroy(gameObject, destroyDelay);
    }

    public void ResetForPlayerRespawn()
    {
        exploded = false;
        ResolveTarget();
    }

    private void DisableEnemyVisualAndCollisions()
    {
        if (movement != null)
        {
            movement.SetAttacking(true);
            movement.enabled = false;
        }

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }
    }

    private void SpawnDamageArea(Vector3 areaPosition)
    {
        GameObject areaObject;
        if (damageAreaPrefab != null)
        {
            areaObject = Instantiate(damageAreaPrefab, areaPosition, Quaternion.identity);
        }
        else
        {
            areaObject = new GameObject("LittleFireDamageArea_Runtime");
            areaObject.transform.position = areaPosition;
        }

        CircleCollider2D circleCollider = areaObject.GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = areaObject.AddComponent<CircleCollider2D>();
        }

        circleCollider.isTrigger = true;
        circleCollider.radius = areaRadius;

        LittleFireDamageArea2D area = areaObject.GetComponent<LittleFireDamageArea2D>();
        if (area == null)
        {
            area = areaObject.AddComponent<LittleFireDamageArea2D>();
        }

        area.Configure(
            damageAmount,
            damageInterval,
            areaDuration,
            requirePlayerTagForDamage,
            playerTag,
            damageImmediatelyOnEnter);
    }

    private void SpawnDamageAreaVfx(Vector3 position)
    {
        if (damageAreaVfxPrefab == null)
        {
            return;
        }

        GameObject spawnedVfx = Instantiate(damageAreaVfxPrefab, position, Quaternion.identity);
        if (areaDuration > 0f)
        {
            Destroy(spawnedVfx, areaDuration);
        }
    }

    private void SpawnExplosionVfx(Vector3 position)
    {
        if (explosionVfxPrefab != null)
        {
            Instantiate(explosionVfxPrefab, position, Quaternion.identity);
            return;
        }

        GameObject fx = new("LittleFireExplosion_Runtime");
        fx.transform.position = position;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.3f, 0.95f),
            new Color(1f, 0.35f, 0.1f, 0.8f));
        main.maxParticles = 140;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        ps.Play();
        Destroy(fx, fallbackExplosionLifetime);
    }
}
