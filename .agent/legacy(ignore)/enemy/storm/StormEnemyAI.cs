using UnityEngine;

public class StormEnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Patrol")]
    [Tooltip("How far left/right the enemy patrols from its start position")]
    [SerializeField] private float patrolDistance = 5f;
    [Tooltip("Time to wait at each patrol endpoint before turning around")]
    [SerializeField] private float waitTime = 0.5f;

    [Header("Anger")]
    [Tooltip("Target to measure distance from (defaults to Player tag if empty).")]
    [SerializeField] private Transform target;
    [Tooltip("Distance at which the enemy becomes angry.")]
    [SerializeField] private float angryDistance = 3f;
    [Tooltip("Distance at which the enemy calms down (use a slightly larger value to avoid flicker).")]
    [SerializeField] private float angryExitDistance = 3.5f;

    private Animator anim;
    private Rigidbody2D body;

    private float startX;
    private int direction = 1; // 1 = right, -1 = left
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isAngry = false;
    private Vector3 originalScale;

    private static readonly int IsAngryHash = Animator.StringToHash("IsAngry");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        // Remember the starting position and original scale
        startX = transform.position.x;
        originalScale = transform.localScale;

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        if (angryExitDistance < angryDistance)
        {
            angryExitDistance = angryDistance;
        }
    }

    private void Update()
    {
        UpdateAnger();

        if (isWaiting)
        {
            // Stop moving during wait
            body.linearVelocity = new Vector2(0, body.linearVelocity.y);

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                direction *= -1; // Reverse direction
            }
            return;
        }

        // Move automatically in current direction
        body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);

        // Flip sprite based on direction (preserve original scale)
        Vector3 scale = originalScale;
        scale.x = Mathf.Abs(originalScale.x) * direction;
        transform.localScale = scale;

        // Check if reached patrol boundary
        float distFromStart = transform.position.x - startX;
        if ((direction > 0 && distFromStart >= patrolDistance) ||
            (direction < 0 && distFromStart <= -patrolDistance))
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    private void UpdateAnger()
    {
        if (anim == null || target == null)
        {
            return;
        }

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        float enterSqr = angryDistance * angryDistance;
        float exitSqr = angryExitDistance * angryExitDistance;

        if (!isAngry)
        {
            if (sqrDist <= enterSqr)
            {
                isAngry = true;
            }
        }
        else
        {
            if (sqrDist >= exitSqr)
            {
                isAngry = false;
            }
        }

        anim.SetBool(IsAngryHash, isAngry);
    }
}
