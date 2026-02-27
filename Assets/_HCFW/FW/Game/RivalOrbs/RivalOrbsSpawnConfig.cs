using System;
using UnityEngine;

namespace Kiqqi.Framework
{
    [Serializable]
    public class RivalOrbsSpawnSetup
    {
        [Header("Spawn Containers")]
        [Tooltip("Parent container with spawn points as children")]
        public Transform topSpawnsContainer;

        [Tooltip("Parent container with spawn points as children")]
        public Transform bottomSpawnsContainer;

        [HideInInspector]
        public Transform[] topSpawnPoints;

        [HideInInspector]
        public Transform[] bottomSpawnPoints;

        public void CacheSpawnPoints()
        {
            if (topSpawnsContainer != null)
            {
                int childCount = topSpawnsContainer.childCount;
                topSpawnPoints = new Transform[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    topSpawnPoints[i] = topSpawnsContainer.GetChild(i);
                }
            }

            if (bottomSpawnsContainer != null)
            {
                int childCount = bottomSpawnsContainer.childCount;
                bottomSpawnPoints = new Transform[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    bottomSpawnPoints[i] = bottomSpawnsContainer.GetChild(i);
                }
            }
        }

        public bool IsValid()
        {
            return topSpawnPoints != null && topSpawnPoints.Length > 0 &&
                   bottomSpawnPoints != null && bottomSpawnPoints.Length > 0;
        }

        public int TotalSpawnCount => (topSpawnPoints?.Length ?? 0) + (bottomSpawnPoints?.Length ?? 0);
    }

    [Serializable]
    public class RivalOrbsDifficultySpawns
    {
        [Header("Difficulty Spawn Configuration")]
        public KiqqiLevelManager.KiqqiDifficulty difficulty;
        public RivalOrbsSpawnSetup spawnSetup;
    }
}
