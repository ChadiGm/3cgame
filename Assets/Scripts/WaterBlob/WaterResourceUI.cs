using UnityEngine;
using UnityEngine.UI;

namespace WaterBlob
{
    /// <summary>
    /// HUD component that visualizes the OneDropWaterResource2D level.
    /// Expects an Image component with ImageType.Filled.
    /// </summary>
    public class WaterResourceUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OneDropWaterResource2D targetResource;
        [SerializeField] private Image fillImage;   // Legacy fill support
        [SerializeField] private RectTransform liquidRect; // New masked liquid support
        [SerializeField] private Image iconImage;   // The mask or icon

        [Header("Appearance")]
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private float smoothSpeed = 8f;

        [Header("Juice")]
        [SerializeField] private float damageShakeAmount = 15f;
        [SerializeField] private float damageShakeTime = 0.4f;

        private const float ResolveRetryInterval = 1f;

        private float visualFillAmount = 1f;
        private float targetFillAmount = 1f;
        private float resolveRetryTimer;
        private float shakeTimer;
        private Vector3 baseIconPosition;
        private Vector2 liquidBasePos;
        private float liquidFullHeight;
        private Image liquidImage;
        private bool isBound;
        private bool loggedMissingVisualRefs;
        private bool loggedMissingIconRef;
        private bool loggedMissingResource;

        public bool IsBound => targetResource != null;
        public bool HasVisualTargets => fillImage != null || liquidRect != null;
        public OneDropWaterResource2D BoundResource => targetResource;

