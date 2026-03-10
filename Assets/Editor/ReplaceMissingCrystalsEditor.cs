using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility that finds all GameObjects in the active scene whose prefab
/// instance is missing (broken references) and replaces them with a simple
/// triangular prefab.  The new prefab is created automatically if it does not
/// already exist at <c>Assets/Prefabs/TriangleCrystal.prefab</c>.
/// </summary>
public static class ReplaceMissingCrystalsEditor
{
    private const string prefabPath = "Assets/Prefabs/TriangleCrystal.prefab";
    private const string meshPath = "Assets/Prefabs/TriangleCrystalMesh.asset";

    [MenuItem("Tools/Replace Missing Crystals")] 
    public static void ReplaceMissing()
    {
        // always recreate the prefab and associated mesh asset; this keeps
        // the logic simple and guarantees the mesh is assigned even if the
        // file was previously empty.
        GameObject trianglePrefab = CreateTrianglePrefab();
        Debug.Log("Created or refreshed triangle prefab at " + prefabPath);

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogError("No active scene loaded");
            return;
        }
        Debug.Log("Active scene name: " + scene.name + " path: " + scene.path);

        // if we already spawned triangle prefabs earlier but they lost their mesh,
        // repair or replace them before we look for missing crystal prefabs.
        int fixedCount = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            fixedCount += FixEmptyTriangles(root.transform, trianglePrefab);

        var replacements = new System.Collections.Generic.List<TransformData>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CollectMissing(root.transform, replacements);
        }

        // destroy old broken instances
        foreach (var data in replacements)
        {
            if (data.go != null)
                Object.DestroyImmediate(data.go);
        }

        // instantiate new triangles
        foreach (var data in replacements)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(trianglePrefab, scene);
            inst.transform.position = data.position;
            inst.transform.rotation = data.rotation;
            inst.transform.localScale = data.scale;
            inst.name = "TriangleCrystal";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Replaced {replacements.Count} missing prefab instances with triangle objects.");
        if (fixedCount > 0)
            Debug.Log($"Fixed {fixedCount} existing triangle objects.");

        // save the scene so that created triangles (and any other changes) are
        // written to disk; otherwise the YAML file will still reflect the previous
        // state and subsequent runs won't show any replacements.
        if (!string.IsNullOrEmpty(scene.path))
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Scene saved.");
        }

        // attempt to clean the scene YAML by stripping any leftover GUID references that
        // point to the missing prefab asset.  This is a crude but effective way to
        // remove stray PrefabInstance blocks that may persist after deleting the
        // GameObjects above.  Unity will normally clear these on save, but doing it
        // manually prevents future load warnings.
        CleanSceneFile(scene.path, "457eca26206b82d4abb8b1726747e9c3");
    }

    private struct TransformData
    {
        public GameObject go;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    /// <summary>
    /// Strips all lines containing <paramref name="guid"/> from the scene file
    /// at <paramref name="scenePath"/>.  This helps remove leftover references to
    /// a missing prefab asset so the scene can load cleanly.
    /// </summary>
    private static void CleanSceneFile(string scenePath, string guid)
    {
        if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
            return;

        string[] lines = System.IO.File.ReadAllLines(scenePath);
        using (var writer = new System.IO.StreamWriter(scenePath, false))
        {
            foreach (string line in lines)
            {
                if (line.Contains(guid))
                    continue;
                writer.WriteLine(line);
            }
        }
    }

    private static void CollectMissing(Transform parent, System.Collections.Generic.List<TransformData> list)
    {
        GameObject go = parent.gameObject;
        PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(go);
        if (status == PrefabInstanceStatus.MissingAsset)
        {
            list.Add(new TransformData
            {
                go = go,
                position = go.transform.position,
                rotation = go.transform.rotation,
                scale = go.transform.localScale
            });
        }

        // recurse
        foreach (Transform child in parent)
            CollectMissing(child, list);
    }

    /// <summary>
    /// Ensure there is a mesh asset for the triangle and return it.  Creates
    /// a new asset if none exists at <see cref="meshPath"/>.
    /// </summary>
    private static Mesh GetOrCreateTriangleMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh != null)
            return mesh;

        // create simple triangle mesh and save it as an asset
        mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(0, 0.5f, 0),
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0)
        };
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();

        string folder = System.IO.Path.GetDirectoryName(meshPath);
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    /// <summary>
    /// Walks the hierarchy looking for GameObjects named "TriangleCrystal" that
    /// have a missing mesh and replaces/reassigns them so they're visible.
    /// </summary>
    private static int FixEmptyTriangles(Transform parent, GameObject prefab)
    {
        int fixedCount = 0;
        GameObject go = parent.gameObject;
        if (go.name == "TriangleCrystal")
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                // destroy and re‑instantiate the prefab at the same transform
                Vector3 pos = go.transform.position;
                Quaternion rot = go.transform.rotation;
                Vector3 scale = go.transform.localScale;
                Object.DestroyImmediate(go);

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
                inst.transform.position = pos;
                inst.transform.rotation = rot;
                inst.transform.localScale = scale;
                inst.name = "TriangleCrystal";
                return 1; // one fixed
            }
        }
        // recurse
        foreach (Transform child in parent)
            fixedCount += FixEmptyTriangles(child, prefab);
        return fixedCount;
    }

    private static GameObject CreateTrianglePrefab()
    {
        // create folder if needed
        string folder = System.IO.Path.GetDirectoryName(prefabPath);
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject temp = new GameObject("TriangleCrystalPrefab");
        MeshFilter mf = temp.AddComponent<MeshFilter>();
        MeshRenderer mr = temp.AddComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        mf.sharedMesh = GetOrCreateTriangleMesh();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);
        return prefab;
    }
}
