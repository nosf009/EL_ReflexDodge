using UnityEngine;
using UnityEngine.UI;

#if TMP_PRESENT
using TMPro;
#endif

namespace Kiqqi.Localization
{
    /// <summary>
    /// Component that automatically applies localized text
    /// based on a key and listens for localization readiness.
    /// Works with Unity UI Text and TextMeshProUGUI (if present).
    /// </summary>
    //[ExecuteAlways]
    [DisallowMultipleComponent]
    public class KiqqiLocalizedText : MonoBehaviour
    {
        [Tooltip("If true, this label is dynamic and may change during runtime via SetKey().")]
        public bool isDynamic = false;

        [Tooltip("Localization key (auto-filled from GameObject name if empty).")]
        public string localizationKey;

        [Tooltip("If true, applies localization automatically at runtime (not in edit mode).")]
        public bool autoApply = true;

        private System.Action _onReadyHandler;


#if UNITY_EDITOR
        private void OnValidate()
        {
            // auto-generate key from GameObject name if missing
            if (string.IsNullOrEmpty(localizationKey))
                localizationKey = gameObject.name.ToLowerInvariant().Replace(" ", "_");
        }
#endif

        private void Start()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return; // Never modify scene text in Edit mode
#endif
            if (Application.isPlaying && autoApply && !isDynamic)
            {
                if (IsLocalizationReady())
                    Apply();
                else
                {
                    _onReadyHandler = () => Apply();
                    KiqqiLocalizationManager.OnLocalizationReady += _onReadyHandler;
                }
            }
            else if (!Application.isPlaying)
            {
                // In Editor mode, keep text synced
                Apply();
            }
        }

        private bool IsLocalizationReady()
        {
            return KiqqiLocalizationManager.IsInitialized;
        }

        /// <summary>
        /// Applies localized text to the attached UI label.
        /// Optional args allow placeholder replacement for dynamic text.
        /// </summary>
        public void Apply(object args = null)
        {
            if (string.IsNullOrEmpty(localizationKey))
                localizationKey = gameObject.name.ToLowerInvariant();

            string localized = KiqqiLocalizationManager.T(localizationKey);

            if (args != null)
                localized = KiqqiTemplateFormatter.Apply(localized, args);

            if (string.IsNullOrEmpty(localized))
                localized = GetCurrentText();

            SetLabelText(localized);
        }

        /// <summary>
        /// Sets a new key at runtime (for dynamic labels).
        /// </summary>
        public void SetKey(string key, object args = null)
        {
            localizationKey = key;
            Apply(args);
        }

        private void OnDestroy()
        {
            if (_onReadyHandler != null)
                KiqqiLocalizationManager.OnLocalizationReady -= _onReadyHandler;
        }


        public void RefreshLanguage()
        {
            if (Application.isPlaying)
                Apply();
        }

        private string GetCurrentText()
        {
#if TMP_PRESENT
            var tmp = GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp.text;
#endif
            var ui = GetComponent<Text>();
            if (ui != null) return ui.text;
            return "";
        }

        private void SetLabelText(string value)
        {
#if TMP_PRESENT
            var tmp = GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = value;
                return;
            }
#endif
            var ui = GetComponent<Text>();
            if (ui != null)
            {
                ui.text = value;
                return;
            }

            Debug.LogWarning($"[KiqqiLocalizedText] No text component on {gameObject.name}");
        }
    }
}
