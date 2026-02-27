using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Difficulty/time mapping for Pattern Repeat.
    /// Registers into the global KiqqiLevelManager automatically.
    /// </summary>
    public class KiqqiPatternRepeatLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        [Header("Difficulty Ranges (PatternRepeat)")]
        public DifficultyRange beginnerRange = new() { minLevel = 1, maxLevel = 3 };
        public DifficultyRange easyRange = new() { minLevel = 4, maxLevel = 10 };
        public DifficultyRange mediumRange = new() { minLevel = 11, maxLevel = 25 };
        public DifficultyRange advancedRange = new() { minLevel = 26, maxLevel = 60 };
        public DifficultyRange hardRange = new() { minLevel = 61, maxLevel = 100 };

        private void Start()
        {
            KiqqiAppManager.Instance.Levels.RegisterSubManager(this);
        }

        public KiqqiLevelManager.KiqqiDifficulty GetCurrentDifficulty(int level)
        {
            if (level >= beginnerRange.minLevel && level <= beginnerRange.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Beginner;
            if (level >= easyRange.minLevel && level <= easyRange.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Easy;
            if (level >= mediumRange.minLevel && level <= mediumRange.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Medium;
            if (level >= advancedRange.minLevel && level <= advancedRange.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Advanced;
            return KiqqiLevelManager.KiqqiDifficulty.Hard;
        }

        public float GetLevelTime(int level)
        {
            // Slightly tighter than GridSwipe to encourage focus
            switch (GetCurrentDifficulty(level))
            {
                case KiqqiLevelManager.KiqqiDifficulty.Beginner:
                case KiqqiLevelManager.KiqqiDifficulty.Easy: return 40f;
                case KiqqiLevelManager.KiqqiDifficulty.Medium: return 35f;
                case KiqqiLevelManager.KiqqiDifficulty.Advanced: return 30f;
                case KiqqiLevelManager.KiqqiDifficulty.Hard: return 25f;
                default: return 35f;
            }
        }

        public int GetTotalLevels()
        {
            return Mathf.Max(hardRange.maxLevel, advancedRange.maxLevel, mediumRange.maxLevel, easyRange.maxLevel);
        }
    }
}
