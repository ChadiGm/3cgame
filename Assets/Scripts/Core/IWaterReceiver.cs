using UnityEngine;

namespace WaterBlob
{
    /// <summary>
    /// Interface for any object that can hold, receive, or lose water.
    /// Decouples world interactions from specific component hierarchies.
    /// </summary>
    public interface IWaterReceiver
    {
        float Current { get; }
        float Max { get; }
        float NormalizedAmount { get; }
        bool IsInvulnerable { get; }

        /// <summary>
        /// Adds water to the receiver.
        /// </summary>
        /// <param name="amount">Amount to add.</param>
        /// <returns>True if the amount changed.</returns>
        bool ReceiveWater(float amount);

        /// <summary>
        /// Removes water from the receiver.
        /// </summary>
        /// <param name="amount">Amount to remove.</param>
        /// <param name="bypassInvulnerability">If true, damage is applied even if the receiver is invulnerable.</param>
        void TakeWaterDamage(float amount, bool bypassInvulnerability = false);
    }
}
