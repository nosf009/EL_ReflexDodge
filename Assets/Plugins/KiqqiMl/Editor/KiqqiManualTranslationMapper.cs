using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;

#if TMP_PRESENT
using TMPro;
#endif

namespace Kiqqi.Localization.Editor
{
    public class KiqqiManualTranslationMapper : EditorWindow
    {
        private Vector2 libraryScrollPosition;
        private Vector2 detailScrollPosition;
        
        private string loadedJsonPath = "";
        private KiqqiLocalizationBundle loadedLibrary;
        
        private Dictionary<string, KiqqiLocalizationBundle> allLanguageLibraries = new Dictionary<string, KiqqiLocalizationBundle>();
        private string[] languageCodes = { "en", "de", "it", "fr" };
        
        private List<KiqqiLocalizedText> sceneComponents = new List<KiqqiLocalizedText>();
        
        private KiqqiLocalizationBundle.Entry selectedLibraryEntry;
        private GameObject currentlySelectedInHierarchy;
        
        private GUIStyle headerStyle;
        private GUIStyle selectedEntryStyle;
        private GUIStyle normalEntryStyle;
        private GUIStyle detailBoxStyle;
        
        private string searchFilter = "";
        
        private SceneAsset targetSyncScene;
        private bool showSceneSync = true;
        
        [MenuItem("Kiqqi/Localization/Manual Translation Mapper")]
        public static void ShowWindow()
        {
            var window = GetWindow<KiqqiManualTranslationMapper>("Manual Translation Mapper");
            window.minSize = new Vector2(1000, 600);
        }
        
        private void OnEnable()
        {
            ScanScene();
        }
        
        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }
            
