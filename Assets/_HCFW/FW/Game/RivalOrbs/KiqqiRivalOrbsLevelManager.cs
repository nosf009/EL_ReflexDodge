using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public class RivalOrbsDifficultyConfig
    {
        [Header("Object Configuration")]
        [Tooltip("Number of objects to spawn")]
        public int objectCount = 6;
        [Tooltip("Base movement speed of objects")]
        public float objectSpeed = 150f;

        [Header("Barrier Settings")]
        [Tooltip("Size of the gap in the barrier (RectTransform height)")]
        public float gapSize = 200f;

        [Header("Time & Scoring")]
        [Tooltip("Time limit in seconds")]
        public float timeLimit = 60f;
        [Tooltip("Points awarded when all objects are sorted")]
        public int levelCompleteScore = 500;
    }

    public class KiqqiRivalOrbsLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 10 };
        public DifficultyRange easy = new() { minLevel = 11, maxLevel = 20 };
        public DifficultyRange medium = new() { minLevel = 21, maxLevel = 30 };
        public DifficultyRange advanced = new() { minLevel = 31, maxLevel = 40 };
        public DifficultyRange hard = new() { minLevel = 41, maxLevel = 60 };

        [Header("Beginner Configuration")]
        public RivalOrbsDifficultyConfig beginnerConfig = new()
        {
            objectCount = 6,
            objectSpeed = 120f,
            gapSize = 250f,
            timeLimit = 60f,
            levelCompleteScore = 500
        };

        [Header("Easy Configuration")]
        public RivalOrbsDifficultyConfig easyConfig = new()
        {
            objectCount = 8,
            objectSpeed = 150f,
            gapSize = 220f,
            timeLimit = 55f,
            levelCompleteScore = 600
        };

        [Header("Medium Configuration")]
        public RivalOrbsDifficultyConfig mediumConfig = new()
        {
            objectCount = 10,
            objectSpeed = 180f,
            gapSize = 200f,
            timeLimit = 50f,
            levelCompleteScore = 750
        };

        [Header("Advanced Configuration")]
        public RivalOrbsDifficultyConfig advancedConfig = new()
        {
            objectCount = 12,
            objectSpeed = 220f,
            gapSize = 180f,
            timeLimit = 45f,
            levelCompleteScore = 900
        };

        [Header("Hard Configuration")]
        public RivalOrbsDifficultyConfig hardConfig = new()
        {
            objectCount = 14,
            objectSpeed = 260f,
            gapSize = 160f,
            timeLimit = 40f,
            levelCompleteScore = 1200
        };

        #endregion

        #region FRAMEWORK REGISTRATION

        private void Start()
        {
            KiqqiAppManager.Instance.Levels.RegisterSubManager(this);
        }

        #endregion

        #region DIFFICULTY RESOLUTION

        public KiqqiLevelManager.KiqqiDifficulty GetCurrentDifficulty(int level)
        {
            if (level <= beginner.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Beginner;
            if (level <= easy.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Easy;
            if (level <= medium.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Medium;
            if (level <= advanced.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Advanced;
            return KiqqiLevelManager.KiqqiDifficulty.Hard;
        }

        public RivalOrbsDifficultyConfig GetDifficultyConfig(int level)
        {
            var diff = GetCurrentDifficulty(level);
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerConfig,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyConfig,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumConfig,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedConfig,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardConfig,
                _ => beginnerConfig
            };
        }

        #endregion

        #region LEVEL SETTINGS

        public float GetLevelTime(int level)
        {
            return GetDifficultyConfig(level).timeLimit;
        }

        public int GetTotalLevels() => hard.maxLevel;

        #endregion
    }
}
