using UnityEngine;

namespace WaterBlob
{
    public interface IGameplay2DProxySource
    {
        Transform ProxySourceTransform { get; }
        float GameplayPlaneZ { get; }
        bool PlaneLockEnabled { get; }
    }
}