            if (selectedEntryStyle == null)
            {
                selectedEntryStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.8f, 0.3f)) },
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }
            
            if (normalEntryStyle == null)
            {
                normalEntryStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }
            
            if (detailBoxStyle == null)
            {
                detailBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }
        
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            UpdateHierarchySelection();
            
            DrawToolbar();
            
            EditorGUILayout.BeginHorizontal();
            
            DrawLibraryPanel();
            
            DrawDetailPanel();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void UpdateHierarchySelection()
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject.scene.IsValid())
            {
                currentlySelectedInHierarchy = Selection.activeGameObject;
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Scan Scene", EditorStyles.toolbarButton, GUILayout.Width(100)))
                ScanScene();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Load JSON", EditorStyles.toolbarButton, GUILayout.Width(100)))
                LoadJson();
            
            if (GUILayout.Button("Quick: Load en.json", EditorStyles.toolbarButton, GUILayout.Width(140)))
                QuickLoadLanguageJson("en");
            
            GUILayout.FlexibleSpace();
            
            if (loadedLibrary != null)
            {
                GUILayout.Label($"Loaded: {loadedLibrary.entries.Count} entries", EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLibraryPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(380));
            
            EditorGUILayout.LabelField("TRANSLATION LIBRARY", headerStyle);
            
            if (!string.IsNullOrEmpty(loadedJsonPath))
            {
                EditorGUILayout.LabelField($"Path: {System.IO.Path.GetFileName(loadedJsonPath)}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.Space(5);
            
            searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
            
            EditorGUILayout.Space(5);
            
            if (loadedLibrary == null || loadedLibrary.entries == null || loadedLibrary.entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No library loaded. Click 'Load JSON' or 'Quick: Load en.json'.", MessageType.Warning);
            }
            else
            {
                libraryScrollPosition = EditorGUILayout.BeginScrollView(libraryScrollPosition);
                
                foreach (var entry in loadedLibrary.entries)
                {
                    if (!PassesSearchFilter(entry))
                        continue;
                    
                    DrawLibraryEntry(entry);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLibraryEntry(KiqqiLocalizationBundle.Entry entry)
        {
            if (entry == null)
                return;
            
            bool isSelected = (selectedLibraryEntry == entry);
            GUIStyle style = isSelected ? selectedEntryStyle : normalEntryStyle;
            
            EditorGUILayout.BeginVertical(style);
            
            if (GUILayout.Button($"Key: {entry.key}", EditorStyles.boldLabel, GUILayout.Height(20)))
            {
                GUI.changed = true;
                EditorApplication.delayCall += () => OnLibraryEntrySelected(entry);
            }
            
            EditorGUILayout.LabelField($"Text: {entry.text}", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"Dynamic: {entry.isDynamic}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        private void OnLibraryEntrySelected(KiqqiLocalizationBundle.Entry entry)
        {
            selectedLibraryEntry = entry;
            Repaint();
        }
        
        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("DETAILS & SYNC", headerStyle);
            EditorGUILayout.Space(5);
            
            if (selectedLibraryEntry == null)
            {
                EditorGUILayout.HelpBox("Select an entry from the library on the left to see details.", MessageType.Info);
            }
            else
            {
                detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);
                
                DrawSelectedEntryDetails();
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSelectedEntryDetails()
        {
            EditorGUILayout.BeginVertical(detailBoxStyle);
            
            EditorGUILayout.LabelField("LIBRARY ENTRY", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Key: {selectedLibraryEntry.key}");
            EditorGUILayout.LabelField($"Text: {selectedLibraryEntry.text}", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"Dynamic: {selectedLibraryEntry.isDynamic}");
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            DrawManualMapping();
            
            EditorGUILayout.Space(10);
            
            DrawMultiLanguageValidation();
            
            EditorGUILayout.Space(10);
            
            DrawSceneSync();
        }
        
        private void DrawManualMapping()
        {
            EditorGUILayout.BeginVertical(detailBoxStyle);
            
            EditorGUILayout.LabelField("MANUAL MAPPING", EditorStyles.boldLabel);
            
            var mappedGameObject = FindGameObjectMappedToEntry(selectedLibraryEntry.key);
            
            if (mappedGameObject != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Currently Mapped To: {mappedGameObject.name}", EditorStyles.boldLabel);
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(mappedGameObject);
                    Selection.activeGameObject = mappedGameObject;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"Path: {GetGameObjectPath(mappedGameObject)}", EditorStyles.miniLabel);
                
                EditorGUILayout.Space(5);
            }
            else
            {
                EditorGUILayout.HelpBox("This library entry is not mapped to any GameObject in the scene.", MessageType.Info);
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.LabelField("Map New GameObject:", EditorStyles.boldLabel);
            
            if (currentlySelectedInHierarchy == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject in the hierarchy to map it to this entry.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Hierarchy Selection: {currentlySelectedInHierarchy.name}");
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(currentlySelectedInHierarchy);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"Path: {GetGameObjectPath(currentlySelectedInHierarchy)}", EditorStyles.miniLabel);
                
                var existingComponent = currentlySelectedInHierarchy.GetComponent<KiqqiLocalizedText>();
                
                if (existingComponent != null)
                {
                    EditorGUILayout.HelpBox($"This GameObject already has KiqqiLocalizedText with key: '{existingComponent.localizationKey}'", MessageType.Info);
                    
                    if (GUILayout.Button("Update Key to Match Library Entry", GUILayout.Height(30)))
                    {
                        var comp = existingComponent;
                        EditorApplication.delayCall += () => MapSelectedGameObject(comp);
                    }
                }
                else
                {
                    var textComponent = currentlySelectedInHierarchy.GetComponent<Text>();
#if TMP_PRESENT
                    var tmpComponent = currentlySelectedInHierarchy.GetComponent<TextMeshProUGUI>();
                    bool hasTextComponent = textComponent != null || tmpComponent != null;
#else
                    bool hasTextComponent = textComponent != null;
#endif
                    
                    if (!hasTextComponent)
                    {
                        EditorGUILayout.HelpBox("This GameObject doesn't have a Text or TextMeshProUGUI component. Add one first.", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("This GameObject doesn't have KiqqiLocalizedText. Click below to add it and map to this library entry.", MessageType.Info);
                        
                        if (GUILayout.Button("Map Selected GameObject to This Entry", GUILayout.Height(30)))
                        {
                            EditorApplication.delayCall += () => MapSelectedGameObject(null);
                        }
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private GameObject FindGameObjectMappedToEntry(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            
            foreach (var component in sceneComponents)
            {
                if (component != null && component.localizationKey.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return component.gameObject;
                }
            }
            
            return null;
        }
        
        private void MapSelectedGameObject(KiqqiLocalizedText existingComponent)
        {
            if (currentlySelectedInHierarchy == null || selectedLibraryEntry == null)
                return;
            
            KiqqiLocalizedText component = existingComponent;
            
            if (component == null)
            {
                Undo.RecordObject(currentlySelectedInHierarchy, "Add KiqqiLocalizedText");
                component = currentlySelectedInHierarchy.AddComponent<KiqqiLocalizedText>();
                Debug.Log($"[ManualMapper] Added KiqqiLocalizedText to '{currentlySelectedInHierarchy.name}'");
            }
            else
            {
                Undo.RecordObject(component, "Update KiqqiLocalizedText Key");
            }
            
            string oldKey = component.localizationKey;
            string oldName = currentlySelectedInHierarchy.name;
            
            component.localizationKey = selectedLibraryEntry.key;
            
            Undo.RecordObject(currentlySelectedInHierarchy, "Rename GameObject to Match Key");
            currentlySelectedInHierarchy.name = selectedLibraryEntry.key;
            
            EditorUtility.SetDirty(component.gameObject);
            
            Debug.Log($"[ManualMapper] Mapped '{oldName}' → '{currentlySelectedInHierarchy.name}': '{oldKey}' → '{selectedLibraryEntry.key}'");
            
            ScanScene();
            
            Repaint();
        }
        
        private void DrawMultiLanguageValidation()
        {
            EditorGUILayout.BeginVertical(detailBoxStyle);
            
            EditorGUILayout.LabelField("MULTI-LANGUAGE VALIDATION", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Load All 4 Languages for Validation", GUILayout.Height(25)))
            {
                LoadAllLanguages();
            }
            
            if (allLanguageLibraries.Count == 0)
            {
                EditorGUILayout.HelpBox("Click above to load en, de, it, fr for validation.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);
                
                bool allLanguagesHaveKey = true;
                List<string> missingLanguages = new List<string>();
                
                foreach (var lang in languageCodes)
                {
                    if (!allLanguageLibraries.ContainsKey(lang))
                    {
                        allLanguagesHaveKey = false;
                        missingLanguages.Add(lang);
                        continue;
                    }
                    
                    var library = allLanguageLibraries[lang];
                    var entry = library.entries.Find(e => e.key.ToLowerInvariant() == selectedLibraryEntry.key.ToLowerInvariant());
                    
                    if (entry == null)
                    {
                        allLanguagesHaveKey = false;
                        missingLanguages.Add(lang);
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"✓ {lang.ToUpper()}:", GUILayout.Width(40));
                        EditorGUILayout.LabelField(entry.text, EditorStyles.wordWrappedLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.Space(5);
                
                if (allLanguagesHaveKey && missingLanguages.Count == 0)
                {
                    EditorGUILayout.HelpBox("✓ All 4 languages have this key with same structure!", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"⚠ Missing in: {string.Join(", ", missingLanguages)}", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ScanScene()
        {
            sceneComponents.Clear();
            
#if UNITY_2023_1_OR_NEWER
            sceneComponents.AddRange(Object.FindObjectsByType<KiqqiLocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None));
#else
            sceneComponents.AddRange(Object.FindObjectsOfType<KiqqiLocalizedText>(true));
#endif
            
            Debug.Log($"[ManualMapper] Scanned scene: {sceneComponents.Count} localized components found");
        }
        
        private void LoadJson()
        {
            string path = EditorUtility.OpenFilePanel("Select JSON File", "Assets/Resources", "json");
            
            if (string.IsNullOrEmpty(path))
                return;
            
            loadedJsonPath = path;
            
            try
            {
                string json = File.ReadAllText(path);
                loadedLibrary = JsonUtility.FromJson<KiqqiLocalizationBundle>(json);
                
                if (loadedLibrary == null || loadedLibrary.entries == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to parse JSON or no entries found.", "OK");
                    return;
                }
                
                Debug.Log($"[ManualMapper] Loaded {loadedLibrary.entries.Count} entries from {System.IO.Path.GetFileName(path)}");
                
                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load JSON: {ex.Message}", "OK");
            }
        }
        
        private void QuickLoadLanguageJson(string lang)
        {
            string relativePath = $"Assets/Resources/{lang}.json";
            
            if (!File.Exists(relativePath))
            {
                EditorUtility.DisplayDialog("Error", $"File not found: {relativePath}", "OK");
                return;
            }
            
            loadedJsonPath = relativePath;
            
            try
            {
                string json = File.ReadAllText(relativePath);
                loadedLibrary = JsonUtility.FromJson<KiqqiLocalizationBundle>(json);
                
                if (loadedLibrary == null || loadedLibrary.entries == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to parse JSON.", "OK");
                    return;
                }
                
                Debug.Log($"[ManualMapper] Loaded {loadedLibrary.entries.Count} entries from {lang}.json");
                
                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load {lang}.json: {ex.Message}", "OK");
            }
        }
        
        private void LoadAllLanguages()
        {
            allLanguageLibraries.Clear();
            
            string basePath = System.IO.Path.GetDirectoryName(loadedJsonPath);
            
            foreach (var lang in languageCodes)
            {
                string langPath = System.IO.Path.Combine(basePath, $"{lang}.json");
                
                if (!File.Exists(langPath))
                {
                    Debug.LogWarning($"[ManualMapper] {lang}.json not found at {langPath}");
                    continue;
                }
                
                try
                {
                    string json = File.ReadAllText(langPath);
                    var bundle = JsonUtility.FromJson<KiqqiLocalizationBundle>(json);
                    
                    if (bundle != null && bundle.entries != null)
                    {
                        allLanguageLibraries[lang] = bundle;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ManualMapper] Failed to load {lang}.json: {ex.Message}");
                }
            }
            
            Debug.Log($"[ManualMapper] Loaded {allLanguageLibraries.Count} language files for validation");
            Repaint();
        }
        
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
        
        private bool PassesSearchFilter(KiqqiLocalizationBundle.Entry entry)
        {
            if (string.IsNullOrEmpty(searchFilter))
                return true;
            
            string filter = searchFilter.ToLowerInvariant();
            
            return entry.key.ToLowerInvariant().Contains(filter) ||
                   entry.text.ToLowerInvariant().Contains(filter);
        }
        
        private void DrawSceneSync()
        {
            EditorGUILayout.BeginVertical(detailBoxStyle);
            
            showSceneSync = EditorGUILayout.Foldout(showSceneSync, "SCENE SYNC", true, EditorStyles.boldLabel);
            
            if (showSceneSync)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "Sync all localization mappings from the current scene to another scene (e.g., from mobile to desktop version). " +
                    "This copies KiqqiLocalizedText components based on matching GameObject paths.",
                    MessageType.Info
                );
                
                EditorGUILayout.Space(5);
                
                var currentScene = EditorSceneManager.GetActiveScene();
                EditorGUILayout.LabelField($"Source Scene: {currentScene.name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Mappings Found: {sceneComponents.Count}", EditorStyles.miniLabel);
                
                EditorGUILayout.Space(5);
                
                targetSyncScene = (SceneAsset)EditorGUILayout.ObjectField(
                    "Target Scene:",
                    targetSyncScene,
                    typeof(SceneAsset),
                    false
                );
                
                EditorGUI.BeginDisabledGroup(targetSyncScene == null || sceneComponents.Count == 0);
                
                if (GUILayout.Button("Sync Mappings to Target Scene", GUILayout.Height(35)))
                {
                    SyncMappingsToScene();
                }
                
                EditorGUI.EndDisabledGroup();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void SyncMappingsToScene()
        {
            if (targetSyncScene == null)
            {
                Debug.LogError("[SceneSync] No target scene selected.");
                return;
            }
            
            string targetScenePath = AssetDatabase.GetAssetPath(targetSyncScene);
            var currentScene = EditorSceneManager.GetActiveScene();
            
            if (currentScene.path == targetScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Scene Sync Error",
                    "Target scene cannot be the same as the source scene.",
                    "OK"
                );
                return;
            }
            
            if (!EditorUtility.DisplayDialog(
                "Confirm Scene Sync",
                $"This will sync {sceneComponents.Count} localization mappings from:\n\n" +
                $"Source: {currentScene.name}\n" +
                $"Target: {targetSyncScene.name}\n\n" +
                $"The target scene will be modified and saved. Continue?",
                "Yes, Sync",
                "Cancel"
            ))
            {
                return;
            }
            
            var sourceData = new List<LocalizationSyncData>();
            foreach (var component in sceneComponents)
            {
                if (component != null && !string.IsNullOrEmpty(component.localizationKey))
                {
                    sourceData.Add(new LocalizationSyncData
                    {
                        path = GetGameObjectPath(component.gameObject),
                        key = component.localizationKey
                    });
                }
            }
            
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[SceneSync] User cancelled. Current scene was not saved.");
                return;
            }
            
            var targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            
            int updated = 0;
            int added = 0;
            int failed = 0;
            
            foreach (var data in sourceData)
            {
                try
                {
                    var targetObj = FindGameObjectByPath(targetScene, data.path);
                    
                    if (targetObj == null)
                    {
                        Debug.LogWarning($"[SceneSync] GameObject not found at path: {data.path}");
                        failed++;
                        continue;
                    }
                    
                    var existingComponent = targetObj.GetComponent<KiqqiLocalizedText>();
                    
                    if (existingComponent != null)
                    {
                        if (existingComponent.localizationKey != data.key)
                        {
                            Undo.RecordObject(existingComponent, "Update Localization Key");
                            existingComponent.localizationKey = data.key;
                            EditorUtility.SetDirty(existingComponent);
                            updated++;
                            Debug.Log($"[SceneSync] Updated: {data.path} → '{data.key}'");
                        }
                    }
                    else
                    {
                        var textComponent = targetObj.GetComponent<Text>();
#if TMP_PRESENT
                        var tmpComponent = targetObj.GetComponent<TMPro.TextMeshProUGUI>();
                        bool hasTextComponent = textComponent != null || tmpComponent != null;
#else
                        bool hasTextComponent = textComponent != null;
#endif
                        
                        if (!hasTextComponent)
                        {
                            Debug.LogWarning($"[SceneSync] No Text/TMP component on: {data.path}");
                            failed++;
                            continue;
                        }
                        
                        Undo.AddComponent<KiqqiLocalizedText>(targetObj);
                        var newComponent = targetObj.GetComponent<KiqqiLocalizedText>();
                        newComponent.localizationKey = data.key;
                        EditorUtility.SetDirty(newComponent);
                        added++;
                        Debug.Log($"[SceneSync] Added: {data.path} → '{data.key}'");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SceneSync] Error processing {data.path}: {ex.Message}");
                    failed++;
                }
            }
            
            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            
            EditorUtility.DisplayDialog(
                "Scene Sync Complete",
                $"Sync completed successfully!\n\n" +
                $"Added: {added}\n" +
                $"Updated: {updated}\n" +
                $"Failed: {failed}\n\n" +
                $"Check the Console for detailed logs.",
                "OK"
            );
            
            Debug.Log($"[SceneSync] === SYNC COMPLETE === Added: {added}, Updated: {updated}, Failed: {failed}");
        }
        
        private GameObject FindGameObjectByPath(UnityEngine.SceneManagement.Scene scene, string path)
        {
            var rootObjects = scene.GetRootGameObjects();
            
            string[] parts = path.Split('/');
            
            GameObject current = null;
            foreach (var root in rootObjects)
            {
                if (root.name == parts[0])
                {
                    current = root;
                    break;
                }
            }
            
            if (current == null)
                return null;
            
            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (child == null)
                    return null;
                current = child.gameObject;
            }
            
            return current;
        }
        
        [System.Serializable]
        private class LocalizationSyncData
        {
            public string path;
            public string key;
        }
    }
}
