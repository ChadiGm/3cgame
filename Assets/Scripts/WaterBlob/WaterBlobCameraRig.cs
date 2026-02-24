using UnityEngine;

namespace WaterBlob
{
#if CINEMACHINE
    using Cinemachine;
#endif

    [DefaultExecutionOrder(-80)]
    public class WaterBlobCameraRig : MonoBehaviour
    {
        [SerializeField] private WaterBlobCharacter2D target;

        [Header("2.5D Camera Mode")]
        [SerializeField] private bool usePerspective = true;
        [SerializeField] private bool allowOrthographicFallback = true;
        [SerializeField] private Vector3 sideViewEuler = new(8f, -88f, 0f);
        [SerializeField] private float distanceFromPlane = 14f;
        [SerializeField] private float followDepthBias = -1.2f;
        [SerializeField] private float perspectiveFov = 48f;
        [SerializeField] private float nearClip = 0.03f;
        [SerializeField] private float farClip = 300f;

        [Header("Orthographic Fallback")]
        [SerializeField] private float orthographicSize = 6f;

        [Header("Follow")]
        [SerializeField] private Vector3 followOffset = new(0f, 1.6f, 0f);
        [SerializeField] private float followSharpness = 7f;
        [SerializeField] private float lookaheadDistance = 1.25f;
        [SerializeField] private float lookaheadVelocity = 6f;

#if CINEMACHINE
        [Header("Cinemachine (Optional)")]
        [SerializeField] private bool preferCinemachine;
        [SerializeField] private Vector2 screenCenter = new(0.45f, 0.5f);
        [SerializeField] private Vector3 damping = new(0.8f, 1.0f, 0.5f);

        private CinemachineVirtualCamera vcam;
        private CinemachineFramingTransposer framing;
#endif

        private Camera mainCam;
        private float lastFacingSign = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
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
            EnsureCameraState();
#if CINEMACHINE
            if (preferCinemachine)
            {
                EnsureCinemachineState();
            }
#endif
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            mainCam ??= Camera.main ?? FindAnyObjectByType<Camera>();
            if (mainCam == null)
            {
                return;
            }

            UpdateFacingFromVelocity();
            EnsureCameraState();

#if CINEMACHINE
            if (preferCinemachine && EnsureCinemachineState())
            {
                ApplyCinemachineFollowSettings();
                return;
            }
#endif

            ApplyTransformFollow();
        }

        private void EnsureCameraState()
        {
            if (mainCam == null)
            {
                return;
            }

            bool useOrtho = !usePerspective && allowOrthographicFallback;
            mainCam.orthographic = useOrtho;
            mainCam.nearClipPlane = Mathf.Max(0.001f, nearClip);
            mainCam.farClipPlane = Mathf.Max(mainCam.nearClipPlane + 1f, farClip);

            if (useOrtho)
            {
                mainCam.orthographicSize = orthographicSize;
            }
            else
            {
                mainCam.fieldOfView = perspectiveFov;
            }
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

        private void ApplyTransformFollow()
        {
            float vx = target.CoreBody != null ? target.CoreBody.linearVelocity.x : 0f;
            float lookaheadT = Mathf.Clamp(vx / Mathf.Max(0.01f, lookaheadVelocity), -1f, 1f);
            float lookaheadX = (lastFacingSign * lookaheadDistance * 0.6f) + (lookaheadDistance * lookaheadT);

            Vector3 targetWorld = target.transform.position + followOffset + new Vector3(lookaheadX, 0f, 0f);
            Vector3 desired;

            if (usePerspective)
            {
                Quaternion rot = Quaternion.Euler(sideViewEuler);
                desired = targetWorld + (rot * Vector3.forward * -Mathf.Max(0.1f, distanceFromPlane));
                desired.z += followDepthBias;
            }
            else
            {
                desired = targetWorld + new Vector3(0f, 0f, -Mathf.Max(0.1f, distanceFromPlane));
            }

            float t = 1f - Mathf.Exp(-Mathf.Max(0.0001f, followSharpness) * Time.deltaTime);
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, desired, t);

            if (usePerspective)
            {
                mainCam.transform.rotation = Quaternion.Slerp(mainCam.transform.rotation, Quaternion.Euler(sideViewEuler), t);
            }
            else
            {
                mainCam.transform.rotation = Quaternion.identity;
            }
        }

#if CINEMACHINE
        private bool EnsureCinemachineState()
        {
            if (!preferCinemachine)
            {
                return false;
            }

            if (mainCam == null)
            {
                return false;
            }

            if (mainCam.GetComponent<CinemachineBrain>() == null)
            {
                mainCam.gameObject.AddComponent<CinemachineBrain>();
            }

            if (vcam == null)
            {
                vcam = FindExistingVCam() ?? CreateVCam();
            }

            framing = vcam.GetCinemachineComponent<CinemachineFramingTransposer>() ??
                      vcam.AddCinemachineComponent<CinemachineFramingTransposer>();

            vcam.Follow = target != null ? target.transform : null;
            vcam.LookAt = target != null ? target.transform : null;
            vcam.m_Lens.Orthographic = !usePerspective && allowOrthographicFallback;
            if (vcam.m_Lens.Orthographic)
            {
                vcam.m_Lens.OrthographicSize = orthographicSize;
            }
            else
            {
                vcam.m_Lens.FieldOfView = perspectiveFov;
            }

            vcam.m_Lens.NearClipPlane = Mathf.Max(0.001f, nearClip);
            vcam.m_Lens.FarClipPlane = Mathf.Max(vcam.m_Lens.NearClipPlane + 1f, farClip);
            return true;
        }

        private void ApplyCinemachineFollowSettings()
        {
            framing.m_CameraDistance = Mathf.Max(0.1f, distanceFromPlane);
            framing.m_TrackedObjectOffset = new Vector3((lookaheadDistance * lastFacingSign), followOffset.y, followDepthBias);
            framing.m_XDamping = damping.x;
            framing.m_YDamping = damping.y;
            framing.m_ZDamping = damping.z;
            framing.m_ScreenX = screenCenter.x;
            framing.m_ScreenY = screenCenter.y;
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

        private static CinemachineVirtualCamera CreateVCam()
        {
            var go = new GameObject("__WaterBlobVCam");
            var cam = go.AddComponent<CinemachineVirtualCamera>();
            cam.Priority = 20;
            return cam;
        }
#endif
    }
}
