using UnityEngine;
using WaterBlob;
using WaterBlob.AI;

namespace WaterBlob.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(StateMachine))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Movement")]
        public float speed = 3f;

        [Header("Patrol")]
        [Tooltip("How far left/right the enemy patrols from its start position")]
        public float patrolDistance = 5f;
        [Tooltip("Time to wait at each patrol endpoint before turning around")]
        public float waitTime = 0.5f;

        [Header("Detection")]
        [Tooltip("Horizontal range to detect player")]
        public float detectionRange = 8f;
        [Tooltip("Range at which enemy can attack")]
        public float attackRange = 1.5f;

        [Tooltip("Layers considered obstacles (walls, ledges).")]
        public LayerMask obstacleMask = 0;
        [Tooltip("Distance to check ahead for walls.")]
        public float wallCheckDistance = 0.2f;
        [Tooltip("Distance downward to check for ground ahead.")]
        public float edgeCheckDistance = 0.5f;
        [Tooltip("Horizontal offset (in local space) from the enemy's center for the ground check raycast.")]
        public Vector2 groundCheckOffset = new Vector2(0.3f, -0.5f);

        public Rigidbody2D Body { get; private set; }
        public Animator Anim { get; private set; }
        public Transform Player { get; private set; }
        
        public float StartX { get; private set; }
        public int Direction { get; set; } = 1; // 1 = right, -1 = left
        public Vector3 OriginalScale { get; private set; }

        private StateMachine stateMachine;
        
        public EnemyPatrolState PatrolState { get; private set; }
        public EnemyWaitState WaitState { get; private set; }
        public EnemyChaseState ChaseState { get; private set; }
        public EnemyAttackState AttackState { get; private set; }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerResourceCreated>(OnPlayerCreated);
            EventBus.Subscribe<PlayerResourceRemoved>(OnPlayerRemoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerResourceCreated>(OnPlayerCreated);
            EventBus.Unsubscribe<PlayerResourceRemoved>(OnPlayerRemoved);
        }

        private void OnPlayerCreated(PlayerResourceCreated evt)
        {
            Player = evt.Resource.transform;
            Debug.Log($"[EnemyController] {gameObject.name} discovered player via EventBus.");
        }

        private void OnPlayerRemoved(PlayerResourceRemoved evt)
        {
            Player = null;
        }

        private void Awake()
        {
            EnsureObstacleMaskScoped();

            Body = GetComponent<Rigidbody2D>();
            Anim = GetComponent<Animator>();
            stateMachine = GetComponent<StateMachine>();

            PatrolState = gameObject.AddComponent<EnemyPatrolState>();
            WaitState = gameObject.AddComponent<EnemyWaitState>();
            ChaseState = gameObject.AddComponent<EnemyChaseState>();
            AttackState = gameObject.AddComponent<EnemyAttackState>();
        }

        private void Start()
        {
            StartX = transform.position.x;
            OriginalScale = transform.localScale;

            stateMachine.Initialize(PatrolState);
        }

        private void Reset()
        {
            EnsureObstacleMaskScoped();
        }

        private void EnsureObstacleMaskScoped()
        {
            // If the mask is significantly customized, don't override it.
            // But if it's "Everything" (-1) or "Nothing" (0), we auto-initialize it.
            if (obstacleMask.value != 0 && obstacleMask.value != -1)
            {
                return;
            }

            int scopedMask = 0;
            AddLayerIfPresent(ref scopedMask, "ground");
            AddLayerIfPresent(ref scopedMask, "Ground");
            AddLayerIfPresent(ref scopedMask, "wall");
            AddLayerIfPresent(ref scopedMask, "Wall");
            AddLayerIfPresent(ref scopedMask, "Obstacle");

            // If we found specific layers, use them. 
            // Otherwise, use Default but attempt to exclude common interactive layers if we are defaulting.
            if (scopedMask != 0)
            {
                obstacleMask = scopedMask;
            }
            else
            {
                // Fallback to Default but try to be safe.
                obstacleMask = LayerMask.GetMask("Default");
            }

            // CRITICAL: Always exclude the player layer to prevent "false stops"
            int playerLayer = LayerMask.NameToLayer("Default"); // Currently player is on Default
            // In a more mature project, we'd check "Player" layer.
            int explicitPlayerLayer = LayerMask.NameToLayer("Player");
            
            if (playerLayer >= 0) obstacleMask &= ~(1 << playerLayer);
            if (explicitPlayerLayer >= 0) obstacleMask &= ~(1 << explicitPlayerLayer);
            
            Debug.Log($"[EnemyController] {gameObject.name} obstacleMask auto-scoped to: {obstacleMask.value}");
        }

        private static void AddLayerIfPresent(ref int mask, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                mask |= 1 << layer;
            }
        }
        
        public void FlipSprite()
        {
            Vector3 scale = OriginalScale;
            scale.x = Mathf.Abs(OriginalScale.x) * Direction;
            transform.localScale = scale;
        }

        public void TakeDamage(int damageAmount)
        {
            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                IWaterReceiver receiver = collision.gameObject.GetComponent<IWaterReceiver>();
                if (receiver != null)
                {
                    // Minimal contact damage even if not in Attack state
                    receiver.TakeWaterDamage(2f);
                    Debug.Log($"[EnemyController] {gameObject.name} contact damage to player.");
                }
            }
        }
    }
}
