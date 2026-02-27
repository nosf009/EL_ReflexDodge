using UnityEngine;
using UnityEditor;
using System.IO;

namespace Kiqqi.EditorTools
{
    public static class KiqqiGuidRegenerator
    {
        [MenuItem("Kiqqi/Tools/Regenerate ALL GUIDs")]
        public static void RegenerateAllGuids()
        {
            string rootPath = Application.dataPath;
            if (!EditorUtility.DisplayDialog(
                "Regenerate ALL GUIDs",
                "This will assign NEW GUIDs to every asset under:\n\n" +
                rootPath +
                "\n\nUse ONLY when you are preparing a unique game build or export.\n\nContinue?",
                "Yes, regenerate", "Cancel"))
            {
                return;
            }

            int changedCount = 0;
            string[] metaFiles = Directory.GetFiles(rootPath, "*.meta", SearchOption.AllDirectories);

            foreach (var metaPath in metaFiles)
            {
                try
                {
                    string[] lines = File.ReadAllLines(metaPath);
                    bool modified = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("guid: "))
                        {
                            lines[i] = "guid: " + System.Guid.NewGuid().ToString("N");
                            modified = true;
                            break;
                        }
                    }

                    if (modified)
                    {
                        File.WriteAllLines(metaPath, lines);
                        changedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[KiqqiGuidRegenerator] Failed on {metaPath}: {ex.Message}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[KiqqiGuidRegenerator] Regenerated {changedCount} GUIDs under Assets/");
        }
    }
}
