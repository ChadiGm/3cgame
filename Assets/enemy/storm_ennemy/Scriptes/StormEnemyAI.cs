using System.Collections;
using UnityEngine;
using WaterBlob;

public class StormEnemyAI : MonoBehaviour, IPlayerRespawnResettable
{
    private enum State { Patrol, Chase, Return }

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float waitTime = 0.5f;

    [Header("Patrol Bounds (optional)")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;

    [Header("Chase / Angry")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float exitBuffer = 1.5f;
    [SerializeField] private float chaseSpeedMultiplier = 1.1f;
    [SerializeField] private float attackRange = 2.75f;

    [Header("Return")]
    [SerializeField] private float returnSpeedMultiplier = 1.1f;
    [SerializeField] private float returnSnapDistance = 0.1f;

    [Header("Lightning Attack")]
    [SerializeField, Min(0f)] private float attackCooldown = 1.25f;
    [SerializeField, Min(0f)] private float attackWindup = 0.08f;
    [SerializeField, Min(1)] private int lightningAnimationLoops = 2;
    [SerializeField, Min(0.01f)] private float lightningFrameDuration = 0.06f;
    [SerializeField] private Vector2 lightningLocalOffset = new Vector2(0f, -2f);
    [SerializeField] private Sprite[] lightningFrames;
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField, Min(0.05f)] private float impactVfxLifetime = 0.35f;
    [SerializeField] private AudioClip lightningSfx;
    [SerializeField, Range(0f, 1f)] private float lightningSfxVolume = 0.9f;
    [SerializeField] private AudioSource lightningAudioSource;

    [Header("Lightning Damage")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0f)] private float lightningDamageRadius = 0.85f;
    [SerializeField] private bool damageOnImpact = true;

    private Animator anim;
    private Rigidbody2D body;
    private Transform lightningAnchor;
    private SpriteRenderer lightningRenderer;
    private float startX;
    private int direction = 1;
    private float waitTimer;
    private bool isWaiting;
    private Vector3 originalScale;
    private float minX;
    private float maxX;
    private State state = State.Patrol;
    private Vector2 desiredVelocity;
    private bool isAttacking;
    private float nextAttackTime;
    private Coroutine attackRoutine;
    private bool warnedMissingLightningFrames;

