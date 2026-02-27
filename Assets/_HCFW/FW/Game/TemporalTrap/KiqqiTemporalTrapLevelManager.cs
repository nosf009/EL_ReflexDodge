
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Defines level difficulty/time mapping for TemporalTrap game.
    /// </summary>
    public class KiqqiTemporalTrapLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        public DifficultyRange beginnerRange = new() { minLevel = 1, maxLevel = 10 };
        public DifficultyRange easyRange = new() { minLevel = 11, maxLevel = 25 };
        public DifficultyRange mediumRange = new() { minLevel = 26, maxLevel = 50 };
        public DifficultyRange advancedRange = new() { minLevel = 51, maxLevel = 80 };
        public DifficultyRange hardRange = new() { minLevel = 81, maxLevel = 100 };

        private void Start()
        {
            KiqqiAppManager.Instance.Levels.RegisterSubManager(this);
        }

        public KiqqiLevelManager.KiqqiDifficulty GetCurrentDifficulty(int level)
        {
            if (level <= beginnerRange.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Beginner;
            if (level <= easyRange.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Easy;
            if (level <= mediumRange.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Medium;
            if (level <= advancedRange.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Advanced;
            return KiqqiLevelManager.KiqqiDifficulty.Hard;
        }

        public float GetLevelTime(int level)
        {
            switch (GetCurrentDifficulty(level))
            {
                case KiqqiLevelManager.KiqqiDifficulty.Beginner: return 25f;
                case KiqqiLevelManager.KiqqiDifficulty.Easy: return 25f;
                case KiqqiLevelManager.KiqqiDifficulty.Medium: return 30f;
                case KiqqiLevelManager.KiqqiDifficulty.Advanced: return 35f;
                case KiqqiLevelManager.KiqqiDifficulty.Hard: return 40f;
                default: return 25f;
            }
        }

        public int GetTotalLevels() => 100;
    }
}
