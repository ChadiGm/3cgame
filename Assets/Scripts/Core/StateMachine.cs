using UnityEngine;

namespace WaterBlob.AI
{
    public class StateMachine : MonoBehaviour
    {
        public State CurrentState { get; private set; }

        public void Initialize(State startingState)
        {
            if (startingState == null) return;
            CurrentState = startingState;
            CurrentState.Enter(this);
        }

        public void ChangeState(State newState)
        {
            if (CurrentState != null)
            {
                CurrentState.Exit();
            }

            CurrentState = newState;

            if (CurrentState != null)
            {
                CurrentState.Enter(this);
            }
        }

        private void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.LogicUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (CurrentState != null)
            {
                CurrentState.PhysicsUpdate();
            }
        }
    }
}
