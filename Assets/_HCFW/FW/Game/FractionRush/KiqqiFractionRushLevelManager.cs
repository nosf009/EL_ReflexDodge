using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public struct FractionRushDifficultyConfig
    {
        public int fractionCount;
        public int minDenominator;
        public int maxDenominator;
        public bool allowImproperFractions;
        public float minimumDifference;
        public float sequenceTimeLimit;
        public float roundTimeLimit;
        public int correctScore;
        public int wrongPenalty;
        public int timeoutPenalty;
        public int comboThreshold;
        public float comboMultiplier;
        public Transform fractionContainer;
    }

    public class KiqqiFractionRushLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 5 };
        public DifficultyRange easy = new() { minLevel = 6, maxLevel = 12 };
        public DifficultyRange medium = new() { minLevel = 13, maxLevel = 30 };
        public DifficultyRange advanced = new() { minLevel = 31, maxLevel = 45 };
        public DifficultyRange hard = new() { minLevel = 46, maxLevel = 60 };

        [Header("Beginner Settings (Levels 1-5)")]
        public int beginnerFractionCount = 3;
        public int beginnerMinDenominator = 2;
        public int beginnerMaxDenominator = 4;
        public bool beginnerAllowImproperFractions = false;
        public float beginnerMinimumDifference = 0.15f;
        public float beginnerSequenceTimeLimit = 10f;
        public float beginnerRoundTimeLimit = 60f;
        public int beginnerCorrectScore = 50;
        public int beginnerWrongPenalty = 10;
        public int beginnerTimeoutPenalty = 20;
        public int beginnerComboThreshold = 3;
        public float beginnerComboMultiplier = 1.5f;
        [Tooltip("Parent GameObject with child Transform positions for Beginner difficulty")]
        public Transform beginnerFractionContainer;

        [Header("Easy Settings (Levels 6-12)")]
        public int easyFractionCount = 4;
        public int easyMinDenominator = 2;
        public int easyMaxDenominator = 8;
        public bool easyAllowImproperFractions = false;
        public float easyMinimumDifference = 0.12f;
        public float easySequenceTimeLimit = 8f;
        public float easyRoundTimeLimit = 60f;
        public int easyCorrectScore = 75;
        public int easyWrongPenalty = 15;
        public int easyTimeoutPenalty = 25;
        public int easyComboThreshold = 4;
        public float easyComboMultiplier = 1.75f;
        [Tooltip("Parent GameObject with child Transform positions for Easy difficulty")]
        public Transform easyFractionContainer;

        [Header("Medium Settings (Levels 13-30)")]
        public int mediumFractionCount = 5;
        public int mediumMinDenominator = 2;
        public int mediumMaxDenominator = 12;
        public bool mediumAllowImproperFractions = false;
        public float mediumMinimumDifference = 0.10f;
        public float mediumSequenceTimeLimit = 7f;
        public float mediumRoundTimeLimit = 60f;
        public int mediumCorrectScore = 100;
        public int mediumWrongPenalty = 20;
        public int mediumTimeoutPenalty = 30;
        public int mediumComboThreshold = 5;
        public float mediumComboMultiplier = 2.0f;
        [Tooltip("Parent GameObject with child Transform positions for Medium difficulty")]
        public Transform mediumFractionContainer;

        [Header("Advanced Settings (Levels 31-45)")]
        public int advancedFractionCount = 6;
        public int advancedMinDenominator = 2;
        public int advancedMaxDenominator = 16;
        public bool advancedAllowImproperFractions = true;
        public float advancedMinimumDifference = 0.08f;
        public float advancedSequenceTimeLimit = 6f;
        public float advancedRoundTimeLimit = 60f;
        public int advancedCorrectScore = 150;
        public int advancedWrongPenalty = 25;
        public int advancedTimeoutPenalty = 40;
        public int advancedComboThreshold = 6;
        public float advancedComboMultiplier = 2.25f;
        [Tooltip("Parent GameObject with child Transform positions for Advanced difficulty")]
        public Transform advancedFractionContainer;

        [Header("Hard Settings (Levels 46-60+)")]
        public int hardFractionCount = 7;
        public int hardMinDenominator = 2;
        public int hardMaxDenominator = 24;
        public bool hardAllowImproperFractions = true;
        public float hardMinimumDifference = 0.05f;
        public float hardSequenceTimeLimit = 5f;
        public float hardRoundTimeLimit = 60f;
        public int hardCorrectScore = 200;
        public int hardWrongPenalty = 30;
        public int hardTimeoutPenalty = 50;
        public int hardComboThreshold = 7;
        public float hardComboMultiplier = 2.5f;
        [Tooltip("Parent GameObject with child Transform positions for Hard difficulty")]
        public Transform hardFractionContainer;

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

        public FractionRushDifficultyConfig GetDifficultyConfig(int level)
        {
            var diff = GetCurrentDifficulty(level);
            Debug.Log($"[FractionRushLevelManager] GetDifficultyConfig for level {level}, difficulty: {diff}");

            var config = diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => CreateConfig(
                    beginnerFractionCount, beginnerMinDenominator, beginnerMaxDenominator,
                    beginnerAllowImproperFractions, beginnerMinimumDifference, beginnerSequenceTimeLimit,
                    beginnerRoundTimeLimit, beginnerCorrectScore, beginnerWrongPenalty, beginnerTimeoutPenalty,
                    beginnerComboThreshold, beginnerComboMultiplier, beginnerFractionContainer),

                KiqqiLevelManager.KiqqiDifficulty.Easy => CreateConfig(
                    easyFractionCount, easyMinDenominator, easyMaxDenominator,
                    easyAllowImproperFractions, easyMinimumDifference, easySequenceTimeLimit,
                    easyRoundTimeLimit, easyCorrectScore, easyWrongPenalty, easyTimeoutPenalty,
                    easyComboThreshold, easyComboMultiplier, easyFractionContainer),

                KiqqiLevelManager.KiqqiDifficulty.Medium => CreateConfig(
                    mediumFractionCount, mediumMinDenominator, mediumMaxDenominator,
                    mediumAllowImproperFractions, mediumMinimumDifference, mediumSequenceTimeLimit,
                    mediumRoundTimeLimit, mediumCorrectScore, mediumWrongPenalty, mediumTimeoutPenalty,
                    mediumComboThreshold, mediumComboMultiplier, mediumFractionContainer),

                KiqqiLevelManager.KiqqiDifficulty.Advanced => CreateConfig(
                    advancedFractionCount, advancedMinDenominator, advancedMaxDenominator,
                    advancedAllowImproperFractions, advancedMinimumDifference, advancedSequenceTimeLimit,
                    advancedRoundTimeLimit, advancedCorrectScore, advancedWrongPenalty, advancedTimeoutPenalty,
                    advancedComboThreshold, advancedComboMultiplier, advancedFractionContainer),

                KiqqiLevelManager.KiqqiDifficulty.Hard => CreateConfig(
                    hardFractionCount, hardMinDenominator, hardMaxDenominator,
                    hardAllowImproperFractions, hardMinimumDifference, hardSequenceTimeLimit,
                    hardRoundTimeLimit, hardCorrectScore, hardWrongPenalty, hardTimeoutPenalty,
                    hardComboThreshold, hardComboMultiplier, hardFractionContainer),

                _ => CreateConfig(
                    beginnerFractionCount, beginnerMinDenominator, beginnerMaxDenominator,
                    beginnerAllowImproperFractions, beginnerMinimumDifference, beginnerSequenceTimeLimit,
                    beginnerRoundTimeLimit, beginnerCorrectScore, beginnerWrongPenalty, beginnerTimeoutPenalty,
                    beginnerComboThreshold, beginnerComboMultiplier, beginnerFractionContainer)
            };

            Debug.Log($"[FractionRushLevelManager] Returning config with {config.fractionCount} fractions, sequence time: {config.sequenceTimeLimit}s");
            return config;
        }

        private FractionRushDifficultyConfig CreateConfig(
            int fractionCount, int minDenominator, int maxDenominator,
            bool allowImproperFractions, float minimumDifference, float sequenceTimeLimit,
            float roundTimeLimit, int correctScore, int wrongPenalty, int timeoutPenalty,
            int comboThreshold, float comboMultiplier, Transform fractionContainer)
        {
            return new FractionRushDifficultyConfig
            {
                fractionCount = fractionCount,
                minDenominator = minDenominator,
                maxDenominator = maxDenominator,
                allowImproperFractions = allowImproperFractions,
                minimumDifference = minimumDifference,
                sequenceTimeLimit = sequenceTimeLimit,
                roundTimeLimit = roundTimeLimit,
                correctScore = correctScore,
                wrongPenalty = wrongPenalty,
                timeoutPenalty = timeoutPenalty,
                comboThreshold = comboThreshold,
                comboMultiplier = comboMultiplier,
                fractionContainer = fractionContainer
            };
        }

        #endregion

        #region LEVEL SETTINGS

        public float GetLevelTime(int level)
        {
            return GetDifficultyConfig(level).roundTimeLimit;
        }

        public int GetTotalLevels() => hard.maxLevel;

        #endregion
    }
}
