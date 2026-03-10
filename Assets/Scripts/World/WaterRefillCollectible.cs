using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class WaterRefillCollectible : MonoBehaviour
    {
        [Header("Refill")]
        [SerializeField, Min(0.01f)] private float refillAmount = 25f;
        [SerializeField] private bool fullRefill;
        [SerializeField] private bool destroyOnCollect = true;

        [Header("Idle Motion")]
        [SerializeField, Min(0f)] private float rotateSpeed = 90f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float bobFrequency = 2f;

        private Vector3 startLocalPosition;
        private bool collected;

        private void Awake()
        {
            startLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            if (rotateSpeed > 0f)
            {
                transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime, Space.Self);
            }

            if (bobAmplitude > 0f && bobFrequency > 0f)
            {
                Vector3 pos = startLocalPosition;
                pos.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                transform.localPosition = pos;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other != null ? other.gameObject : null);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryCollect(collision != null ? collision.gameObject : null);
        }

        private void TryCollect(GameObject target)
        {
            if (collected || target == null)
            {
                return;
            }

            IWaterReceiver water = target.GetComponent<IWaterReceiver>();
            if (water == null)
            {
                return;
            }

            bool changed = water.ReceiveWater(fullRefill ? -1f : refillAmount);
            if (!changed)
            {
                return;
            }

            collected = true;
            Debug.Log($"[WaterRefillCollectible] Refilled '{target.name}' to {water.Current:F1}/{water.Max:F1}.");

            if (destroyOnCollect)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

    }
}
