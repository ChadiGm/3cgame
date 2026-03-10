using UnityEngine;

namespace WaterBlob.Enemies
{
    public class EnemyAttackState : AI.State
    {
        [Header("Attack Settings")]
        [SerializeField] private float damagePerAttack = 10f;
        [SerializeField] private float attackCooldown = 1.0f;
        
        private EnemyController enemy;
        private Transform player;
        private float nextAttackTime;

        public override void Enter(AI.StateMachine fsm)
        {
            base.Enter(fsm);
            if (enemy == null) enemy = GetComponent<EnemyController>();
            
            player = enemy.Player;

            enemy.Body.linearVelocity = new Vector2(0, enemy.Body.linearVelocity.y);
            enemy.Anim.SetBool("run", false);
        }

        public override void PhysicsUpdate()
        {
            if (player == null)
            {
                fsm.ChangeState(enemy.PatrolState);
                return;
            }

            float distance = Vector2.Distance(enemy.Body.position, player.position);

            if (distance > enemy.attackRange * 1.2f)
            {
                fsm.ChangeState(enemy.ChaseState);
                return;
            }

            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        private void Attack()
        {
            if (player == null) return;

            IWaterReceiver receiver = player.GetComponent<IWaterReceiver>();
            if (receiver != null)
            {
                receiver.TakeWaterDamage(damagePerAttack);
                Debug.Log($"[EnemyAttackState] {gameObject.name} bit the player! Damage: {damagePerAttack}");
                
                // Trigger attack animation if exists
                enemy.Anim.SetTrigger("attack");
            }
        }
    }
}
