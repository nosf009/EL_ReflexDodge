using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public struct RetroCodeDifficultyConfig
    {
        public int minNumber;
        public int maxNumber;
        public int buttonCount;
        public float timeLimit;
        public int correctScore;
        public int wrongPenalty;
        public int comboThreshold;
        public float comboMultiplier;
        
        [Range(0f, 1f)]
        public float squareOperationChance;
    }

    public class KiqqiRetroCodeLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 5 };
        public DifficultyRange easy = new() { minLevel = 6, maxLevel = 13 };
        public DifficultyRange medium = new() { minLevel = 14, maxLevel = 27 };
        public DifficultyRange advanced = new() { minLevel = 28, maxLevel = 44 };
        public DifficultyRange hard = new() { minLevel = 45, maxLevel = 60 };

        [Header("Beginner Settings (Levels 1-5)")]
        [Tooltip("Number range for questions")]
        public int beginnerMinNumber = 2;
        public int beginnerMaxNumber = 8;
        public int beginnerButtonCount = 4;
        public float beginnerTimeLimit = 40f;
        public int beginnerCorrectScore = 100;
        public int beginnerWrongPenalty = 0;
        public int beginnerComboThreshold = 3;
        public float beginnerComboMultiplier = 1.5f;
        [Range(0f, 1f)]
        [Tooltip("Chance of Square operation (0=only sqrt, 1=only square, 0.5=50/50 mix)")]
        public float beginnerSquareChance = 0.8f;

        [Header("Easy Settings (Levels 6-13)")]
        public int easyMinNumber = 5;
        public int easyMaxNumber = 12;
        public int easyButtonCount = 6;
        public float easyTimeLimit = 45f;
        public int easyCorrectScore = 125;
        public int easyWrongPenalty = 25;
        public int easyComboThreshold = 4;
        public float easyComboMultiplier = 1.75f;
        [Range(0f, 1f)]
        public float easySquareChance = 0.65f;

        [Header("Medium Settings (Levels 14-27)")]
        public int mediumMinNumber = 8;
        public int mediumMaxNumber = 16;
        public int mediumButtonCount = 8;
        public float mediumTimeLimit = 50f;
        public int mediumCorrectScore = 150;
        public int mediumWrongPenalty = 50;
        public int mediumComboThreshold = 5;
        public float mediumComboMultiplier = 2.0f;
        [Range(0f, 1f)]
        public float mediumSquareChance = 0.5f;

        [Header("Advanced Settings (Levels 28-44)")]
        public int advancedMinNumber = 12;
        public int advancedMaxNumber = 20;
        public int advancedButtonCount = 10;
        public float advancedTimeLimit = 55f;
        public int advancedCorrectScore = 175;
        public int advancedWrongPenalty = 75;
        public int advancedComboThreshold = 6;
        public float advancedComboMultiplier = 2.25f;
        [Range(0f, 1f)]
        public float advancedSquareChance = 0.45f;

        [Header("Hard Settings (Levels 45-60+)")]
        public int hardMinNumber = 15;
        public int hardMaxNumber = 25;
        public int hardButtonCount = 12;
        public float hardTimeLimit = 60f;
        public int hardCorrectScore = 250;
        public int hardWrongPenalty = 100;
        public int hardComboThreshold = 7;
        public float hardComboMultiplier = 2.5f;
        [Range(0f, 1f)]
        public float hardSquareChance = 0.5f;

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

        public RetroCodeDifficultyConfig GetDifficultyConfig(int level)
        {
            var diff = GetCurrentDifficulty(level);
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => new RetroCodeDifficultyConfig
                {
                    minNumber = beginnerMinNumber,
                    maxNumber = beginnerMaxNumber,
                    buttonCount = beginnerButtonCount,
                    timeLimit = beginnerTimeLimit,
                    correctScore = beginnerCorrectScore,
                    wrongPenalty = beginnerWrongPenalty,
                    comboThreshold = beginnerComboThreshold,
                    comboMultiplier = beginnerComboMultiplier,
                    squareOperationChance = beginnerSquareChance
                },
                KiqqiLevelManager.KiqqiDifficulty.Easy => new RetroCodeDifficultyConfig
                {
                    minNumber = easyMinNumber,
                    maxNumber = easyMaxNumber,
                    buttonCount = easyButtonCount,
                    timeLimit = easyTimeLimit,
                    correctScore = easyCorrectScore,
                    wrongPenalty = easyWrongPenalty,
                    comboThreshold = easyComboThreshold,
                    comboMultiplier = easyComboMultiplier,
                    squareOperationChance = easySquareChance
                },
                KiqqiLevelManager.KiqqiDifficulty.Medium => new RetroCodeDifficultyConfig
                {
                    minNumber = mediumMinNumber,
                    maxNumber = mediumMaxNumber,
                    buttonCount = mediumButtonCount,
                    timeLimit = mediumTimeLimit,
                    correctScore = mediumCorrectScore,
                    wrongPenalty = mediumWrongPenalty,
                    comboThreshold = mediumComboThreshold,
                    comboMultiplier = mediumComboMultiplier,
                    squareOperationChance = mediumSquareChance
                },
                KiqqiLevelManager.KiqqiDifficulty.Advanced => new RetroCodeDifficultyConfig
                {
                    minNumber = advancedMinNumber,
                    maxNumber = advancedMaxNumber,
                    buttonCount = advancedButtonCount,
                    timeLimit = advancedTimeLimit,
                    correctScore = advancedCorrectScore,
                    wrongPenalty = advancedWrongPenalty,
                    comboThreshold = advancedComboThreshold,
                    comboMultiplier = advancedComboMultiplier,
                    squareOperationChance = advancedSquareChance
                },
                KiqqiLevelManager.KiqqiDifficulty.Hard => new RetroCodeDifficultyConfig
                {
                    minNumber = hardMinNumber,
                    maxNumber = hardMaxNumber,
                    buttonCount = hardButtonCount,
                    timeLimit = hardTimeLimit,
                    correctScore = hardCorrectScore,
                    wrongPenalty = hardWrongPenalty,
                    comboThreshold = hardComboThreshold,
                    comboMultiplier = hardComboMultiplier,
                    squareOperationChance = hardSquareChance
                },
                _ => new RetroCodeDifficultyConfig
                {
                    minNumber = beginnerMinNumber,
                    maxNumber = beginnerMaxNumber,
                    buttonCount = beginnerButtonCount,
                    timeLimit = beginnerTimeLimit,
                    correctScore = beginnerCorrectScore,
                    wrongPenalty = beginnerWrongPenalty,
                    comboThreshold = beginnerComboThreshold,
                    comboMultiplier = beginnerComboMultiplier,
                    squareOperationChance = beginnerSquareChance
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
