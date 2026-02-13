using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 1.2f, -10f);
        [SerializeField, Min(0f)] private float smoothTime = 0.13f;
        [SerializeField] private bool clampX = true;
        [SerializeField] private bool clampY = false;
        [SerializeField] private Vector2 xLimits = new(-13f, 34f);
        [SerializeField] private Vector2 yLimits = new(-3.5f, 8f);

        private Vector3 velocity;

        private void Awake()
        {
            if (target == null)
            {
                GameObject player = GameObject.Find("WaterBlobPlayer");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            if (clampX)
            {
                desired.x = Mathf.Clamp(desired.x, xLimits.x, xLimits.y);
            }

            if (clampY)
            {
                desired.y = Mathf.Clamp(desired.y, yLimits.x, yLimits.y);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
