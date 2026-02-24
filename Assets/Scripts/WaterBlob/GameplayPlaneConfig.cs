using System;
using UnityEngine;

namespace WaterBlob
{
    public enum GameplayPlaneSyncMode
    {
        HardSnap,
        Damped
    }

    [Serializable]
    public struct GameplayPlaneConfig
    {
        public float planeZ;
        public GameplayPlaneSyncMode syncMode;
        [Min(0f)] public float dampedSharpness;

        public static GameplayPlaneConfig Default => new()
        {
            planeZ = 0f,
            syncMode = GameplayPlaneSyncMode.Damped,
            dampedSharpness = 18f
        };
    }
}
