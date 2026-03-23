using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint2D : MonoBehaviour
    {
        [SerializeField] private bool isDefaultSpawn = false;
        [SerializeField] private bool forceTrigger = true;
        [SerializeField] private bool activateOnce = true;
        [SerializeField] private bool requirePlayerController = true;
        [SerializeField] private string playerTag = "Player";

        private bool activated;
        private bool registeredDefault;

        private void Awake()
        {
            TryRegisterDefault();
        }

        private void OnEnable()
        {
            TryRegisterDefault();
            EnsureTrigger();
        }

        private void Start()
        {
            TryRegisterDefault();
            EnsureTrigger();
        }

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryActivate(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryActivate(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision != null)
            {
                TryActivate(collision.collider);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision != null)
            {
                TryActivate(collision.collider);
            }
        }

        private void EnsureTrigger()
        {
            if (!forceTrigger)
            {
                return;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void TryRegisterDefault()
        {
            if (!isDefaultSpawn || registeredDefault)
            {
                return;
            }

            CheckpointManager2D manager = CheckpointManager2D.EnsureInstance();
            if (manager == null)
            {
                return;
            }

            manager.SetDefaultSpawnPoint(transform);
            registeredDefault = true;
        }

        private void TryActivate(Collider2D other)
        {
            TryRegisterDefault();
            if (activated && activateOnce)
            {
                return;
            }

            if (!IsPlayer(other))
            {
                return;
            }

            CheckpointManager2D manager = CheckpointManager2D.Instance;
            if (manager == null)
            {
                return;
            }

            manager.ActivateCheckpoint(transform);
            activated = true;
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            {
                return true;
            }

            if (requirePlayerController)
            {
                if (other.GetComponentInParent<OneDropController2D>() != null)
                {
                    return true;
                }

                return IsLinkedToPlayerBlobPoint(other);
            }

            if (other.GetComponentInParent<OneDropWaterResource2D>() != null)
            {
                return true;
            }

            return IsLinkedToPlayerBlobPoint(other);
        }

        private static bool IsLinkedToPlayerBlobPoint(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null)
            {
                return false;
            }

            SpringJoint2D[] joints = body.GetComponents<SpringJoint2D>();
            for (int i = 0; i < joints.Length; i++)
            {
                SpringJoint2D joint = joints[i];
                if (joint == null || joint.connectedBody == null)
                {
                    continue;
                }

                if (joint.connectedBody.GetComponent<OneDropController2D>() != null)
                {
                    return true;
                }

                if (joint.connectedBody.GetComponent<OneDropWaterResource2D>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = activated ? new Color(0.2f, 1f, 0.4f, 0.85f) : new Color(1f, 0.8f, 0.2f, 0.85f);
            Gizmos.DrawSphere(transform.position, 0.25f);
        }
    }
}
