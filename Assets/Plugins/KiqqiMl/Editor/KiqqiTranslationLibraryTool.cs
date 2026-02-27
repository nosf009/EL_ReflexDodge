using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;

#if TMP_PRESENT
using TMPro;
#endif

namespace Kiqqi.Localization.Editor
{
    public class KiqqiTranslationLibraryTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private Vector2 libraryScrollPosition;
        
        private string libraryJsonPath = "";
        private KiqqiLocalizationBundle loadedLibrary;
        private List<LocalizedItem> sceneItems = new List<LocalizedItem>();
        
        private string searchFilter = "";
        private bool showOnlyUnmatched = false;
        private bool showOnlyMatched = false;
        private bool autoDetectPrefix = true;
        private string detectedPrefix = "";
        private float minMatchScore = 70f;
        
        private bool showCreateNewSection = false;
        private string newKeyTemplate = "";
        private string newKeyPreview = "";
        
        private GUIStyle headerStyle;
        private GUIStyle matchStyle;
        private GUIStyle noMatchStyle;
        
        [MenuItem("Kiqqi/Localization/Translation Library Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<KiqqiTranslationLibraryTool>("Translation Library Tool");
            window.minSize = new Vector2(900, 600);
        }
        
        private void OnEnable()
        {
            ScanCurrentScene();
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            DrawToolbar();
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            DrawLibraryPanel();
            DrawSceneItemsPanel();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 12;
                headerStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
            }
            
            if (matchStyle == null)
            {
                matchStyle = new GUIStyle(EditorStyles.label);
                matchStyle.normal.textColor = new Color(0.5f, 1f, 0.5f);
            }
            
            if (noMatchStyle == null)
            {
                noMatchStyle = new GUIStyle(EditorStyles.label);
                noMatchStyle.normal.textColor = new Color(1f, 0.6f, 0.4f);
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Scan Scene", EditorStyles.toolbarButton, GUILayout.Width(100)))
                ScanCurrentScene();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Load Library JSON", EditorStyles.toolbarButton, GUILayout.Width(150)))
                LoadLibraryJson();
            
            if (GUILayout.Button("Quick: Load EN Library", EditorStyles.toolbarButton, GUILayout.Width(150)))
                QuickLoadStandardLibrary("en");
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Open Multi-Lang Exporter", EditorStyles.toolbarButton, GUILayout.Width(180)))
                KiqqiMultiLanguageExporter.ShowWindow();
            
            autoDetectPrefix = GUILayout.Toggle(autoDetectPrefix, "Auto-Detect Prefix", EditorStyles.toolbarButton, GUILayout.Width(150));
            
            if (!string.IsNullOrEmpty(detectedPrefix))
            {
                GUILayout.Label($"Prefix: [{detectedPrefix}]", EditorStyles.toolbarButton, GUILayout.Width(80));
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Library Path:", GUILayout.Width(80));
            libraryJsonPath = EditorGUILayout.TextField(libraryJsonPath);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField("Search:", searchFilter, GUILayout.Width(250));
            showOnlyUnmatched = GUILayout.Toggle(showOnlyUnmatched, "Unmatched Only", GUILayout.Width(120));
            showOnlyMatched = GUILayout.Toggle(showOnlyMatched, "Matched Only", GUILayout.Width(120));
            GUILayout.Label("Min Score:", GUILayout.Width(70));
            minMatchScore = EditorGUILayout.Slider(minMatchScore, 0f, 100f, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Apply All Smart Matches (Use Library Keys)", GUILayout.Height(25)))
                ApplyAllSmartMatches(false);
            
            if (GUILayout.Button($"Remap All to '{detectedPrefix}' Prefix", GUILayout.Height(25)))
                ApplyAllSmartMatches(true);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            showCreateNewSection = EditorGUILayout.Foldout(showCreateNewSection, "CREATE NEW PREFIXED KEYS FROM LIBRARY", true);
            
            if (showCreateNewSection)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.HelpBox(
                    $"Select a library key and add your prefix '{detectedPrefix}' to create a new localization key.\n" +
                    "This allows you to map multiple game-specific prefixes to the same standard translation.",
                    MessageType.Info
                );
                
                if (loadedLibrary != null && loadedLibrary.entries != null && loadedLibrary.entries.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Select Library Key:", GUILayout.Width(120));
                    
                    string[] libraryKeys = loadedLibrary.entries.Select(e => e.key).ToArray();
                    int selectedIndex = System.Array.IndexOf(libraryKeys, newKeyTemplate);
                    if (selectedIndex == -1) selectedIndex = 0;
                    
                    int newIndex = EditorGUILayout.Popup(selectedIndex, libraryKeys);
                    newKeyTemplate = libraryKeys[newIndex];
                    
                    EditorGUILayout.EndHorizontal();
                    
                    string coreKey = StripPrefix(newKeyTemplate);
                    newKeyPreview = detectedPrefix + coreKey;
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("New Key Will Be:", GUILayout.Width(120));
                    EditorGUILayout.SelectableLabel(newKeyPreview, EditorStyles.textField, GUILayout.Height(18));
                    EditorGUILayout.EndHorizontal();
                    
                    var libraryEntry = loadedLibrary.entries.Find(e => e.key == newKeyTemplate);
                    if (libraryEntry != null)
                    {
                        EditorGUILayout.LabelField($"Translation: {libraryEntry.text}", EditorStyles.wordWrappedLabel);
                    }
                    
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("This key will point to the same translation in all language JSONs.", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawLibraryPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            
            EditorGUILayout.LabelField("TRANSLATION LIBRARY", headerStyle);
            EditorGUILayout.Space(5);
            
            if (loadedLibrary == null || loadedLibrary.entries == null || loadedLibrary.entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No library loaded. Click 'Load Library JSON' to import translations.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Loaded: {loadedLibrary.entries.Count} entries from library", MessageType.Info);
                
                libraryScrollPosition = EditorGUILayout.BeginScrollView(libraryScrollPosition);
                
                foreach (var entry in loadedLibrary.entries)
                {
                    if (!PassesFilter(entry.key, entry.text))
                        continue;
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"Key: {entry.key}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Text: {entry.text}", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField($"Dynamic: {entry.isDynamic}");
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneItemsPanel()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("SCENE LOCALIZED ITEMS", headerStyle);
            EditorGUILayout.Space(5);
            
            if (sceneItems.Count == 0)
            {
                EditorGUILayout.HelpBox("No localized items found in scene. Click 'Scan Scene'.", MessageType.Warning);
            }
            else
            {
                int matchedCount = sceneItems.Count(i => i.bestMatch != null && i.matchScore >= minMatchScore);
                int unmatchedCount = sceneItems.Count - matchedCount;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(
                    $"Total: {sceneItems.Count} items\n" +
                    $"Matched: {matchedCount} (≥{minMatchScore}% score)\n" +
                    $"Unmatched: {unmatchedCount}",
                    MessageType.Info,
                    true
                );
                
                if (matchedCount > 0)
                {
                    float matchPercentage = (float)matchedCount / sceneItems.Count * 100f;
                    EditorGUILayout.BeginVertical(GUILayout.Width(150));
                    EditorGUILayout.LabelField($"Match Rate: {matchPercentage:F1}%", EditorStyles.boldLabel);
                    Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                    EditorGUI.ProgressBar(progressRect, matchPercentage / 100f, $"{matchPercentage:F0}%");
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndHorizontal();
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                foreach (var item in sceneItems)
                {
                    if (item.component == null) continue;
                    
                    if (showOnlyUnmatched && item.bestMatch != null)
                        continue;
                    
                    if (showOnlyMatched && item.bestMatch == null)
                        continue;
                    
                    if (item.matchScore < minMatchScore && item.bestMatch != null)
                        continue;
                    
                    if (!PassesFilter(item.currentKey, item.currentText))
                        continue;
                    
                    DrawSceneItem(item);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneItem(LocalizedItem item)
        {
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = item.bestMatch != null ? new Color(0.7f, 1f, 0.7f, 0.3f) : new Color(1f, 0.8f, 0.6f, 0.3f);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetCompactPath(item.component.transform), GUILayout.Width(200));
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = item.component.gameObject;
                EditorGUIUtility.PingObject(item.component.gameObject);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Key:", GUILayout.Width(80));
            
            string newKey = EditorGUILayout.TextField(item.currentKey, GUILayout.Width(200));
            if (newKey != item.currentKey)
            {
                ApplyKeyChange(item, newKey);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"Text: {item.currentText}", EditorStyles.wordWrappedLabel);
            
            if (item.bestMatch != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("SMART MATCH FOUND:", matchStyle);
                EditorGUILayout.LabelField($"  Library Key: {item.bestMatch.key}", matchStyle);
                EditorGUILayout.LabelField($"  Translation: {item.bestMatch.text}", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField($"  Match Score: {item.matchScore:F2}%", matchStyle);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Use Library Key", GUILayout.Width(120)))
                {
                    ApplyKeyChange(item, item.bestMatch.key);
                }
                
                if (GUILayout.Button($"Remap to '{detectedPrefix}' + Core", GUILayout.Width(180)))
                {
                    string coreKey = StripPrefix(item.bestMatch.key);
                    string newRemappedKey = detectedPrefix + coreKey;
                    ApplyKeyChange(item, newRemappedKey);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("No library match found", noMatchStyle);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("MANUAL LIBRARY ASSIGNMENT:", EditorStyles.miniLabel);
            
            if (loadedLibrary != null && loadedLibrary.entries != null)
            {
                EditorGUILayout.BeginHorizontal();
                
                string[] libraryKeys = loadedLibrary.entries.Select(e => e.key).ToArray();
                int currentIndex = System.Array.IndexOf(libraryKeys, item.currentKey);
                if (currentIndex == -1) currentIndex = 0;
                
                int newIndex = EditorGUILayout.Popup("Pick Library Key:", currentIndex, libraryKeys, GUILayout.Width(400));
                
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < libraryKeys.Length)
                {
                    ApplyKeyChange(item, libraryKeys[newIndex]);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void ScanCurrentScene()
        {
            sceneItems.Clear();
            
            var components = new List<KiqqiLocalizedText>();
            
#if UNITY_2023_1_OR_NEWER
            components.AddRange(Object.FindObjectsByType<KiqqiLocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None));
#else
            components.AddRange(Object.FindObjectsOfType<KiqqiLocalizedText>(true));
#endif
            
            components = components.OrderBy(c => GetHierarchyIndexChain(c.transform), new HierarchyIndexComparer()).ToList();
            
            foreach (var comp in components)
            {
                string text = ExtractText(comp);
                
                var item = new LocalizedItem
                {
                    component = comp,
                    currentKey = comp.localizationKey,
                    currentText = text,
                    strippedKey = StripPrefix(comp.localizationKey)
                };
                
                sceneItems.Add(item);
            }
            
            if (autoDetectPrefix && sceneItems.Count > 0)
            {
                DetectCommonPrefix();
            }
            
            PerformSmartMatching();
            
            Repaint();
        }
        
        private void LoadLibraryJson()
        {
            string path = EditorUtility.OpenFilePanel("Select Translation Library JSON", "Assets/Resources", "json");
            
            if (string.IsNullOrEmpty(path))
                return;
            
            libraryJsonPath = path;
            
            try
            {
                string json = File.ReadAllText(path);
                loadedLibrary = JsonUtility.FromJson<KiqqiLocalizationBundle>(json);
                
                if (loadedLibrary == null || loadedLibrary.entries == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to parse JSON or no entries found.", "OK");
                    return;
                }
                
                EditorUtility.DisplayDialog("Success", $"Loaded {loadedLibrary.entries.Count} entries from library.", "OK");
                
                PerformSmartMatching();
                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load JSON: {ex.Message}", "OK");
            }
        }
        
        private void QuickLoadStandardLibrary(string lang)
        {
            string relativePath = $"Assets/Resources/ref-ml/standard_library_{lang}.json";
            
            if (!File.Exists(relativePath))
            {
                EditorUtility.DisplayDialog("Error", $"Standard library not found at:\n{relativePath}", "OK");
                return;
            }
            
            libraryJsonPath = relativePath;
            
            try
            {
                string json = File.ReadAllText(relativePath);
                loadedLibrary = JsonUtility.FromJson<KiqqiLocalizationBundle>(json);
                
                if (loadedLibrary == null || loadedLibrary.entries == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to parse standard library JSON.", "OK");
                    return;
                }
                
                Debug.Log($"[TranslationLibrary] Loaded standard {lang.ToUpper()} library: {loadedLibrary.entries.Count} entries");
                
                PerformSmartMatching();
                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load standard library: {ex.Message}", "OK");
            }
        }
        
        private void PerformSmartMatching()
        {
            if (loadedLibrary == null || loadedLibrary.entries == null)
                return;
            
            foreach (var item in sceneItems)
            {
                item.bestMatch = null;
                item.matchScore = 0f;
                
                foreach (var entry in loadedLibrary.entries)
                {
                    float score = CalculateMatchScore(item.strippedKey, StripPrefix(entry.key), item.currentText, entry.text);
                    
                    if (score > item.matchScore)
                    {
                        item.matchScore = score;
                        item.bestMatch = entry;
                    }
                }
            }
        }
        
        private float CalculateMatchScore(string strippedKey1, string strippedKey2, string text1, string text2)
        {
            float score = 0f;
            
            if (strippedKey1 == strippedKey2)
                score += 70f;
            else if (strippedKey1.Contains(strippedKey2) || strippedKey2.Contains(strippedKey1))
                score += 40f;
            else if (LevenshteinDistance(strippedKey1, strippedKey2) <= 3)
                score += 30f;
            
            if (!string.IsNullOrEmpty(text1) && !string.IsNullOrEmpty(text2))
            {
                if (text1.ToLower() == text2.ToLower())
                    score += 30f;
                else if (text1.ToLower().Contains(text2.ToLower()) || text2.ToLower().Contains(text1.ToLower()))
                    score += 15f;
            }
            
            return score;
        }
        
        private string StripPrefix(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";
            
            if (key.Length <= 2)
                return key;
            
            for (int prefixLen = 2; prefixLen <= 3; prefixLen++)
            {
                if (key.Length > prefixLen)
                {
                    string potentialPrefix = key.Substring(0, prefixLen);
                    string rest = key.Substring(prefixLen);
                    
                    if (char.IsLower(potentialPrefix[0]) && char.IsLower(rest[0]))
                    {
                        return rest;
                    }
                }
            }
            
            return key;
        }
        
        private void DetectCommonPrefix()
        {
            if (sceneItems.Count == 0)
                return;
            
            var prefixCounts = new Dictionary<string, int>();
            
            foreach (var item in sceneItems)
            {
                if (string.IsNullOrEmpty(item.currentKey) || item.currentKey.Length < 3)
                    continue;
                
                for (int len = 2; len <= 3; len++)
                {
                    if (item.currentKey.Length > len)
                    {
                        string prefix = item.currentKey.Substring(0, len);
                        
                        if (char.IsLower(prefix[0]))
                        {
                            if (!prefixCounts.ContainsKey(prefix))
                                prefixCounts[prefix] = 0;
                            prefixCounts[prefix]++;
                        }
                    }
                }
            }
            
            if (prefixCounts.Count > 0)
            {
                detectedPrefix = prefixCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            }
        }
        
        private void ApplyKeyChange(LocalizedItem item, string newKey)
        {
            if (item.component == null)
                return;
            
            Undo.RecordObject(item.component, "Change Localization Key");
            item.component.localizationKey = newKey;
            item.currentKey = newKey;
            item.strippedKey = StripPrefix(newKey);
            EditorUtility.SetDirty(item.component.gameObject);
            
            PerformSmartMatching();
            Repaint();
        }
        
        private void ApplyAllSmartMatches(bool remapToDetectedPrefix)
        {
            if (!EditorUtility.DisplayDialog(
                "Batch Apply Translations",
                $"This will change keys for {sceneItems.Count(i => i.bestMatch != null && i.matchScore >= minMatchScore)} items.\n\nAre you sure?",
                "Yes", "Cancel"))
            {
                return;
            }
            
            int appliedCount = 0;
            
            foreach (var item in sceneItems)
            {
                if (item.component == null || item.bestMatch == null)
                    continue;
                
                if (item.matchScore < minMatchScore)
                    continue;
                
                string newKey;
                
                if (remapToDetectedPrefix)
                {
                    string coreKey = StripPrefix(item.bestMatch.key);
                    newKey = detectedPrefix + coreKey;
                }
                else
                {
                    newKey = item.bestMatch.key;
                }
                
                Undo.RecordObject(item.component, "Batch Apply Translation Keys");
                item.component.localizationKey = newKey;
                item.currentKey = newKey;
                item.strippedKey = StripPrefix(newKey);
                EditorUtility.SetDirty(item.component.gameObject);
                
                appliedCount++;
            }
            
            EditorUtility.DisplayDialog("Success", $"Applied {appliedCount} translation key changes.", "OK");
            
            PerformSmartMatching();
            Repaint();
        }
        
        private string ExtractText(KiqqiLocalizedText comp)
        {
#if TMP_PRESENT
            var tmp = comp.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp.text;
#endif
            var ui = comp.GetComponent<Text>();
            if (ui != null) return ui.text;
            return "";
        }
        
        private string GetCompactPath(Transform t)
        {
            string full = GetHierarchyPath(t);
            string[] parts = full.Split('/');
            
            if (parts.Length > 1 && parts[0].ToLower().Contains("canvas"))
                parts = parts.Skip(1).ToArray();
            
            return string.Join(" > ", parts);
        }
        
        private string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";
            
            string path = t.name;
            Transform p = t.parent;
            
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            
            return path;
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
        
        private bool PassesFilter(string key, string text)
        {
            if (string.IsNullOrEmpty(searchFilter))
                return true;
            
            string filter = searchFilter.ToLower();
            
            if (key.ToLower().Contains(filter))
                return true;
            
            if (text.ToLower().Contains(filter))
                return true;
            
            return false;
        }
        
        private int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];
            
            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
            
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;
            
            for (int j = 1; j <= s2.Length; j++)
            {
                for (int i = 1; i <= s1.Length; i++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            
            return d[s1.Length, s2.Length];
        }
        
        private class LocalizedItem
        {
            public KiqqiLocalizedText component;
            public string currentKey;
            public string currentText;
            public string strippedKey;
            public KiqqiLocalizationBundle.Entry bestMatch;
            public float matchScore;
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
    }
}
