using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public enum FruitType
    {
        Apple,
        Banana,
        Orange,
        Grape,
        Strawberry,
        Watermelon,
        Pineapple,
        Cherry,
        Lemon,
        Blueberry
    }

    public class FruitData
    {
        public FruitType type;
        public GameObject instance;
        public Vector3 startPosition;
        public float fallSpeed;
        public bool isTarget;
    }

    public class KiqqiColorActionManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Core References")]
        [SerializeField] private KiqqiColorActionLevelManager levelLogic;
        [SerializeField] private KiqqiColorActionAssetDB assetDB;

        [Header("Boundaries")]
        [SerializeField] private float bottomBoundary = -600f;
        [Tooltip("Height above spawn point where fruit grows before falling")]
        [SerializeField] private float growHeight = 50f;

        [Header("Timer Settings")]
        [Tooltip("Don't spawn new fruit when this much time is left (seconds)")]
        public float noNewFruitThreshold = 2f;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;

        protected KiqqiColorActionView view;
        protected KiqqiInputController input;

        private List<Transform> spawnPoints = new();
        private List<FruitData> activeFruits = new();
        private List<FruitType> currentTargetFruits = new();

        private Transform lastSpawnPoint = null;
        private Transform secondLastSpawnPoint = null;

        private int currentComboStreak = 0;
        private int fruitsCollected = 0;
        private int mistakeCount = 0;

        private float nextSpawnTime = 0f;
        private Coroutine spawnCoroutine;

        private List<FruitType> cachedNonTargetFruits = new();
        private FruitType[] allFruitTypesCache;

        private List<Transform> availableSpawnPointsCache = new();
        private WaitForSeconds hangWait;
        private Vector3 cachedVectorDown = Vector3.down;

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiColorActionView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiColorActionView>();
            input = context.Input;

            allFruitTypesCache = (FruitType[])System.Enum.GetValues(typeof(FruitType));
            hangWait = new WaitForSeconds(0.1f);

            CacheSpawnPoints();

            Debug.Log("[KiqqiColorActionManager] Initialized.");
        }

        private void CacheSpawnPoints()
        {
            spawnPoints.Clear();

            var spawnPointGroups = levelLogic.GetActiveSpawnPointGroups(app.Levels.currentLevel);

            if (spawnPointGroups == null || spawnPointGroups.Count == 0)
            {
                Debug.LogError("[KiqqiColorActionManager] No spawn point groups available!");
                return;
            }

            foreach (var group in spawnPointGroups)
            {
                if (group != null && group.gameObject.activeSelf)
                {
                    for (int i = 0; i < group.childCount; i++)
                    {
                        spawnPoints.Add(group.GetChild(i));
                    }
                }
            }

            Debug.Log($"[KiqqiColorActionManager] Cached {spawnPoints.Count} spawn points from {spawnPointGroups.Count} groups.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            sessionRunning = false;
            timeExpired = false;
            isActive = false;
            isComplete = true;

            ClearAllFruits();
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            Debug.Log("[KiqqiColorActionManager] OnMiniGameExit -> cleaned up.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            sessionScore = 0;
            masterGame.CurrentScore = 0;
            currentComboStreak = 0;
            fruitsCollected = 0;
            mistakeCount = 0;
            sessionRunning = true;
            timeExpired = false;

            ClearAllFruits();
            CacheSpawnPoints();
            SelectTargetFruits();

            view?.ShowTargetFruits(currentTargetFruits);

            spawnCoroutine = StartCoroutine(SpawnFruitLoop());

            Debug.Log("[KiqqiColorActionManager] Session started.");
        }

        private void SelectTargetFruits()
        {
            currentTargetFruits.Clear();

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            int targetCount = config.targetFruitCount;

            int activeFruitCount = assetDB != null ? assetDB.GetActiveFruitCount() : 10;
            int availableCount = Mathf.Min(activeFruitCount, allFruitTypesCache.Length);

            for (int i = 0; i < targetCount && i < availableCount; i++)
            {
                FruitType selectedFruit;
                int attempts = 0;
                do
                {
                    selectedFruit = (FruitType)Random.Range(0, availableCount);
                    attempts++;
                } while (currentTargetFruits.Contains(selectedFruit) && attempts < 20);

                if (!currentTargetFruits.Contains(selectedFruit))
                {
                    currentTargetFruits.Add(selectedFruit);
                }
            }

            UpdateNonTargetCache();
        }

        private void UpdateNonTargetCache()
        {
            cachedNonTargetFruits.Clear();
            int activeFruitCount = assetDB != null ? assetDB.GetActiveFruitCount() : 10;

            for (int i = 0; i < activeFruitCount && i < allFruitTypesCache.Length; i++)
            {
                FruitType fruitType = (FruitType)i;
                if (!currentTargetFruits.Contains(fruitType))
                {
                    cachedNonTargetFruits.Add(fruitType);
                }
            }
        }

        public void NotifyTimeExpired()
        {
            timeExpired = true;
            Debug.Log("[KiqqiColorActionManager] Time expired - waiting for final fruit interaction.");

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            StartCoroutine(WaitForFinalFruits());
        }

        private IEnumerator WaitForFinalFruits()
        {
            yield return new WaitForSeconds(0.5f);

            while (activeFruits.Count > 0)
            {
                yield return null;
            }

            Debug.Log("[KiqqiColorActionManager] All fruit cleared after time expired - ending session.");
            EndSession();
        }

        #endregion

        #region FRUIT SPAWNING & MANAGEMENT

        private IEnumerator SpawnFruitLoop()
        {
            while (sessionRunning && !timeExpired)
            {
                float timeLeft = view?.RemainingTime ?? 999f;
                if (timeLeft <= noNewFruitThreshold)
                {
                    Debug.Log($"[KiqqiColorActionManager] Stopping spawn - only {timeLeft:F1}s left.");
                    yield break;
                }

                yield return new WaitForSeconds(GetNextSpawnDelay());

                if (sessionRunning && !timeExpired)
                {
                    SpawnFruit();
                }
            }
        }

        private float GetNextSpawnDelay()
        {
            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            return Random.Range(config.spawnDelayMin, config.spawnDelayMax);
        }

        private void SpawnFruit()
        {
            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning("[KiqqiColorActionManager] No spawn points available!");
                return;
            }

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);

            Transform spawnPoint = SelectSpawnPoint();

            FruitType fruitType = SelectRandomFruitType(config);
            bool isTarget = currentTargetFruits.Contains(fruitType);

            if (assetDB == null || assetDB.fruitTemplate == null)
            {
                Debug.LogError("[KiqqiColorActionManager] AssetDB or fruitTemplate not assigned!");
                return;
            }

            GameObject fruitObj = Instantiate(assetDB.fruitTemplate, view.transform);
            fruitObj.SetActive(true);
            fruitObj.transform.position = spawnPoint.position;

            var fruitImage = fruitObj.GetComponent<UnityEngine.UI.Image>();
            if (fruitImage)
            {
                fruitImage.sprite = assetDB.GetFruitSprite(fruitType);
            }

            var button = fruitObj.GetComponent<UnityEngine.UI.Button>();
            if (button)
            {
                FruitData fruitData = new FruitData
                {
                    type = fruitType,
                    instance = fruitObj,
                    startPosition = spawnPoint.position,
                    fallSpeed = Random.Range(config.fallSpeedMin, config.fallSpeedMax),
                    isTarget = isTarget
                };

                activeFruits.Add(fruitData);

                var clickHandler = fruitObj.AddComponent<FruitClickHandler>();
                clickHandler.Initialize(this, fruitData);

                StartCoroutine(GrowAndFallFruit(fruitData));
            }
        }

        private Transform SelectSpawnPoint()
        {
            if (spawnPoints.Count == 1)
            {
                return spawnPoints[0];
            }

            if (spawnPoints.Count == 2)
            {
                Transform selected = spawnPoints[Random.Range(0, 2)];
                if (selected == lastSpawnPoint)
                {
                    selected = spawnPoints[0] == lastSpawnPoint ? spawnPoints[1] : spawnPoints[0];
                }
                secondLastSpawnPoint = lastSpawnPoint;
                lastSpawnPoint = selected;
                return selected;
            }

            availableSpawnPointsCache.Clear();
            
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Transform point = spawnPoints[i];
                if (point != lastSpawnPoint && point != secondLastSpawnPoint)
                {
                    availableSpawnPointsCache.Add(point);
                }
            }

            if (availableSpawnPointsCache.Count == 0)
            {
                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    availableSpawnPointsCache.Add(spawnPoints[i]);
                }
            }

            Transform chosenPoint = availableSpawnPointsCache[Random.Range(0, availableSpawnPointsCache.Count)];
            secondLastSpawnPoint = lastSpawnPoint;
            lastSpawnPoint = chosenPoint;
            
            return chosenPoint;
        }

        private FruitType SelectRandomFruitType(ColorActionDifficultyConfig config)
        {
            float targetChance = config.targetFruitSpawnChance;

            if (Random.value <= targetChance && currentTargetFruits.Count > 0)
            {
                return currentTargetFruits[Random.Range(0, currentTargetFruits.Count)];
            }
            else
            {
                if (cachedNonTargetFruits.Count > 0)
                {
                    return cachedNonTargetFruits[Random.Range(0, cachedNonTargetFruits.Count)];
                }
                else
                {
                    int activeFruitCount = assetDB != null ? assetDB.GetActiveFruitCount() : 10;
                    return (FruitType)Random.Range(0, activeFruitCount);
                }
            }
        }

        private IEnumerator GrowAndFallFruit(FruitData fruit)
        {
            float growDuration = 0.3f;

            Transform fruitTransform = fruit.instance.transform;
            fruitTransform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < growDuration && fruit.instance != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / growDuration;
                float eased = Mathf.SmoothStep(0f, 1f, t);
                fruitTransform.localScale = new Vector3(eased, eased, eased);
                yield return null;
            }

            if (fruit.instance != null)
            {
                fruitTransform.localScale = Vector3.one;
            }

            yield return hangWait;

            float speedDelta = fruit.fallSpeed * Time.deltaTime;
            while (fruit.instance != null && fruitTransform.position.y > bottomBoundary)
            {
                Vector3 pos = fruitTransform.position;
                pos.y -= speedDelta;
                fruitTransform.position = pos;
                speedDelta = fruit.fallSpeed * Time.deltaTime;
                yield return null;
            }

            if (fruit.instance != null)
            {
                RemoveFruit(fruit, false);
            }
        }

        public void OnFruitClicked(FruitData fruit)
        {
            if (!isActive || isComplete || !sessionRunning)
            {
                Debug.LogWarning("[KiqqiColorActionManager] Fruit clicked but session not active.");
                return;
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            bool correct = fruit.isTarget;

            if (correct)
            {
                currentComboStreak++;
                fruitsCollected++;

                var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
                int baseScore = config.correctScore;

                int earnedScore = baseScore;
                if (currentComboStreak >= config.comboThreshold)
                {
                    earnedScore = Mathf.RoundToInt(baseScore * config.comboMultiplier);
                    Debug.Log($"[KiqqiColorActionManager] Combo bonus! Streak={currentComboStreak}, Score={earnedScore}");
                }

                sessionScore += earnedScore;
                masterGame.AddScore(earnedScore);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowCatchFeedback(fruit.instance.transform.position, true, earnedScore);

                app.Levels.NextLevel();
            }
            else
            {
                currentComboStreak = 0;
                mistakeCount++;

                var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
                int penalty = config.wrongPenalty;

                sessionScore = Mathf.Max(0, sessionScore - penalty);
                masterGame.AddScore(-penalty);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowCatchFeedback(fruit.instance.transform.position, false, penalty);

                Debug.Log($"[KiqqiColorActionManager] Wrong fruit! Penalty = {penalty}");
            }

            RemoveFruit(fruit, true);
        }

        private void RemoveFruit(FruitData fruit, bool wasClicked)
        {
            if (fruit.instance != null)
            {
                if (!wasClicked)
                {
                    StartCoroutine(FadeFruitOut(fruit.instance));
                }
                else
                {
                    Destroy(fruit.instance);
                }
            }

            int index = activeFruits.IndexOf(fruit);
            if (index >= 0)
            {
                activeFruits[index] = activeFruits[activeFruits.Count - 1];
                activeFruits.RemoveAt(activeFruits.Count - 1);
            }
        }

        private IEnumerator FadeFruitOut(GameObject fruitObj)
        {
            var canvasGroup = fruitObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = fruitObj.AddComponent<CanvasGroup>();

            float fadeDuration = 0.3f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            Destroy(fruitObj);
        }

        private void ClearAllFruits()
        {
            foreach (var fruit in activeFruits)
            {
                if (fruit.instance != null)
                {
                    Destroy(fruit.instance);
                }
            }

            activeFruits.Clear();
        }

        #endregion

        #region SESSION MANAGEMENT

        public void EndSession()
        {
            if (!sessionRunning) return;

            sessionRunning = false;

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            ClearAllFruits();

            Debug.Log($"[KiqqiColorActionManager] Session ended. Final Score={sessionScore}, Fruits Collected={fruitsCollected}");
            CompleteMiniGame(sessionScore, true);
        }

        public void ResumeFromPause(KiqqiColorActionView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;

            view?.UpdateScoreLabel(masterGame.CurrentScore);

            if (spawnCoroutine == null && !timeExpired)
            {
                spawnCoroutine = StartCoroutine(SpawnFruitLoop());
            }

            Debug.Log("[KiqqiColorActionManager] Resumed from pause.");
        }

        #endregion

        #region UTILITY

        public int GetCurrentComboStreak() => currentComboStreak;
        public int GetFruitsCollected() => fruitsCollected;
        public int GetMistakeCount() => mistakeCount;

        #endregion
    }

    public class FruitClickHandler : MonoBehaviour
    {
        private KiqqiColorActionManager manager;
        private FruitData fruitData;

        public void Initialize(KiqqiColorActionManager mgr, FruitData data)
        {
            manager = mgr;
            fruitData = data;

            var button = GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (manager != null)
            {
                manager.OnFruitClicked(fruitData);
            }
        }
    }
}
