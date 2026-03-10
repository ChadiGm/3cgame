using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class WaterHUDPlayModeSmokeTests
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private readonly List<string> hudErrorLogs = new List<string>();

    [UnitySetUp]
    public System.Collections.IEnumerator LoadScene()
    {
        hudErrorLogs.Clear();
        Application.logMessageReceived += HandleLog;

        AsyncOperation op = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
        while (!op.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public System.Collections.IEnumerator TearDown()
    {
        Application.logMessageReceived -= HandleLog;
        yield return null;
    }

    [UnityTest]
    public System.Collections.IEnumerator AutoSpawner_CreatesSingleBoundHud()
    {
        InvokeAutoSpawner();
        yield return null;
        yield return null;

        Type hudType = GetHudType();
        Object[] huds = Object.FindObjectsByType(hudType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(huds.Length, Is.EqualTo(1), "Expected exactly one WaterResourceUI instance.");

        object hud = huds[0];
        Assert.That(GetBoolProperty(hud, "IsBound"), Is.True, "HUD should bind to OneDropWaterResource2D.");
        Assert.That(GetObjectProperty(hud, "BoundResource"), Is.Not.Null, "BoundResource should not be null.");
        Assert.That(GetBoolProperty(hud, "HasVisualTargets"), Is.True, "HUD should have at least one visual target.");
        Assert.That(hudErrorLogs.Count, Is.EqualTo(0), string.Join("\n", hudErrorLogs));
    }

    [UnityTest]
    public System.Collections.IEnumerator AutoSpawner_RemovesDuplicateHudInstances()
    {
        InvokeAutoSpawner();
        yield return null;

        Type hudType = GetHudType();
        Object[] initialHuds = Object.FindObjectsByType(hudType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        Component existingHud = initialHuds.Length > 0 ? initialHuds[0] as Component : null;
        Assert.That(existingHud, Is.Not.Null, "Expected bootstrap to spawn initial HUD.");

        Object.Instantiate(existingHud.gameObject);
        yield return null;

        InvokeAutoSpawner();
        yield return null;

        Object[] huds = Object.FindObjectsByType(hudType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(huds.Length, Is.EqualTo(1), "Duplicate HUD instances should be reduced to one.");
        Assert.That(hudErrorLogs.Count, Is.EqualTo(0), string.Join("\n", hudErrorLogs));
    }

    [UnityTest]
    public System.Collections.IEnumerator ProceduralFallback_SpawnsHudWhenInvoked()
    {
        Type hudType = GetHudType();
        Object[] existingHuds = Object.FindObjectsByType(hudType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existingHuds.Length; i++)
        {
            Component c = existingHuds[i] as Component;
            if (c != null) Object.Destroy(c.gameObject);
        }
        yield return null;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        InvokeProceduralFallback(canvas);
        yield return null;

        Object[] huds = Object.FindObjectsByType(hudType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(huds.Length, Is.EqualTo(1), "Procedural fallback should create a single HUD instance.");
        Assert.That(GetBoolProperty(huds[0], "HasVisualTargets"), Is.True, "Fallback HUD should have visual targets.");
        Assert.That(hudErrorLogs.Count, Is.EqualTo(0), string.Join("\n", hudErrorLogs));
    }

    private static void InvokeAutoSpawner()
    {
        Type spawnerType = Type.GetType("WaterBlob.WaterHUDAutoSpawner, Assembly-CSharp");
        Assert.That(spawnerType, Is.Not.Null, "Could not resolve WaterHUDAutoSpawner type.");
        MethodInfo method = spawnerType.GetMethod("AutoSpawnIfMissing", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "Could not find WaterHUDAutoSpawner.AutoSpawnIfMissing via reflection.");
        method.Invoke(null, null);
    }

    private static void InvokeProceduralFallback(Canvas canvas)
    {
        Type spawnerType = Type.GetType("WaterBlob.WaterHUDAutoSpawner, Assembly-CSharp");
        Assert.That(spawnerType, Is.Not.Null, "Could not resolve WaterHUDAutoSpawner type.");
        MethodInfo method = spawnerType.GetMethod("SpawnProceduralFallback", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "Could not find WaterHUDAutoSpawner.SpawnProceduralFallback via reflection.");
        method.Invoke(null, new object[] { canvas });
    }

    private static Type GetHudType()
    {
        Type hudType = Type.GetType("WaterBlob.WaterResourceUI, Assembly-CSharp");
        Assert.That(hudType, Is.Not.Null, "Could not resolve WaterResourceUI type.");
        return hudType;
    }

    private static bool GetBoolProperty(object instance, string propertyName)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null, $"Property '{propertyName}' not found on HUD component.");
        return (bool)property.GetValue(instance);
    }

    private static object GetObjectProperty(object instance, string propertyName)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null, $"Property '{propertyName}' not found on HUD component.");
        return property.GetValue(instance);
    }

    private void HandleLog(string condition, string stacktrace, LogType type)
    {
        bool isHudLog = condition.Contains("[WaterResourceUI]") || condition.Contains("[WaterHUDAutoSpawner]");
        bool isFailureType = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
        if (isHudLog && isFailureType)
        {
            hudErrorLogs.Add(condition);
        }
    }
}
