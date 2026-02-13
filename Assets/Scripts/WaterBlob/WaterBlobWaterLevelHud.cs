using UnityEngine;
using UnityEngine.UI;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class WaterBlobWaterLevelHud : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private WaterBlobCharacter2D target;
        [SerializeField] private bool autoFindTarget = true;

        [Header("Layout")]
        [SerializeField] private Vector2 panelSize = new(320f, 84f);
        [SerializeField] private Vector2 panelOffset = new(0f, 36f);
        [SerializeField] private float fillLerpSpeed = 9f;

        [Header("Theme")]
        [SerializeField] private Color panelColor = new(0.05f, 0.09f, 0.16f, 0.82f);
        [SerializeField] private Color panelOutline = new(0.45f, 0.72f, 1f, 0.34f);
        [SerializeField] private Color gaugeBack = new(0.06f, 0.13f, 0.2f, 0.78f);
        [SerializeField] private Color textColor = new(0.84f, 0.95f, 1f, 0.96f);

        [Header("Fluid Animation")]
        [SerializeField] private float fluidScrollSpeed = 0.18f;
        [SerializeField] private float fluidPulseAmplitude = 0.06f;
        [SerializeField] private float fluidPulseFrequency = 1.6f;

        private RectTransform fillRect;
        private RawImage fluidRawImage;
        private Text percentageText;

        private float smoothedLevel = 1f;
        private float gaugeHeight;
        private Texture2D gradientTexture;
        private Sprite dropletSprite;

        public static WaterBlobWaterLevelHud EnsureInScene(WaterBlobCharacter2D preferredTarget = null)
        {
            WaterBlobWaterLevelHud existing = FindFirstObjectByType<WaterBlobWaterLevelHud>();
            if (existing != null)
            {
                if (preferredTarget != null)
                {
                    existing.target = preferredTarget;
                }

                return existing;
            }

            GameObject root = new("__WaterLevelHud");
            WaterBlobWaterLevelHud hud = root.AddComponent<WaterBlobWaterLevelHud>();
            hud.target = preferredTarget;
            return hud;
        }

        private void Awake()
        {
            BuildUi();
            smoothedLevel = GetTargetLevel();
        }

        private void Update()
        {
            if ((target == null || !target.isActiveAndEnabled) && autoFindTarget)
            {
                target = FindFirstObjectByType<WaterBlobCharacter2D>();
            }

            float targetLevel = GetTargetLevel();
            smoothedLevel = Mathf.MoveTowards(smoothedLevel, targetLevel, fillLerpSpeed * Time.deltaTime);
            UpdateFluidFill(smoothedLevel);
            UpdateText(smoothedLevel);
            AnimateFluid();
        }

        private float GetTargetLevel()
        {
            if (target == null)
            {
                return 1f;
            }

            return target.WaterLevel;
        }

        private void BuildUi()
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new("Canvas");
                canvasGo.transform.SetParent(transform, false);

                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 150;

                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.6f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }

            RectTransform panel = CreateRect("WaterLevelPanel", canvas.transform as RectTransform, panelSize);
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = panelOffset;

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = panelColor;

            Outline panelGlow = panel.gameObject.AddComponent<Outline>();
            panelGlow.effectColor = panelOutline;
            panelGlow.effectDistance = new Vector2(1.5f, 1.5f);

            RectTransform iconRoot = CreateRect("IconRoot", panel, new Vector2(64f, 64f));
            iconRoot.anchorMin = new Vector2(0f, 0.5f);
            iconRoot.anchorMax = new Vector2(0f, 0.5f);
            iconRoot.pivot = new Vector2(0.5f, 0.5f);
            iconRoot.anchoredPosition = new Vector2(38f, 0f);

            Image iconBack = iconRoot.gameObject.AddComponent<Image>();
            iconBack.color = new Color(0.12f, 0.2f, 0.32f, 0.9f);

            RectTransform iconGraphicRect = CreateRect("DropletIcon", iconRoot, new Vector2(36f, 44f));
            Image iconGraphic = iconGraphicRect.gameObject.AddComponent<Image>();
            iconGraphic.sprite = GetDropletSprite();
            iconGraphic.preserveAspect = true;
            iconGraphic.color = new Color(0.62f, 0.9f, 1f, 1f);

            RectTransform gaugeFrame = CreateRect("GaugeFrame", panel, new Vector2(184f, 42f));
            gaugeFrame.anchorMin = new Vector2(0f, 0.5f);
            gaugeFrame.anchorMax = new Vector2(0f, 0.5f);
            gaugeFrame.pivot = new Vector2(0f, 0.5f);
            gaugeFrame.anchoredPosition = new Vector2(78f, 0f);

            Image gaugeFrameImage = gaugeFrame.gameObject.AddComponent<Image>();
            gaugeFrameImage.color = gaugeBack;

            RectTransform maskRect = CreateRect("FluidMask", gaugeFrame, Vector2.zero);
            maskRect.anchorMin = new Vector2(0f, 0f);
            maskRect.anchorMax = new Vector2(1f, 1f);
            maskRect.offsetMin = new Vector2(3f, 3f);
            maskRect.offsetMax = new Vector2(-3f, -3f);

            Image maskImage = maskRect.gameObject.AddComponent<Image>();
            maskImage.color = Color.white;

            Mask mask = maskRect.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            fillRect = CreateRect("FluidFill", maskRect, Vector2.zero);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 0f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.anchoredPosition = Vector2.zero;

            gaugeHeight = Mathf.Max(1f, gaugeFrame.rect.height - 6f);
            fillRect.sizeDelta = new Vector2(0f, gaugeHeight);

            RectTransform fluidRect = CreateRect("FluidGradient", fillRect, Vector2.zero);
            fluidRect.anchorMin = Vector2.zero;
            fluidRect.anchorMax = Vector2.one;
            fluidRect.offsetMin = Vector2.zero;
            fluidRect.offsetMax = Vector2.zero;

            fluidRawImage = fluidRect.gameObject.AddComponent<RawImage>();
            fluidRawImage.texture = GetGradientTexture();
            fluidRawImage.color = Color.white;
            fluidRawImage.uvRect = new Rect(0f, 0f, 1f, 1f);

            RectTransform percentRect = CreateRect("PercentText", panel, new Vector2(72f, 40f));
            percentRect.anchorMin = new Vector2(1f, 0.5f);
            percentRect.anchorMax = new Vector2(1f, 0.5f);
            percentRect.pivot = new Vector2(1f, 0.5f);
            percentRect.anchoredPosition = new Vector2(-14f, 0f);

            percentageText = percentRect.gameObject.AddComponent<Text>();
            percentageText.text = "100%";
            percentageText.alignment = TextAnchor.MiddleRight;
            percentageText.color = textColor;
            percentageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (percentageText.font == null)
            {
                percentageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            percentageText.resizeTextForBestFit = true;
            percentageText.resizeTextMinSize = 16;
            percentageText.resizeTextMaxSize = 32;
        }

        private void UpdateFluidFill(float normalizedLevel)
        {
            if (fillRect == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(normalizedLevel);
            fillRect.sizeDelta = new Vector2(0f, gaugeHeight * clamped);
        }

        private void UpdateText(float normalizedLevel)
        {
            if (percentageText == null)
            {
                return;
            }

            int percent = Mathf.RoundToInt(Mathf.Clamp01(normalizedLevel) * 100f);
            percentageText.text = $"{percent}%";
        }

        private void AnimateFluid()
        {
            if (fluidRawImage == null)
            {
                return;
            }

            Rect uv = fluidRawImage.uvRect;
            float pulse = Mathf.Sin(Time.unscaledTime * fluidPulseFrequency * Mathf.PI * 2f) * fluidPulseAmplitude;
            uv.y = (Time.unscaledTime * fluidScrollSpeed) + pulse;
            fluidRawImage.uvRect = uv;
        }

        private Texture2D GetGradientTexture()
        {
            if (gradientTexture != null)
            {
                return gradientTexture;
            }

            const int w = 32;
            const int h = 256;
            gradientTexture = new Texture2D(w, h, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "WaterLevelGradientRuntime"
            };

            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f);
                Color baseColor = Color.Lerp(new Color(0.07f, 0.37f, 0.74f, 0.9f), new Color(0.48f, 0.9f, 1f, 1f), t);
                float waveBand = Mathf.Sin((t * 16f) * Mathf.PI) * 0.04f;
                baseColor.r = Mathf.Clamp01(baseColor.r + waveBand);
                baseColor.g = Mathf.Clamp01(baseColor.g + waveBand * 0.8f);
                baseColor.b = Mathf.Clamp01(baseColor.b + waveBand * 0.6f);

                for (int x = 0; x < w; x++)
                {
                    gradientTexture.SetPixel(x, y, baseColor);
                }
            }

            gradientTexture.Apply(false, false);
            return gradientTexture;
        }

        private Sprite GetDropletSprite()
        {
            if (dropletSprite != null)
            {
                return dropletSprite;
            }

            const int size = 96;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "WaterDropletIconRuntime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new(size * 0.5f, size * 0.52f);
            float radius = size * 0.28f;
            Vector2 tip = new(size * 0.5f, size * 0.1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new(x + 0.5f, y + 0.5f);
                    float circle = Vector2.Distance(p, center) - radius;
                    float sideA = SignedDistanceToLine(p, tip, new Vector2(size * 0.24f, size * 0.52f));
                    float sideB = SignedDistanceToLine(new Vector2(size - p.x, p.y), tip, new Vector2(size * 0.24f, size * 0.52f));
                    float triangle = Mathf.Max(sideA, sideB);
                    float droplet = Mathf.Min(circle, triangle);

                    float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(-2f, 2f, droplet));
                    Color col = Color.Lerp(new Color(0.22f, 0.72f, 1f, 0f), new Color(0.76f, 0.95f, 1f, 1f), y / (size - 1f));
                    col.a *= alpha;
                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply(false, false);
            dropletSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return dropletSprite;
        }

        private static float SignedDistanceToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 n = new(a.y - b.y, b.x - a.x);
            n.Normalize();
            return Vector2.Dot(p - a, n);
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        private void OnDestroy()
        {
            if (gradientTexture != null)
            {
                DestroySafe(gradientTexture);
            }

            if (dropletSprite != null)
            {
                Texture tex = dropletSprite.texture;
                DestroySafe(dropletSprite);
                if (tex != null)
                {
                    DestroySafe(tex);
                }
            }
        }

        private static void DestroySafe(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
    }
}
