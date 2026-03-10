using UnityEngine;
using UnityEngine.UI;

namespace WaterBlob
{
    /// <summary>
    /// This script automatically spawns the Water HUD at runtime if it's missing.
    /// It fulfills the "do it yourself" request by handling the UI hierarchy creation procedurally.
    /// </summary>
    public static class WaterHUDAutoSpawner
    {
        private const string AuthoredHudResourcePath = "UI/WaterHUD";
        private const string DropletSpriteResourcePath = "WaterDropletIcon";
        private const string GradientSpriteResourcePath = "WaterLiquidGradient";

        private static bool loggedMissingPrefab;
        private static bool loggedMissingSprites;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawnIfMissing()
        {
            if (EnsureSingleHudInstance())
            {
                return;
            }

            Canvas canvas = EnsureCanvas();
            if (canvas == null)
            {
                Debug.LogError("[WaterHUDAutoSpawner] Could not ensure a Canvas for HUD bootstrap.");
                return;
            }

            if (TryUseAuthoredHUD(canvas))
            {
                return;
            }

            SpawnProceduralFallback(canvas);
        }

        private static bool EnsureSingleHudInstance()
        {
            WaterResourceUI[] huds = Object.FindObjectsByType<WaterResourceUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (huds.Length == 0) return false;

            if (huds.Length > 1)
            {
                for (int i = 1; i < huds.Length; i++)
                {
                    if (huds[i] != null)
                    {
                        Object.Destroy(huds[i].gameObject);
                    }
                }
                Debug.LogWarning($"[WaterHUDAutoSpawner] Found {huds.Length} HUD instances. Kept one and removed duplicates.");
            }

            return true;
        }

        private static Canvas EnsureCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) return canvas;

            GameObject canvasObj = new GameObject("HUD_Canvas_AutoSpawn");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[WaterHUDAutoSpawner] No Canvas found. Created fallback HUD canvas.");
            return canvas;
        }

        private static bool TryUseAuthoredHUD(Canvas canvas)
        {
            GameObject authoredPrefab = Resources.Load<GameObject>(AuthoredHudResourcePath);
            if (authoredPrefab == null)
            {
                if (!loggedMissingPrefab)
                {
                    Debug.LogWarning($"[WaterHUDAutoSpawner] Authored HUD prefab not found at Resources path '{AuthoredHudResourcePath}'. Falling back to procedural build.");
                    loggedMissingPrefab = true;
                }
                return false;
            }

            GameObject hudInstance = Object.Instantiate(authoredPrefab, canvas.transform, false);
            hudInstance.name = "WaterHUD_Auto";

            WaterResourceUI ui = hudInstance.GetComponent<WaterResourceUI>();
            if (ui == null)
            {
                Debug.LogError("[WaterHUDAutoSpawner] Authored HUD prefab is missing WaterResourceUI. Using procedural fallback instead.");
                Object.Destroy(hudInstance);
                return false;
            }

            TryConfigureVisualsFromHierarchy(ui, hudInstance.transform);
            Debug.Log("[WaterHUDAutoSpawner] Spawned authored HUD prefab.");
            return true;
        }

        private static void SpawnProceduralFallback(Canvas canvas)
        {
            Sprite dropletSprite = Resources.Load<Sprite>(DropletSpriteResourcePath);
            Sprite gradientSprite = Resources.Load<Sprite>(GradientSpriteResourcePath);

            if ((dropletSprite == null || gradientSprite == null) && !loggedMissingSprites)
            {
                Debug.LogWarning($"[WaterHUDAutoSpawner] Missing fallback sprite(s). Expected '{DropletSpriteResourcePath}' and '{GradientSpriteResourcePath}' in Resources.");
                loggedMissingSprites = true;
            }

            GameObject hudRoot = new GameObject("WaterHUD_Auto", typeof(RectTransform));
            hudRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = hudRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(50f, -50f);
            rootRect.sizeDelta = new Vector2(100f, 100f);

            GameObject vesselObj = new GameObject("VesselOutline", typeof(RectTransform));
            vesselObj.transform.SetParent(hudRoot.transform, false);
            Image vesselImage = vesselObj.AddComponent<Image>();
            vesselImage.sprite = dropletSprite;
            vesselImage.color = Color.white;
            vesselImage.preserveAspect = true;
            RectTransform vesselRect = vesselObj.GetComponent<RectTransform>();
            vesselRect.anchorMin = Vector2.zero;
            vesselRect.anchorMax = Vector2.one;
            vesselRect.sizeDelta = Vector2.zero;

            GameObject maskObj = new GameObject("DropletMask", typeof(RectTransform));
            maskObj.transform.SetParent(hudRoot.transform, false);
            Image maskImage = maskObj.AddComponent<Image>();
            maskImage.sprite = dropletSprite;
            maskImage.preserveAspect = true;
            Mask maskComponent = maskObj.AddComponent<Mask>();
            maskComponent.showMaskGraphic = false;
            RectTransform maskRect = maskObj.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.sizeDelta = new Vector2(-6f, -6f);

            GameObject liqObj = new GameObject("LiquidGradient", typeof(RectTransform));
            liqObj.transform.SetParent(maskObj.transform, false);
            Image liqImage = liqObj.AddComponent<Image>();
            liqImage.sprite = gradientSprite;
            liqImage.color = Color.white;
            RectTransform liqRect = liqObj.GetComponent<RectTransform>();
            liqRect.anchorMin = Vector2.zero;
            liqRect.anchorMax = Vector2.one;
            liqRect.pivot = new Vector2(0.5f, 0f);
            liqRect.sizeDelta = Vector2.zero;

            WaterResourceUI ui = hudRoot.AddComponent<WaterResourceUI>();
            ui.ConfigureVisuals(null, vesselImage, liqRect);
            Debug.Log("[WaterHUDAutoSpawner] Spawned procedural fallback HUD.", hudRoot);
        }

        private static void TryConfigureVisualsFromHierarchy(WaterResourceUI ui, Transform root)
        {
            Image fill = FindImageByName(root, "WaterFill");
            Image icon = FindImageByName(root, "VesselOutline");
            RectTransform liquid = FindRectByName(root, "LiquidGradient");

            if (icon == null)
            {
                Image[] images = root.GetComponentsInChildren<Image>(true);
                if (images.Length > 0) icon = images[0];
            }

            ui.ConfigureVisuals(fill, icon, liquid);
        }

        private static Image FindImageByName(Transform root, string name)
        {
            Transform[] nodes = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].name == name)
                {
                    return nodes[i].GetComponent<Image>();
                }
            }
            return null;
        }

        private static RectTransform FindRectByName(Transform root, string name)
        {
            Transform[] nodes = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].name == name)
                {
                    return nodes[i] as RectTransform;
                }
            }
            return null;
        }
    }
}