    private static readonly int IsAngryHash = Animator.StringToHash("IsAngry");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        EnsureLightningVisual();
        EnsureAudioSource();
    }

    private void Start()
    {
        startX = transform.position.x;
        originalScale = transform.localScale;

        if (leftPoint != null && rightPoint != null)
        {
            minX = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
            maxX = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
        }
        else
        {
            minX = startX - patrolDistance;
            maxX = startX + patrolDistance;
        }

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }
    }

    private void Update()
    {
        HandleState();
        TickState();
        TryStartLightningAttack();
        UpdateAngerFlag();
    }

    private void FixedUpdate()
    {
        body.linearVelocity = desiredVelocity;
    }

    private void HandleState()
    {
        if (target == null)
        {
            state = State.Patrol;
            return;
        }

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        float enterSqr = detectionRange * detectionRange;
        float exitSqr = (detectionRange + exitBuffer) * (detectionRange + exitBuffer);

        switch (state)
        {
            case State.Patrol:
            case State.Return:
                if (sqrDist <= enterSqr)
                {
                    state = State.Chase;
                    isWaiting = false;
                }
                break;
            case State.Chase:
                if (sqrDist > exitSqr)
                {
                    state = State.Return;
                    isWaiting = false;
                }
                break;
        }
    }

    private void TickState()
    {
        switch (state)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Return:
                ReturnToPatrol();
                break;
        }
    }

    private void Patrol()
    {
        if (isAttacking)
        {
            desiredVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        if (isWaiting)
        {
            desiredVelocity = new Vector2(0f, body.linearVelocity.y);
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                direction *= -1;
            }
            return;
        }

        desiredVelocity = new Vector2(direction * speed, body.linearVelocity.y);
        Flip(direction);

        float x = transform.position.x;
        if ((direction > 0 && x >= maxX) || (direction < 0 && x <= minX))
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    private void Chase()
    {
        if (isAttacking)
        {
            desiredVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        if (target == null)
        {
            state = State.Return;
            return;
        }

        float chaseDir = Mathf.Sign(target.position.x - transform.position.x);
        desiredVelocity = new Vector2(chaseDir * speed * chaseSpeedMultiplier, body.linearVelocity.y);
        Flip((int)chaseDir);
    }

    private void ReturnToPatrol()
    {
        if (isAttacking)
        {
            desiredVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        float clampX = Mathf.Clamp(transform.position.x, minX, maxX);
        float dir = clampX > transform.position.x ? 1f : -1f;

        if (Mathf.Abs(transform.position.x - clampX) <= returnSnapDistance)
        {
            transform.position = new Vector3(clampX, transform.position.y, transform.position.z);
            isWaiting = false;
            state = State.Patrol;
            return;
        }

        desiredVelocity = new Vector2(dir * speed * returnSpeedMultiplier, body.linearVelocity.y);
        Flip((int)dir);
    }

    private void Flip(int dirSign)
    {
        if (dirSign == 0) return;
        Vector3 scale = originalScale;
        scale.x = Mathf.Abs(originalScale.x) * Mathf.Sign(dirSign);
        transform.localScale = scale;
        direction = dirSign;
    }

    private void UpdateAngerFlag()
    {
        if (anim == null) return;
        anim.SetBool(IsAngryHash, state == State.Chase);
    }

    private void TryStartLightningAttack()
    {
        if (isAttacking || target == null || state != State.Chase || Time.time < nextAttackTime)
        {
            return;
        }

        if (lightningFrames == null || lightningFrames.Length == 0)
        {
            if (!warnedMissingLightningFrames)
            {
                Debug.LogWarning("[StormEnemyAI] No lightning frames assigned. Fill 'Lightning Frames' with your 5 sprites.", this);
                warnedMissingLightningFrames = true;
            }
            return;
        }

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > attackRange * attackRange)
        {
            return;
        }

        attackRoutine = StartCoroutine(LightningAttackRoutine());
    }

    private IEnumerator LightningAttackRoutine()
    {
        isAttacking = true;
        desiredVelocity = new Vector2(0f, body.linearVelocity.y);

        if (target != null)
        {
            int faceSign = target.position.x >= transform.position.x ? 1 : -1;
            Flip(faceSign);
        }

        if (attackWindup > 0f)
        {
            yield return new WaitForSeconds(attackWindup);
        }

        int loops = Mathf.Max(1, lightningAnimationLoops);
        float frameWait = Mathf.Max(0.01f, lightningFrameDuration);

        for (int loop = 0; loop < loops; loop++)
        {
            for (int i = 0; i < lightningFrames.Length; i++)
            {
                Sprite frame = lightningFrames[i];
                if (frame != null)
                {
                    ShowLightningFrame(frame);
                }

                bool isFinalFrameOfAttack = loop == loops - 1 && i == lightningFrames.Length - 1;
                if (isFinalFrameOfAttack)
                {
                    OnLightningImpact();
                }

                yield return new WaitForSeconds(frameWait);
            }
        }

        HideLightning();
        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
        attackRoutine = null;
    }

    private void EnsureLightningVisual()
    {
        if (lightningAnchor == null)
        {
            GameObject anchorObj = new GameObject("LightningAnchor");
            lightningAnchor = anchorObj.transform;
            lightningAnchor.SetParent(transform);
            lightningAnchor.localPosition = new Vector3(lightningLocalOffset.x, lightningLocalOffset.y, 0f);
            lightningAnchor.localRotation = Quaternion.identity;
            lightningAnchor.localScale = Vector3.one;
        }

        lightningRenderer = lightningAnchor.GetComponent<SpriteRenderer>();
        if (lightningRenderer == null)
        {
            lightningRenderer = lightningAnchor.gameObject.AddComponent<SpriteRenderer>();
        }

        SpriteRenderer bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer != null)
        {
            lightningRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            lightningRenderer.sortingOrder = bodyRenderer.sortingOrder - 1;
        }

        lightningRenderer.enabled = false;
    }

    private void EnsureAudioSource()
    {
        if (lightningAudioSource == null)
        {
            lightningAudioSource = GetComponent<AudioSource>();
        }

        if (lightningAudioSource == null)
        {
            lightningAudioSource = gameObject.AddComponent<AudioSource>();
        }

        lightningAudioSource.playOnAwake = false;
        lightningAudioSource.loop = false;
        lightningAudioSource.spatialBlend = 0f;
    }

    private void ShowLightningFrame(Sprite frame)
    {
        if (lightningAnchor != null)
        {
            lightningAnchor.localPosition = new Vector3(lightningLocalOffset.x, lightningLocalOffset.y, 0f);
        }

        if (lightningRenderer == null)
        {
            return;
        }

        lightningRenderer.sprite = frame;
        lightningRenderer.enabled = true;
    }

    private void HideLightning()
    {
        if (lightningRenderer == null)
        {
            return;
        }

        lightningRenderer.enabled = false;
        lightningRenderer.sprite = null;
    }

    private void OnLightningImpact()
    {
        Vector3 impactPosition = lightningAnchor != null ? lightningAnchor.position : transform.position;

        if (impactVfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(impactVfxPrefab, impactPosition, Quaternion.identity);
            if (impactVfxLifetime > 0f)
            {
                Destroy(vfxInstance, impactVfxLifetime);
            }
        }
        else
        {
            SpawnFallbackImpactVfx(impactPosition);
        }

        if (lightningSfx != null && lightningAudioSource != null)
        {
            lightningAudioSource.PlayOneShot(lightningSfx, lightningSfxVolume);
        }

        if (damageOnImpact)
        {
            ApplyLightningDamage(impactPosition);
        }
    }

    private void SpawnFallbackImpactVfx(Vector3 position)
    {
        GameObject fx = new GameObject("StormLightningImpactVfx");
        fx.transform.position = position;

        SpriteRenderer fxRenderer = fx.AddComponent<SpriteRenderer>();
        if (lightningRenderer != null)
        {
            fxRenderer.sortingLayerID = lightningRenderer.sortingLayerID;
            fxRenderer.sortingOrder = lightningRenderer.sortingOrder + 1;
            fxRenderer.sprite = lightningRenderer.sprite;
        }

        fxRenderer.color = new Color(0.65f, 0.9f, 1f, 0.85f);

        StormLightningImpactPulse pulse = fx.AddComponent<StormLightningImpactPulse>();
        pulse.Initialize(impactVfxLifetime);
    }

    private void ApplyLightningDamage(Vector3 impactPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(impactPosition, lightningDamageRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.CompareTag(playerTag))
            {
                continue;
            }

            OneDropWaterResource2D resource = hit.GetComponentInParent<OneDropWaterResource2D>();
            if (resource != null)
            {
                resource.ConsumeDamage();
            }
        }
    }

    public void ResetForPlayerRespawn()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isWaiting = false;
        waitTimer = 0f;
        desiredVelocity = Vector2.zero;
        state = State.Patrol;
        direction = transform.localScale.x >= 0f ? 1 : -1;
        isAttacking = false;
        nextAttackTime = 0f;
        warnedMissingLightningFrames = false;

        HideLightning();

        if (lightningAudioSource != null)
        {
            lightningAudioSource.Stop();
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        UpdateAngerFlag();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        Vector3 center = transform.position + (Vector3)lightningLocalOffset;
        Gizmos.DrawWireSphere(center, lightningDamageRadius);
    }
}

public class StormLightningImpactPulse : MonoBehaviour
{
    private float life = 0.35f;
    private float timer;
    private SpriteRenderer spriteRenderer;

    public void Initialize(float lifetime)
    {
        life = Mathf.Max(0.05f, lifetime);
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / life);

        transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.2f, t);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(0.85f, 0f, t);
            spriteRenderer.color = color;
        }

        if (timer >= life)
        {
            Destroy(gameObject);
        }
    }
}
