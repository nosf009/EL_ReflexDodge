using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public class FuelCarDifficultyConfig
    {
        [Header("Lane Configuration")]
        [Tooltip("Number of active lanes for this difficulty")]
        public int laneCount = 2;

        [Header("Spawn Timing")]
        [Tooltip("Minimum delay between car spawns (seconds)")]
        public float spawnDelayMin = 2.0f;
        [Tooltip("Maximum delay between car spawns (seconds)")]
        public float spawnDelayMax = 4.0f;

        [Header("Wait Time")]
        [Tooltip("Minimum time car will wait before timing out (seconds)")]
        public float waitTimeMin = 5.0f;
        [Tooltip("Maximum time car will wait before timing out (seconds)")]
        public float waitTimeMax = 8.0f;

        [Header("Scoring")]
        [Tooltip("Points awarded for correct fuel selection")]
        public int correctScore = 100;
        [Tooltip("Penalty for wrong fuel selection")]
        public int wrongPenalty = 0;
        [Tooltip("Penalty when car times out")]
        public int timeoutPenalty = 0;

        [Header("Combo System")]
        [Tooltip("Number of consecutive correct fuels needed to activate combo")]
        public int comboThreshold = 5;
        [Tooltip("Score multiplier when combo is active")]
        public float comboMultiplier = 1.5f;
    }

    public class KiqqiFuelCarLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 10 };
        public DifficultyRange easy = new() { minLevel = 11, maxLevel = 20 };
        public DifficultyRange medium = new() { minLevel = 21, maxLevel = 30 };
        public DifficultyRange advanced = new() { minLevel = 31, maxLevel = 40 };
        public DifficultyRange hard = new() { minLevel = 41, maxLevel = 60 };

        [Header("Beginner Configuration (2 lanes)")]
        public FuelCarDifficultyConfig beginnerConfig = new()
        {
            laneCount = 2,
            spawnDelayMin = 3.0f,
            spawnDelayMax = 5.0f,
            waitTimeMin = 8.0f,
            waitTimeMax = 12.0f,
            correctScore = 100,
            wrongPenalty = 0,
            timeoutPenalty = 0,
            comboThreshold = 5,
            comboMultiplier = 1.5f
        };

        [Header("Easy Configuration (3 lanes)")]
        public FuelCarDifficultyConfig easyConfig = new()
        {
            laneCount = 3,
            spawnDelayMin = 2.5f,
            spawnDelayMax = 4.0f,
            waitTimeMin = 7.0f,
            waitTimeMax = 10.0f,
            correctScore = 125,
            wrongPenalty = 25,
            timeoutPenalty = 50,
            comboThreshold = 5,
            comboMultiplier = 1.5f
        };

        [Header("Medium Configuration (4 lanes)")]
        public FuelCarDifficultyConfig mediumConfig = new()
        {
            laneCount = 4,
            spawnDelayMin = 2.0f,
            spawnDelayMax = 3.5f,
            waitTimeMin = 6.0f,
            waitTimeMax = 9.0f,
            correctScore = 150,
            wrongPenalty = 50,
            timeoutPenalty = 75,
            comboThreshold = 5,
            comboMultiplier = 2.0f
        };

        [Header("Advanced Configuration (5 lanes)")]
        public FuelCarDifficultyConfig advancedConfig = new()
        {
            laneCount = 5,
            spawnDelayMin = 1.5f,
            spawnDelayMax = 3.0f,
            waitTimeMin = 5.0f,
            waitTimeMax = 8.0f,
            correctScore = 175,
            wrongPenalty = 75,
            timeoutPenalty = 100,
            comboThreshold = 5,
            comboMultiplier = 2.0f
        };

        [Header("Hard Configuration (6 lanes)")]
        public FuelCarDifficultyConfig hardConfig = new()
        {
            laneCount = 6,
            spawnDelayMin = 1.0f,
            spawnDelayMax = 2.5f,
            waitTimeMin = 4.0f,
            waitTimeMax = 7.0f,
            correctScore = 250,
            wrongPenalty = 100,
            timeoutPenalty = 150,
            comboThreshold = 5,
            comboMultiplier = 2.5f
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

        public FuelCarDifficultyConfig GetDifficultyConfig(int level)
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
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 40f,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 45f,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 50f,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 55f,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 60f,
                _ => 40f
            };
        }

        public int GetLaneCount(int level)
        {
            return GetDifficultyConfig(level).laneCount;
        }

        public int GetTotalLevels() => hard.maxLevel;

        #endregion
    }
}
