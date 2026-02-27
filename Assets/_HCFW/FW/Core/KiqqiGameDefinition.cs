using UnityEngine;

namespace Kiqqi.Framework
{
    [CreateAssetMenu(fileName = "Kiqqi", menuName = "Kiqqi/Definitions", order = 10)]
    public class KiqqiGameDefinition : ScriptableObject
    {
        [Header("Identification")]
        public string gameId = "kiqqi-default";
        public string displayName = "Kiqqi Game";

        [Header("Networking")]
        public string apiRoot = "https://brain-teacher-api.flowly.com";

        [Header("Prefs")]
        [Tooltip("Automatically prefixes all PlayerPrefs keys with this ID.")]
        public bool prefixPlayerPrefs = true;

        public string GetPrefixedKey(string rawKey)
        {
            if (!prefixPlayerPrefs || string.IsNullOrEmpty(gameId))
                return rawKey;
            return $"{gameId}_{rawKey}";
        }
    }
}
