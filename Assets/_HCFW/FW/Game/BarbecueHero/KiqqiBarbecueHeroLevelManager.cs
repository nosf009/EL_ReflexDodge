using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public struct BarbecueHeroDifficultyConfig
    {
        public int availableMeatTypes;
        public float spawnDelayMin;
        public float spawnDelayMax;
        public float cookingSpeedMin;
        public float cookingSpeedMax;
        public float timeLimit;
        public int perfectScore;
        public int goodScore;
        public int wrongPenalty;
        public int comboThreshold;
        public float comboMultiplier;
    }

    public class KiqqiBarbecueHeroLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 5 };
        public DifficultyRange easy = new() { minLevel = 6, maxLevel = 13 };
        public DifficultyRange medium = new() { minLevel = 14, maxLevel = 27 };
        public DifficultyRange advanced = new() { minLevel = 28, maxLevel = 44 };
        public DifficultyRange hard = new() { minLevel = 45, maxLevel = 60 };

        [Header("Beginner Settings (Levels 1-5)")]
        [Tooltip("Number of meat types available (1-6)")]
        public int beginnerAvailableMeatTypes = 3;
        [Tooltip("Time between spawns")]
        public float beginnerSpawnDelayMin = 2.5f;
        public float beginnerSpawnDelayMax = 3.5f;
        [Tooltip("Cooking progress per second (lower = slower cooking)")]
        public float beginnerCookingSpeedMin = 0.15f;
        public float beginnerCookingSpeedMax = 0.20f;
        public float beginnerTimeLimit = 40f;
        public int beginnerPerfectScore = 150;
        public int beginnerGoodScore = 75;
        public int beginnerWrongPenalty = 0;
        public int beginnerComboThreshold = 3;
        public float beginnerComboMultiplier = 1.5f;

        [Header("Easy Settings (Levels 6-13)")]
        public int easyAvailableMeatTypes = 4;
        public float easySpawnDelayMin = 2.0f;
        public float easySpawnDelayMax = 3.0f;
        public float easyCookingSpeedMin = 0.18f;
        public float easyCookingSpeedMax = 0.25f;
        public float easyTimeLimit = 45f;
        public int easyPerfectScore = 175;
        public int easyGoodScore = 90;
        public int easyWrongPenalty = 25;
        public int easyComboThreshold = 4;
        public float easyComboMultiplier = 1.75f;

        [Header("Medium Settings (Levels 14-27)")]
        public int mediumAvailableMeatTypes = 5;
        public float mediumSpawnDelayMin = 1.5f;
        public float mediumSpawnDelayMax = 2.5f;
        public float mediumCookingSpeedMin = 0.22f;
        public float mediumCookingSpeedMax = 0.32f;
        public float mediumTimeLimit = 50f;
        public int mediumPerfectScore = 200;
        public int mediumGoodScore = 100;
        public int mediumWrongPenalty = 50;
        public int mediumComboThreshold = 5;
        public float mediumComboMultiplier = 2.0f;

        [Header("Advanced Settings (Levels 28-44)")]
        public int advancedAvailableMeatTypes = 6;
        public float advancedSpawnDelayMin = 1.2f;
        public float advancedSpawnDelayMax = 2.0f;
        public float advancedCookingSpeedMin = 0.28f;
        public float advancedCookingSpeedMax = 0.40f;
        public float advancedTimeLimit = 55f;
        public int advancedPerfectScore = 250;
        public int advancedGoodScore = 125;
        public int advancedWrongPenalty = 75;
        public int advancedComboThreshold = 6;
        public float advancedComboMultiplier = 2.25f;

        [Header("Hard Settings (Levels 45-60+)")]
        public int hardAvailableMeatTypes = 6;
        public float hardSpawnDelayMin = 0.8f;
        public float hardSpawnDelayMax = 1.5f;
        public float hardCookingSpeedMin = 0.35f;
        public float hardCookingSpeedMax = 0.50f;
        public float hardTimeLimit = 60f;
        public int hardPerfectScore = 300;
        public int hardGoodScore = 150;
        public int hardWrongPenalty = 100;
        public int hardComboThreshold = 7;
        public float hardComboMultiplier = 2.5f;

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

        public BarbecueHeroDifficultyConfig GetDifficultyConfig(int level)
        {
            var diff = GetCurrentDifficulty(level);
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => new BarbecueHeroDifficultyConfig
                {
                    availableMeatTypes = beginnerAvailableMeatTypes,
                    spawnDelayMin = beginnerSpawnDelayMin,
                    spawnDelayMax = beginnerSpawnDelayMax,
                    cookingSpeedMin = beginnerCookingSpeedMin,
                    cookingSpeedMax = beginnerCookingSpeedMax,
                    timeLimit = beginnerTimeLimit,
                    perfectScore = beginnerPerfectScore,
                    goodScore = beginnerGoodScore,
                    wrongPenalty = beginnerWrongPenalty,
                    comboThreshold = beginnerComboThreshold,
                    comboMultiplier = beginnerComboMultiplier
                },
                KiqqiLevelManager.KiqqiDifficulty.Easy => new BarbecueHeroDifficultyConfig
                {
                    availableMeatTypes = easyAvailableMeatTypes,
                    spawnDelayMin = easySpawnDelayMin,
                    spawnDelayMax = easySpawnDelayMax,
                    cookingSpeedMin = easyCookingSpeedMin,
                    cookingSpeedMax = easyCookingSpeedMax,
                    timeLimit = easyTimeLimit,
                    perfectScore = easyPerfectScore,
                    goodScore = easyGoodScore,
                    wrongPenalty = easyWrongPenalty,
                    comboThreshold = easyComboThreshold,
                    comboMultiplier = easyComboMultiplier
                },
                KiqqiLevelManager.KiqqiDifficulty.Medium => new BarbecueHeroDifficultyConfig
                {
                    availableMeatTypes = mediumAvailableMeatTypes,
                    spawnDelayMin = mediumSpawnDelayMin,
                    spawnDelayMax = mediumSpawnDelayMax,
                    cookingSpeedMin = mediumCookingSpeedMin,
                    cookingSpeedMax = mediumCookingSpeedMax,
                    timeLimit = mediumTimeLimit,
                    perfectScore = mediumPerfectScore,
                    goodScore = mediumGoodScore,
                    wrongPenalty = mediumWrongPenalty,
                    comboThreshold = mediumComboThreshold,
                    comboMultiplier = mediumComboMultiplier
                },
                KiqqiLevelManager.KiqqiDifficulty.Advanced => new BarbecueHeroDifficultyConfig
                {
                    availableMeatTypes = advancedAvailableMeatTypes,
                    spawnDelayMin = advancedSpawnDelayMin,
                    spawnDelayMax = advancedSpawnDelayMax,
                    cookingSpeedMin = advancedCookingSpeedMin,
                    cookingSpeedMax = advancedCookingSpeedMax,
                    timeLimit = advancedTimeLimit,
                    perfectScore = advancedPerfectScore,
                    goodScore = advancedGoodScore,
                    wrongPenalty = advancedWrongPenalty,
                    comboThreshold = advancedComboThreshold,
                    comboMultiplier = advancedComboMultiplier
                },
                KiqqiLevelManager.KiqqiDifficulty.Hard => new BarbecueHeroDifficultyConfig
                {
                    availableMeatTypes = hardAvailableMeatTypes,
                    spawnDelayMin = hardSpawnDelayMin,
                    spawnDelayMax = hardSpawnDelayMax,
                    cookingSpeedMin = hardCookingSpeedMin,
                    cookingSpeedMax = hardCookingSpeedMax,
                    timeLimit = hardTimeLimit,
                    perfectScore = hardPerfectScore,
                    goodScore = hardGoodScore,
                    wrongPenalty = hardWrongPenalty,
                    comboThreshold = hardComboThreshold,
                    comboMultiplier = hardComboMultiplier
                },
                _ => new BarbecueHeroDifficultyConfig
                {
                    availableMeatTypes = beginnerAvailableMeatTypes,
                    spawnDelayMin = beginnerSpawnDelayMin,
                    spawnDelayMax = beginnerSpawnDelayMax,
                    cookingSpeedMin = beginnerCookingSpeedMin,
                    cookingSpeedMax = beginnerCookingSpeedMax,
                    timeLimit = beginnerTimeLimit,
                    perfectScore = beginnerPerfectScore,
                    goodScore = beginnerGoodScore,
                    wrongPenalty = beginnerWrongPenalty,
                    comboThreshold = beginnerComboThreshold,
                    comboMultiplier = beginnerComboMultiplier
                }
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
