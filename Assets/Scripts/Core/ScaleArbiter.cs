using System.Collections.Generic;
using UnityEngine;
using WaterBlob;

namespace Core
{
    public interface IScaleModifier
    {
        string SourceId { get; }
        int Priority { get; }
        float Multiplier { get; }
        bool IsActive { get; }
    }

    /// <summary>
    /// Arbitrates between multiple scale sources (Resource, PowerUps, Hazards).
    /// Decouples physical size from any single game-loop logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScaleArbiter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float baseRadius = 1.0f;
        [SerializeField] private float minRadius = 0.5f;
        [SerializeField] private float smoothSpeed = 10f;

        private OneDropWaterResource2D resource;
        private float currentTargetRadius;
        private float resourceTargetRadius;

        public float CurrentTargetRadius => currentTargetRadius;

        private void Awake()
        {
            resource = GetComponent<OneDropWaterResource2D>();
            currentTargetRadius = baseRadius;
            resourceTargetRadius = baseRadius;
        }

        private void Start()
        {
            if (resource != null)
            {
                // Initial sync
                UpdateResourceRadius(resource.Current, resource.maxAmount);
                resource.OnWaterChanged += UpdateResourceRadius;
            }
        }

        private void OnDestroy()
        {
            if (resource != null)
            {
                resource.OnWaterChanged -= UpdateResourceRadius;
            }
        }

        private void UpdateResourceRadius(float current, float max)
        {
            float t = current / Mathf.Max(0.01f, max);
            // We still derive the 'standard' size from water, but now it's just one input.
            resourceTargetRadius = Mathf.Lerp(minRadius, baseRadius, t);
        }

        private void Update()
        {
            // Calculate final arbitration
            // For now, we just use the resource as the base. 
            // Future ARCH tasks can add a stack of modifiers here.
            float finalTarget = resourceTargetRadius;

            // Apply modifiers (placeholder for stack logic)
            // foreach(var mod in modifiers) finalTarget *= mod.Multiplier;

            currentTargetRadius = Mathf.Lerp(currentTargetRadius, finalTarget, Time.deltaTime * smoothSpeed);
        }

        // External API for PowerUps/Hazards
        public void SetBaseSettings(float baseR, float minR)
        {
            baseRadius = baseR;
            minRadius = minR;
        }
    }
}
