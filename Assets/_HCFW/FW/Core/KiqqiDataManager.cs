using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Minimal data manager. Wraps PlayerPrefs-style storage and stubs Initialize().
    /// Adds automatic key prefixing via KiqqiGameDefinition.
    /// </summary>
    public class KiqqiDataManager : MonoBehaviour
    {
        private const int DATA_VERSION = 1;

        [Header("Game Definition Reference")]
        public KiqqiGameDefinition gameDefinition;   // assign in the SYN scene

        // helper to prefix keys
        private string Key(string raw) =>
            gameDefinition ? gameDefinition.GetPrefixedKey(raw) : raw;

        // --------------------------------------------------
        public void Initialize()
        {
            int storedVersion = PlayerPrefs.GetInt("data_version", 0);
            if (storedVersion != DATA_VERSION)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.SetInt("data_version", DATA_VERSION);
                PlayerPrefs.Save();
                Debug.Log("[KiqqiDataManager] Cleared old prefs due to version change.");
            }
            else
            {
                Debug.Log("[KiqqiDataManager] Initialized (v" + DATA_VERSION + ")");
            }
        }

        // --------------------------------------------------
        // INT
        // --------------------------------------------------
        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Key(key), value);
            PlayerPrefs.Save();
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(Key(key), defaultValue);
        }

        // --------------------------------------------------
        // FLOAT
        // --------------------------------------------------
        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(Key(key), value);
            PlayerPrefs.Save();
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(Key(key), defaultValue);
        }

        // --------------------------------------------------
        // STRING
        // --------------------------------------------------
        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(Key(key), value);
            PlayerPrefs.Save();
        }

        public string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(Key(key), defaultValue);
        }

        // --------------------------------------------------
        // BOOL (helper)
        // --------------------------------------------------
        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(Key(key), value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            int def = defaultValue ? 1 : 0;
            return PlayerPrefs.GetInt(Key(key), def) == 1;
        }

        // --------------------------------------------------
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(Key(key));
        }

        public void SaveNow()
        {
            PlayerPrefs.Save();
        }

        public void ClearAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[KiqqiDataManager] Cleared all stored data.");
        }
    }
}
