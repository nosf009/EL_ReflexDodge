using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Kiqqi.Localization
{
    public static class KiqqiLocalizationManager
    {
        public static bool IsInitialized => isInitialized;

        public static bool UseOfflineMode = true;

        private static Dictionary<string, string> localizedMap = new Dictionary<string, string>();
        private static bool isInitialized = false;

        private static string languagesUrl = "https://kiqqi.com/elml/languages.json";
        private static string currentLang = "en";

        private const string CountryPrefsKey = "KiqqiMl_CountryCode";

        public static event System.Action OnLocalizationReady;

        public static void ForceReset()
        {
            isInitialized = false;
            localizedMap.Clear();
        }

        public static async Task InitAsync()
        {
            if (isInitialized)
                return;

            localizedMap.Clear();

            if (UseOfflineMode)
            {
                string detectedLanguage = await DetectLanguageAsync();
                currentLang = ParseLanguageCode(detectedLanguage);

                await LoadOfflineLanguage(currentLang);

                isInitialized = true;
                Debug.Log($"[KiqqiML] Initialized: Language={currentLang}, Entries={localizedMap.Count}");
                OnLocalizationReady?.Invoke();
                return;
            }

            var langJson = await FetchJson(languagesUrl);
            if (string.IsNullOrEmpty(langJson))
            {
                isInitialized = true;
                OnLocalizationReady?.Invoke();
                return;
            }

            var langData = JsonUtility.FromJson<LangMap>(langJson);
            currentLang = langData.defaultLang ?? "en";

            var available = new Dictionary<string, LangEntry>();
            if (langData.available != null)
            {
                foreach (var item in langData.available)
                {
                    if (item == null || string.IsNullOrEmpty(item.code) || item.entry == null)
                        continue;

                    string key = item.code.ToLowerInvariant();
                    available[key] = item.entry;
                }
            }

            if (available.Count == 0)
            {
                isInitialized = true;
                OnLocalizationReady?.Invoke();
                return;
            }

            string detectedLang = await DetectLanguageAsync();
            string parsedLang = ParseLanguageCode(detectedLang);

            if (!string.IsNullOrEmpty(parsedLang) && available.ContainsKey(parsedLang))
                currentLang = parsedLang;

            if (!available.ContainsKey(currentLang))
                currentLang = "en";

            var langConfig = available[currentLang];

            await LoadAndMerge(langConfig.staticUrl);
            await LoadAndMerge(langConfig.dynamicUrl);

            isInitialized = true;
            Debug.Log($"[KiqqiML] Initialized: Language={currentLang}, Entries={localizedMap.Count}");
            OnLocalizationReady?.Invoke();
        }

        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            if (!isInitialized)
                return "";

            string low = key.ToLowerInvariant();

            if (localizedMap.TryGetValue(low, out string val))
                return val;

            return "";
        }

        public static string Format(string key, object args)
        {
            string template = T(key);
            return KiqqiTemplateFormatter.Apply(template, args);
        }

        private static async Task LoadOfflineLanguage(string lang)
        {
            TextAsset textAsset = Resources.Load<TextAsset>(lang);

            if (textAsset == null && lang != "en")
            {
                Debug.LogWarning($"[KiqqiML] Language '{lang}' not found, falling back to 'en'");
                textAsset = Resources.Load<TextAsset>("en");
            }

            if (textAsset == null)
            {
                Debug.LogError($"[KiqqiML] Failed to load language files (tried: '{lang}', 'en')");
                return;
            }
            
            string json = textAsset.text;

            try
            {
                var bundle = JsonUtility.FromJson<KiqqiLocalizationBundle>(json);
                if (bundle != null && bundle.entries != null)
                {
                    foreach (var entry in bundle.entries)
                    {
                        if (string.IsNullOrEmpty(entry.key))
                            continue;

                        localizedMap[entry.key.ToLowerInvariant()] = entry.text ?? "";
                    }
                }
                else
                {
                    Debug.LogWarning("[KiqqiML] JSON parsed but bundle or entries are null");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[KiqqiML] Failed to parse JSON: {ex.Message}");
            }

            await Task.Yield();
        }

        private static async Task LoadAndMerge(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            string json = await FetchJson(url);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var bundle = JsonUtility.FromJson<KiqqiLocalizationBundle>(json);
                if (bundle != null && bundle.entries != null)
                {
                    foreach (var entry in bundle.entries)
                    {
                        if (string.IsNullOrEmpty(entry.key))
                            continue;
                        localizedMap[entry.key.ToLowerInvariant()] = entry.text ?? "";
                    }
                    return;
                }
            }
            catch
            {
            }

            try
            {
                var data = JsonUtility.FromJson<Serialization<string, string>>(json);
                if (data != null && data.keys != null)
                {
                    for (int i = 0; i < data.keys.Count; i++)
                    {
                        string key = data.keys[i].ToLowerInvariant();
                        string value = i < data.values.Count ? data.values[i] : "";
                        localizedMap[key] = value;
                    }
                }
            }
            catch
            {
            }
        }

        private static async Task<string> FetchJson(string url)
        {
            using UnityWebRequest req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();

            while (!op.isDone)
                await Task.Yield();

            if (req.result == UnityWebRequest.Result.Success)
                return req.downloadHandler.text;

            return null;
        }

        private static async Task<string> DetectLanguageAsync()
        {
#if UNITY_EDITOR
            return await DetectLanguageInEditorAsync();
#else
            return await DetectLanguageFromPortalAsync();
#endif
        }

        private static async Task<string> DetectLanguageInEditorAsync()
        {
            if (PlayerPrefs.HasKey(CountryPrefsKey))
            {
                string cached = PlayerPrefs.GetString(CountryPrefsKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    Debug.Log($"[KiqqiML] Detection: Using cached='{cached}' (Editor)");
                    return cached;
                }
            }

            using UnityWebRequest req = UnityWebRequest.Get("https://api.country.is/");
            var op = req.SendWebRequest();

            while (!op.isDone)
                await Task.Yield();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var result = JsonUtility.FromJson<CountryResponse>(req.downloadHandler.text);
                    string code = result.country.ToLowerInvariant();

                    PlayerPrefs.SetString(CountryPrefsKey, code);
                    PlayerPrefs.Save();

                    Debug.Log($"[KiqqiML] Detection: country.is='{code}' (Editor)");
                    return code;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[KiqqiML] Detection failed (Editor): {ex.Message}");
                    return "en";
                }
            }

            Debug.LogWarning($"[KiqqiML] Detection failed: country.is returned {req.result} (Editor)");
            return "en";
        }

        private static async Task<string> DetectLanguageFromPortalAsync()
        {
            using UnityWebRequest req = UnityWebRequest.Get("get-language");
            var op = req.SendWebRequest();

            while (!op.isDone)
                await Task.Yield();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string response = req.downloadHandler.text;
                if (!string.IsNullOrEmpty(response))
                {
                    string trimmed = response.Trim();
                    Debug.Log($"[KiqqiML] Detection: Portal='{trimmed}' (WebGL)");
                    return trimmed;
                }
                else
                {
                    Debug.LogWarning("[KiqqiML] Detection failed: Portal returned empty (WebGL)");
                }
            }
            else
            {
                Debug.LogWarning($"[KiqqiML] Detection failed: Portal returned {req.result} (WebGL)");
            }

            return "en";
        }

        private static string ParseLanguageCode(string rawLanguageCode)
        {
            if (string.IsNullOrEmpty(rawLanguageCode))
                return "en";

            string normalized = rawLanguageCode.ToLowerInvariant().Trim();

            if (normalized.Contains("de"))
                return "de";

            if (normalized.Contains("fr"))
                return "fr";

            if (normalized.Contains("it"))
                return "it";

            if (normalized.Contains("en"))
                return "en";

            Debug.LogWarning($"[KiqqiML] Unknown language '{rawLanguageCode}', using 'en'");
            return "en";
        }

        [System.Serializable]
        private class CountryResponse
        {
            public string country;
        }

        [System.Serializable]
        private class LangMap
        {
            public string defaultLang;
            public LangEntryItem[] available;
        }

        [System.Serializable]
        private class LangEntryItem
        {
            public string code;
            public LangEntry entry;
        }

        [System.Serializable]
        private class LangEntry
        {
            public string staticUrl;
            public string dynamicUrl;
        }

        [System.Serializable]
        private class Serialization<K, V>
        {
            public List<K> keys = new List<K>();
            public List<V> values = new List<V>();
        }

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
    }
}
