using UnityEngine;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public class CandyItemDefinition
    {
        [Tooltip("Visual sprite for this candy type")]
        public Sprite sprite;

        [Tooltip("Unique identifier for matching shelf items to bags")]
        public int itemID;
    }

    public class KiqqiCandyFactoryLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Item Library")]
        [Tooltip("All available candy types - assign sprites and IDs")]
        public CandyItemDefinition[] availableItems;

        [Header("Difficulty Item Pools")]
        [Tooltip("Item IDs available for Beginner difficulty")]
        public int[] beginnerItemPool = { 0, 1, 2, 3 };
        [Tooltip("Item IDs available for Easy difficulty")]
        public int[] easyItemPool = { 0, 1, 2, 3, 4, 5 };
        [Tooltip("Item IDs available for Medium difficulty")]
        public int[] mediumItemPool = { 0, 1, 2, 3, 4, 5, 6 };
        [Tooltip("Item IDs available for Advanced difficulty")]
        public int[] advancedItemPool = { 0, 1, 2, 3, 4, 5, 6, 7 };
        [Tooltip("Item IDs available for Hard difficulty")]
        public int[] hardItemPool = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        [Header("Difficulty Ranges (CandyFactory)")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 12 };
        public DifficultyRange easy = new() { minLevel = 13, maxLevel = 24 };
        public DifficultyRange medium = new() { minLevel = 25, maxLevel = 36 };
        public DifficultyRange advanced = new() { minLevel = 37, maxLevel = 48 };
        public DifficultyRange hard = new() { minLevel = 49, maxLevel = 60 };

        [Header("Preview Duration (seconds)")]
        [Tooltip("How long the bag contents are shown before hiding")]
        public float beginnerPreviewDuration = 5f;
        public float easyPreviewDuration = 4f;
        public float mediumPreviewDuration = 3f;
        public float advancedPreviewDuration = 2.5f;
        public float hardPreviewDuration = 2f;

        [Header("Bag Count")]
        [Tooltip("Number of bags per round for each difficulty")]
        public int beginnerBagCount = 2;
        public int easyBagCount = 2;
        public int mediumBagCount = 3;
        public int advancedBagCount = 3;
        public int hardBagCount = 4;

        [Header("Items Per Round")]
        [Tooltip("Total number of items to sort per round")]
        public int beginnerItemsPerRound = 4;
        public int easyItemsPerRound = 6;
        public int mediumItemsPerRound = 6;
        public int advancedItemsPerRound = 8;
        public int hardItemsPerRound = 8;

        [Header("Score Per Correct Placement")]
        [Tooltip("Points awarded for correct item placement per difficulty tier")]
        public int beginnerScore = 100;
        public int easyScore = 125;
        public int mediumScore = 150;
        public int advancedScore = 175;
        public int hardScore = 200;

        [Header("Penalty Per Wrong Placement")]
        [Tooltip("Points subtracted for wrong placement per difficulty tier")]
        public int beginnerPenalty = 0;
        public int easyPenalty = 20;
        public int mediumPenalty = 40;
        public int advancedPenalty = 60;
        public int hardPenalty = 80;

        [Header("Combo System")]
        [Tooltip("Number of consecutive correct placements needed for combo bonus")]
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
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 50f,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 50f,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 55f,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 55f,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 60f,
                _ => 50f
            };
        }

        public int GetTotalLevels() => hard.maxLevel;

        public float GetPreviewDuration(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerPreviewDuration,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyPreviewDuration,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumPreviewDuration,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedPreviewDuration,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardPreviewDuration,
                _ => beginnerPreviewDuration
            };
        }

        public int GetBagCount(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerBagCount,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyBagCount,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumBagCount,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedBagCount,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardBagCount,
                _ => beginnerBagCount
            };
        }

        public int GetItemsPerRound(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerItemsPerRound,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyItemsPerRound,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumItemsPerRound,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedItemsPerRound,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardItemsPerRound,
                _ => beginnerItemsPerRound
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

        public int[] GetItemPoolForLevel(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerItemPool,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyItemPool,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumItemPool,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedItemPool,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardItemPool,
                _ => beginnerItemPool
            };
        }

        public CandyItemDefinition GetItemDefinition(int itemID)
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
