using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class OneDropHealthBlobUI2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OneDropWaterResource2D healthSource;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private WaterBlobMeshRenderer2D playerVisual;

        [Header("Layout")]
        [SerializeField] private Vector2 hudOffset = new(28f, -28f);
        [SerializeField, Min(12f)] private float iconSize = 40f;
        [SerializeField, Min(0f)] private float iconSpacing = 8f;

        [Header("Style")]
        [SerializeField] private bool syncColorWithPlayer = true;
        [SerializeField] private Color activeTint = new(0.19f, 0.72f, 1f, 1f);
        [SerializeField] private Color lostTint = new(0.55f, 0.72f, 0.82f, 0.85f);
        [SerializeField, Range(0f, 1f)] private float lostAlpha = 0.2f;

        [Header("Inside Fill")]
        [SerializeField] private bool syncInsideFillWithWater = true;
        [SerializeField, Range(0f, 1f)] private float lostFillAmount = 0.12f;

        [Header("Juicy Animation")]
        [SerializeField, Min(0.05f)] private float loseAnimDuration = 0.34f;
        [SerializeField, Min(0f)] private float loseWobbleDegrees = 14f;
        [SerializeField, Min(0f)] private float loseOvershoot = 0.2f;
        [SerializeField, Min(0.05f)] private float restoreAnimDuration = 0.2f;

        private readonly List<BlobIcon> icons = new();
        private RectTransform root;
        private int displayedHealth = -1;
        private float currentInsideFill = 1f;

        private static Sprite cachedBlobSprite;

        private sealed class BlobIcon
        {
            public RectTransform Rect;
            public Image FrameImage;
            public Image FillImage;
            public CanvasGroup Group;
            public Coroutine Animation;
            public bool IsActive;
        }

        private void Awake()
        {
            ResolveSource();
        }

        private void OnEnable()
        {
            ResolveSource();
            EnsureUi();
            Subscribe();
            RefreshInstant();
            SyncStyleAndFill(forceApply: true);
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopAllIconAnimations();
        }

        private void LateUpdate()
        {
            SyncStyleAndFill(forceApply: false);
        }

        private void Subscribe()
        {
            if (healthSource != null)
            {
                healthSource.HealthChanged += HandleHealthChanged;
            }
        }

        private void Unsubscribe()
        {
            if (healthSource != null)
            {
                healthSource.HealthChanged -= HandleHealthChanged;
            }
        }

        private void ResolveSource()
        {
            if (healthSource == null)
            {
                healthSource = GetComponent<OneDropWaterResource2D>();
            }

            if (healthSource == null)
            {
                healthSource = GetComponentInParent<OneDropWaterResource2D>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponent<WaterBlobMeshRenderer2D>();
            }

            if (playerVisual == null)
            {
                playerVisual = GetComponentInParent<WaterBlobMeshRenderer2D>();
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            EnsureIconCount(max);
            SetHealth(current, instant: false);
        }

        private void RefreshInstant()
        {
            if (healthSource == null)
            {
                return;
            }

            EnsureIconCount(healthSource.MaxHealthPoints);
            SetHealth(healthSource.CurrentHealthPoints, instant: true);
        }

        private void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas canvas = ResolveCanvas();
            if (canvas == null)
            {
                return;
            }

            GameObject rootGo = new("BlobHealthUI", typeof(RectTransform));
            root = rootGo.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = hudOffset;
            root.localScale = Vector3.one;
        }

        private Canvas ResolveCanvas()
        {
            if (targetCanvas != null)
            {
                return targetCanvas;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                targetCanvas = canvas;
                return targetCanvas;
            }

            GameObject canvasGo = new("HUDCanvas_Auto", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas createdCanvas = canvasGo.GetComponent<Canvas>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.pixelPerfect = false;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            targetCanvas = createdCanvas;
            return targetCanvas;
        }

        private void EnsureIconCount(int count)
        {
            EnsureUi();
            if (root == null)
            {
                return;
            }

            count = Mathf.Max(0, count);

            while (icons.Count < count)
            {
                icons.Add(CreateIcon(icons.Count));
            }

            while (icons.Count > count)
            {
                BlobIcon icon = icons[icons.Count - 1];
                StopIconAnimation(icon);
                if (icon.Rect != null)
                {
                    Destroy(icon.Rect.gameObject);
                }

                icons.RemoveAt(icons.Count - 1);
            }

            LayoutIcons();
        }

        private BlobIcon CreateIcon(int index)
        {
            GameObject iconGo = new($"BlobIcon_{index + 1}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Shadow));
            RectTransform rect = iconGo.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * iconSize;

            Image frameImage = iconGo.GetComponent<Image>();
            frameImage.sprite = GetBlobSprite();
            frameImage.type = Image.Type.Simple;
            frameImage.preserveAspect = true;
            frameImage.color = new Color(1f, 1f, 1f, 0.24f);

            Shadow shadow = iconGo.GetComponent<Shadow>();
            shadow.effectColor = new Color(0.02f, 0.18f, 0.26f, 0.35f);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;

            CanvasGroup group = iconGo.GetComponent<CanvasGroup>();
            group.alpha = 1f;

            GameObject fillGo = new("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.SetParent(rect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.localScale = Vector3.one;

            Image fillImage = fillGo.GetComponent<Image>();
            fillImage.sprite = GetBlobSprite();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillImage.fillClockwise = true;
            fillImage.fillAmount = 1f;
            fillImage.preserveAspect = true;
            fillImage.color = activeTint;

            return new BlobIcon
            {
                Rect = rect,
                FrameImage = frameImage,
                FillImage = fillImage,
                Group = group,
                IsActive = true
            };
        }

        private void LayoutIcons()
        {
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < icons.Count; i++)
            {
                BlobIcon icon = icons[i];
                if (icon.Rect != null)
                {
                    icon.Rect.anchoredPosition = new Vector2(i * (iconSize + iconSpacing), 0f);
                    icon.Rect.sizeDelta = Vector2.one * iconSize;
                }
            }

            float width = icons.Count * iconSize + Mathf.Max(0, icons.Count - 1) * iconSpacing;
            root.sizeDelta = new Vector2(width, iconSize);
        }

        private void SetHealth(int current, bool instant)
        {
            if (icons.Count == 0)
            {
                displayedHealth = Mathf.Clamp(current, 0, 0);
                return;
            }

            int clampedCurrent = Mathf.Clamp(current, 0, icons.Count);

            if (instant || displayedHealth < 0)
            {
                for (int i = 0; i < icons.Count; i++)
                {
                    SetIconStateInstant(icons[i], i < clampedCurrent);
                }

                displayedHealth = clampedCurrent;
                SyncStyleAndFill(forceApply: true);
                return;
            }

            for (int i = 0; i < icons.Count; i++)
            {
                bool wasAlive = i < displayedHealth;
                bool shouldBeAlive = i < clampedCurrent;

                if (wasAlive && !shouldBeAlive)
                {
                    PlayLoseAnimation(icons[i]);
                }
                else if (!wasAlive && shouldBeAlive)
                {
                    PlayRestoreAnimation(icons[i]);
                }
                else
                {
                    SetIconStateInstant(icons[i], shouldBeAlive);
                }
            }

            displayedHealth = clampedCurrent;
            SyncStyleAndFill(forceApply: true);
        }

        private void PlayLoseAnimation(BlobIcon icon)
        {
            StopIconAnimation(icon);
            icon.Animation = StartCoroutine(AnimateLose(icon));
        }

        private void PlayRestoreAnimation(BlobIcon icon)
        {
            StopIconAnimation(icon);
            icon.Animation = StartCoroutine(AnimateRestore(icon));
        }

        private IEnumerator AnimateLose(BlobIcon icon)
        {
            float duration = Mathf.Max(0.05f, loseAnimDuration);
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                float wobble = Mathf.Sin(t * Mathf.PI * 9f) * (1f - t) * loseWobbleDegrees;
                float burst = 1f + Mathf.Sin(t * Mathf.PI) * loseOvershoot;
                float shrink = Mathf.Lerp(1f, 0.62f, t * t);
                float alpha = Mathf.Lerp(1f, lostAlpha, Mathf.SmoothStep(0f, 1f, t));

                icon.Rect.localScale = Vector3.one * (burst * shrink);
                icon.Rect.localRotation = Quaternion.Euler(0f, 0f, wobble);
                icon.Group.alpha = alpha;
                icon.FillImage.color = Color.Lerp(activeTint, lostTint, Mathf.SmoothStep(0f, 1f, t));
                icon.FrameImage.color = Color.Lerp(new Color(1f, 1f, 1f, 0.24f), new Color(1f, 1f, 1f, 0.15f), t);
                icon.FillImage.fillAmount = Mathf.Lerp(currentInsideFill, lostFillAmount, t);

                yield return null;
            }

            SetIconAsLost(icon);
            icon.Animation = null;
        }

        private IEnumerator AnimateRestore(BlobIcon icon)
        {
            float duration = Mathf.Max(0.05f, restoreAnimDuration);
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                float wobble = Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * 4f;
                float pop = Mathf.Lerp(0.78f, 1f, t) + Mathf.Sin(t * Mathf.PI) * 0.1f;

                icon.Rect.localScale = Vector3.one * pop;
                icon.Rect.localRotation = Quaternion.Euler(0f, 0f, wobble);
                icon.Group.alpha = Mathf.Lerp(lostAlpha, 1f, t);
                icon.FillImage.color = Color.Lerp(lostTint, activeTint, t);
                icon.FrameImage.color = Color.Lerp(new Color(1f, 1f, 1f, 0.15f), new Color(1f, 1f, 1f, 0.24f), t);
                icon.FillImage.fillAmount = Mathf.Lerp(lostFillAmount, currentInsideFill, t);

                yield return null;
            }

            SetIconAsActive(icon);
            icon.Animation = null;
        }

        private void SetIconStateInstant(BlobIcon icon, bool active)
        {
            StopIconAnimation(icon);
            if (active)
            {
                SetIconAsActive(icon);
            }
            else
            {
                SetIconAsLost(icon);
            }
        }

        private void SetIconAsActive(BlobIcon icon)
        {
            icon.IsActive = true;
            icon.Rect.localScale = Vector3.one;
            icon.Rect.localRotation = Quaternion.identity;
            icon.Group.alpha = 1f;
            icon.FillImage.color = activeTint;
            icon.FillImage.fillAmount = 1f;
            icon.FrameImage.color = new Color(1f, 1f, 1f, 0.24f);
        }

        private void SetIconAsLost(BlobIcon icon)
        {
            icon.IsActive = false;
            icon.Rect.localScale = Vector3.one * 0.62f;
            icon.Rect.localRotation = Quaternion.identity;
            icon.Group.alpha = lostAlpha;
            icon.FillImage.color = lostTint;
            icon.FillImage.fillAmount = lostFillAmount;
            icon.FrameImage.color = new Color(1f, 1f, 1f, 0.15f);
        }

        private void SyncStyleAndFill(bool forceApply)
        {
            if (syncColorWithPlayer && playerVisual != null)
            {
                Color playerColor = playerVisual.fillColor;
                activeTint = new Color(playerColor.r, playerColor.g, playerColor.b, 1f);

                Color dimmed = Color.Lerp(activeTint, Color.white, 0.35f);
                lostTint = new Color(dimmed.r, dimmed.g, dimmed.b, 0.85f);
            }

            float targetFill = currentInsideFill;
            if (syncInsideFillWithWater && healthSource != null)
            {
                targetFill = healthSource.WaterRatio;
            }

            if (!forceApply && Mathf.Abs(targetFill - currentInsideFill) < 0.0001f)
            {
                return;
            }

            currentInsideFill = Mathf.Clamp01(targetFill);

            for (int i = 0; i < icons.Count; i++)
            {
                BlobIcon icon = icons[i];
                if (icon.Animation != null)
                {
                    continue;
                }

                if (icon.IsActive)
                {
                    icon.FillImage.color = activeTint;
                    icon.FillImage.fillAmount = GetActiveFillAmountForIndex(i);
                }
                else
                {
                    icon.FillImage.color = lostTint;
                    icon.FillImage.fillAmount = lostFillAmount;
                }
            }
        }

        private float GetActiveFillAmountForIndex(int index)
        {
            if (displayedHealth <= 0)
            {
                return lostFillAmount;
            }

            int lastActiveIndex = displayedHealth - 1;
            if (index < lastActiveIndex)
            {
                return 1f;
            }

            if (index == lastActiveIndex)
            {
                return currentInsideFill;
            }

            return 1f;
        }

        private void StopAllIconAnimations()
        {
            for (int i = 0; i < icons.Count; i++)
            {
                StopIconAnimation(icons[i]);
            }
        }

        private void StopIconAnimation(BlobIcon icon)
        {
            if (icon.Animation == null)
            {
                return;
            }

            StopCoroutine(icon.Animation);
            icon.Animation = null;
        }

        private static Sprite GetBlobSprite()
        {
            if (cachedBlobSprite != null)
            {
                return cachedBlobSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "HealthCircleIcon_Runtime"
            };

            float inv = 1f / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) * inv * 2f - 1f;
                    float v = (y + 0.5f) * inv * 2f - 1f;
                    float dist = Mathf.Sqrt(u * u + v * v);

                    float alpha = Mathf.Clamp01((0.95f - dist) / 0.12f);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    if (alpha <= 0.0001f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float topHighlight = Mathf.Clamp01((v + 1f) * 0.5f);
                    float sideSoft = Mathf.Clamp01(1f - Mathf.Abs(u) * 0.9f);
                    float brightness = 0.82f + topHighlight * 0.16f + sideSoft * 0.05f;

                    Color c = new(brightness, brightness, brightness, alpha);
                    texture.SetPixel(x, y, c);
                }
            }

            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            cachedBlobSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            cachedBlobSprite.name = "HealthCircleIconSprite_Runtime";
            return cachedBlobSprite;
        }
    }
}
