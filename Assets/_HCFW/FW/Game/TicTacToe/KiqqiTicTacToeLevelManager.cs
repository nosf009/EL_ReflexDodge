using UnityEngine;

namespace Kiqqi.Framework
{
    public class KiqqiTicTacToeLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        [Header("Difficulty Ranges (TicTacToe)")]
        public DifficultyRange beginnerRange = new() { minLevel = 1, maxLevel = 3 };
        public DifficultyRange easyRange = new() { minLevel = 4, maxLevel = 8 };
        public DifficultyRange mediumRange = new() { minLevel = 9, maxLevel = 20 };
        public DifficultyRange advancedRange = new() { minLevel = 21, maxLevel = 50 };
        public DifficultyRange hardRange = new() { minLevel = 51, maxLevel = 100 };

        private void Start()
        {
            // Automatically register with the global LevelManager
            KiqqiAppManager.Instance.Levels.RegisterSubManager(this);
        }

        public KiqqiLevelManager.KiqqiDifficulty GetCurrentDifficulty(int level)
        {
            if (level >= beginnerRange.minLevel && level <= beginnerRange.maxLevel)
                return KiqqiLevelManager.KiqqiDifficulty.Beginner;
            if (level >= easyRange.minLevel && level <= easyRange.maxLevel)
                return KiqqiLevelManager.KiqqiDifficulty.Easy;
            if (level >= mediumRange.minLevel && level <= mediumRange.maxLevel)
                return KiqqiLevelManager.KiqqiDifficulty.Medium;
            if (level >= advancedRange.minLevel && level <= advancedRange.maxLevel)
                return KiqqiLevelManager.KiqqiDifficulty.Advanced;
            return KiqqiLevelManager.KiqqiDifficulty.Hard;
        }

        public float GetLevelTime(int level)
        {
            switch (GetCurrentDifficulty(level))
            {
                case KiqqiLevelManager.KiqqiDifficulty.Beginner:
                case KiqqiLevelManager.KiqqiDifficulty.Easy: return 20f;
                case KiqqiLevelManager.KiqqiDifficulty.Medium:
                case KiqqiLevelManager.KiqqiDifficulty.Advanced: return 25f;
                case KiqqiLevelManager.KiqqiDifficulty.Hard: return 30f;
                default: return 20f;
            }
        }

        public int GetTotalLevels()
        {
            return Mathf.Max(hardRange.maxLevel, advancedRange.maxLevel, mediumRange.maxLevel, easyRange.maxLevel);
        }
    }
}
