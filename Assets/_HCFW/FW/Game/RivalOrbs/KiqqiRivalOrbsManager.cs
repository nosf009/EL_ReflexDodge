using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public enum OrbSide
    {
        Top,
        Bottom
    }

    public class OrbData
    {
        public int id;
        public OrbSide correctSide;
        public GameObject instance;
        public RectTransform rectTransform;
        public Vector2 velocity;
        public bool isOnCorrectSide;
    }

    public class KiqqiRivalOrbsManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Core References")]
        [SerializeField] private KiqqiRivalOrbsLevelManager levelLogic;

        [Header("Gameplay Area")]
        [SerializeField] private RectTransform gameplayBounds;
        [SerializeField] private RectTransform topArea;
        [SerializeField] private RectTransform bottomArea;
        [SerializeField] private RectTransform barrier;
        [SerializeField] private RectTransform barrierGap;

        [Header("Spawn Configuration (Direct References)")]
        [SerializeField] private RivalOrbsSpawnSetup beginnerSpawns;
        [SerializeField] private RivalOrbsSpawnSetup easySpawns;
        [SerializeField] private RivalOrbsSpawnSetup mediumSpawns;
        [SerializeField] private RivalOrbsSpawnSetup advancedSpawns;
        [SerializeField] private RivalOrbsSpawnSetup hardSpawns;

        [Header("Orb Prefabs")]
        [SerializeField] private GameObject topOrbPrefab;
        [SerializeField] private GameObject bottomOrbPrefab;
        [SerializeField] private Transform orbContainer;

        [Header("Barrier Movement")]
        [SerializeField] private float barrierMoveSpeed = 1000f;
        [SerializeField] private float barrierMinX = -200f;
        [SerializeField] private float barrierMaxX = 200f;

        [Header("Orb Settings")]
        [SerializeField] private float orbSize = 60f;
        [SerializeField] private float spawnMargin = 80f;
        [SerializeField] private bool enableOrbToOrbCollision = true;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;

        protected KiqqiRivalOrbsView view;
        protected KiqqiInputController input;

        private List<OrbData> activeOrbs = new List<OrbData>();
        private int orbIdCounter = 0;

        private float targetBarrierX = 0f;
        private float currentBarrierX = 0f;

        private RivalOrbsDifficultyConfig currentConfig;

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiRivalOrbsView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);
            input = app.Input;

            CacheSpawnPoints();
        }

        private void CacheSpawnPoints()
        {
            beginnerSpawns?.CacheSpawnPoints();
            easySpawns?.CacheSpawnPoints();
            mediumSpawns?.CacheSpawnPoints();
            advancedSpawns?.CacheSpawnPoints();
            hardSpawns?.CacheSpawnPoints();
        }

        #endregion

        #region MINI GAME LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            view = app.UI.GetView<KiqqiRivalOrbsView>();
            if (view == null)
            {
                Debug.LogError("[KiqqiRivalOrbsManager] View not found!");
                return;
            }

            view.OnBarrierSwipe += OnBarrierSwipe;
            StartGameSession();
        }

        private void StartGameSession()
        {
            int currentLevel = app.Levels.currentLevel;
            currentConfig = levelLogic.GetDifficultyConfig(currentLevel);

            SetupBarrier();
            SpawnOrbs();

            sessionRunning = true;
            timeExpired = false;
        }

        private void SetupBarrier()
        {
            if (barrierGap != null)
            {
                barrierGap.sizeDelta = new Vector2(barrierGap.sizeDelta.x, currentConfig.gapSize);
            }

            currentBarrierX = 0f;
            targetBarrierX = 0f;
            if (barrier != null)
            {
                barrier.anchoredPosition = new Vector2(currentBarrierX, barrier.anchoredPosition.y);
            }
        }

        private void SpawnOrbs()
        {
            ClearOrbs();

            RivalOrbsSpawnSetup currentSpawnSetup = GetCurrentDifficultySpawnSetup();
            
            if (currentSpawnSetup == null || !currentSpawnSetup.IsValid())
            {
                Debug.LogError("[RivalOrbs] Invalid spawn setup!");
                return;
            }

            int topCount = Mathf.Min(currentConfig.objectCount / 2, currentSpawnSetup.topSpawnPoints.Length);
            int bottomCount = Mathf.Min(currentConfig.objectCount - topCount, currentSpawnSetup.bottomSpawnPoints.Length);

            for (int i = 0; i < topCount; i++)
            {
                Transform spawnPoint = currentSpawnSetup.topSpawnPoints[i];
                if (spawnPoint != null)
                {
                    RectTransform rectTransform = spawnPoint.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Vector2 spawnPos = rectTransform.anchoredPosition;
                        SpawnOrbAtPosition(OrbSide.Bottom, spawnPos);
                    }
                }
            }

            for (int i = 0; i < bottomCount; i++)
            {
                Transform spawnPoint = currentSpawnSetup.bottomSpawnPoints[i];
                if (spawnPoint != null)
                {
                    RectTransform rectTransform = spawnPoint.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Vector2 spawnPos = rectTransform.anchoredPosition;
                        SpawnOrbAtPosition(OrbSide.Top, spawnPos);
                    }
                }
            }
        }

        private RivalOrbsSpawnSetup GetCurrentDifficultySpawnSetup()
        {
            int currentLevel = app.Levels.currentLevel;
            var difficulty = levelLogic.GetCurrentDifficulty(currentLevel);

            return difficulty switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerSpawns,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easySpawns,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumSpawns,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedSpawns,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardSpawns,
                _ => beginnerSpawns
            };
        }

        private void SpawnOrbAtPosition(OrbSide correctSide, Vector2 spawnPos)
        {
            GameObject prefab = correctSide == OrbSide.Top ? topOrbPrefab : bottomOrbPrefab;
            
            if (prefab == null || orbContainer == null)
            {
                Debug.LogError("[RivalOrbs] Missing prefab or container!");
                return;
            }

            GameObject orbInstance = Instantiate(prefab, orbContainer);
            RectTransform rt = orbInstance.GetComponent<RectTransform>();

            rt.anchoredPosition = spawnPos;
            rt.sizeDelta = new Vector2(orbSize, orbSize);

            OrbData orb = new OrbData
            {
                id = orbIdCounter++,
                correctSide = correctSide,
                instance = orbInstance,
                rectTransform = rt,
                velocity = GetRandomVelocity(),
                isOnCorrectSide = false
            };

            activeOrbs.Add(orb);
            orbInstance.SetActive(true);
        }

        private void SpawnOrb(OrbSide correctSide)
        {
            GameObject prefab = correctSide == OrbSide.Top ? topOrbPrefab : bottomOrbPrefab;
            if (prefab == null || orbContainer == null)
            {
                Debug.LogError("[RivalOrbs] Missing prefab or container!");
                return;
            }

            GameObject orbInstance = Instantiate(prefab, orbContainer);
            RectTransform rt = orbInstance.GetComponent<RectTransform>();

            Vector2 spawnPos = GetRandomSpawnPosition();
            rt.anchoredPosition = spawnPos;
            rt.sizeDelta = new Vector2(orbSize, orbSize);

            OrbData orb = new OrbData
            {
                id = orbIdCounter++,
                correctSide = correctSide,
                instance = orbInstance,
                rectTransform = rt,
                velocity = GetRandomVelocity(),
                isOnCorrectSide = false
            };

            activeOrbs.Add(orb);
            orbInstance.SetActive(true);
        }

        private Vector2 GetRandomSpawnPosition()
        {
            if (gameplayBounds == null) return Vector2.zero;

            Rect bounds = gameplayBounds.rect;
            float halfOrbSize = orbSize / 2f;

            float minX = bounds.xMin + halfOrbSize + spawnMargin;
            float maxX = bounds.xMax - halfOrbSize - spawnMargin;
            float minY = bounds.yMin + halfOrbSize + spawnMargin;
            float maxY = bounds.yMax - halfOrbSize - spawnMargin;

            int side = Random.Range(0, 2);
            float x, y;

            if (side == 0)
            {
                x = Random.Range(minX, maxX);
                y = Random.Range(minY, 0f - halfOrbSize);
            }
            else
            {
                x = Random.Range(minX, maxX);
                y = Random.Range(0f + halfOrbSize, maxY);
            }

            return new Vector2(x, y);
        }

        private Vector2 GetRandomVelocity()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return direction.normalized * currentConfig.objectSpeed;
        }

        private void ClearOrbs()
        {
            foreach (var orb in activeOrbs)
            {
                if (orb.instance != null)
                {
                    Destroy(orb.instance);
                }
            }
            activeOrbs.Clear();
        }

        #endregion

        #region INPUT HANDLING

        private void OnBarrierSwipe(Vector2 swipeDelta)
        {
            if (!sessionRunning) return;

            targetBarrierX += swipeDelta.x * 0.5f;
            targetBarrierX = Mathf.Clamp(targetBarrierX, barrierMinX, barrierMaxX);
        }

        #endregion

        #region UPDATE LOOP

        public override void TickMiniGame()
        {
            if (!sessionRunning) return;

            UpdateBarrierPosition();
            UpdateOrbs();
            CheckWinCondition();
        }

        private void UpdateBarrierPosition()
        {
            if (barrier == null) return;

            currentBarrierX = Mathf.Lerp(currentBarrierX, targetBarrierX, Time.deltaTime * 10f);
            barrier.anchoredPosition = new Vector2(currentBarrierX, barrier.anchoredPosition.y);
        }

        private void UpdateOrbs()
        {
            foreach (var orb in activeOrbs)
            {
                if (orb.instance == null) continue;

                Vector2 currentPos = orb.rectTransform.anchoredPosition;
                Vector2 newPos = currentPos + orb.velocity * Time.deltaTime;

                newPos = HandleBoundsCollision(newPos, orb);
                newPos = HandleBarrierCollision(newPos, currentPos, orb);

                orb.rectTransform.anchoredPosition = newPos;

                CheckOrbSide(orb);
            }

            if (enableOrbToOrbCollision)
            {
                HandleOrbToOrbCollisions();
            }
        }

        private void HandleOrbToOrbCollisions()
        {
            for (int i = 0; i < activeOrbs.Count; i++)
            {
                for (int j = i + 1; j < activeOrbs.Count; j++)
                {
                    OrbData orbA = activeOrbs[i];
                    OrbData orbB = activeOrbs[j];

                    if (orbA.instance == null || orbB.instance == null) continue;

                    Vector2 posA = orbA.rectTransform.anchoredPosition;
                    Vector2 posB = orbB.rectTransform.anchoredPosition;

                    float distance = Vector2.Distance(posA, posB);
                    float minDistance = orbSize;

                    if (distance < minDistance && distance > 0.1f)
                    {
                        Vector2 collisionNormal = (posA - posB).normalized;

                        float overlap = minDistance - distance;
                        orbA.rectTransform.anchoredPosition = posA + collisionNormal * (overlap * 0.5f);
                        orbB.rectTransform.anchoredPosition = posB - collisionNormal * (overlap * 0.5f);

                        float speedA = orbA.velocity.magnitude;
                        float speedB = orbB.velocity.magnitude;
                        
                        float randomAngleA = Random.Range(-30f, 30f);
                        float randomAngleB = Random.Range(-30f, 30f);
                        
                        float baseAngleA = Mathf.Atan2(collisionNormal.y, collisionNormal.x) * Mathf.Rad2Deg;
                        float baseAngleB = Mathf.Atan2(-collisionNormal.y, -collisionNormal.x) * Mathf.Rad2Deg;
                        
                        float newAngleA = baseAngleA + randomAngleA;
                        float newAngleB = baseAngleB + randomAngleB;
                        
                        orbA.velocity = new Vector2(
                            Mathf.Cos(newAngleA * Mathf.Deg2Rad),
                            Mathf.Sin(newAngleA * Mathf.Deg2Rad)
                        ) * speedA;
                        
                        orbB.velocity = new Vector2(
                            Mathf.Cos(newAngleB * Mathf.Deg2Rad),
                            Mathf.Sin(newAngleB * Mathf.Deg2Rad)
                        ) * speedB;
                    }
                }
            }
        }

        private Vector2 HandleBoundsCollision(Vector2 pos, OrbData orb)
        {
            if (gameplayBounds == null) return pos;

            Rect bounds = gameplayBounds.rect;
            float halfSize = orbSize / 2f;

            if (pos.x - halfSize < bounds.xMin)
            {
                pos.x = bounds.xMin + halfSize;
                BounceWithRandomAngle(orb, true);
            }
            else if (pos.x + halfSize > bounds.xMax)
            {
                pos.x = bounds.xMax - halfSize;
                BounceWithRandomAngle(orb, true);
            }

            if (pos.y - halfSize < bounds.yMin)
            {
                pos.y = bounds.yMin + halfSize;
                BounceWithRandomAngle(orb, false);
            }
            else if (pos.y + halfSize > bounds.yMax)
            {
                pos.y = bounds.yMax - halfSize;
                BounceWithRandomAngle(orb, false);
            }

            return pos;
        }

        private void BounceWithRandomAngle(OrbData orb, bool reflectX)
        {
            float currentSpeed = orb.velocity.magnitude;
            float randomAngleOffset = Random.Range(-30f, 30f);
            
            if (reflectX)
            {
                float currentAngle = Mathf.Atan2(orb.velocity.y, orb.velocity.x) * Mathf.Rad2Deg;
                float newAngle = 180f - currentAngle + randomAngleOffset;
                orb.velocity = new Vector2(
                    Mathf.Cos(newAngle * Mathf.Deg2Rad),
                    Mathf.Sin(newAngle * Mathf.Deg2Rad)
                ) * currentSpeed;
            }
            else
            {
                float currentAngle = Mathf.Atan2(orb.velocity.y, orb.velocity.x) * Mathf.Rad2Deg;
                float newAngle = -currentAngle + randomAngleOffset;
                orb.velocity = new Vector2(
                    Mathf.Cos(newAngle * Mathf.Deg2Rad),
                    Mathf.Sin(newAngle * Mathf.Deg2Rad)
                ) * currentSpeed;
            }
        }

        private Vector2 HandleBarrierCollision(Vector2 newPos, Vector2 oldPos, OrbData orb)
        {
            if (barrier == null || barrierGap == null) return newPos;

            float barrierHeight = barrier.sizeDelta.y;
            float barrierTop = barrierHeight / 2f;
            float barrierBottom = -barrierHeight / 2f;

            float gapLeft = currentBarrierX - (currentConfig.gapSize / 2f);
            float gapRight = currentBarrierX + (currentConfig.gapSize / 2f);

            float halfSize = orbSize / 2f;

            bool crossingFromTop = (oldPos.y - halfSize > barrierTop) && (newPos.y - halfSize <= barrierTop);
            bool crossingFromBottom = (oldPos.y + halfSize < barrierBottom) && (newPos.y + halfSize >= barrierBottom);

            if (crossingFromTop || crossingFromBottom)
            {
                bool inGap = (newPos.x + halfSize >= gapLeft) && (newPos.x - halfSize <= gapRight);

                if (!inGap)
                {
                    BounceWithRandomAngle(orb, false);
                    
                    if (crossingFromTop)
                        newPos.y = barrierTop + halfSize;
                    else
                        newPos.y = barrierBottom - halfSize;
                }
            }

            return newPos;
        }

        private void CheckOrbSide(OrbData orb)
        {
            bool wasOnCorrectSide = orb.isOnCorrectSide;
            float yPos = orb.rectTransform.anchoredPosition.y;

            if (orb.correctSide == OrbSide.Top)
            {
                orb.isOnCorrectSide = yPos > 0f;
            }
            else
            {
                orb.isOnCorrectSide = yPos < 0f;
            }
        }

        private void CheckWinCondition()
        {
            if (activeOrbs.Count == 0) return;

            bool allCorrect = true;
            foreach (var orb in activeOrbs)
            {
                if (!orb.isOnCorrectSide)
                {
                    allCorrect = false;
                    break;
                }
            }

            if (allCorrect)
            {
                OnLevelComplete();
            }
        }

        #endregion

        #region GAME END

        private void OnLevelComplete()
        {
            if (!sessionRunning) return;

            sessionRunning = false;

            int finalScore = currentConfig.levelCompleteScore;
            app.Levels.currentLevel++;
            app.Data.SetInt("currentLevel", app.Levels.currentLevel);

            CompleteMiniGame(finalScore, true);
        }

        public void OnTimeExpired()
        {
            if (!sessionRunning) return;

            sessionRunning = false;
            timeExpired = true;

            CompleteMiniGame(0, false);
        }

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();
            ClearOrbs();
            sessionRunning = false;
            timeExpired = false;
        }

        #endregion

        #region CLEANUP

        private void OnDestroy()
        {
            if (view != null)
            {
                view.OnBarrierSwipe -= OnBarrierSwipe;
            }
            ClearOrbs();
        }

        #endregion
    }
}
