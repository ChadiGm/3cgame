using UnityEngine;

/// <summary>
/// Fire enemy movement brain:
/// - Patrol between bounds
/// - Chase player when detected
/// - Return to patrol when player leaves
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FireEnemyMovement : MonoBehaviour
{
    private enum State { Patrol, Chase, Return }

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float waitTime = 0.5f;

    [Header("Patrol Bounds (optional)")]
    [Tooltip("If both are assigned, the enemy patrols between these points instead of start +/- patrolDistance.")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;

    [Header("Chase")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float exitBuffer = 1.5f;
    [SerializeField] private float chaseSpeedMultiplier = 1.3f;

    [Header("Return")]
    [SerializeField] private float returnSpeedMultiplier = 1.1f;
    [SerializeField] private float returnSnapDistance = 0.1f;

    [Header("Animator (optional)")]
    [SerializeField] private bool setMoveBool = false;
    [SerializeField] private string moveBoolName = "run";
    [SerializeField] private Animator animator;

    private Rigidbody2D body;
    private float startX;
    private int direction = 1; // 1 = right, -1 = left
    private float waitTimer;
    private bool isWaiting;
    private bool isAttacking;
    private Vector3 originalScale;
    private float minX;
    private float maxX;
    private State state = State.Patrol;
    private Vector2 desiredVelocity;
    private int moveBoolHash;
    private bool hasMoveBool;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (animator == null && setMoveBool)
        {
            animator = GetComponent<Animator>();
        }
        if (setMoveBool && animator != null && !string.IsNullOrEmpty(moveBoolName))
        {
            moveBoolHash = Animator.StringToHash(moveBoolName);
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.nameHash == moveBoolHash)
                {
                    hasMoveBool = true;
                    break;
                }
            }
        }
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
        if (isAttacking)
        {
            desiredVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        HandleState();
        TickState();
    }

    private void FixedUpdate()
    {
        body.linearVelocity = desiredVelocity;
        if (setMoveBool && hasMoveBool && animator != null)
        {
            bool moving = Mathf.Abs(desiredVelocity.x) > 0.01f;
            animator.SetBool(moveBoolHash, moving);
        }
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
        if (isWaiting)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
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

        // Safety: if we are inside patrol band and almost stopped, resume patrol.
        if (transform.position.x > minX && transform.position.x < maxX && Mathf.Abs(desiredVelocity.x) < 0.01f)
        {
            state = State.Patrol;
            isWaiting = false;
        }
    }

    private void Flip(int dirSign)
    {
        if (dirSign == 0) return;
        Vector3 scale = originalScale;
        scale.x = Mathf.Abs(originalScale.x) * Mathf.Sign(dirSign);
        transform.localScale = scale;
        direction = dirSign;
    }

    public void SetAttacking(bool value)
    {
        isAttacking = value;
        if (value)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }
    }
}
