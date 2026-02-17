using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class RotatingObstacle2D : MonoBehaviour
    {
        [SerializeField] private float rotationSpeedDeg = 180f;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool usePhysicsRotation = true;

        private Rigidbody2D cachedBody;

        private void Awake()
        {
            cachedBody = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (!usePhysicsRotation || cachedBody == null)
            {
                return;
            }

            float dt = useUnscaledTime ? Time.fixedUnscaledDeltaTime : Time.fixedDeltaTime;
            float delta = rotationSpeedDeg * dt;
            cachedBody.MoveRotation(cachedBody.rotation + delta);
        }

        private void Update()
        {
            if (usePhysicsRotation && cachedBody != null)
            {
                return;
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(0f, 0f, rotationSpeedDeg * dt, Space.Self);
        }
    }
}
