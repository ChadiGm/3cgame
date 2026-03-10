using UnityEngine;

namespace WaterBlob.Enemies
{
    public class EnemyPatrolState : AI.State
    {
        private EnemyController enemy;
        private Transform player;

        public override void Enter(AI.StateMachine fsm)
        {
            base.Enter(fsm);
            if (enemy == null) enemy = GetComponent<EnemyController>();
            
            enemy.FlipSprite();
            enemy.Anim.SetBool("run", true);

            player = enemy.Player;
        }

        private bool CheckObstacle(Vector2 start, Vector2 dir, float dist)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(start, dir, dist, enemy.obstacleMask);
            foreach (var hit in hits)
            {
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != enemy.gameObject && !hit.collider.transform.IsChildOf(enemy.transform))
                {
                    Debug.Log($"[EnemyPatrol] {enemy.name} detected obstacle: {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    return true;
                }
            }
            return false;
        }

        public override void PhysicsUpdate()
        {
            // Check for player detection
            if (player != null)
            {
                float distance = Vector2.Distance(enemy.Body.position, player.position);
                if (distance <= enemy.detectionRange && HasLOS())
                {
                    fsm.ChangeState(enemy.ChaseState);
                    return;
                }
            }

            // Detect walls and ledges early so the enemy doesn't get stuck moving into geometry.
            Vector2 origin = enemy.Body.position;
            Vector2 forward = Vector2.right * enemy.Direction;

            // Wall check (start from center, cast further out to account for enemy width)
            float adjustedWallCheckDist = enemy.wallCheckDistance + Mathf.Abs(enemy.groundCheckOffset.x);
            bool wallAhead = CheckObstacle(origin, forward, adjustedWallCheckDist);

            // Edge check (raycast slightly ahead and down to see if there is ground coming up)
            Vector2 groundCheckOrigin = origin + new Vector2(enemy.groundCheckOffset.x * enemy.Direction, enemy.groundCheckOffset.y);
            bool groundAhead = CheckObstacle(groundCheckOrigin, Vector2.down, enemy.edgeCheckDistance);

            // Debug drawing (editor only)
            Debug.DrawRay(origin, forward * adjustedWallCheckDist, wallAhead ? Color.red : Color.green);
            Debug.DrawRay(groundCheckOrigin, Vector2.down * enemy.edgeCheckDistance, groundAhead ? Color.green : Color.red);

            if (wallAhead || !groundAhead)
            {
                fsm.ChangeState(enemy.WaitState);
                return;
            }

            // Use Rigidbody2D.velocity for consistent physics movement.
            enemy.Body.linearVelocity = new Vector2(enemy.Direction * enemy.speed, enemy.Body.linearVelocity.y);

            float distFromStart = enemy.Body.position.x - enemy.StartX;
            if ((enemy.Direction > 0 && distFromStart >= enemy.patrolDistance) ||
                (enemy.Direction < 0 && distFromStart <= -enemy.patrolDistance))
            {
                fsm.ChangeState(enemy.WaitState);
            }
        }

        private bool HasLOS()
        {
            if (player == null) return false;
            
            Vector2 dir = (player.position - enemy.transform.position).normalized;
            float dist = Vector2.Distance(enemy.transform.position, player.position);
            
            RaycastHit2D hit = Physics2D.Raycast(enemy.transform.position, dir, dist, enemy.obstacleMask);
            
            // If we hit nothing, the path is clear.
            return hit.collider == null;
        }
    }
}
