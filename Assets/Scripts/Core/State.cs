using UnityEngine;

namespace WaterBlob.AI
{
    public abstract class State : MonoBehaviour
    {
        protected StateMachine fsm;
        
        public virtual void Enter(StateMachine fsm)
        {
            this.fsm = fsm;
        }

        public virtual void LogicUpdate() { }
        public virtual void PhysicsUpdate() { }
        public virtual void Exit() { }
    }
}
