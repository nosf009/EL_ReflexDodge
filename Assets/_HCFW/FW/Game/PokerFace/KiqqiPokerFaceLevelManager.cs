using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Difficulty and timing configuration for the "Poker Face" (Other Ones) mini-game.
    /// Handles level ranges, difficulty scaling, scoring, and penalties.
    /// Registers automatically into the global KiqqiLevelManager.
    /// </summary>
    public class KiqqiPokerFaceLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges (PokerFace)")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 10 };
        public DifficultyRange easy = new() { minLevel = 11, maxLevel = 20 };
        public DifficultyRange medium = new() { minLevel = 21, maxLevel = 30 };
        public DifficultyRange advanced = new() { minLevel = 31, maxLevel = 40 };
        public DifficultyRange hard = new() { minLevel = 41, maxLevel = 50 };

        [Header("Score Per Correct Answer (by Difficulty)")]
        [Tooltip("Points awarded for a correct answer per difficulty tier.")]
        public int beginnerScore = 100;
        public int easyScore = 125;
        public int mediumScore = 150;
        public int advancedScore = 175;
        public int hardScore = 250;

        [Header("Penalty Per Wrong Answer (by Difficulty)")]
        [Tooltip("Points subtracted for a wrong answer per difficulty tier.")]
        public int beginnerPenalty = 0;
        public int easyPenalty = 25;
        public int mediumPenalty = 50;
        public int advancedPenalty = 75;
        public int hardPenalty = 100;

        #endregion

        #region FRAMEWORK REGISTRATION (DO NOT MODIFY)

        private void Start()
        {
            // Automatically register this sub-manager into the global LevelManager.
            KiqqiAppManager.Instance.Levels.RegisterSubManager(this);
        }

        #endregion

        #region DIFFICULTY RESOLUTION

        /// <summary>
        /// Returns the difficulty tier for the provided level index.
        /// </summary>
        public KiqqiLevelManager.KiqqiDifficulty GetCurrentDifficulty(int level)
        {
            if (level <= beginner.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Beginner;
            if (level <= easy.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Easy;
            if (level <= medium.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Medium;
            if (level <= advanced.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Advanced;
            return KiqqiLevelManager.KiqqiDifficulty.Hard;
        }

        #endregion

        #region SCORING & PENALTIES

        /// <summary>
        /// Returns score reward per correct answer for the given level.
        /// </summary>
        public int GetScoreForLevel(int level)
        {
            var diff = GetCurrentDifficulty(level);
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerScore,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyScore,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumScore,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedScore,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardScore,
                _ => beginnerScore
            };
        }

        /// <summary>
        /// Returns penalty amount per wrong answer for the given level.
        /// </summary>
        public int GetPenaltyForLevel(int level)
        {
            var diff = GetCurrentDifficulty(level);
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerPenalty,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyPenalty,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumPenalty,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedPenalty,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardPenalty,
                _ => beginnerPenalty
            };
        }

        #endregion

        #region LEVEL SETTINGS

        /// <summary>
        /// Returns available time (seconds) for the specified level.
        /// </summary>
        public float GetLevelTime(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 40f,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 45f,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 50f,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 55f,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 60f,
                _ => 30f
            };
        }

        /// <summary>
        /// Returns how many visual elements/cards are spawned at the given level.
        /// </summary>
        public int GetElementCount(int level)
        {
            var diff = GetCurrentDifficulty(level);
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 4,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 5,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 6,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 7,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 8,
                _ => 4
            };
        }

        /// <summary>
        /// Returns total number of levels available for this mini-game.
        /// </summary>
        public int GetTotalLevels() => hard.maxLevel;

        #endregion
    }
}
