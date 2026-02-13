using UnityEngine;

namespace WaterBlob
{
#if CINEMACHINE
    using Cinemachine;

    [DefaultExecutionOrder(-80)]
    public class WaterBlobCameraRig : MonoBehaviour
    {
        [SerializeField] private WaterBlobCharacter2D target;
        [SerializeField] private Vector3 followOffset = new(0f, 1.6f, -10f);
        [SerializeField] private float horizontalBias = 1.2f;
        [SerializeField] private Vector2 screenCenter = new(0.45f, 0.5f);
        [SerializeField] private Vector3 damping = new(0.8f, 1.0f, 0.5f);
        [SerializeField] private float lookaheadVelocity = 3f;
        [SerializeField] private float lookaheadBias = 0.6f;
        [SerializeField] private bool forceOrthographic = true;
        [SerializeField] private float orthographicSize = 6f;

        private CinemachineVirtualCamera vcam;
        private CinemachineFramingTransposer framing;
        private float lastFacingSign = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<WaterBlobCameraRig>() == null)
            {
                var go = new GameObject("__WaterBlobCameraRig");
                go.hideFlags = HideFlags.DontSave;
                go.AddComponent<WaterBlobCameraRig>();
            }
        }

        private void Awake()
        {
            if (target == null)
            {
                target = FindFirstObjectByType<WaterBlobCharacter2D>();
            }

            EnsureCameraSetup();
        }

        private void LateUpdate()
        {
            if (vcam == null || framing == null || target == null)
            {
                return;
            }

            UpdateFacingFromVelocity();
            ApplyFollowSettings();
        }

        private void EnsureCameraSetup()
        {
            Camera mainCam = Camera.main ?? FindAnyObjectByType<Camera>();
            if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
            {
                mainCam.gameObject.AddComponent<CinemachineBrain>();
            }

            vcam = FindExistingVCam() ?? CreateVCam();
            framing = vcam.GetCinemachineComponent<CinemachineFramingTransposer>() ??
                      vcam.AddCinemachineComponent<CinemachineFramingTransposer>();

            if (forceOrthographic && mainCam != null)
            {
                mainCam.orthographic = true;
            }

            vcam.m_Lens.Orthographic = forceOrthographic;
            if (forceOrthographic)
            {
                vcam.m_Lens.OrthographicSize = orthographicSize;
            }

            vcam.Follow = target != null ? target.transform : null;
            vcam.LookAt = target != null ? target.transform : null;
        }

        private CinemachineVirtualCamera FindExistingVCam()
        {
            CinemachineVirtualCamera[] all = FindObjectsOfType<CinemachineVirtualCamera>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                {
                    continue;
                }

                if (all[i].Follow == target?.transform || all[i].name == "__WaterBlobVCam")
                {
                    return all[i];
                }
            }

            return null;
        }

        private CinemachineVirtualCamera CreateVCam()
        {
            var go = new GameObject("__WaterBlobVCam");
            var cam = go.AddComponent<CinemachineVirtualCamera>();
            cam.Priority = 20;
            return cam;
        }

        private void UpdateFacingFromVelocity()
        {
            if (target == null || target.CoreBody == null)
            {
                return;
            }

            float vx = target.CoreBody.linearVelocity.x;
            if (Mathf.Abs(vx) > 0.05f)
            {
                lastFacingSign = Mathf.Sign(vx);
            }
        }

        private void ApplyFollowSettings()
        {
            framing.m_CameraDistance = Mathf.Abs(followOffset.z);
            framing.m_TrackedObjectOffset = new Vector3(horizontalBias * lastFacingSign, followOffset.y, 0f);
            framing.m_XDamping = damping.x;
            framing.m_YDamping = damping.y;
            framing.m_ZDamping = damping.z;
            framing.m_ScreenX = screenCenter.x;
            framing.m_ScreenY = screenCenter.y;

            if (target != null && target.CoreBody != null)
            {
                float vx = target.CoreBody.linearVelocity.x;
                float lean = Mathf.Clamp(vx / Mathf.Max(0.01f, lookaheadVelocity), -1f, 1f);
                framing.m_TrackedObjectOffset.x += lookaheadBias * lean;
            }
        }
    }
#else
    [DefaultExecutionOrder(-80)]
    public class WaterBlobCameraRig : MonoBehaviour
    {
        [SerializeField] private WaterBlobCharacter2D target;
        [SerializeField] private Vector3 followOffset = new(0f, 1.6f, -10f);
        [SerializeField] private float followSharpness = 7f;
        [SerializeField] private float lookaheadDistance = 1.25f;
        [SerializeField] private float lookaheadVelocity = 6f;
        [SerializeField] private bool forceOrthographic = true;
        [SerializeField] private float orthographicSize = 6f;

        private Camera mainCam;
        private float lastFacingSign = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapFallback()
        {
            if (FindFirstObjectByType<WaterBlobCameraRig>() == null)
            {
                var go = new GameObject("__WaterBlobCameraRig");
                go.hideFlags = HideFlags.DontSave;
                go.AddComponent<WaterBlobCameraRig>();
            }
        }

        private void Awake()
        {
            if (target == null)
            {
                target = FindFirstObjectByType<WaterBlobCharacter2D>();
            }

            mainCam = Camera.main ?? FindAnyObjectByType<Camera>();
            if (mainCam != null && forceOrthographic)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = orthographicSize;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (mainCam == null)
            {
                mainCam = Camera.main ?? FindAnyObjectByType<Camera>();
                if (mainCam == null)
                {
                    return;
                }
            }

            float vx = 0f;
            if (target.CoreBody != null)
            {
                vx = target.CoreBody.linearVelocity.x;
                if (Mathf.Abs(vx) > 0.05f)
                {
                    lastFacingSign = Mathf.Sign(vx);
                }
            }

            float lookaheadT = Mathf.Clamp(vx / Mathf.Max(0.01f, lookaheadVelocity), -1f, 1f);
            float lookaheadX = (lastFacingSign * lookaheadDistance * 0.6f) + (lookaheadDistance * lookaheadT);
            Vector3 desired = target.transform.position + followOffset + new Vector3(lookaheadX, 0f, 0f);

            float t = 1f - Mathf.Exp(-Mathf.Max(0.0001f, followSharpness) * Time.deltaTime);
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, desired, t);
        }
    }
#endif
}
