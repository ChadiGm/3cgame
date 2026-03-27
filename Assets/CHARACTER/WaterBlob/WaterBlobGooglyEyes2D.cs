using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class WaterBlobGooglyEyes2D : MonoBehaviour
    {
        [SerializeField] private WaterBlobCharacter2D blob;

        [Header("Anchor")]
        public Vector2 normalizedOffset = new(0.34f, 0.1f);
        [Min(0f)] public float anchorFollow = 16f;
        [Range(0f, 1f)] public float anchorRotationInfluence = 0.45f;
        public float depth = -0.35f;

        [Header("Eye Shape")]
        [Min(0.01f)] public float eyeSeparation = 0.45f;
        [Min(0.01f)] public float eyeRadius = 0.19f;
        [Min(0.01f)] public float pupilRadius = 0.08f;

        [Header("Eye Rotation")]
        [Range(0f, 1f)] public float eyeRotationInfluence = 0.38f;
        [Min(0f)] public float maxEyeTiltDeg = 28f;
        [Min(0f)] public float eyeRotationFollow = 14f;
        [Min(0f)] public float eyeDirectionalLeanBoost = 1.35f;
        public bool invertEyeDirection = true;

        [Header("Googly Motion")]
        [Min(0f)] public float pupilRange = 0.07f;
        [Min(0f)] public float pupilFollow = 16f;
        [Min(0f)] public float lookVelocityScale = 0.2f;
        [Min(0f)] public float jitterAmount = 0.018f;

        private Transform faceRoot;
        private Transform leftPupil;
        private Transform rightPupil;
        private Rigidbody2D targetBody;
        private float radius = 1f;

        private readonly List<Material> runtimeMaterials = new();

        private void Awake()
        {
            if (blob == null)
            {
                blob = GetComponent<WaterBlobCharacter2D>();
            }

            targetBody = ResolveTargetBody();
            radius = ResolveRadius();

            BuildFace();
        }

        private void OnEnable()
        {
            BuildFace();
        }

        private void LateUpdate()
        {
            if (targetBody == null)
            {
                targetBody = ResolveTargetBody();
            }

            if (targetBody == null || faceRoot == null)
            {
                return;
            }

            radius = ResolveRadius();
            float movementSign = invertEyeDirection ? -1f : 1f;
            float movementTilt = movementSign * targetBody.linearVelocity.x * eyeDirectionalLeanBoost;
            float coreAnchorContribution = targetBody.rotation * anchorRotationInfluence;
            float anchorAngle = coreAnchorContribution + movementTilt * anchorRotationInfluence * 0.4f;
            anchorAngle = Mathf.Clamp(anchorAngle, -maxEyeTiltDeg, maxEyeTiltDeg);
            Vector2 baseOffset = normalizedOffset * radius;
            Vector2 orientedOffset = (Vector2)(Quaternion.Euler(0f, 0f, anchorAngle) * (Vector3)baseOffset);

            Vector3 targetPosition = new(targetBody.position.x + orientedOffset.x, targetBody.position.y + orientedOffset.y, depth);
            float anchorLerp = DampedLerp(anchorFollow, Time.deltaTime);
            faceRoot.position = Vector3.Lerp(faceRoot.position, targetPosition, anchorLerp);

            float coreRotationContribution = targetBody.rotation * eyeRotationInfluence;
            float desiredEyeAngle = coreRotationContribution + movementTilt;
            desiredEyeAngle = Mathf.Clamp(Mathf.DeltaAngle(0f, desiredEyeAngle), -maxEyeTiltDeg, maxEyeTiltDeg);

            float currentEyeAngle = Mathf.DeltaAngle(0f, faceRoot.eulerAngles.z);
            float nextEyeAngle = Mathf.LerpAngle(currentEyeAngle, desiredEyeAngle, DampedLerp(eyeRotationFollow, Time.deltaTime));
            faceRoot.rotation = Quaternion.Euler(0f, 0f, nextEyeAngle);

            Vector2 velocityLook = targetBody.linearVelocity * lookVelocityScale;
            Vector2 desiredLook = velocityLook.sqrMagnitude > 0.0001f ? velocityLook.normalized * pupilRange : Vector2.zero;

            UpdatePupil(leftPupil, desiredLook, 0.3f);
            UpdatePupil(rightPupil, desiredLook, 1.3f);
        }

        private void BuildFace()
        {
            if (targetBody == null)
            {
                return;
            }

            CleanupFace();
            RemoveLegacyFaceChild();
            RemoveOrphanWorldFace();

            string faceName = $"__BlobFace_{name}";
            faceRoot = new GameObject(faceName).transform;

            Transform parent = transform.parent;
            faceRoot.SetParent(parent, true);
            faceRoot.position = new Vector3(transform.position.x, transform.position.y, depth);
            faceRoot.rotation = Quaternion.identity;
            faceRoot.localScale = Vector3.one;

            Transform leftEye = CreateEyePart("LeftEye", faceRoot, new Vector3(-eyeSeparation * 0.5f, 0f, 0f), eyeRadius, Color.white);
            Transform rightEye = CreateEyePart("RightEye", faceRoot, new Vector3(eyeSeparation * 0.5f, 0f, 0f), eyeRadius, Color.white);

            leftPupil = CreateEyePart("LeftPupil", leftEye, new Vector3(0f, 0f, -0.08f), pupilRadius, new Color(0.05f, 0.05f, 0.05f, 1f));
            rightPupil = CreateEyePart("RightPupil", rightEye, new Vector3(0f, 0f, -0.08f), pupilRadius, new Color(0.05f, 0.05f, 0.05f, 1f));
        }

        private void RemoveLegacyFaceChild()
        {
            Transform legacy = transform.Find("__BlobFace");
            if (legacy != null)
            {
                SafeDestroy(legacy.gameObject);
            }
        }

        private void RemoveOrphanWorldFace()
        {
            string faceName = $"__BlobFace_{name}";
            Transform parent = transform.parent;

            Transform existing = parent != null ? parent.Find(faceName) : null;
            if (existing != null)
            {
                SafeDestroy(existing.gameObject);
                return;
            }

            if (parent == null)
            {
                GameObject loose = GameObject.Find(faceName);
                if (loose != null)
                {
                    SafeDestroy(loose);
                }
            }
        }

        private void UpdatePupil(Transform pupil, Vector2 desiredLook, float phase)
        {
            if (pupil == null)
            {
                return;
            }

            float time = Time.time * 4f + phase;
            Vector2 jitter = new(Mathf.Sin(time * 1.7f), Mathf.Cos(time * 2.2f));
            jitter *= jitterAmount;

            Vector2 localTarget = Vector2.ClampMagnitude(desiredLook + jitter, pupilRange);
            Vector3 target = new(localTarget.x, localTarget.y, -0.08f);

            float lerp = DampedLerp(pupilFollow, Time.deltaTime);
            pupil.localPosition = Vector3.Lerp(pupil.localPosition, target, lerp);
        }

        private Transform CreateEyePart(string objectName, Transform parent, Vector3 localPosition, float radius, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one * (radius * 2f);
            part.layer = gameObject.layer;

            Collider col = part.GetComponent<Collider>();
            if (col != null)
            {
                SafeDestroy(col);
            }

            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material material = CreateMaterial(color);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            return part.transform;
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new(shader)
            {
                color = color,
                name = "WaterBlobEyeRuntime"
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            runtimeMaterials.Add(material);
            return material;
        }

        private static float DampedLerp(float sharpness, float dt)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0.0001f, sharpness) * dt);
        }

        private Rigidbody2D ResolveTargetBody()
        {
            if (blob != null && blob.CoreBody != null)
            {
                return blob.CoreBody;
            }

            Rigidbody2D localBody = GetComponent<Rigidbody2D>();
            if (localBody != null)
            {
                return localBody;
            }

            return null;
        }

        private float ResolveRadius()
        {
            if (blob != null)
            {
                return Mathf.Max(0.1f, blob.Radius);
            }

            CircleCollider2D circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                return Mathf.Max(0.1f, circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Bounds bounds = col.bounds;
                return Mathf.Max(0.1f, Mathf.Max(bounds.extents.x, bounds.extents.y));
            }

            return 1f;
        }

        private void CleanupFace()
        {
            if (faceRoot != null)
            {
                SafeDestroy(faceRoot.gameObject);
                faceRoot = null;
            }

            leftPupil = null;
            rightPupil = null;

            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                {
                    SafeDestroy(runtimeMaterials[i]);
                }
            }

            runtimeMaterials.Clear();
        }

        private void OnDestroy()
        {
            CleanupFace();
        }

        private static void SafeDestroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
