using UnityEngine;

namespace WaterBlob
{
    /// <summary>
    /// Proxy component to be placed on child GameObjects (like softbody points) 
    /// to redirect water interactions to a central receiver.
    /// </summary>
    public class WaterInteractionProxy : MonoBehaviour, IWaterReceiver
    {
        [SerializeField] private GameObject receiverObject;
        private IWaterReceiver cachedReceiver;

        public float Current => GetReceiver()?.Current ?? 0f;
        public float Max => GetReceiver()?.Max ?? 0f;
        public float NormalizedAmount => GetReceiver()?.NormalizedAmount ?? 0f;
        public bool IsInvulnerable => GetReceiver()?.IsInvulnerable ?? false;

        private IWaterReceiver GetReceiver()
        {
            if (cachedReceiver != null) return cachedReceiver;
            
            if (receiverObject == null)
            {
                // Fallback: look in parents if not assigned
                cachedReceiver = GetComponentInParent<IWaterReceiver>();
            }
            else
            {
                cachedReceiver = receiverObject.GetComponent<IWaterReceiver>();
            }
            
            return cachedReceiver;
        }

        public bool ReceiveWater(float amount) => GetReceiver()?.ReceiveWater(amount) ?? false;
        
        public void TakeWaterDamage(float amount, bool bypassInvulnerability = false) 
            => GetReceiver()?.TakeWaterDamage(amount, bypassInvulnerability);

        public void SetReceiver(IWaterReceiver receiver)
        {
            cachedReceiver = receiver;
        }
    }
}
