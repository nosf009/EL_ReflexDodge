using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public struct DifficultyRange
    {
        [Min(1)] public int minLevel;
        [Min(1)] public int maxLevel;
    }

    public class KiqqiLevelManager : MonoBehaviour
    {
        [Header("General Settings")]
        public bool hasLevelSelect = true;

        public enum KiqqiDifficulty { Beginner, Easy, Medium, Advanced, Hard }

        [Header("Runtime State")]
        [Min(1)] public int currentLevel = 1;
        [HideInInspector]
        [Min(1)] public int totalLevels = 1;

        private KiqqiDataManager data;
        private IKiqqiSubLevelManager activeSubManager;

        // -------------------------------------------------- Initialization
        public void Initialize(KiqqiDataManager dataManager)
        {
            data = dataManager;
            currentLevel = Mathf.Max(1, data.GetInt("currentLevel", currentLevel));
            //totalLevels = data.GetInt("totalLevels", totalLevels);
            totalLevels = GetTotalLevels();
            Debug.Log($"[KiqqiLevelManager] Init > Current={currentLevel}");
        }

        public void RegisterSubManager(IKiqqiSubLevelManager sub)
        {
            activeSubManager = sub;
            totalLevels = sub.GetTotalLevels();
            Debug.Log($"[KiqqiLevelManager] Registered sub-level manager: {sub.GetType().Name}");
        }

        // -------------------------------------------------- Delegated Difficulty API
        public KiqqiDifficulty GetCurrentDifficulty()
        {
            if (activeSubManager != null)
                return activeSubManager.GetCurrentDifficulty(currentLevel);
            return KiqqiDifficulty.Beginner;
        }

        public float GetLevelTime()
        {
            if (activeSubManager != null)
                return activeSubManager.GetLevelTime(currentLevel);
            return 20f;
        }

        public int GetTotalLevels()
        {
            if (activeSubManager != null)
                return activeSubManager.GetTotalLevels();
            return totalLevels;
        }

        // -------------------------------------------------- Progression
        public void RegisterWin() => NextLevel();

        public void NextLevel()
        {
            int max = GetTotalLevels();

            // If already at or beyond max, keep it locked at max
            if (currentLevel >= max)
            {
                currentLevel = max;
                data?.SetInt("currentLevel", currentLevel);
                Debug.Log($"[KiqqiLevelManager] Reached max level ({max}).");
                return;
            }

            currentLevel++;
            data?.SetInt("currentLevel", currentLevel);
            //Debug.Log($"[KiqqiLevelManager] NextLevel > {currentLevel}/{max}");
        }


        public void ResetProgress()
        {
            currentLevel = 1;
            data?.SetInt("currentLevel", currentLevel);
            PlayerPrefs.SetInt("unlockedLevel", 1);
            PlayerPrefs.Save();
            Debug.Log("[KiqqiLevelManager] ResetProgress > Level 1");
        }

        // -------------------------------------------------- Legacy Compatibility Helpers
        public int GetUnlockedLevel()
        {
            return PlayerPrefs.GetInt("unlockedLevel", 1);
        }

        public void SetCurrentLevel(int level)
        {
            int max = GetTotalLevels();
            currentLevel = Mathf.Clamp(level, 1, max);
            data?.SetInt("currentLevel", currentLevel);
        }

        public void LoadCurrentLevel()
        {
            totalLevels = GetTotalLevels();
            currentLevel = Mathf.Clamp(data?.GetInt("currentLevel", currentLevel) ?? currentLevel, 1, totalLevels);
            if (currentLevel >= totalLevels)
                currentLevel = totalLevels;
            Debug.Log($"[KiqqiLevelManager] LoadCurrentLevel > Level {currentLevel} ({GetCurrentDifficulty()})");
        }

        public void UnlockNextLevel()
        {
            int unlocked = GetUnlockedLevel();
            int max = GetTotalLevels();

            if (currentLevel >= unlocked && currentLevel < max)
            {
                unlocked = currentLevel + 1;
                PlayerPrefs.SetInt("unlockedLevel", unlocked);
                PlayerPrefs.Save();
                Debug.Log($"[KiqqiLevelManager] New level unlocked: {unlocked}");
            }
        }
    }
}

namespace Kiqqi.Framework
{
    public interface IKiqqiSubLevelManager
    {
        KiqqiLevelManager.KiqqiDifficulty GetCurrentDifficulty(int level);
        float GetLevelTime(int level);
        int GetTotalLevels();
    }
}



