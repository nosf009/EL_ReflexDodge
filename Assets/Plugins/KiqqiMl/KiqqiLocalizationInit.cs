using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kiqqi.Localization
{
    public class KiqqiLocalizationInit : MonoBehaviour
    {
        public bool offlineMode = true;

#if UNITY_EDITOR
        [Header("Editor Debug")]
        public string editorOverrideCountryCode = "";
        public bool editorClearCacheOnStart = false;
#endif

        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            KiqqiLocalizationManager.UseOfflineMode = offlineMode;

#if UNITY_EDITOR
            if (editorClearCacheOnStart)
            {
                PlayerPrefs.DeleteKey("KiqqiMl_CountryCode");
            }

            if (!string.IsNullOrEmpty(editorOverrideCountryCode))
            {
                string code = editorOverrideCountryCode.ToLowerInvariant();
                PlayerPrefs.SetString("KiqqiMl_CountryCode", code);
                PlayerPrefs.Save();
                
                // Force re-initialization if override is set
                KiqqiLocalizationManager.ForceReset();
            }
#endif

            await KiqqiLocalizationManager.InitAsync();
            ApplyLocalizationToAll();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyLocalizationToAll();
        }

        private void ApplyLocalizationToAll()
        {
#if UNITY_2023_1_OR_NEWER
            var all = Object.FindObjectsByType<KiqqiLocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = Object.FindObjectsOfType<KiqqiLocalizedText>(true);
#endif
            foreach (var t in all)
                t.Apply();
        }
    }
}
