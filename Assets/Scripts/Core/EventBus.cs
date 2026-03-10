using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaterBlob
{
    /// <summary>
    /// A simple global event bus for decoupling disparate systems.
    /// Prefers the "Event-driven broadcasts" pattern from Architecture.md.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }
            _subscribers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            if (_subscribers.ContainsKey(type))
            {
                _subscribers[type].Remove(handler);
            }
        }

        public static void Publish<T>(T eventData)
        {
            Type type = typeof(T);
            if (_subscribers.TryGetValue(type, out var handlers))
            {
                // Copy list to avoid issues if handlers modify the subscription during iteration
                var handlersCopy = new List<Delegate>(handlers);
                foreach (var handler in handlersCopy)
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
            }
        }
    }

    /// <summary>
    /// Event fired when a WaterResource (player) is initialized and ready.
    /// </summary>
    public struct PlayerResourceCreated
    {
        public OneDropWaterResource2D Resource;
    }

    /// <summary>
    /// Event fired when a WaterResource (player) is being disabled or destroyed.
    /// </summary>
    public struct PlayerResourceRemoved
    {
        public OneDropWaterResource2D Resource;
    }

    /// <summary>
    /// Supported audio event types for decoupling.
    /// </summary>
    public enum AudioEventType
    {
        Shoot,
        ChargeShoot,
        Damage,
        Jump,
        Death
    }

    /// <summary>
    /// Event used to trigger SFX without direct AudioManager reference.
    /// </summary>
    public struct AudioTriggerEvent
    {
        public AudioEventType EventType;

        public AudioTriggerEvent(AudioEventType type)
        {
            EventType = type;
        }
    }
}
