using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public struct JarSortingDifficultyConfig
    {
        public int objectTypeCount;
        public int emptyJarCount;
        public int itemsPerJar;
        public int shuffleMoves;
        public float timeLimit;
        public int solveScore;
        public int moveScore;
        public int wrongPenalty;
    }

    public class KiqqiJarSortingLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 5 };
        public DifficultyRange easy = new() { minLevel = 6, maxLevel = 12 };
        public DifficultyRange medium = new() { minLevel = 13, maxLevel = 30 };
        public DifficultyRange advanced = new() { minLevel = 31, maxLevel = 45 };
        public DifficultyRange hard = new() { minLevel = 46, maxLevel = 60 };

        [Header("Beginner Settings (Levels 1-5)")]
        public int beginnerObjectTypeCount = 2;
        public int beginnerEmptyJarCount = 1;
        public int beginnerItemsPerJar = 3;
        public int beginnerShuffleMoves = 10;
        public float beginnerTimeLimit = 40f;
        public int beginnerSolveScore = 100;
        public int beginnerMoveScore = 10;
        public int beginnerWrongPenalty = 0;

        [Header("Easy Settings (Levels 6-12)")]
        public int easyObjectTypeCount = 3;
        public int easyEmptyJarCount = 1;
        public int easyItemsPerJar = 3;
        public int easyShuffleMoves = 15;
        public float easyTimeLimit = 45f;
        public int easySolveScore = 150;
        public int easyMoveScore = 15;
        public int easyWrongPenalty = 5;

        [Header("Medium Settings (Levels 13-30)")]
        public int mediumObjectTypeCount = 4;
        public int mediumEmptyJarCount = 1;
        public int mediumItemsPerJar = 4;
        public int mediumShuffleMoves = 20;
        public float mediumTimeLimit = 50f;
        public int mediumSolveScore = 200;
        public int mediumMoveScore = 20;
        public int mediumWrongPenalty = 10;

        [Header("Advanced Settings (Levels 31-45)")]
        public int advancedObjectTypeCount = 6;
        public int advancedEmptyJarCount = 2;
        public int advancedItemsPerJar = 4;
        public int advancedShuffleMoves = 25;
        public float advancedTimeLimit = 55f;
        public int advancedSolveScore = 250;
        public int advancedMoveScore = 25;
        public int advancedWrongPenalty = 15;

        [Header("Hard Settings (Levels 46-60+)")]
        public int hardObjectTypeCount = 6;
        public int hardEmptyJarCount = 1;
        public int hardItemsPerJar = 4;
        public int hardShuffleMoves = 30;
        public float hardTimeLimit = 60f;
        public int hardSolveScore = 300;
        public int hardMoveScore = 30;
        public int hardWrongPenalty = 20;

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

        public JarSortingDifficultyConfig GetDifficultyConfig(int level)
        {
            var diff = GetCurrentDifficulty(level);
            
            Debug.Log($"[JarSortingLevelManager] GetDifficultyConfig for level {level}, difficulty: {diff}");

            var config = diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => new JarSortingDifficultyConfig
                {
                    objectTypeCount = beginnerObjectTypeCount,
                    emptyJarCount = beginnerEmptyJarCount,
                    itemsPerJar = beginnerItemsPerJar,
                    shuffleMoves = beginnerShuffleMoves,
                    timeLimit = beginnerTimeLimit,
                    solveScore = beginnerSolveScore,
                    moveScore = beginnerMoveScore,
                    wrongPenalty = beginnerWrongPenalty
                },
                KiqqiLevelManager.KiqqiDifficulty.Easy => new JarSortingDifficultyConfig
                {
                    objectTypeCount = easyObjectTypeCount,
                    emptyJarCount = easyEmptyJarCount,
                    itemsPerJar = easyItemsPerJar,
                    shuffleMoves = easyShuffleMoves,
                    timeLimit = easyTimeLimit,
                    solveScore = easySolveScore,
                    moveScore = easyMoveScore,
                    wrongPenalty = easyWrongPenalty
                },
                KiqqiLevelManager.KiqqiDifficulty.Medium => new JarSortingDifficultyConfig
                {
                    objectTypeCount = mediumObjectTypeCount,
                    emptyJarCount = mediumEmptyJarCount,
                    itemsPerJar = mediumItemsPerJar,
                    shuffleMoves = mediumShuffleMoves,
                    timeLimit = mediumTimeLimit,
                    solveScore = mediumSolveScore,
                    moveScore = mediumMoveScore,
                    wrongPenalty = mediumWrongPenalty
                },
                KiqqiLevelManager.KiqqiDifficulty.Advanced => new JarSortingDifficultyConfig
                {
                    objectTypeCount = advancedObjectTypeCount,
                    emptyJarCount = advancedEmptyJarCount,
                    itemsPerJar = advancedItemsPerJar,
                    shuffleMoves = advancedShuffleMoves,
                    timeLimit = advancedTimeLimit,
                    solveScore = advancedSolveScore,
                    moveScore = advancedMoveScore,
                    wrongPenalty = advancedWrongPenalty
                },
                KiqqiLevelManager.KiqqiDifficulty.Hard => new JarSortingDifficultyConfig
                {
                    objectTypeCount = hardObjectTypeCount,
                    emptyJarCount = hardEmptyJarCount,
                    itemsPerJar = hardItemsPerJar,
                    shuffleMoves = hardShuffleMoves,
                    timeLimit = hardTimeLimit,
                    solveScore = hardSolveScore,
                    moveScore = hardMoveScore,
                    wrongPenalty = hardWrongPenalty
                },
                _ => new JarSortingDifficultyConfig
                {
                    objectTypeCount = beginnerObjectTypeCount,
                    emptyJarCount = beginnerEmptyJarCount,
                    itemsPerJar = beginnerItemsPerJar,
                    shuffleMoves = beginnerShuffleMoves,
                    timeLimit = beginnerTimeLimit,
                    solveScore = beginnerSolveScore,
                    moveScore = beginnerMoveScore,
                    wrongPenalty = beginnerWrongPenalty
                }
            };
            
            int totalJars = config.objectTypeCount + config.emptyJarCount;
            Debug.Log($"[JarSortingLevelManager] Config: {config.objectTypeCount} types + {config.emptyJarCount} empty = {totalJars} total jars, {config.itemsPerJar} items/jar");
            
            return config;
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
