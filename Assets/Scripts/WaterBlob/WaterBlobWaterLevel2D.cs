using System;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class WaterBlobWaterLevel2D : MonoBehaviour
    {
        [Header("Water")]
        [Range(0f, 1f)]
        [SerializeField] private float waterLevel = 1f;

        public float WaterLevel => Mathf.Clamp01(waterLevel);
        public event Action<float> WaterLevelChanged;

        public void SetWaterLevel(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, waterLevel))
            {
                return;
            }

            waterLevel = clamped;
            WaterLevelChanged?.Invoke(waterLevel);
        }

        public void ChangeWaterLevel(float delta)
        {
            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            SetWaterLevel(waterLevel + delta);
        }

        public void AddWater(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            ChangeWaterLevel(amount);
        }

        public void RemoveWater(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            ChangeWaterLevel(-amount);
        }

        private void OnValidate()
        {
            waterLevel = Mathf.Clamp01(waterLevel);
            if (Application.isPlaying)
            {
                WaterLevelChanged?.Invoke(waterLevel);
            }
        }
    }
}
