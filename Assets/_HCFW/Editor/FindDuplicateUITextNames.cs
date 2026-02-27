using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public static class FindDuplicateUITextNames
{
    [MenuItem("Kiqqi/Tools/Find Duplicate UI Text Names")]
    public static void FindDuplicates()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogWarning("No active scene loaded.");
            return;
        }

        // Collect all GameObjects that have a UI text component (Unity UI or TMP)
        var uiObjects = new List<GameObject>();

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var go in GetAllChildrenRecursive(root))
            {
                if (go.GetComponent<Text>() != null)
                    uiObjects.Add(go);
#if TMP_PRESENT
                else if (go.GetComponent<TextMeshProUGUI>() != null)
                    uiObjects.Add(go);
#endif
            }
        }

        // Group by GameObject name
        var duplicates = uiObjects
            .GroupBy(go => go.name)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count == 0)
        {
            Debug.Log("No duplicate UI text GameObject names found.");
            EditorUtility.DisplayDialog("Check Complete", "No duplicate UI text names found in this scene.", "OK");
            return;
        }

        Debug.LogWarning($"Found {duplicates.Count} duplicated UI text name groups:\n");

        foreach (var group in duplicates)
        {
            string log = $"Name: \"{group.Key}\" ({group.Count()} objects)\n";
            foreach (var go in group)
            {
                string path = go.transform.GetHierarchyPath();
                log += $"   - {path}\n";
            }
            Debug.LogWarning(log);
        }

        EditorUtility.DisplayDialog(
            "Duplicate UI Texts Found",
            $"Found {duplicates.Count} duplicated UI text name groups.\nSee Console for details.",
            "OK");
    }

    // Recursively collect all GameObjects in hierarchy
    private static IEnumerable<GameObject> GetAllChildrenRecursive(GameObject root)
    {
        yield return root;
        foreach (Transform child in root.transform)
        {
            foreach (var c in GetAllChildrenRecursive(child.gameObject))
                yield return c;
        }
    }
}

// ------------------------------------------------------
// Helper extension for readable hierarchy paths
// ------------------------------------------------------
public static class TransformPathExtensions
{
    public static string GetHierarchyPath(this Transform t)
    {
        if (t == null) return "";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
