using UnityEngine;

namespace WaterBlob
{
    public enum YAxisFollowTriggerAction
    {
        StartFollow,
        StopFollow
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class YAxisFollowTrigger2D : MonoBehaviour
    {
        [SerializeField] private YAxisFollower2D follower;
        [SerializeField] private YAxisFollowTriggerAction action = YAxisFollowTriggerAction.StartFollow;
        [SerializeField] private bool requireTag = false;
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private bool debugLogs = false;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(requiredTag))
            {
                requiredTag = "Player";
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        private void Awake()
        {
            if (follower == null)
            {
                follower = FindObjectOfType<YAxisFollower2D>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTrigger2D(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleTrigger2D(other);
        }

        private void HandleTrigger2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (!IsPlayer(other))
            {
                return;
            }

            if (follower == null)
            {
                follower = FindObjectOfType<YAxisFollower2D>();
            }

            if (follower == null)
            {
                if (debugLogs)
                {
                    Debug.LogWarning("YAxisFollowTrigger2D: follower missing.", this);
                }
                return;
            }

            Transform target = ResolveFollowTarget(other);
            if (action == YAxisFollowTriggerAction.StartFollow)
            {
                if (!follower.IsFollowing)
                {
                    follower.BeginFollow(target);
                    if (debugLogs)
                    {
                        Debug.Log($"YAxisFollowTrigger2D: StartFollow -> {target?.name}", this);
                    }
                }
            }
            else
            {
                if (follower.IsFollowing)
                {
                    follower.StopFollowAndReturn();
                    if (debugLogs)
                    {
                        Debug.Log("YAxisFollowTrigger2D: StopFollow", this);
                    }
                }
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            if (requireTag && !other.CompareTag(requiredTag))
            {
                return false;
            }

            if (other.GetComponentInParent<OneDropController2D>() != null)
            {
                return true;
            }

            if (other.GetComponentInParent<OneDropWaterResource2D>() != null)
            {
                return true;
            }

            if (other.GetComponentInParent<WaterBlobCharacter2D>() != null)
            {
                return true;
            }

            return false;
        }

        private Transform ResolveFollowTarget(Collider2D other)
        {
            OneDropController2D controller = other.GetComponentInParent<OneDropController2D>();
            if (controller != null)
            {
                return controller.transform;
            }

            OneDropWaterResource2D water = other.GetComponentInParent<OneDropWaterResource2D>();
            if (water != null)
            {
                return water.transform;
            }

            WaterBlobCharacter2D blob = other.GetComponentInParent<WaterBlobCharacter2D>();
            if (blob != null)
            {
                return blob.transform;
            }

            if (other.attachedRigidbody != null)
            {
                return other.attachedRigidbody.transform;
            }

            return other.transform;
        }
    }
}
