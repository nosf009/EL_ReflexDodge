using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public struct ColorActionDifficultyConfig
    {
        public int targetFruitCount;
        public int maxActiveSpawnPoints;
        public float spawnDelayMin;
        public float spawnDelayMax;
        public float fallSpeedMin;
        public float fallSpeedMax;
        public float timeLimit;
        public int correctScore;
        public int wrongPenalty;
        public int comboThreshold;
        public float comboMultiplier;

        [Range(0f, 1f)]
        public float targetFruitSpawnChance;
    }

    public class KiqqiColorActionLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Spawn Point Parents (One per Difficulty)")]
        [Tooltip("Parent GameObject with child spawn transforms for Beginner difficulty")]
        public Transform beginnerSpawnParent;
        [Tooltip("Parent GameObject with child spawn transforms for Easy difficulty")]
        public Transform easySpawnParent;
        [Tooltip("Parent GameObject with child spawn transforms for Medium difficulty")]
        public Transform mediumSpawnParent;
        [Tooltip("Parent GameObject with child spawn transforms for Advanced difficulty")]
        public Transform advancedSpawnParent;
        [Tooltip("Parent GameObject with child spawn transforms for Hard difficulty")]
        public Transform hardSpawnParent;

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 5 };
        public DifficultyRange easy = new() { minLevel = 6, maxLevel = 13 };
        public DifficultyRange medium = new() { minLevel = 14, maxLevel = 27 };
        public DifficultyRange advanced = new() { minLevel = 28, maxLevel = 44 };
        public DifficultyRange hard = new() { minLevel = 45, maxLevel = 60 };

        [Header("Beginner Settings (Levels 1-5)")]
        [Tooltip("Number of target fruit types to catch")]
        public int beginnerTargetFruitCount = 1;
        [Tooltip("Maximum active spawn points (inspector-driven)")]
        public int beginnerMaxActiveSpawnPoints = 3;
        [Tooltip("Spawn delay range (seconds between spawns)")]
        public float beginnerSpawnDelayMin = 1.5f;
        public float beginnerSpawnDelayMax = 2.5f;
        [Tooltip("Fall speed range (units per second)")]
        public float beginnerFallSpeedMin = 100f;
        public float beginnerFallSpeedMax = 150f;
        public float beginnerTimeLimit = 40f;
        public int beginnerCorrectScore = 100;
        public int beginnerWrongPenalty = 0;
        public int beginnerComboThreshold = 3;
        public float beginnerComboMultiplier = 1.5f;
        [Range(0f, 1f)]
        [Tooltip("Chance that spawned fruit is a target type (0=never, 1=always)")]
        public float beginnerTargetChance = 0.7f;

        [Header("Easy Settings (Levels 6-13)")]
        public int easyTargetFruitCount = 1;
        public int easyMaxActiveSpawnPoints = 4;
        public float easySpawnDelayMin = 1.2f;
        public float easySpawnDelayMax = 2.0f;
        public float easyFallSpeedMin = 120f;
        public float easyFallSpeedMax = 180f;
        public float easyTimeLimit = 45f;
        public int easyCorrectScore = 125;
        public int easyWrongPenalty = 25;
        public int easyComboThreshold = 4;
        public float easyComboMultiplier = 1.75f;
        [Range(0f, 1f)]
        public float easyTargetChance = 0.65f;

        [Header("Medium Settings (Levels 14-27)")]
        public int mediumTargetFruitCount = 2;
        public int mediumMaxActiveSpawnPoints = 5;
        public float mediumSpawnDelayMin = 1.0f;
        public float mediumSpawnDelayMax = 1.8f;
        public float mediumFallSpeedMin = 140f;
        public float mediumFallSpeedMax = 200f;
        public float mediumTimeLimit = 50f;
        public int mediumCorrectScore = 150;
        public int mediumWrongPenalty = 50;
        public int mediumComboThreshold = 5;
        public float mediumComboMultiplier = 2.0f;
        [Range(0f, 1f)]
        public float mediumTargetChance = 0.6f;

        [Header("Advanced Settings (Levels 28-44)")]
        public int advancedTargetFruitCount = 2;
        public int advancedMaxActiveSpawnPoints = 6;
        public float advancedSpawnDelayMin = 0.8f;
        public float advancedSpawnDelayMax = 1.5f;
        public float advancedFallSpeedMin = 160f;
        public float advancedFallSpeedMax = 220f;
        public float advancedTimeLimit = 55f;
        public int advancedCorrectScore = 175;
        public int advancedWrongPenalty = 75;
        public int advancedComboThreshold = 6;
        public float advancedComboMultiplier = 2.25f;
        [Range(0f, 1f)]
        public float advancedTargetChance = 0.55f;

        [Header("Hard Settings (Levels 45-60+)")]
        public int hardTargetFruitCount = 3;
        public int hardMaxActiveSpawnPoints = 8;
        public float hardSpawnDelayMin = 0.6f;
        public float hardSpawnDelayMax = 1.2f;
        public float hardFallSpeedMin = 180f;
        public float hardFallSpeedMax = 250f;
        public float hardTimeLimit = 60f;
        public int hardCorrectScore = 250;
        public int hardWrongPenalty = 100;
        public int hardComboThreshold = 7;
        public float hardComboMultiplier = 2.5f;
        [Range(0f, 1f)]
        public float hardTargetChance = 0.5f;

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

        public ColorActionDifficultyConfig GetDifficultyConfig(int level)
        {
            var diff = GetCurrentDifficulty(level);
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => new ColorActionDifficultyConfig
                {
                    targetFruitCount = beginnerTargetFruitCount,
                    maxActiveSpawnPoints = beginnerMaxActiveSpawnPoints,
                    spawnDelayMin = beginnerSpawnDelayMin,
                    spawnDelayMax = beginnerSpawnDelayMax,
                    fallSpeedMin = beginnerFallSpeedMin,
                    fallSpeedMax = beginnerFallSpeedMax,
                    timeLimit = beginnerTimeLimit,
                    correctScore = beginnerCorrectScore,
                    wrongPenalty = beginnerWrongPenalty,
                    comboThreshold = beginnerComboThreshold,
                    comboMultiplier = beginnerComboMultiplier,
                    targetFruitSpawnChance = beginnerTargetChance
                },
                KiqqiLevelManager.KiqqiDifficulty.Easy => new ColorActionDifficultyConfig
                {
                    targetFruitCount = easyTargetFruitCount,
                    maxActiveSpawnPoints = easyMaxActiveSpawnPoints,
                    spawnDelayMin = easySpawnDelayMin,
                    spawnDelayMax = easySpawnDelayMax,
                    fallSpeedMin = easyFallSpeedMin,
                    fallSpeedMax = easyFallSpeedMax,
                    timeLimit = easyTimeLimit,
                    correctScore = easyCorrectScore,
                    wrongPenalty = easyWrongPenalty,
                    comboThreshold = easyComboThreshold,
                    comboMultiplier = easyComboMultiplier,
                    targetFruitSpawnChance = easyTargetChance
                },
                KiqqiLevelManager.KiqqiDifficulty.Medium => new ColorActionDifficultyConfig
                {
                    targetFruitCount = mediumTargetFruitCount,
                    maxActiveSpawnPoints = mediumMaxActiveSpawnPoints,
                    spawnDelayMin = mediumSpawnDelayMin,
                    spawnDelayMax = mediumSpawnDelayMax,
                    fallSpeedMin = mediumFallSpeedMin,
                    fallSpeedMax = mediumFallSpeedMax,
                    timeLimit = mediumTimeLimit,
                    correctScore = mediumCorrectScore,
                    wrongPenalty = mediumWrongPenalty,
                    comboThreshold = mediumComboThreshold,
                    comboMultiplier = mediumComboMultiplier,
                    targetFruitSpawnChance = mediumTargetChance
                },
                KiqqiLevelManager.KiqqiDifficulty.Advanced => new ColorActionDifficultyConfig
                {
                    targetFruitCount = advancedTargetFruitCount,
                    maxActiveSpawnPoints = advancedMaxActiveSpawnPoints,
                    spawnDelayMin = advancedSpawnDelayMin,
                    spawnDelayMax = advancedSpawnDelayMax,
                    fallSpeedMin = advancedFallSpeedMin,
                    fallSpeedMax = advancedFallSpeedMax,
                    timeLimit = advancedTimeLimit,
                    correctScore = advancedCorrectScore,
                    wrongPenalty = advancedWrongPenalty,
                    comboThreshold = advancedComboThreshold,
                    comboMultiplier = advancedComboMultiplier,
                    targetFruitSpawnChance = advancedTargetChance
                },
                KiqqiLevelManager.KiqqiDifficulty.Hard => new ColorActionDifficultyConfig
                {
                    targetFruitCount = hardTargetFruitCount,
                    maxActiveSpawnPoints = hardMaxActiveSpawnPoints,
                    spawnDelayMin = hardSpawnDelayMin,
                    spawnDelayMax = hardSpawnDelayMax,
                    fallSpeedMin = hardFallSpeedMin,
                    fallSpeedMax = hardFallSpeedMax,
                    timeLimit = hardTimeLimit,
                    correctScore = hardCorrectScore,
                    wrongPenalty = hardWrongPenalty,
                    comboThreshold = hardComboThreshold,
                    comboMultiplier = hardComboMultiplier,
                    targetFruitSpawnChance = hardTargetChance
                },
                _ => new ColorActionDifficultyConfig
                {
                    targetFruitCount = beginnerTargetFruitCount,
                    maxActiveSpawnPoints = beginnerMaxActiveSpawnPoints,
                    spawnDelayMin = beginnerSpawnDelayMin,
                    spawnDelayMax = beginnerSpawnDelayMax,
                    fallSpeedMin = beginnerFallSpeedMin,
                    fallSpeedMax = beginnerFallSpeedMax,
                    timeLimit = beginnerTimeLimit,
                    correctScore = beginnerCorrectScore,
                    wrongPenalty = beginnerWrongPenalty,
                    comboThreshold = beginnerComboThreshold,
                    comboMultiplier = beginnerComboMultiplier,
                    targetFruitSpawnChance = beginnerTargetChance
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

        #region SPAWN POINT MANAGEMENT

        public List<Transform> GetActiveSpawnPointGroups(int level)
        {
            var diff = GetCurrentDifficulty(level);

            Transform activeParent = diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerSpawnParent,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easySpawnParent,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumSpawnParent,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedSpawnParent,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardSpawnParent,
                _ => beginnerSpawnParent
            };

            DisableAllSpawnParents();

            if (activeParent != null)
            {
                activeParent.gameObject.SetActive(true);
            }

            var result = new List<Transform>();
            if (activeParent != null)
            {
                result.Add(activeParent);
            }

            return result;
        }

        private void DisableAllSpawnParents()
        {
            if (beginnerSpawnParent != null) beginnerSpawnParent.gameObject.SetActive(false);
            if (easySpawnParent != null) easySpawnParent.gameObject.SetActive(false);
            if (mediumSpawnParent != null) mediumSpawnParent.gameObject.SetActive(false);
            if (advancedSpawnParent != null) advancedSpawnParent.gameObject.SetActive(false);
            if (hardSpawnParent != null) hardSpawnParent.gameObject.SetActive(false);
        }

        #endregion
    }
}