        private static Gradient CreateDefaultGradient()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.red, 0f),
                    new GradientColorKey(Color.yellow, 0.5f),
                    new GradientColorKey(new Color(0.2f, 0.6f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return g;
        }

        private void EnsureGradient()
        {
            if (healthGradient == null)
            {
                healthGradient = CreateDefaultGradient();
            }
        }

        private void Awake()
        {
            EnsureGradient();
            CacheVisualAnchors();

            // Procedural builders assign references after AddComponent/Awake.
            if (fillImage != null || liquidRect != null || iconImage != null)
            {
                ValidateReferences();
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerResourceCreated>(HandlePlayerCreated);
            EventBus.Subscribe<PlayerResourceRemoved>(HandlePlayerRemoved);

            if (targetResource != null)
            {
                Bind(targetResource);
            }
            else
            {
                TryResolveAndBind();
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerResourceCreated>(HandlePlayerCreated);
            EventBus.Unsubscribe<PlayerResourceRemoved>(HandlePlayerRemoved);
            UnbindEvents();
        }

        private void HandlePlayerCreated(PlayerResourceCreated ev)
        {
            if (ev.Resource != null)
            {
                Bind(ev.Resource);
            }
        }

        private void HandlePlayerRemoved(PlayerResourceRemoved ev)
        {
            if (ev.Resource == targetResource)
            {
                UnbindEvents();
            }
        }

        private void Start()
        {
            if (!isBound)
            {
                TryResolveAndBind();
            }
        }

        public void ConfigureVisuals(Image fill, Image icon, RectTransform liquid)
        {
            fillImage = fill;
            iconImage = icon;
            liquidRect = liquid;

            EnsureGradient();

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
            }

            CacheVisualAnchors();
            ValidateReferences();

            if (targetResource != null)
            {
                ApplyFillImmediate(targetResource.WaterRatio);
            }
        }

        /// <summary>
        /// Backward-compatible wrapper used by runtime HUD builders.
        /// </summary>
        public void SetupRuntime(Image fill, Image icon, RectTransform liquid = null)
        {
            ConfigureVisuals(fill, icon, liquid);
            if (!isBound)
            {
                TryResolveAndBind();
            }
            Debug.Log("[WaterResourceUI] Runtime setup complete.", this);
        }

        public void Bind(OneDropWaterResource2D resource)
        {
            if (resource == null)
            {
                Debug.LogWarning("[WaterResourceUI] Bind called with null resource.", this);
                return;
            }

            if (targetResource == resource && isBound)
            {
                return;
            }

            UnbindEvents();
            targetResource = resource;
            targetResource.OnWaterRatioChanged += HandleWaterChanged;
            targetResource.OnTakeDamage += HandleTakeDamage;
            isBound = true;
            loggedMissingResource = false;

            ApplyFillImmediate(targetResource.WaterRatio);
            Debug.Log($"[WaterResourceUI] Bound to OneDropWaterResource2D on '{resource.gameObject.name}'.", this);
        }

        private void ValidateReferences()
        {
            if (fillImage == null && liquidRect == null)
            {
                if (!loggedMissingVisualRefs)
                {
                    Debug.LogError($"[WaterResourceUI] {name} needs either 'fillImage' or 'liquidRect'.", this);
                    loggedMissingVisualRefs = true;
                }
            }
            else
            {
                loggedMissingVisualRefs = false;
            }

            if (iconImage == null)
            {
                if (!loggedMissingIconRef)
                {
                    Debug.LogWarning($"[WaterResourceUI] {name} is missing optional 'iconImage'.", this);
                    loggedMissingIconRef = true;
                }
            }
            else
            {
                loggedMissingIconRef = false;
            }
        }

        private void TryResolveAndBind()
        {
            if (isBound && targetResource != null) return;

            OneDropWaterResource2D discovered = FindResourceCandidate();
            if (discovered != null)
            {
                Bind(discovered);
                return;
            }

            if (!loggedMissingResource)
            {
                Debug.LogWarning("[WaterResourceUI] Could not find OneDropWaterResource2D yet. Retrying...", this);
                loggedMissingResource = true;
            }
        }

        private static OneDropWaterResource2D FindResourceCandidate()
        {
            GameObject taggedPlayer = GameObject.FindWithTag("Player");
            if (taggedPlayer != null)
            {
                OneDropWaterResource2D taggedResource = taggedPlayer.GetComponentInChildren<OneDropWaterResource2D>();
                if (taggedResource != null) return taggedResource;
            }

            GameObject namedPlayer = GameObject.Find("WaterBlobPlayer");
            if (namedPlayer != null)
            {
                OneDropWaterResource2D namedResource = namedPlayer.GetComponentInChildren<OneDropWaterResource2D>();
                if (namedResource != null) return namedResource;
            }

            return Object.FindAnyObjectByType<OneDropWaterResource2D>();
        }

        private void CacheVisualAnchors()
        {
            if (iconImage != null)
            {
                baseIconPosition = iconImage.transform.localPosition;
            }

            if (liquidRect != null)
            {
                liquidBasePos = liquidRect.anchoredPosition;
                liquidFullHeight = Mathf.Max(1f, liquidRect.rect.height);
                liquidImage = liquidRect.GetComponent<Image>();
            }
            else
            {
                liquidImage = null;
                liquidFullHeight = 1f;
            }
        }

        private void UnbindEvents()
        {
            if (!isBound || targetResource == null) return;
            targetResource.OnWaterRatioChanged -= HandleWaterChanged;
            targetResource.OnTakeDamage -= HandleTakeDamage;
            isBound = false;
        }

        private void HandleWaterChanged(float ratio)
        {
            targetFillAmount = ratio;
        }

        private void OnDestroy()
        {
            // Ensure unsubscription if not handled by OnDisable
            EventBus.Unsubscribe<PlayerResourceCreated>(HandlePlayerCreated);
            EventBus.Unsubscribe<PlayerResourceRemoved>(HandlePlayerRemoved);
            UnbindEvents();
        }

        private void HandleTakeDamage()
        {
            shakeTimer = damageShakeTime;
        }

        private void ApplyFillImmediate(float amount01)
        {
            targetFillAmount = amount01;
            visualFillAmount = amount01;
            UpdateUI(visualFillAmount);
        }

        private void Update()
        {
            if (fillImage == null && liquidRect == null) return;

            visualFillAmount = Mathf.Lerp(visualFillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
            UpdateUI(visualFillAmount);

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                ApplyShake();
            }
            else if (iconImage != null)
            {
                iconImage.transform.localPosition = baseIconPosition;
            }
        }

        private void ApplyShake()
        {
            if (iconImage == null) return;
            float p = shakeTimer / damageShakeTime;
            Vector3 shake = Random.insideUnitSphere * damageShakeAmount * p;
            iconImage.transform.localPosition = baseIconPosition + shake;
        }

        private void UpdateUI(float amount01)
        {
            // Legacy fill support
            if (fillImage != null)
            {
                fillImage.fillAmount = amount01;

                if (healthGradient != null && healthGradient.colorKeys.Length > 0)
                {
                    fillImage.color = healthGradient.Evaluate(amount01);
                }
                else
                {
                    if (amount01 > 0.5f) fillImage.color = Color.Lerp(Color.yellow, new Color(0.2f, 0.6f, 1f), (amount01 - 0.5f) * 2f);
                    else fillImage.color = Color.Lerp(Color.red, Color.yellow, amount01 * 2f);
                }
            }

            // Modern masked liquid support
            if (liquidRect != null)
            {
                float yOffset = (1f - amount01) * -liquidFullHeight;
                Vector2 pos = liquidBasePos;
                pos.y += yOffset;
                liquidRect.anchoredPosition = pos;

                Image liqImg = liquidImage;
                if (liqImg != null)
                {
                    if (healthGradient != null && healthGradient.colorKeys.Length > 0)
                    {
                        liqImg.color = healthGradient.Evaluate(amount01);
                    }
                    else
                    {
                        if (amount01 > 0.5f) liqImg.color = Color.Lerp(Color.yellow, new Color(0.2f, 0.6f, 1f), (amount01 - 0.5f) * 2f);
                        else liqImg.color = Color.Lerp(Color.red, Color.yellow, amount01 * 2f);
                    }
                }
            }

            if (iconImage != null && amount01 < 0.25f)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.1f;
                iconImage.transform.localScale = Vector3.one * pulse;
            }
            else if (iconImage != null)
            {
                iconImage.transform.localScale = Vector3.one;
            }
        }
    }
}
