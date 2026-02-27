using UnityEngine;
using UnityEngine.UI;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class WaterBlobWaterLevelHud : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private WaterBlobWaterLevel2D targetWater;
        [SerializeField] private bool autoFindTarget = true;

        [Header("Layout")]
        [SerializeField] private Vector2 iconSize = new(88f, 88f);
        [SerializeField] private Vector2 iconOffset = new(0f, 34f);
        [SerializeField] private float fillLerpSpeed = 8f;

        [Header("Style")]
        [SerializeField] private Color backgroundColor = new(0.06f, 0.11f, 0.18f, 0.9f);
        [SerializeField] private Color borderColor = new(0.72f, 0.9f, 1f, 0.92f);
        [SerializeField] private Color gradientBottom = new(0.1f, 0.45f, 0.86f, 0.95f);
        [SerializeField] private Color gradientTop = new(0.56f, 0.9f, 1f, 1f);

        private RectTransform fillRect;
        private float smoothedLevel = 1f;
        private float fillMaxHeight;

        private Texture2D circleTexture;
        private Sprite circleSprite;
        private Texture2D gradientTexture;

        private void Awake()
        {
            BuildUi();
            smoothedLevel = GetTargetLevel();
            UpdateFill(smoothedLevel);
        }

        private void Update()
        {
            if ((targetWater == null || !targetWater.isActiveAndEnabled) && autoFindTarget)
            {
                targetWater = FindFirstObjectByType<WaterBlobWaterLevel2D>();
            }

            float targetLevel = GetTargetLevel();
            smoothedLevel = Mathf.MoveTowards(smoothedLevel, targetLevel, fillLerpSpeed * Time.deltaTime);
            UpdateFill(smoothedLevel);
        }

        private float GetTargetLevel()
        {
            return targetWater != null ? targetWater.WaterLevel : 1f;
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

            RectTransform root = CreateRect("WaterLevelRoot", canvas.transform as RectTransform, iconSize);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = iconOffset;

            Image back = root.gameObject.AddComponent<Image>();
            back.sprite = GetCircleSprite();
            back.type = Image.Type.Sliced;
            back.color = backgroundColor;

            Outline border = root.gameObject.AddComponent<Outline>();
            border.effectColor = borderColor;
            border.effectDistance = new Vector2(1.2f, 1.2f);

            RectTransform maskRect = CreateRect("Mask", root, Vector2.zero);
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = new Vector2(5f, 5f);
            maskRect.offsetMax = new Vector2(-5f, -5f);

            Image maskImage = maskRect.gameObject.AddComponent<Image>();
            maskImage.sprite = GetCircleSprite();
            maskImage.type = Image.Type.Sliced;
            maskImage.color = Color.white;

            Mask mask = maskRect.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            fillRect = CreateRect("Fill", maskRect, Vector2.zero);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 0f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.anchoredPosition = Vector2.zero;

            fillMaxHeight = Mathf.Max(1f, iconSize.y - 10f);
            fillRect.sizeDelta = new Vector2(0f, fillMaxHeight);

            RectTransform gradientRect = CreateRect("Gradient", fillRect, Vector2.zero);
            gradientRect.anchorMin = Vector2.zero;
            gradientRect.anchorMax = Vector2.one;
            gradientRect.offsetMin = Vector2.zero;
            gradientRect.offsetMax = Vector2.zero;

            RawImage gradientImage = gradientRect.gameObject.AddComponent<RawImage>();
            gradientImage.texture = GetGradientTexture();
            gradientImage.color = Color.white;

            RectTransform shineRect = CreateRect("Shine", root, Vector2.zero);
            shineRect.anchorMin = Vector2.zero;
            shineRect.anchorMax = Vector2.one;
            shineRect.offsetMin = new Vector2(10f, 34f);
            shineRect.offsetMax = new Vector2(-10f, -10f);

            Image shine = shineRect.gameObject.AddComponent<Image>();
            shine.sprite = GetCircleSprite();
            shine.type = Image.Type.Sliced;
            shine.color = new Color(1f, 1f, 1f, 0.08f);
        }

        private void UpdateFill(float normalizedLevel)
        {
            if (fillRect == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(normalizedLevel);
            fillRect.sizeDelta = new Vector2(0f, fillMaxHeight * clamped);
        }

        private Texture2D GetGradientTexture()
        {
            if (gradientTexture != null)
            {
                return gradientTexture;
            }

            const int width = 16;
            const int height = 128;
            gradientTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "WaterHudGradientRuntime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                Color c = Color.Lerp(gradientBottom, gradientTop, t);
                for (int x = 0; x < width; x++)
                {
                    gradientTexture.SetPixel(x, y, c);
                }
            }

            gradientTexture.Apply(false, false);
            return gradientTexture;
        }

        private Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 128;
            circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "WaterHudCircleRuntime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new(size * 0.5f, size * 0.5f);
            float radius = (size * 0.5f) - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(radius - 1.5f, radius + 1.5f, dist));
                    circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            circleTexture.Apply(false, false);
            circleSprite = Sprite.Create(circleTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 1, SpriteMeshType.FullRect, Vector4.zero, false);
            return circleSprite;
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
            DestroySafe(circleSprite);
            DestroySafe(circleTexture);
            DestroySafe(gradientTexture);
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
