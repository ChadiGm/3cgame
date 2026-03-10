using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WaterBlob;

public static class WaterHUDPrefabBuilder
{
    private const string PrefabFolder = "Assets/UI/Resources/UI";
    private const string PrefabPath = PrefabFolder + "/WaterHUD.prefab";
    private const string DropletSpritePath = "Assets/Resources/WaterDropletIcon.png";
    private const string GradientSpritePath = "Assets/Resources/WaterLiquidGradient.png";

    [MenuItem("OneDrop/UI/Rebuild Water HUD Prefab")]
    public static void RebuildWaterHudPrefab()
    {
        EnsureFolder("Assets/UI");
        EnsureFolder("Assets/UI/Resources");
        EnsureFolder(PrefabFolder);

        Sprite dropletSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DropletSpritePath);
        Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GradientSpritePath);

        GameObject root = new GameObject("WaterHUD", typeof(RectTransform));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(50f, -50f);
        rootRect.sizeDelta = new Vector2(100f, 100f);

        GameObject vesselObj = CreateUiImage("VesselOutline", root.transform, dropletSprite, true);
        Image vesselImage = vesselObj.GetComponent<Image>();
        vesselImage.color = Color.white;

        GameObject maskObj = CreateUiImage("DropletMask", root.transform, dropletSprite, true);
        Mask maskComponent = maskObj.AddComponent<Mask>();
        maskComponent.showMaskGraphic = false;
        RectTransform maskRect = maskObj.GetComponent<RectTransform>();
        maskRect.sizeDelta = new Vector2(-6f, -6f);

        GameObject liquidObj = CreateUiImage("LiquidGradient", maskObj.transform, gradientSprite, false);
        RectTransform liquidRect = liquidObj.GetComponent<RectTransform>();
        liquidRect.pivot = new Vector2(0.5f, 0f);
        liquidObj.GetComponent<Image>().color = Color.white;

        WaterResourceUI ui = root.AddComponent<WaterResourceUI>();
        ui.ConfigureVisuals(null, vesselImage, liquidRect);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (success)
        {
            Debug.Log($"[WaterHUDPrefabBuilder] Rebuilt authored HUD prefab at '{PrefabPath}'.");
        }
        else
        {
            Debug.LogError($"[WaterHUDPrefabBuilder] Failed to save prefab at '{PrefabPath}'.");
        }
    }

    private static GameObject CreateUiImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        return obj;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int slash = path.LastIndexOf('/');
        if (slash <= 0) return;

        string parent = path.Substring(0, slash);
        string child = path.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }
}
