using UnityEngine;
using UnityEngine.Events;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class EarthquakeReceiver2D : MonoBehaviour
    {
        [SerializeField] private UnityEvent onEarthquakeEnter;
        [SerializeField] private UnityEvent onEarthquakeExit;

        public void OnEarthquakeEnter()
        {
            onEarthquakeEnter?.Invoke();
        }

        public void OnEarthquakeExit()
        {
            onEarthquakeExit?.Invoke();
        }
    }
}
