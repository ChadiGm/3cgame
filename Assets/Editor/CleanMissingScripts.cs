using UnityEngine;
using UnityEditor;

public class CleanMissingScripts
{
    [MenuItem("Tools/Clean Missing Scripts In Scene")]
    public static void CleanAllMissingScripts()
    {
        int totalRemoved = 0;
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removedCount > 0)
            {
                totalRemoved += removedCount;
                EditorUtility.SetDirty(go);
            }
        }
        Debug.Log($"Removed {totalRemoved} missing scripts from Scene GameObjects.");
    }
}
