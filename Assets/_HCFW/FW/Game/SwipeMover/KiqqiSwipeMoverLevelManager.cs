using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Level configuration for SwipeMover.
    /// </summary>
    public class KiqqiSwipeMoverLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        [Header("Difficulty Ranges")]
        public DifficultyRange easy = new() { minLevel = 1, maxLevel = 5 };
        public DifficultyRange medium = new() { minLevel = 6, maxLevel = 15 };
        public DifficultyRange hard = new() { minLevel = 16, maxLevel = 30 };

        private void Start()
        {
            KiqqiAppManager.Instance.Levels.RegisterSubManager(this);
        }

        public KiqqiLevelManager.KiqqiDifficulty GetCurrentDifficulty(int level)
        {
            if (level <= easy.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Easy;
            if (level <= medium.maxLevel) return KiqqiLevelManager.KiqqiDifficulty.Medium;
            return KiqqiLevelManager.KiqqiDifficulty.Hard;
        }

        public float GetLevelTime(int level)
        {
            switch (GetCurrentDifficulty(level))
            {
                case KiqqiLevelManager.KiqqiDifficulty.Easy: return 40f;
                case KiqqiLevelManager.KiqqiDifficulty.Medium: return 30f;
                case KiqqiLevelManager.KiqqiDifficulty.Hard: return 25f;
                default: return 35f;
            }
        }

        public int GetTotalLevels() => hard.maxLevel;
    }
}
