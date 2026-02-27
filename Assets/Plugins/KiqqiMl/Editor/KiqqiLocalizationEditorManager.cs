using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

#if TMP_PRESENT
using TMPro;
#endif

namespace Kiqqi.Localization.Editor
{
    public class KiqqiLocalizationEditorManager : EditorWindow
    {
        private Vector2 scrollPosition;
        private Vector2 ignoredScroll;

        private bool scanned = false;
        private string searchQuery = "";

        private List<KiqqiLocalizedText> localizedItems = new List<KiqqiLocalizedText>();
        private List<Component> unlocalizedItems = new List<Component>();

        // Group by view root Transform, not string name
        private Dictionary<Transform, List<KiqqiLocalizedText>> grouped =
            new Dictionary<Transform, List<KiqqiLocalizedText>>();

        private HashSet<string> ignoredGuids = new HashSet<string>();
        private bool showIgnored = false;

        [MenuItem("Kiqqi/Localization/Localization Manager")]
        public static void ShowWindow()
        {
            GetWindow<KiqqiLocalizationEditorManager>("Kiqqi Localization Manager");
        }

        private void OnGUI()
        {
            if (ignoredGuids.Count == 0)
                LoadIgnored();

            DrawToolbar();
            DrawIgnoredToggle();

            if (!scanned)
            {
                EditorGUILayout.HelpBox("Press Scan Scene to detect all UI labels.", MessageType.Info);
                return;
            }

            DrawSearchBar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawLocalizedGroups();
            EditorGUILayout.Space(20);
            DrawUnlocalized();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(20);
            DrawIgnoredListSection();

            EditorGUILayout.Space(20);
            if (localizedItems.Count > 0)
            {
                if (GUILayout.Button("Export All Localized Texts To JSON", GUILayout.Height(30)))
                    ExportUnifiedJson();
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Scan Scene", EditorStyles.toolbarButton, GUILayout.Width(120)))
                ScanSceneForTexts();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawIgnoredToggle()
        {
            showIgnored = GUILayout.Toggle(showIgnored, "Show Ignored List", GUILayout.Width(200));
        }

        private void DrawSearchBar()
        {
            GUILayout.Space(10);
            searchQuery = EditorGUILayout.TextField("Search", searchQuery);
            GUILayout.Space(10);
        }

        private void DrawLocalizedGroups()
        {
            EditorGUILayout.LabelField("Localized Items", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            foreach (var kvp in grouped)
            {
                Transform root = kvp.Key;

                string rootName = root != null ? root.name : "Ungrouped";
                EditorGUILayout.LabelField(rootName, EditorStyles.boldLabel);

                foreach (var item in kvp.Value)
                {
                    if (item == null) continue;
                    if (IsIgnored(item)) continue;
                    if (!PassesSearch(item)) continue;

                    EditorGUILayout.BeginHorizontal();

                    GUI.color = item.isDynamic ? new Color(0.9f, 0.8f, 0.4f) : Color.white;
                    GUILayout.Label(GetPrettyPath(item.transform), GUILayout.Width(250));
                    GUI.color = Color.white;

                    GUILayout.Label(item.localizationKey, GUILayout.Width(180));

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        SelectObject(item.gameObject);

                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        RemoveLocalizationComponent(item);

                    if (GUILayout.Button("Ignore", GUILayout.Width(60)))
                        QueueIgnore(item);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(10);
            }
        }

        private void DrawUnlocalized()
        {
            EditorGUILayout.LabelField("Unlocalized Items", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (unlocalizedItems.Count == 0)
            {
                EditorGUILayout.HelpBox("No unlocalized labels found.", MessageType.Info);
                return;
            }

            foreach (var item in unlocalizedItems)
            {
                if (item == null) continue;
                if (IsIgnored(item)) continue;
                if (!PassesSearch(item)) continue;

                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(GetPrettyPath(item.transform), GUILayout.Width(250));

                if (GUILayout.Button("Add Static", GUILayout.Width(100)))
                    AddLocalizationComponent(item.gameObject, false);

                if (GUILayout.Button("Add Dynamic", GUILayout.Width(100)))
                    AddLocalizationComponent(item.gameObject, true);

                if (GUILayout.Button("Ignore", GUILayout.Width(80)))
                    QueueIgnore(item);

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawIgnoredListSection()
        {
            if (!showIgnored)
                return;

            EditorGUILayout.LabelField("Ignored Items", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (ignoredGuids.Count == 0)
            {
                EditorGUILayout.HelpBox("No ignored items.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Unignore All", GUILayout.Width(120)))
                QueueUnignoreAll();

            ignoredScroll = EditorGUILayout.BeginScrollView(ignoredScroll, GUILayout.Height(200));

            foreach (var guid in ignoredGuids.ToList())
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(guid, GUILayout.Width(350));

                if (GUILayout.Button("Unignore", GUILayout.Width(100)))
                    QueueUnignore(guid);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private bool PassesSearch(Object obj)
        {
            if (string.IsNullOrEmpty(searchQuery))
                return true;

            string path = GetPrettyPath(((Component)obj).transform);
            string key = obj is KiqqiLocalizedText lt ? lt.localizationKey : "";
            string name = ((Component)obj).name;

            searchQuery = searchQuery.ToLower();

            if (path.ToLower().Contains(searchQuery)) return true;
            if (key.ToLower().Contains(searchQuery)) return true;
            if (name.ToLower().Contains(searchQuery)) return true;

            return false;
        }

        // =================================================
        // SCANNING + HIERARCHY ORDER SORTING + NO DEDUPE
        // =================================================
        private void ScanSceneForTexts()
        {
            localizedItems.Clear();
            unlocalizedItems.Clear();
            grouped.Clear();

            var found = new List<Component>();

#if UNITY_2023_1_OR_NEWER
            found.AddRange(Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None));
#else
            found.AddRange(Object.FindObjectsOfType<Text>(true));
#endif

#if TMP_PRESENT
#if UNITY_2023_1_OR_NEWER
            found.AddRange(Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None));
#else
            found.AddRange(Object.FindObjectsOfType<TextMeshProUGUI>(true));
#endif
#endif

            // Hierarchy-accurate sort
            found = found.OrderBy(f => GetHierarchyIndexChain(f.transform), new HierarchyIndexComparer()).ToList();

            foreach (var comp in found)
            {
                if (comp.TryGetComponent(out KiqqiLocalizedText loc))
                {
                    if (!IsIgnored(loc))
                        localizedItems.Add(loc);
                }
                else
                {
                    if (!IsIgnored(comp))
                        unlocalizedItems.Add(comp);
                }
            }

            BuildGroups();

            scanned = true;
            Repaint();
        }

        private List<int> GetHierarchyIndexChain(Transform t)
        {
            var list = new List<int>();
            Transform current = t;

            while (current != null)
            {
                list.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            list.Reverse();
            return list;
        }

        private class HierarchyIndexComparer : IComparer<List<int>>
        {
            public int Compare(List<int> a, List<int> b)
            {
                int min = Mathf.Min(a.Count, b.Count);
                for (int i = 0; i < min; i++)
                {
                    if (a[i] < b[i]) return -1;
                    if (a[i] > b[i]) return 1;
                }
                return a.Count.CompareTo(b.Count);
            }
        }

        private void BuildGroups()
        {
            grouped.Clear();

            foreach (var item in localizedItems)
            {
                Transform root = GetViewRoot(item.transform);

                if (!grouped.ContainsKey(root))
                    grouped[root] = new List<KiqqiLocalizedText>();

                grouped[root].Add(item);
            }

            // Sort groups by actual hierarchy order of root
            grouped = grouped
                .OrderBy(g => GetHierarchyIndexChain(g.Key), new HierarchyIndexComparer())
                .ToDictionary(k => k.Key, v => v.Value);
        }

        private Transform GetViewRoot(Transform t)
        {
            Transform cur = t;
            while (cur.parent != null)
            {
                if (cur.parent.GetComponent<Canvas>() != null)
                    return cur;
                cur = cur.parent;
            }
            return t;
        }

        // =================================================
        // DISPLAY HELPERS
        // =================================================
        private string GetPrettyPath(Transform t)
        {
            string full = t.GetHierarchyPath(false, 0);
            string[] parts = full.Split('/');

            // Remove Canvas from display, but NOT from internal logic
            if (parts.Length > 1 && parts[0].ToLower().Contains("canvas"))
                parts = parts.Skip(1).ToArray();

            string result = "";
            for (int i = 0; i < parts.Length; i++)
            {
                string prefix = new string(' ', i * 2);
                result += prefix + parts[i];
                if (i < parts.Length - 1)
                    result += "\n";
            }

            return result;
        }

        // =================================================
        // ACTIONS
        // =================================================
        private void AddLocalizationComponent(GameObject go, bool dynamic)
        {
            Undo.RecordObject(go, "Add KiqqiLocalizedText");
            var comp = go.AddComponent<KiqqiLocalizedText>();
            comp.isDynamic = dynamic;
            comp.localizationKey = go.name.ToLowerInvariant().Replace(" ", "_");

            EditorUtility.SetDirty(go);
            Selection.activeGameObject = go;

            EditorApplication.delayCall += () =>
            {
                ScanSceneForTexts();
                Repaint();
            };
        }

        private void RemoveLocalizationComponent(KiqqiLocalizedText comp)
        {
            if (comp == null) return;

            var go = comp.gameObject;
            Undo.DestroyObjectImmediate(comp);
            EditorUtility.SetDirty(go);

            EditorApplication.delayCall += () =>
            {
                ScanSceneForTexts();
                Repaint();
            };
        }

        private void QueueIgnore(Object obj)
        {
            EditorApplication.delayCall += () =>
            {
                ignoredGuids.Add(GetObjectGuid(obj));
                SaveIgnored();
                ScanSceneForTexts();
                Repaint();
            };
        }

        private void QueueUnignore(string guid)
        {
            EditorApplication.delayCall += () =>
            {
                ignoredGuids.Remove(guid);
                SaveIgnored();
                ScanSceneForTexts();
                Repaint();
            };
        }

        private void QueueUnignoreAll()
        {
            EditorApplication.delayCall += () =>
            {
                ignoredGuids.Clear();
                SaveIgnored();
                ScanSceneForTexts();
                Repaint();
            };
        }

        private bool IsIgnored(Object obj)
        {
            return ignoredGuids.Contains(GetObjectGuid(obj));
        }

        private string GetObjectGuid(Object obj)
        {
            if (obj == null) return "";

            string path = AssetDatabase.GetAssetPath(obj);
            string guid = "";

            if (!string.IsNullOrEmpty(path))
                guid = AssetDatabase.AssetPathToGUID(path);

            // Ensure unique even for scene objects
            guid = guid + "_" + obj.name;

            return guid;
        }

        private void LoadIgnored()
        {
            string data = EditorPrefs.GetString("KiqqiLoc_Ignored", "");
            ignoredGuids = new HashSet<string>(data.Split('|').Where(s => !string.IsNullOrEmpty(s)));
        }

        private void SaveIgnored()
        {
            string data = string.Join("|", ignoredGuids);
            EditorPrefs.SetString("KiqqiLoc_Ignored", data);
        }

        private void SelectObject(GameObject go)
        {
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        // =================================================
        // EXPORT (same logic as before, dedupe by key)
        // =================================================
        private void ExportUnifiedJson()
        {
            if (localizedItems.Count == 0)
            {
                EditorUtility.DisplayDialog("Kiqqi Localization", "No localized texts to export.", "OK");
                return;
            }

            var bundle = new KiqqiLocalizationBundle
            {
                metadata = new KiqqiLocalizationBundle.Metadata
                {
                    language = "en",
                    generatedAt = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount
                },
                entries = new List<KiqqiLocalizationBundle.Entry>()
            };

            var usedKeys = new HashSet<string>();

            foreach (var item in localizedItems)
            {
                if (item == null || string.IsNullOrEmpty(item.localizationKey))
                    continue;

                string key = item.localizationKey.Trim().ToLowerInvariant();
                string value = ExtractLabelText(item);

                if (!usedKeys.Add(key))
                    continue;

                bundle.entries.Add(new KiqqiLocalizationBundle.Entry
                {
                    key = key,
                    text = value,
                    isDynamic = item.isDynamic
                });
            }

            string folder = "Assets/LocalizationExports";
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            string path = System.IO.Path.Combine(folder, "ui_texts_en.json");
            string json = JsonUtility.ToJson(bundle, true);

            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Export Complete",
                "Exported localized texts to:\n" + path,
                "OK"
            );
        }

        private string ExtractLabelText(KiqqiLocalizedText item)
        {
#if TMP_PRESENT
            var tmp = item.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                return tmp.text;
#endif

            var ui = item.GetComponent<Text>();
            if (ui != null)
                return ui.text;

            return "";
        }
    }

    // ==========================================================
    // BUNDLE + EXTENSIONS
    // ==========================================================
    [System.Serializable]
    public class KiqqiLocalizationBundle
    {
        public Metadata metadata;
        public List<Entry> entries = new List<Entry>();

        [System.Serializable]
        public class Metadata
        {
            public string language;
            public string generatedAt;
            public int sceneCount;
        }

        [System.Serializable]
        public class Entry
        {
            public string key;
            public string text;
            public bool isDynamic;
        }
    }

    public static class TransformExtensions
    {
        public static string GetHierarchyPath(this Transform t, bool trimRoot = true, int levelsToTrim = 2)
        {
            if (t == null) return "";

            string path = t.name;
            Transform p = t.parent;

            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }

            if (!trimRoot)
                return path;

            string[] parts = path.Split('/');

            // Preserve actual structure internally (Canvas not removed here)
            if (parts.Length > levelsToTrim)
                return string.Join("/", parts.Skip(levelsToTrim));

            return string.Join("/", parts.Skip(1));
        }
    }
}
