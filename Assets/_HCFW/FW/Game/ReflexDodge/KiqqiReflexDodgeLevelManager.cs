using UnityEngine;

namespace Kiqqi.Framework
{
    public class KiqqiReflexDodgeLevelManager : MonoBehaviour, IKiqqiSubLevelManager
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty Ranges")]
        public DifficultyRange beginner = new() { minLevel = 1, maxLevel = 12 };
        public DifficultyRange easy = new() { minLevel = 13, maxLevel = 24 };
        public DifficultyRange medium = new() { minLevel = 25, maxLevel = 36 };
        public DifficultyRange advanced = new() { minLevel = 37, maxLevel = 48 };
        public DifficultyRange hard = new() { minLevel = 49, maxLevel = 60 };

        [Header("═══ BEGINNER (Levels 1-12) ═══")]
        [Tooltip("Max simultaneous zones")] public int beginnerMaxZones = 2;
        [Tooltip("Spawn interval (sec)")] public float beginnerSpawnInterval = 2.0f;
        [Tooltip("Warning duration (sec)")] public float beginnerWarningDuration = 1.8f;
        [Tooltip("Zone radius min/max")] public float beginnerMinRadius = 80f;
        public float beginnerMaxRadius = 120f;
        [Tooltip("Behavior chances (0-1)")] public float beginnerExpandingChance = 0f;
        public float beginnerChasingChance = 0f;
        public float beginnerChaseSpeed = 0f;
        [Tooltip("Scoring")] public int beginnerDodgeScore = 50;
        public int beginnerHitPenalty = 10;
        public int beginnerStreakThreshold = 3;
        public float beginnerStreakMultiplier = 1.5f;

        [Header("═══ EASY (Levels 13-24) ═══")]
        public int easyMaxZones = 3;
        public float easySpawnInterval = 1.5f;
        public float easyWarningDuration = 1.5f;
        public float easyMinRadius = 70f;
        public float easyMaxRadius = 110f;
        public float easyExpandingChance = 0.2f;
        public float easyChasingChance = 0f;
        public float easyChaseSpeed = 0f;
        public int easyDodgeScore = 75;
        public int easyHitPenalty = 25;
        public int easyStreakThreshold = 3;
        public float easyStreakMultiplier = 2.0f;

        [Header("═══ MEDIUM (Levels 25-36) ═══")]
        public int mediumMaxZones = 4;
        public float mediumSpawnInterval = 1.2f;
        public float mediumWarningDuration = 1.2f;
        public float mediumMinRadius = 60f;
        public float mediumMaxRadius = 100f;
        public float mediumExpandingChance = 0.3f;
        public float mediumChasingChance = 0.1f;
        public float mediumChaseSpeed = 100f;
        public int mediumDodgeScore = 100;
        public int mediumHitPenalty = 40;
        public int mediumStreakThreshold = 4;
        public float mediumStreakMultiplier = 2.5f;

        [Header("═══ ADVANCED (Levels 37-48) ═══")]
        public int advancedMaxZones = 5;
        public float advancedSpawnInterval = 1.0f;
        public float advancedWarningDuration = 1.0f;
        public float advancedMinRadius = 50f;
        public float advancedMaxRadius = 90f;
        public float advancedExpandingChance = 0.3f;
        public float advancedChasingChance = 0.25f;
        public float advancedChaseSpeed = 150f;
        public int advancedDodgeScore = 150;
        public int advancedHitPenalty = 60;
        public int advancedStreakThreshold = 5;
        public float advancedStreakMultiplier = 3.0f;

        [Header("═══ HARD (Levels 49-60) ═══")]
        public int hardMaxZones = 6;
        public float hardSpawnInterval = 0.8f;
        public float hardWarningDuration = 0.8f;
        public float hardMinRadius = 40f;
        public float hardMaxRadius = 80f;
        public float hardExpandingChance = 0.4f;
        public float hardChasingChance = 0.35f;
        public float hardChaseSpeed = 200f;
        public int hardDodgeScore = 200;
        public int hardHitPenalty = 80;
        public int hardStreakThreshold = 6;
        public float hardStreakMultiplier = 4.0f;

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
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 60f,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 60f,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 60f,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 60f,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 60f,
                _ => 60f
            };
        }

        public int GetTotalLevels() => hard.maxLevel;

        public int GetMaxZones(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerMaxZones,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyMaxZones,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumMaxZones,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedMaxZones,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardMaxZones,
                _ => beginnerMaxZones
            };
        }

        public float GetSpawnInterval(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerSpawnInterval,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easySpawnInterval,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumSpawnInterval,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedSpawnInterval,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardSpawnInterval,
                _ => beginnerSpawnInterval
            };
        }

        public float GetWarningDuration(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerWarningDuration,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyWarningDuration,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumWarningDuration,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedWarningDuration,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardWarningDuration,
                _ => beginnerWarningDuration
            };
        }

        public Vector2 GetZoneRadiusRange(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => new Vector2(beginnerMinRadius, beginnerMaxRadius),
                KiqqiLevelManager.KiqqiDifficulty.Easy => new Vector2(easyMinRadius, easyMaxRadius),
                KiqqiLevelManager.KiqqiDifficulty.Medium => new Vector2(mediumMinRadius, mediumMaxRadius),
                KiqqiLevelManager.KiqqiDifficulty.Advanced => new Vector2(advancedMinRadius, advancedMaxRadius),
                KiqqiLevelManager.KiqqiDifficulty.Hard => new Vector2(hardMinRadius, hardMaxRadius),
                _ => new Vector2(beginnerMinRadius, beginnerMaxRadius)
            };
        }

        public float GetExpandingChance(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerExpandingChance,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyExpandingChance,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumExpandingChance,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedExpandingChance,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardExpandingChance,
                _ => beginnerExpandingChance
            };
        }

        public float GetChasingChance(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerChasingChance,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyChasingChance,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumChasingChance,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedChasingChance,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardChasingChance,
                _ => beginnerChasingChance
            };
        }

        public float GetChaseSpeed(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerChaseSpeed,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyChaseSpeed,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumChaseSpeed,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedChaseSpeed,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardChaseSpeed,
                _ => beginnerChaseSpeed
            };
        }

        #endregion

        #region SCORING & PENALTIES

        public int GetDodgeScore(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerDodgeScore,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyDodgeScore,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumDodgeScore,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedDodgeScore,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardDodgeScore,
                _ => beginnerDodgeScore
            };
        }

        public int GetHitPenalty(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerHitPenalty,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyHitPenalty,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumHitPenalty,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedHitPenalty,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardHitPenalty,
                _ => beginnerHitPenalty
            };
        }

        public int GetStreakThreshold(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerStreakThreshold,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyStreakThreshold,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumStreakThreshold,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedStreakThreshold,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardStreakThreshold,
                _ => beginnerStreakThreshold
            };
        }

        public float GetStreakMultiplier(int level)
        {
            return GetCurrentDifficulty(level) switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerStreakMultiplier,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyStreakMultiplier,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumStreakMultiplier,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedStreakMultiplier,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardStreakMultiplier,
                _ => beginnerStreakMultiplier
            };
        }

        #endregion
    }
}
