using UnityEngine;

namespace WaterBlob
{
    /// <summary>
    /// Simple container for the player's "water" resource used by the 2D character.
    /// The class exists mainly to satisfy dependencies in <see cref="OneDropController2D"/>
    /// and <see cref="PlayerCombatController"/>. Current implementation is minimal and
    /// can be expanded later when balance values are available.
    /// </summary>
    [DisallowMultipleComponent]
    public class OneDropWaterResource2D : MonoBehaviour, IWaterReceiver
    {
        [Header("Resource Settings")]
        [Min(0f)] public float maxAmount = 100f;
        [Min(0f)] public float shootCost = 5f;
        [Min(0f)] public float chargeShootCost = 15f;
        [Min(0f)] public float damageCost = 15f;
        [Min(0f)] public float moveCostPerSecond = 1.5f;
        [Min(0f)] public float slideCost = 10f;
        [Min(0f)] public float jumpCost = 8f;

        [Header("Survival")]
        [SerializeField, Min(0f)] private float invulnerabilityTime = 0.8f;

        // Events
        public event System.Action<float, float> OnWaterChanged; // (current, max)
        public event System.Action OnDied;
        public event System.Action OnTakeDamage;

        private float currentAmount;
        private float invulnerabilityTimer;

        public float Current => currentAmount;
        public float Max => maxAmount;
        public float NormalizedAmount => Mathf.Clamp01(currentAmount / Mathf.Max(0.01f, maxAmount));
        public bool IsInvulnerable => invulnerabilityTimer > 0f;
        public bool IsFull => currentAmount >= maxAmount - 0.001f;

        private void Awake()
        {
            currentAmount = maxAmount;
        }

        private void OnEnable()
        {
            EventBus.Publish(new PlayerResourceCreated { Resource = this });
        }

        private void OnDisable()
        {
            EventBus.Publish(new PlayerResourceRemoved { Resource = this });
        }



        private void Update()
        {
            if (invulnerabilityTimer > 0f)
            {
                invulnerabilityTimer -= Time.deltaTime;
            }
        }

        public void TakeWaterDamage(float amount, bool bypassInvulnerability = false)
        {
            TakeDamage(amount, bypassInvulnerability);
        }

        public bool ReceiveWater(float amount)
        {
            return TryRefill(amount);
        }

        /// <summary>
        /// Unified damage method. Includes invulnerability frames by default.
        /// </summary>
        public void TakeDamage(float amount, bool ignoreInvulnerability = false)
        {
            if (amount <= 0 || currentAmount <= 0) return;
            if (!ignoreInvulnerability && IsInvulnerable) return;

            if (!ignoreInvulnerability)
            {
                invulnerabilityTimer = invulnerabilityTime;
            }
            
            currentAmount = Mathf.Max(0f, currentAmount - amount);
            
            if (!ignoreInvulnerability) 
            {
                EventBus.Publish(new AudioTriggerEvent(AudioEventType.Damage));
            }

            OnTakeDamage?.Invoke();
            NotifyChange(ignoreInvulnerability ? "Hazard Damage" : "Taken Damage");

            if (currentAmount <= 0.001f)
            {
                EventBus.Publish(new AudioTriggerEvent(AudioEventType.Death));
                OnDied?.Invoke();
            }
        }

        /// <summary>
        /// Legacy support for existing scripts, now calls TakeDamage with default value.
        /// </summary>
        public void ConsumeDamage()
        {
            TakeDamage(damageCost);
        }

        public bool ConsumeShoot()
        {
            if (currentAmount >= shootCost)
            {
                currentAmount -= shootCost;
                NotifyChange("Shot Fired");
                return true;
            }
            return false;
        }

        public bool ConsumeChargeShoot()
        {
            if (currentAmount >= chargeShootCost)
            {
                currentAmount -= chargeShootCost;
                NotifyChange("Charged Shot Fired");
                return true;
            }
            return false;
        }

        public void ConsumeMove(float deltaTime, bool isMoving)
        {
            if (!isMoving || deltaTime <= 0f) return;
            float prevAmount = currentAmount;
            currentAmount = Mathf.Max(0f, currentAmount - moveCostPerSecond * deltaTime);
            
            if (!Mathf.Approximately(prevAmount, currentAmount))
            {
                NotifyChange("Moving");
            }
        }

        public void ConsumeSlide()
        {
            currentAmount = Mathf.Max(0f, currentAmount - slideCost);
            NotifyChange("Sliding");
        }

        public void ConsumeJump()
        {
            currentAmount = Mathf.Max(0f, currentAmount - jumpCost);
            NotifyChange("Jumping");
        }

        public bool TryRefill(float amount = -1f)
        {
            float previousAmount = currentAmount;
            if (amount < 0f)
            {
                currentAmount = maxAmount;
            }
            else
            {
                if (amount <= 0f)
                {
                    return false;
                }

                currentAmount = Mathf.Min(maxAmount, currentAmount + amount);
            }

            if (Mathf.Approximately(previousAmount, currentAmount))
            {
                return false;
            }

            NotifyChange("Refilled");
            return true;
        }

        public void Refill(float amount = -1f)
        {
            TryRefill(amount);
        }

        private void NotifyChange(string reason)
        {
            Debug.Log($"[WaterResource] {reason}. Current: {currentAmount:F1}/{maxAmount:F1} ({NormalizedAmount:P0})");
            OnWaterChanged?.Invoke(currentAmount, maxAmount);
        }

        public void SetInvulnerable(float time)
        {
            invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, time);
        }
    }
}
