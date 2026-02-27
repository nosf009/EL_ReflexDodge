using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public class LillyItemDefinition
    {
        [Tooltip("Visual sprite for this lilly item")]
        public Sprite sprite;

        [Tooltip("Unique identifier for this item")]
        public int itemID;

        [Tooltip("Color tint for this item")]
        public Color color = Color.white;
    }

    public class KiqqiWaterLilliesLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Item Library")]
        [Tooltip("All available lilly items - assign sprites, IDs, and colors")]
        public LillyItemDefinition[] availableItems;

        [Header("Difficulty Ranges (WaterLillies)")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 12 };
        public DifficultyRange easy = new() { minLevel = 13, maxLevel = 24 };
        public DifficultyRange medium = new() { minLevel = 25, maxLevel = 36 };
        public DifficultyRange advanced = new() { minLevel = 37, maxLevel = 48 };
        public DifficultyRange hard = new() { minLevel = 49, maxLevel = 60 };

        [Header("Sequence Length")]
        [Tooltip("Number of items in the highlight sequence per difficulty")]
        public int beginnerSequenceLength = 3;
        public int easySequenceLength = 4;
        public int mediumSequenceLength = 5;
        public int advancedSequenceLength = 6;
        public int hardSequenceLength = 7;

        [Header("Total Items On Screen")]
        [Tooltip("Total number of items displayed on screen per difficulty")]
        public int beginnerTotalItems = 6;
        public int easyTotalItems = 8;
        public int mediumTotalItems = 9;
        public int advancedTotalItems = 12;
        public int hardTotalItems = 12;

        [Header("Highlight Timing (seconds)")]
        [Tooltip("Duration each item stays highlighted")]
        public float beginnerHighlightDuration = 0.6f;
        public float easyHighlightDuration = 0.5f;
        public float mediumHighlightDuration = 0.4f;
        public float advancedHighlightDuration = 0.35f;
        public float hardHighlightDuration = 0.3f;

        [Tooltip("Delay between each highlight")]
        public float beginnerHighlightDelay = 0.8f;
        public float easyHighlightDelay = 0.7f;
        public float mediumHighlightDelay = 0.6f;
        public float advancedHighlightDelay = 0.5f;
        public float hardHighlightDelay = 0.4f;

        [Header("Score Per Correct Sequence")]
        [Tooltip("Points awarded for correct sequence per difficulty tier")]
        public int beginnerScore = 100;
        public int easyScore = 125;
        public int mediumScore = 150;
        public int advancedScore = 175;
        public int hardScore = 200;

        [Header("Penalty Per Wrong Sequence")]
        [Tooltip("Points subtracted for wrong sequence per difficulty tier")]
        public int beginnerPenalty = 0;
        public int easyPenalty = 10;
        public int mediumPenalty = 20;
        public int advancedPenalty = 30;
        public int hardPenalty = 40;

        [Header("Combo System")]
        [Tooltip("Number of consecutive correct sequences needed for combo bonus")]
        public int beginnerComboThreshold = 4;
        public int easyComboThreshold = 4;
        public int mediumComboThreshold = 3;
        public int advancedComboThreshold = 3;
        public int hardComboThreshold = 3;

        [Tooltip("Score multiplier when combo threshold is reached")]
        public float beginnerComboMultiplier = 1.5f;
        public float easyComboMultiplier = 1.5f;
        public float mediumComboMultiplier = 2f;
        public float advancedComboMultiplier = 2f;
        public float hardComboMultiplier = 2.5f;

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

        #endregion

        #region LEVEL SETTINGS

        public float GetLevelTime(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 45f,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 50f,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 55f,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 55f,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 60f,
                _ => 45f
            };
        }

        public int GetTotalLevels() => hard.maxLevel;

        public int GetSequenceLength(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerSequenceLength,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easySequenceLength,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumSequenceLength,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedSequenceLength,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardSequenceLength,
                _ => beginnerSequenceLength
            };
        }

        public int GetTotalItemsOnScreen(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerTotalItems,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyTotalItems,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumTotalItems,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedTotalItems,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardTotalItems,
                _ => beginnerTotalItems
            };
        }

        public float GetHighlightDuration(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerHighlightDuration,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyHighlightDuration,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumHighlightDuration,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedHighlightDuration,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardHighlightDuration,
                _ => beginnerHighlightDuration
            };
        }

        public float GetHighlightDelay(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerHighlightDelay,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyHighlightDelay,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumHighlightDelay,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedHighlightDelay,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardHighlightDelay,
                _ => beginnerHighlightDelay
            };
        }

        #endregion

        #region SCORING & PENALTIES

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

        public int GetComboThreshold(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerComboThreshold,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyComboThreshold,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumComboThreshold,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedComboThreshold,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardComboThreshold,
                _ => beginnerComboThreshold
            };
        }

        public float GetComboMultiplier(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerComboMultiplier,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyComboMultiplier,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumComboMultiplier,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedComboMultiplier,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardComboMultiplier,
                _ => beginnerComboMultiplier
            };
        }

        #endregion

        #region ITEM LIBRARY ACCESS

        public LillyItemDefinition[] GetItemPoolForLevel(int level)
        {
            return availableItems;
        }

        public LillyItemDefinition GetItemDefinition(int itemID)
        {
            if (availableItems == null || availableItems.Length == 0)
                return null;

            foreach (var item in availableItems)
            {
                if (item.itemID == itemID)
                    return item;
            }

            return null;
        }

        #endregion
    }
}
