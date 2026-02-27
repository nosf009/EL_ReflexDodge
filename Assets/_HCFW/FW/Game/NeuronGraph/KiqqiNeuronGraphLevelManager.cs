using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public struct NeuronGraphDifficultyConfig
    {
        public int nodeCount;
        public int colorCount;
        public int shuffleMoves;
        public float timePerPuzzle;
        public float timeLimit;
        public int solveScore;
        public int wrongPenalty;
        public int comboThreshold;
        public float comboMultiplier;
        public KiqqiNeuronGraphLayoutData selectedLayout;
    }

    public class KiqqiNeuronGraphLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 5 };
        public DifficultyRange easy = new() { minLevel = 6, maxLevel = 12 };
        public DifficultyRange medium = new() { minLevel = 13, maxLevel = 30 };
        public DifficultyRange advanced = new() { minLevel = 31, maxLevel = 45 };
        public DifficultyRange hard = new() { minLevel = 46, maxLevel = 60 };

        [Header("Beginner Settings (Levels 1-5)")]
        public int beginnerNodeCount = 3;
        public int beginnerColorCount = 2;
        public int beginnerShuffleMoves = 2;
        public float beginnerTimePerPuzzle = 10f;
        public float beginnerTimeLimit = 40f;
        public int beginnerSolveScore = 100;
        public int beginnerWrongPenalty = 0;
        public int beginnerComboThreshold = 3;
        public float beginnerComboMultiplier = 1.5f;
        [Tooltip("Pool of layout variants for Beginner difficulty")]
        public KiqqiNeuronGraphLayoutData[] beginnerLayouts;

        [Header("Easy Settings (Levels 6-12)")]
        public int easyNodeCount = 4;
        public int easyColorCount = 2;
        public int easyShuffleMoves = 4;
        public float easyTimePerPuzzle = 8f;
        public float easyTimeLimit = 45f;
        public int easySolveScore = 125;
        public int easyWrongPenalty = 25;
        public int easyComboThreshold = 4;
        public float easyComboMultiplier = 1.75f;
        [Tooltip("Pool of layout variants for Easy difficulty")]
        public KiqqiNeuronGraphLayoutData[] easyLayouts;

        [Header("Medium Settings (Levels 13-30)")]
        public int mediumNodeCount = 5;
        public int mediumColorCount = 3;
        public int mediumShuffleMoves = 6;
        public float mediumTimePerPuzzle = 7f;
        public float mediumTimeLimit = 50f;
        public int mediumSolveScore = 150;
        public int mediumWrongPenalty = 50;
        public int mediumComboThreshold = 5;
        public float mediumComboMultiplier = 2.0f;
        [Tooltip("Pool of layout variants for Medium difficulty")]
        public KiqqiNeuronGraphLayoutData[] mediumLayouts;

        [Header("Advanced Settings (Levels 31-45)")]
        public int advancedNodeCount = 7;
        public int advancedColorCount = 3;
        public int advancedShuffleMoves = 10;
        public float advancedTimePerPuzzle = 6f;
        public float advancedTimeLimit = 55f;
        public int advancedSolveScore = 200;
        public int advancedWrongPenalty = 75;
        public int advancedComboThreshold = 6;
        public float advancedComboMultiplier = 2.25f;
        [Tooltip("Pool of layout variants for Advanced difficulty")]
        public KiqqiNeuronGraphLayoutData[] advancedLayouts;

        [Header("Hard Settings (Levels 46-60+)")]
        public int hardNodeCount = 9;
        public int hardColorCount = 4;
        public int hardShuffleMoves = 15;
        public float hardTimePerPuzzle = 5f;
        public float hardTimeLimit = 60f;
        public int hardSolveScore = 250;
        public int hardWrongPenalty = 100;
        public int hardComboThreshold = 7;
        public float hardComboMultiplier = 2.5f;
        [Tooltip("Pool of layout variants for Hard difficulty")]
        public KiqqiNeuronGraphLayoutData[] hardLayouts;

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

        public NeuronGraphDifficultyConfig GetDifficultyConfig(int level)
        {
            var diff = GetCurrentDifficulty(level);
            Debug.Log($"[NeuronGraphLevelManager] GetDifficultyConfig for level {level}, difficulty: {diff}");
            
            var config = diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => CreateConfig(
                    beginnerNodeCount, beginnerColorCount, beginnerShuffleMoves,
                    beginnerTimePerPuzzle, beginnerTimeLimit, beginnerSolveScore,
                    beginnerWrongPenalty, beginnerComboThreshold, beginnerComboMultiplier,
                    beginnerLayouts),
                    
                KiqqiLevelManager.KiqqiDifficulty.Easy => CreateConfig(
                    easyNodeCount, easyColorCount, easyShuffleMoves,
                    easyTimePerPuzzle, easyTimeLimit, easySolveScore,
                    easyWrongPenalty, easyComboThreshold, easyComboMultiplier,
                    easyLayouts),
                    
                KiqqiLevelManager.KiqqiDifficulty.Medium => CreateConfig(
                    mediumNodeCount, mediumColorCount, mediumShuffleMoves,
                    mediumTimePerPuzzle, mediumTimeLimit, mediumSolveScore,
                    mediumWrongPenalty, mediumComboThreshold, mediumComboMultiplier,
                    mediumLayouts),
                    
                KiqqiLevelManager.KiqqiDifficulty.Advanced => CreateConfig(
                    advancedNodeCount, advancedColorCount, advancedShuffleMoves,
                    advancedTimePerPuzzle, advancedTimeLimit, advancedSolveScore,
                    advancedWrongPenalty, advancedComboThreshold, advancedComboMultiplier,
                    advancedLayouts),
                    
                KiqqiLevelManager.KiqqiDifficulty.Hard => CreateConfig(
                    hardNodeCount, hardColorCount, hardShuffleMoves,
                    hardTimePerPuzzle, hardTimeLimit, hardSolveScore,
                    hardWrongPenalty, hardComboThreshold, hardComboMultiplier,
                    hardLayouts),
                    
                _ => CreateConfig(
                    beginnerNodeCount, beginnerColorCount, beginnerShuffleMoves,
                    beginnerTimePerPuzzle, beginnerTimeLimit, beginnerSolveScore,
                    beginnerWrongPenalty, beginnerComboThreshold, beginnerComboMultiplier,
                    beginnerLayouts)
            };
            
            Debug.Log($"[NeuronGraphLevelManager] Returning config with nodeCount: {config.nodeCount}, colorCount: {config.colorCount}, layout: {(config.selectedLayout != null ? config.selectedLayout.gameObject.name : "NULL")}");
            return config;
        }

        private NeuronGraphDifficultyConfig CreateConfig(
            int nodeCount, int colorCount, int shuffleMoves,
            float timePerPuzzle, float timeLimit, int solveScore,
            int wrongPenalty, int comboThreshold, float comboMultiplier,
            KiqqiNeuronGraphLayoutData[] layoutPool)
        {
            KiqqiNeuronGraphLayoutData selectedLayout = null;

            if (layoutPool != null && layoutPool.Length > 0)
            {
                int randomIndex = Random.Range(0, layoutPool.Length);
                selectedLayout = layoutPool[randomIndex];
                Debug.Log($"[CreateConfig] Selected layout variant {randomIndex + 1}/{layoutPool.Length}: {(selectedLayout != null ? selectedLayout.gameObject.name : "NULL")}");
            }
            else
            {
                Debug.LogWarning($"[CreateConfig] No layouts in pool for {nodeCount} nodes!");
            }

            return new NeuronGraphDifficultyConfig
            {
                nodeCount = nodeCount,
                colorCount = colorCount,
                shuffleMoves = shuffleMoves,
                timePerPuzzle = timePerPuzzle,
                timeLimit = timeLimit,
                solveScore = solveScore,
                wrongPenalty = wrongPenalty,
                comboThreshold = comboThreshold,
                comboMultiplier = comboMultiplier,
                selectedLayout = selectedLayout
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
