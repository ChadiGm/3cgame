using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Patrol")]
    [Tooltip("How far left/right the enemy patrols from its start position")]
    [SerializeField] private float patrolDistance = 5f;
    [Tooltip("Time to wait at each patrol endpoint before turning around")]
    [SerializeField] private float waitTime = 0.5f;

    private Animator anim;
    private Rigidbody2D body;

    private float startX;
    private int direction = 1; // 1 = right, -1 = left
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private Vector3 originalScale;

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
    }

    void Update()
    {
        if (isWaiting)
        {
            // Stop moving during wait
            body.linearVelocity = new Vector2(0, body.linearVelocity.y);
            anim.SetBool("run", false);

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

        // Animation: always running while moving
        anim.SetBool("run", true);

        // Check if reached patrol boundary
        float distFromStart = transform.position.x - startX;
        if ((direction > 0 && distFromStart >= patrolDistance) ||
            (direction < 0 && distFromStart <= -patrolDistance))
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }
}
