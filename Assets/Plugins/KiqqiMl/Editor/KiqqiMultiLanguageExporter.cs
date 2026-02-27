using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kiqqi.Localization.Editor
{
    public class KiqqiMultiLanguageExporter : EditorWindow
    {
        private string[] languageCodes = { "en", "de", "it", "fr" };
        private string libraryBasePath = "Assets/Resources/ref-ml/standard_library";
        private string outputPath = "Assets/Resources";
        
        private Dictionary<string, KiqqiLocalizationBundle> loadedLibraries = new Dictionary<string, KiqqiLocalizationBundle>();
        private List<string> sceneKeys = new List<string>();
        
        [MenuItem("Kiqqi/Localization/Multi-Language Exporter")]
        public static void ShowWindow()
        {
            var window = GetWindow<KiqqiMultiLanguageExporter>("Multi-Language Exporter");
            window.minSize = new Vector2(600, 400);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("MULTI-LANGUAGE EXPORT TOOL", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool generates all 4 language JSONs (en, de, it, fr) for the current scene,\n" +
                "using the standard library translations where available and current scene text as fallback.",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            libraryBasePath = EditorGUILayout.TextField("Library Base Path:", libraryBasePath);
            outputPath = EditorGUILayout.TextField("Output Path:", outputPath);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Load Standard Libraries", GUILayout.Height(30)))
                LoadAllLibraries();
            
            if (loadedLibraries.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField($"Loaded {loadedLibraries.Count} language libraries:", EditorStyles.boldLabel);
                
                foreach (var lang in loadedLibraries.Keys)
                {
                    EditorGUILayout.LabelField($"  - {lang.ToUpper()}: {loadedLibraries[lang].entries.Count} entries");
                }
            }
            
            EditorGUILayout.Space(20);
            
            if (GUILayout.Button("Generate All Language Files for Current Scene", GUILayout.Height(40)))
                GenerateAllLanguageFiles();
        }
        
        private void LoadAllLibraries()
        {
            loadedLibraries.Clear();
            
            foreach (var lang in languageCodes)
            {
                string path = $"{libraryBasePath}_{lang}.json";
                
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[MultiLangExporter] Library not found: {path}");
                    continue;
                }
                
                try
                {
                    string json = File.ReadAllText(path);
                    var bundle = JsonUtility.FromJson<KiqqiLocalizationBundle>(json);
                    
                    if (bundle != null && bundle.entries != null)
                    {
                        loadedLibraries[lang] = bundle;
                        Debug.Log($"[MultiLangExporter] Loaded {lang}: {bundle.entries.Count} entries");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[MultiLangExporter] Failed to load {lang}: {ex.Message}");
                }
            }
            
            EditorUtility.DisplayDialog("Success", $"Loaded {loadedLibraries.Count} language libraries.", "OK");
            Repaint();
        }
        
        private void GenerateAllLanguageFiles()
        {
            if (loadedLibraries.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No libraries loaded. Click 'Load Standard Libraries' first.", "OK");
                return;
            }
            
            var sceneItems = ScanCurrentScene();
            
            if (sceneItems.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No localized items found in the current scene.", "OK");
                return;
            }
            
            foreach (var lang in languageCodes)
            {
                GenerateLanguageFile(lang, sceneItems);
            }
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Export Complete",
                $"Generated {languageCodes.Length} language files in:\n{outputPath}\n\n" +
                $"Files: {string.Join(", ", languageCodes.Select(l => $"{l}.json"))}",
                "OK"
            );
        }
        
        private void GenerateLanguageFile(string lang, List<KiqqiLocalizedText> sceneItems)
        {
            var bundle = new KiqqiLocalizationBundle
            {
                metadata = new KiqqiLocalizationBundle.Metadata
                {
                    language = lang,
                    generatedAt = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount
                },
                entries = new List<KiqqiLocalizationBundle.Entry>()
            };
            
            var usedKeys = new HashSet<string>();
            
            foreach (var item in sceneItems)
            {
                if (item == null || string.IsNullOrEmpty(item.localizationKey))
                    continue;
                
                string key = item.localizationKey.ToLowerInvariant().Trim();
                
                if (!usedKeys.Add(key))
                    continue;
                
                string translatedText = GetTranslationFromLibrary(lang, key);
                
                if (string.IsNullOrEmpty(translatedText))
                {
                    translatedText = ExtractText(item);
                }
                
                bundle.entries.Add(new KiqqiLocalizationBundle.Entry
                {
                    key = key,
                    text = translatedText,
                    isDynamic = item.isDynamic
                });
            }
            
            string outputFilePath = Path.Combine(outputPath, $"{lang}.json");
            string json = JsonUtility.ToJson(bundle, true);
            
            File.WriteAllText(outputFilePath, json);
            
            Debug.Log($"[MultiLangExporter] Generated {lang}.json with {bundle.entries.Count} entries");
        }
        
        private string GetTranslationFromLibrary(string lang, string key)
        {
            if (!loadedLibraries.ContainsKey(lang))
                return "";
            
            var library = loadedLibraries[lang];
            string strippedKey = StripPrefix(key);
            
            foreach (var entry in library.entries)
            {
                if (entry.key.ToLowerInvariant() == key.ToLowerInvariant())
                    return entry.text;
                
                if (entry.key.ToLowerInvariant() == strippedKey.ToLowerInvariant())
                    return entry.text;
            }
            
            return "";
        }
        
        private string StripPrefix(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length <= 2)
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
        
        private List<KiqqiLocalizedText> ScanCurrentScene()
        {
            var components = new List<KiqqiLocalizedText>();
            
#if UNITY_2023_1_OR_NEWER
            components.AddRange(Object.FindObjectsByType<KiqqiLocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None));
#else
            components.AddRange(Object.FindObjectsOfType<KiqqiLocalizedText>(true));
#endif
            
            return components;
        }
        
        private string ExtractText(KiqqiLocalizedText comp)
        {
#if TMP_PRESENT
            var tmp = comp.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp.text;
#endif
            var ui = comp.GetComponent<UnityEngine.UI.Text>();
            if (ui != null) return ui.text;
            return "";
        }
    }
}
