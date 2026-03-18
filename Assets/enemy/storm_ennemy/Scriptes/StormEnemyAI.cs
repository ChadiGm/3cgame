using UnityEngine;

public class StormEnemyAI : MonoBehaviour
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

    [Header("Return")]
    [SerializeField] private float returnSpeedMultiplier = 1.1f;
    [SerializeField] private float returnSnapDistance = 0.1f;

    private Animator anim;
    private Rigidbody2D body;
    private float startX;
    private int direction = 1;
    private float waitTimer;
    private bool isWaiting;
    private Vector3 originalScale;
    private float minX;
    private float maxX;
    private State state = State.Patrol;
    private Vector2 desiredVelocity;

    private static readonly int IsAngryHash = Animator.StringToHash("IsAngry");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
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
}
