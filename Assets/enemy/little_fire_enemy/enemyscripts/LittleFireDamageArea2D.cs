using System.Collections.Generic;
using UnityEngine;
using WaterBlob;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class LittleFireDamageArea2D : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Manual damage amount applied each tick while the player is inside.")]
    [SerializeField, Min(0.01f)] private float damageAmount = 20f;
    [SerializeField, Min(0.01f)] private float damageInterval = 0.45f;
    [SerializeField] private bool damageImmediatelyOnEnter = true;

    [Header("Lifetime")]
    [SerializeField, Min(0f)] private float lifetime = 2.5f;

    [Header("Target Filter")]
    [SerializeField] private bool requirePlayerTag = true;
    [SerializeField] private string playerTag = "Player";

    private readonly Dictionary<OneDropWaterResource2D, float> nextDamageTimes = new();

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void Start()
    {
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }

    private void OnDisable()
    {
        nextDamageTimes.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other, forceImmediate: damageImmediatelyOnEnter);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other, forceImmediate: false);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        OneDropWaterResource2D resource = ResolveResource(other);
        if (resource != null)
        {
            nextDamageTimes.Remove(resource);
        }
    }

    public void Configure(
        float configuredDamageAmount,
        float configuredDamageInterval,
        float configuredLifetime,
        bool configuredRequirePlayerTag,
        string configuredPlayerTag,
        bool configuredDamageImmediatelyOnEnter)
    {
        damageAmount = Mathf.Max(0.01f, configuredDamageAmount);
        damageInterval = Mathf.Max(0.01f, configuredDamageInterval);
        lifetime = Mathf.Max(0f, configuredLifetime);
        requirePlayerTag = configuredRequirePlayerTag;
        playerTag = string.IsNullOrWhiteSpace(configuredPlayerTag) ? "Player" : configuredPlayerTag;
        damageImmediatelyOnEnter = configuredDamageImmediatelyOnEnter;
    }

    private void TryDamage(Collider2D other, bool forceImmediate)
    {
        OneDropWaterResource2D resource = ResolveResource(other);
        if (resource == null || !PassesTagFilter(resource))
        {
            return;
        }

        if (!nextDamageTimes.TryGetValue(resource, out float nextDamageTime))
        {
            if (!forceImmediate)
            {
                nextDamageTimes[resource] = Time.time + damageInterval;
                return;
            }

            nextDamageTime = 0f;
        }

        if (!forceImmediate && Time.time < nextDamageTime)
        {
            return;
        }

        resource.ConsumeDamage(damageAmount);
        nextDamageTimes[resource] = Time.time + damageInterval;
    }

    private bool PassesTagFilter(OneDropWaterResource2D resource)
    {
        if (!requirePlayerTag)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(playerTag))
        {
            return false;
        }

        return resource.CompareTag(playerTag);
    }

    private static OneDropWaterResource2D ResolveResource(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        OneDropWaterResource2D resource = other.GetComponentInParent<OneDropWaterResource2D>();
        if (resource != null)
        {
            return resource;
        }

        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.GetComponentInParent<OneDropWaterResource2D>();
        }

        return null;
    }

    private void EnsureTriggerCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
}
