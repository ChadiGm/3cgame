using UnityEngine;

namespace WaterBlob.Enemies
{
    public class EnemyWaitState : AI.State
    {
        private EnemyController enemy;
        private Transform player;
        private float timer;

        public override void Enter(AI.StateMachine fsm)
        {
            base.Enter(fsm);
            if (enemy == null) enemy = GetComponent<EnemyController>();
            
            // Halt horizontal movement while waiting.
            enemy.Body.linearVelocity = new Vector2(0f, enemy.Body.linearVelocity.y);
            enemy.Anim.SetBool("run", false);
            timer = enemy.waitTime;

            player = enemy.Player;
        }

        public override void LogicUpdate()
        {
            if (player != null)
            {
                float distance = Vector2.Distance(enemy.Body.position, player.position);
                if (distance <= enemy.detectionRange)
                {
                    fsm.ChangeState(enemy.ChaseState);
                    return;
                }
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                enemy.Direction *= -1;
                fsm.ChangeState(enemy.PatrolState);
            }
        }
    }
}
