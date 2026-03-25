using System.Collections.Generic;
using UnityEngine;

namespace WaterBlob
{
    public interface IPlayerRespawnResettable
    {
        void ResetForPlayerRespawn();
    }

    [DisallowMultipleComponent]
    public class ResetOnPlayerRespawn2D : MonoBehaviour
    {
        [Header("Reset")]
        [SerializeField] private bool reactivateOnRespawn = true;
        [SerializeField] private bool resetTransform = true;
        [SerializeField] private bool resetRigidbodies = true;
        [SerializeField] private bool callCustomResetHandlers = true;

        private static readonly HashSet<ResetOnPlayerRespawn2D> Registered = new();
        private static readonly List<ResetOnPlayerRespawn2D> Snapshot = new();

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale;
        private Transform initialParent;

        private Rigidbody2D rb2D;
        private RigidbodyType2D initialBodyType2D;
        private float initialGravityScale2D;
        private bool initialSimulated2D;
        private RigidbodyConstraints2D initialConstraints2D;

        private Rigidbody rb3D;
        private bool initialIsKinematic3D;
        private bool initialUseGravity3D;
        private bool initialDetectCollisions3D;
        private RigidbodyConstraints initialConstraints3D;

        private IPlayerRespawnResettable[] customResetHandlers;

        private void Awake()
        {
            CacheInitialState();
            Registered.Add(this);
        }

        private void OnDestroy()
        {
            Registered.Remove(this);
        }

        public void Despawn()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public static void ResetAll()
        {
            if (Registered.Count == 0)
            {
                return;
            }

            Snapshot.Clear();
            foreach (ResetOnPlayerRespawn2D entry in Registered)
            {
                if (entry != null)
                {
                    Snapshot.Add(entry);
                }
            }

            for (int i = 0; i < Snapshot.Count; i++)
            {
                Snapshot[i].ResetInstanceState();
            }

            Snapshot.Clear();
        }

        private void CacheInitialState()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialScale = transform.localScale;
            initialParent = transform.parent;

            rb2D = GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                initialBodyType2D = rb2D.bodyType;
                initialGravityScale2D = rb2D.gravityScale;
                initialSimulated2D = rb2D.simulated;
                initialConstraints2D = rb2D.constraints;
            }

            rb3D = GetComponent<Rigidbody>();
            if (rb3D != null)
            {
                initialIsKinematic3D = rb3D.isKinematic;
                initialUseGravity3D = rb3D.useGravity;
                initialDetectCollisions3D = rb3D.detectCollisions;
                initialConstraints3D = rb3D.constraints;
            }

            customResetHandlers = GetComponents<IPlayerRespawnResettable>();
        }

        private void ResetInstanceState()
        {
            if (reactivateOnRespawn && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (initialParent != null && transform.parent != initialParent)
            {
                transform.SetParent(initialParent, worldPositionStays: true);
            }

            if (resetTransform)
            {
                transform.SetPositionAndRotation(initialPosition, initialRotation);
                transform.localScale = initialScale;
            }

            if (resetRigidbodies)
            {
                ResetRigidbody2DState();
                ResetRigidbody3DState();
            }

            if (!callCustomResetHandlers || customResetHandlers == null)
            {
                return;
            }

            for (int i = 0; i < customResetHandlers.Length; i++)
            {
                IPlayerRespawnResettable handler = customResetHandlers[i];
                if (handler != null)
                {
                    handler.ResetForPlayerRespawn();
                }
            }
        }

        private void ResetRigidbody2DState()
        {
            if (rb2D == null)
            {
                return;
            }

            rb2D.bodyType = initialBodyType2D;
            rb2D.gravityScale = initialGravityScale2D;
            rb2D.constraints = initialConstraints2D;
            rb2D.simulated = initialSimulated2D;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }

        private void ResetRigidbody3DState()
        {
            if (rb3D == null)
            {
                return;
            }

            rb3D.isKinematic = initialIsKinematic3D;
            rb3D.useGravity = initialUseGravity3D;
            rb3D.detectCollisions = initialDetectCollisions3D;
            rb3D.constraints = initialConstraints3D;
            rb3D.linearVelocity = Vector3.zero;
            rb3D.angularVelocity = Vector3.zero;
        }
    }
}
