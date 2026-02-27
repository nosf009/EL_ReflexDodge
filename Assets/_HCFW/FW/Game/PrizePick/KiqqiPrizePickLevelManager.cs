using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public struct PrizePickDifficultyConfig
    {
        public int optionCount;
        public float timePerChoice;
        public int maxBaseValue;
        public float mathExpressionProbability;
        public int correctScore;
        public int wrongPenalty;
        public int comboThreshold;
        public float comboMultiplier;
        public int challengesPerLevel;
    }

    public class KiqqiPrizePickLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 5 };
        public DifficultyRange easy = new() { minLevel = 6, maxLevel = 12 };
        public DifficultyRange medium = new() { minLevel = 13, maxLevel = 30 };
        public DifficultyRange advanced = new() { minLevel = 31, maxLevel = 45 };
        public DifficultyRange hard = new() { minLevel = 46, maxLevel = 60 };

        [Header("Beginner Settings (Levels 1-5)")]
        public int beginnerOptionCount = 3;
        public float beginnerTimePerChoice = 5f;
        public int beginnerMaxBaseValue = 30;
        public float beginnerMathProbability = 0.2f;
        public int beginnerCorrectScore = 50;
        public int beginnerWrongPenalty = 0;
        public int beginnerComboThreshold = 3;
        public float beginnerComboMultiplier = 1.5f;
        public int beginnerChallengesPerLevel = 8;

        [Header("Easy Settings (Levels 6-12)")]
        public int easyOptionCount = 4;
        public float easyTimePerChoice = 4.5f;
        public int easyMaxBaseValue = 60;
        public float easyMathProbability = 0.4f;
        public int easyCorrectScore = 75;
        public int easyWrongPenalty = 10;
        public int easyComboThreshold = 4;
        public float easyComboMultiplier = 1.75f;
        public int easyChallengesPerLevel = 10;

        [Header("Medium Settings (Levels 13-30)")]
        public int mediumOptionCount = 4;
        public float mediumTimePerChoice = 3.5f;
        public int mediumMaxBaseValue = 100;
        public float mediumMathProbability = 0.6f;
        public int mediumCorrectScore = 100;
        public int mediumWrongPenalty = 20;
        public int mediumComboThreshold = 5;
        public float mediumComboMultiplier = 2f;
        public int mediumChallengesPerLevel = 12;

        [Header("Advanced Settings (Levels 31-45)")]
        public int advancedOptionCount = 5;
        public float advancedTimePerChoice = 3f;
        public int advancedMaxBaseValue = 150;
        public float advancedMathProbability = 0.75f;
        public int advancedCorrectScore = 125;
        public int advancedWrongPenalty = 30;
        public int advancedComboThreshold = 5;
        public float advancedComboMultiplier = 2.25f;
        public int advancedChallengesPerLevel = 14;

        [Header("Hard Settings (Levels 46-60+)")]
        public int hardOptionCount = 5;
        public float hardTimePerChoice = 2f;
        public int hardMaxBaseValue = 200;
        public float hardMathProbability = 0.9f;
        public int hardCorrectScore = 150;
        public int hardWrongPenalty = 40;
        public int hardComboThreshold = 6;
        public float hardComboMultiplier = 2.5f;
        public int hardChallengesPerLevel = 15;

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

        public float GetLevelTime(int level)
        {
            var diff = GetCurrentDifficulty(level);
            
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 50f,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 50f,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 45f,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 45f,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 40f,
                _ => 50f
            };
        }

        public int GetTotalLevels()
        {
            return hard.maxLevel;
        }

        public PrizePickDifficultyConfig GetDifficultyConfig(int level)
        {
            var diff = GetCurrentDifficulty(level);
            
            Debug.Log($"[PrizePickLevelManager] GetDifficultyConfig for level {level}, difficulty: {diff}");

            var config = diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => new PrizePickDifficultyConfig
                {
                    optionCount = beginnerOptionCount,
                    timePerChoice = beginnerTimePerChoice,
                    maxBaseValue = beginnerMaxBaseValue,
                    mathExpressionProbability = beginnerMathProbability,
                    correctScore = beginnerCorrectScore,
                    wrongPenalty = beginnerWrongPenalty,
                    comboThreshold = beginnerComboThreshold,
                    comboMultiplier = beginnerComboMultiplier,
                    challengesPerLevel = beginnerChallengesPerLevel
                },
                KiqqiLevelManager.KiqqiDifficulty.Easy => new PrizePickDifficultyConfig
                {
                    optionCount = easyOptionCount,
                    timePerChoice = easyTimePerChoice,
                    maxBaseValue = easyMaxBaseValue,
                    mathExpressionProbability = easyMathProbability,
                    correctScore = easyCorrectScore,
                    wrongPenalty = easyWrongPenalty,
                    comboThreshold = easyComboThreshold,
                    comboMultiplier = easyComboMultiplier,
                    challengesPerLevel = easyChallengesPerLevel
                },
                KiqqiLevelManager.KiqqiDifficulty.Medium => new PrizePickDifficultyConfig
                {
                    optionCount = mediumOptionCount,
                    timePerChoice = mediumTimePerChoice,
                    maxBaseValue = mediumMaxBaseValue,
                    mathExpressionProbability = mediumMathProbability,
                    correctScore = mediumCorrectScore,
                    wrongPenalty = mediumWrongPenalty,
                    comboThreshold = mediumComboThreshold,
                    comboMultiplier = mediumComboMultiplier,
                    challengesPerLevel = mediumChallengesPerLevel
                },
                KiqqiLevelManager.KiqqiDifficulty.Advanced => new PrizePickDifficultyConfig
                {
                    optionCount = advancedOptionCount,
                    timePerChoice = advancedTimePerChoice,
                    maxBaseValue = advancedMaxBaseValue,
                    mathExpressionProbability = advancedMathProbability,
                    correctScore = advancedCorrectScore,
                    wrongPenalty = advancedWrongPenalty,
                    comboThreshold = advancedComboThreshold,
                    comboMultiplier = advancedComboMultiplier,
                    challengesPerLevel = advancedChallengesPerLevel
                },
                KiqqiLevelManager.KiqqiDifficulty.Hard => new PrizePickDifficultyConfig
                {
                    optionCount = hardOptionCount,
                    timePerChoice = hardTimePerChoice,
                    maxBaseValue = hardMaxBaseValue,
                    mathExpressionProbability = hardMathProbability,
                    correctScore = hardCorrectScore,
                    wrongPenalty = hardWrongPenalty,
                    comboThreshold = hardComboThreshold,
                    comboMultiplier = hardComboMultiplier,
                    challengesPerLevel = hardChallengesPerLevel
                },
                _ => throw new System.ArgumentException($"Unknown difficulty: {diff}")
            };

            return config;
        }

        #endregion
    }
}
