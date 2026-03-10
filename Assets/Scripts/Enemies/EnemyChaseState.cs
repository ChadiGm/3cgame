using UnityEngine;

namespace WaterBlob.Enemies
{
    public class EnemyChaseState : AI.State
    {
        [Header("Chase Settings")]
        [SerializeField] private float aggroRetentionTime = 2.0f;
        
        private EnemyController enemy;
        private Transform player;
        private float aggroLossTimer;

        public override void Enter(AI.StateMachine fsm)
        {
            base.Enter(fsm);
            if (enemy == null) enemy = GetComponent<EnemyController>();
            
            player = enemy.Player;
            aggroLossTimer = 0f;

            enemy.Anim.SetBool("run", true);
        }

        public override void PhysicsUpdate()
        {
            if (player == null)
            {
                fsm.ChangeState(enemy.PatrolState);
                return;
            }

            float distance = Vector2.Distance(enemy.Body.position, player.position);
            bool hasLOS = HasLOS();

            if (distance > enemy.detectionRange * 1.5f) // Lose player if too far
            {
                fsm.ChangeState(enemy.WaitState);
                return;
            }

            if (!hasLOS)
            {
                aggroLossTimer += Time.deltaTime;
                if (aggroLossTimer >= aggroRetentionTime)
                {
                    Debug.Log($"[EnemyChaseState] {gameObject.name} lost LOS and aggro expired.");
                    fsm.ChangeState(enemy.WaitState);
                    return;
                }
            }
            else
            {
                aggroLossTimer = 0f;
            }

            if (distance <= enemy.attackRange && hasLOS)
            {
                fsm.ChangeState(enemy.AttackState);
                return;
            }

            // Move towards player
            int targetDir = player.position.x > enemy.transform.position.x ? 1 : -1;
            
            if (targetDir != enemy.Direction)
            {
                enemy.Direction = targetDir;
                enemy.FlipSprite();
            }

            enemy.Body.linearVelocity = new Vector2(enemy.Direction * enemy.speed * 1.5f, enemy.Body.linearVelocity.y);
        }

        private bool HasLOS()
        {
            if (player == null) return false;
            
            Vector2 dir = (Vector2)player.position - (Vector2)enemy.transform.position;
            float dist = dir.magnitude;
            
            RaycastHit2D hit = Physics2D.Raycast(enemy.transform.position, dir.normalized, dist, enemy.obstacleMask);
            
            return hit.collider == null;
        }
    }
}
