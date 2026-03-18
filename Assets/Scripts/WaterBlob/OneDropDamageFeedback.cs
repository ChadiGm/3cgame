using System.Collections;
using UnityEngine;

namespace WaterBlob
{
    /// <summary>
    /// Simple hit feedback: quick shrink + wobble then restore.
    /// </summary>
    public class OneDropDamageFeedback : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 1f)] private float shrinkScale = 0.85f;
        [SerializeField, Range(0.05f, 0.5f)] private float duration = 0.18f;
        [SerializeField, Range(0f, 0.2f)] private float shakeOffset = 0.06f;
        [SerializeField, Range(1f, 40f)] private float shakeFrequency = 18f;

        private Coroutine routine;
        private Vector3 baseScale;
        private Vector3 basePos;
        private WaterBlobMeshRenderer2D blobRenderer;

        private void Awake()
        {
            baseScale = transform.localScale;
            basePos = transform.localPosition;
            blobRenderer = GetComponent<WaterBlobMeshRenderer2D>();
            if (blobRenderer == null)
            {
                blobRenderer = GetComponentInChildren<WaterBlobMeshRenderer2D>();
            }
        }

        public void Play()
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(DoFeedback());
        }

        private IEnumerator DoFeedback()
        {
            baseScale = transform.localScale;
            basePos = transform.localPosition;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float norm = Mathf.Clamp01(t / duration);
                float scale = Mathf.Lerp(1f, shrinkScale, 1f - Mathf.Cos(norm * Mathf.PI));
                float shake = Mathf.Sin(norm * Mathf.PI * shakeFrequency) * shakeOffset * (1f - norm);

                if (blobRenderer != null)
                {
                    blobRenderer.SetRuntimeVisual(scale, new Vector2(shake, 0f));
                }
                else
                {
                    transform.localScale = baseScale * scale;
                    transform.localPosition = basePos + new Vector3(shake, 0f, 0f);
                }
                yield return null;
            }

            if (blobRenderer != null)
            {
                blobRenderer.ResetRuntimeVisual();
            }
            else
            {
                transform.localScale = baseScale;
                transform.localPosition = basePos;
            }
            routine = null;
        }
    }
}
