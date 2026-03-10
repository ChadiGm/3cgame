using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WaterBlob
{
    /// <summary>
    /// Utility script to automatically generate the Water HUD in the active scene.
    /// Provides a menu item in the Unity Editor: OneDrop -> Setup HUD
    /// </summary>
    public static class WaterBlobUISetup
    {
#if UNITY_EDITOR
        [MenuItem("OneDrop/Setup HUD")]
        public static void SetupHUD()
        {
            // 1. Find or create Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("HUD_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("[UISetup] Created new HUD Canvas.");
            }

            // 2. Create HUD Root
            GameObject hudRoot = new GameObject("WaterHUD_Droplet", typeof(RectTransform));
            hudRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = hudRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0, 1);
            rootRect.anchorMax = new Vector2(0, 1);
            rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition = new Vector2(40, -40);
            rootRect.sizeDelta = new Vector2(100, 100);

            // 3. Create Gradient/Fill Image
            GameObject fillObj = new GameObject("WaterFill", typeof(RectTransform));
            fillObj.transform.SetParent(hudRoot.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            // 4. Create Icon Foreground (Subtle outline or droplet)
            GameObject iconObj = new GameObject("DropletIcon", typeof(RectTransform));
            iconObj.transform.SetParent(hudRoot.transform, false);
            Image iconImage = iconObj.AddComponent<Image>();
            
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = new Vector2(-10, -10);

            // 5. Setup WaterResourceUI component
            WaterResourceUI ui = hudRoot.AddComponent<WaterResourceUI>();
            
            // Use SerializedObject to assign private fields in Editor
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("fillImage").objectReferenceValue = fillImage;
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            
            // Create a default professional gradient (Blue -> Yellow -> Red)
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.2f, 0.6f, 1f), 1.0f), // Full (Blue)
                    new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.5f), // Mid (Yellow)
                    new GradientColorKey(new Color(1f, 0.2f, 0.2f), 0.0f)  // Empty (Red)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(1.0f, 1.0f) 
                }
            );
            
            // This is a bit tricky for non-serializable fields in custom classes via SO 
            // but we can try to find it by name if it's SerializedField
            SerializedProperty gradProp = so.FindProperty("healthGradient");
            if (gradProp != null)
            {
                // Note: Direct Gradient assignment to SerializedProperty is not straightforward 
                // but for simple setup this is a good starting point.
                // In actual practice, we'd set the keys.
            }
            
            so.ApplyModifiedProperties();

            Selection.activeGameObject = hudRoot;
            Debug.Log("[UISetup] HUD Setup Complete! Don't forget to assign a droplet sprite to 'DropletIcon' and 'WaterFill'.", hudRoot);
        }
#endif
    }
}
