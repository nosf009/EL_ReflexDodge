using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public enum MeatType
    {
        Steak,
        Sausage,
        Fish,
        Chicken,
        Burger,
        Ribs
    }

    public enum CookingState
    {
        Raw,
        Ready,
        Burnt
    }

    public class MeatItem
    {
        public MeatType type;
        public GameObject instance;
        public Image meatImage;
        public Image progressRing;
        public Button button;
        public CookingState currentState;
        public float cookingProgress;
        public float cookingSpeed;
        public Vector3 position;
        public bool isActive;
        public bool wasScored;
    }

    public class KiqqiBarbecueHeroManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Core References")]
        [SerializeField] private KiqqiBarbecueHeroLevelManager levelLogic;

        [Header("Spawning")]
        [Tooltip("Parent transform containing grill positions")]
        [SerializeField] private Transform grillPositionsParent;
        [Tooltip("Meat item prefab with Image for meat sprite and Image for progress ring")]
        [SerializeField] private GameObject meatItemPrefab;

        [Header("Cooking Zones")]
        [Tooltip("Raw zone ends at this progress (0-1)")]
        [SerializeField] private float rawZoneEnd = 0.3f;
        [Tooltip("Ready zone starts at this progress (0-1)")]
        [SerializeField] private float readyZoneStart = 0.3f;
        [Tooltip("Ready zone ends at this progress (0-1)")]
        [SerializeField] private float readyZoneEnd = 0.7f;
        [Tooltip("Burnt zone starts at this progress (0-1)")]
        [SerializeField] private float burntZoneStart = 0.7f;

        [Header("Timing Settings")]
        [Tooltip("Don't spawn new items when this much time is left (seconds)")]
        public float noNewItemsThreshold = 2.5f;

        [Header("Visual Settings")]
        [SerializeField] private Color rawColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color readyColor = new Color(1f, 0.6f, 0f);
        [SerializeField] private Color burntColor = new Color(1f, 0.2f, 0.2f);

        [Header("Meat Sprites")]
        [SerializeField] private Sprite steakSprite;
        [SerializeField] private Sprite sausageSprite;
        [SerializeField] private Sprite fishSprite;
        [SerializeField] private Sprite chickenSprite;
        [SerializeField] private Sprite burgerSprite;
        [SerializeField] private Sprite ribsSprite;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;

        protected KiqqiBarbecueHeroView view;
        protected KiqqiInputController input;

        private List<Transform> grillPositions = new();
        private List<MeatItem> activeMeatItems = new();

        private int currentComboStreak = 0;
        private int perfectTaps = 0;
        private int goodTaps = 0;
        private int badTaps = 0;

        private Coroutine spawnCoroutine;

        private List<Transform> availableGrillPositionsCache = new();
        private WaitForSeconds finalWaitTime;

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiBarbecueHeroView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiBarbecueHeroView>();
            input = context.Input;

            finalWaitTime = new WaitForSeconds(0.5f);

            CacheGrillPositions();

            Debug.Log("[KiqqiBarbecueHeroManager] Initialized.");
        }

        private void CacheGrillPositions()
        {
            grillPositions.Clear();

            if (grillPositionsParent == null)
            {
                Debug.LogError("[KiqqiBarbecueHeroManager] Grill positions parent not assigned!");
                return;
            }

            for (int i = 0; i < grillPositionsParent.childCount; i++)
            {
                grillPositions.Add(grillPositionsParent.GetChild(i));
            }

            Debug.Log($"[KiqqiBarbecueHeroManager] Cached {grillPositions.Count} grill positions.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            sessionRunning = false;
            timeExpired = false;
            isActive = false;
            isComplete = true;

            ClearAllMeatItems();
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            Debug.Log("[KiqqiBarbecueHeroManager] OnMiniGameExit -> cleaned up.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            sessionScore = 0;
            masterGame.CurrentScore = 0;
            currentComboStreak = 0;
            perfectTaps = 0;
            goodTaps = 0;
            badTaps = 0;
            sessionRunning = true;
            timeExpired = false;

            ClearAllMeatItems();
            CacheGrillPositions();

            spawnCoroutine = StartCoroutine(SpawnMeatLoop());

            Debug.Log("[KiqqiBarbecueHeroManager] Session started.");
        }

        public void NotifyTimeExpired()
        {
            timeExpired = true;
            Debug.Log("[KiqqiBarbecueHeroManager] Time expired - waiting for final meat interactions.");

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            StartCoroutine(WaitForFinalMeatItems());
        }

        private IEnumerator WaitForFinalMeatItems()
        {
            yield return finalWaitTime;

            while (activeMeatItems.Count > 0)
            {
                yield return null;
            }

            Debug.Log("[KiqqiBarbecueHeroManager] All meat cleared after time expired - ending session.");
            EndSession();
        }

        #endregion

        #region MEAT SPAWNING & MANAGEMENT

        private IEnumerator SpawnMeatLoop()
        {
            while (sessionRunning && !timeExpired)
            {
                float timeLeft = view?.RemainingTime ?? 999f;
                if (timeLeft <= noNewItemsThreshold)
                {
                    Debug.Log($"[KiqqiBarbecueHeroManager] Stopping spawn - only {timeLeft:F1}s left.");
                    yield break;
                }

                yield return new WaitForSeconds(GetNextSpawnDelay());

                if (sessionRunning && !timeExpired)
                {
                    SpawnMeatItem();
                }
            }
        }

        private float GetNextSpawnDelay()
        {
            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            return Random.Range(config.spawnDelayMin, config.spawnDelayMax);
        }

        private void SpawnMeatItem()
        {
            if (grillPositions.Count == 0)
            {
                Debug.LogWarning("[KiqqiBarbecueHeroManager] No grill positions available!");
                return;
            }

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);

            Transform spawnPos = SelectAvailableGrillPosition();
            if (spawnPos == null)
            {
                return;
            }

            if (meatItemPrefab == null)
            {
                Debug.LogError("[KiqqiBarbecueHeroManager] Meat item prefab not assigned!");
                return;
            }

            GameObject meatObj = Instantiate(meatItemPrefab, view.transform);
            meatObj.SetActive(true);
            meatObj.transform.position = spawnPos.position;

            MeatType meatType = SelectRandomMeatType(config);
            float cookingSpeed = Random.Range(config.cookingSpeedMin, config.cookingSpeedMax);

            MeatItem meatItem = new MeatItem
            {
                type = meatType,
                instance = meatObj,
                position = spawnPos.position,
                cookingProgress = 0f,
                cookingSpeed = cookingSpeed,
                currentState = CookingState.Raw,
                isActive = true,
                wasScored = false
            };

            meatItem.meatImage = meatObj.transform.Find("MeatSprite")?.GetComponent<Image>();
            meatItem.progressRing = meatObj.transform.Find("ProgressRing")?.GetComponent<Image>();
            meatItem.button = meatObj.GetComponent<Button>();

            if (meatItem.meatImage)
            {
                meatItem.meatImage.sprite = GetMeatSprite(meatType);
            }

            if (meatItem.progressRing)
            {
                meatItem.progressRing.fillAmount = 0f;
                meatItem.progressRing.color = rawColor;
            }

            if (meatItem.button)
            {
                meatItem.button.onClick.RemoveAllListeners();
                meatItem.button.onClick.AddListener(() => OnMeatItemTapped(meatItem));
            }

            activeMeatItems.Add(meatItem);
            StartCoroutine(CookMeatItem(meatItem));
        }

        private Transform SelectAvailableGrillPosition()
        {
            availableGrillPositionsCache.Clear();

            for (int i = 0; i < grillPositions.Count; i++)
            {
                Transform pos = grillPositions[i];
                bool occupied = false;
                
                for (int j = 0; j < activeMeatItems.Count; j++)
                {
                    MeatItem meat = activeMeatItems[j];
                    if (meat.isActive && Vector3.SqrMagnitude(meat.position - pos.position) < 100f)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    availableGrillPositionsCache.Add(pos);
                }
            }

            if (availableGrillPositionsCache.Count == 0)
            {
                return null;
            }

            return availableGrillPositionsCache[Random.Range(0, availableGrillPositionsCache.Count)];
        }

        private MeatType SelectRandomMeatType(BarbecueHeroDifficultyConfig config)
        {
            int availableMeatTypes = config.availableMeatTypes;
            return (MeatType)Random.Range(0, Mathf.Min(availableMeatTypes, 6));
        }

        private Sprite GetMeatSprite(MeatType type)
        {
            return type switch
            {
                MeatType.Steak => steakSprite,
                MeatType.Sausage => sausageSprite,
                MeatType.Fish => fishSprite,
                MeatType.Chicken => chickenSprite,
                MeatType.Burger => burgerSprite,
                MeatType.Ribs => ribsSprite,
                _ => steakSprite
            };
        }

        private IEnumerator CookMeatItem(MeatItem meat)
        {
            Transform meatTransform = meat.instance.transform;
            meatTransform.localScale = Vector3.zero;
            
            float growDuration = 0.25f;
            float elapsed = 0f;
            while (elapsed < growDuration && meat.instance != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / growDuration;
                meatTransform.localScale = new Vector3(t, t, t);
                yield return null;
            }

            if (meat.instance != null)
            {
                meatTransform.localScale = Vector3.one;
            }

            while (meat.isActive && meat.cookingProgress < 1f)
            {
                meat.cookingProgress += meat.cookingSpeed * Time.deltaTime;
                meat.cookingProgress = Mathf.Clamp01(meat.cookingProgress);

                UpdateMeatState(meat);
                UpdateMeatVisuals(meat);

                yield return null;
            }

            if (meat.isActive && !meat.wasScored)
            {
                RemoveMeatItem(meat, false);
            }
        }

        private void UpdateMeatState(MeatItem meat)
        {
            if (meat.cookingProgress < readyZoneStart)
            {
                meat.currentState = CookingState.Raw;
            }
            else if (meat.cookingProgress >= readyZoneStart && meat.cookingProgress < burntZoneStart)
            {
                meat.currentState = CookingState.Ready;
            }
            else
            {
                meat.currentState = CookingState.Burnt;
            }
        }

        private void UpdateMeatVisuals(MeatItem meat)
        {
            if (meat.progressRing != null)
            {
                meat.progressRing.fillAmount = meat.cookingProgress;

                Color targetColor = meat.currentState switch
                {
                    CookingState.Raw => rawColor,
                    CookingState.Ready => readyColor,
                    CookingState.Burnt => burntColor,
                    _ => rawColor
                };

                meat.progressRing.color = targetColor;
            }
        }

        public void OnMeatItemTapped(MeatItem meat)
        {
            if (!isActive || isComplete || !sessionRunning || meat.wasScored)
            {
                return;
            }

            meat.wasScored = true;
            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            int earnedScore = 0;
            bool isPerfect = false;
            bool isGood = false;

            if (meat.currentState == CookingState.Ready)
            {
                float readyProgress = (meat.cookingProgress - readyZoneStart) / (readyZoneEnd - readyZoneStart);
                float centerDistance = Mathf.Abs(readyProgress - 0.5f);

                if (centerDistance < 0.15f)
                {
                    isPerfect = true;
                    perfectTaps++;
                    earnedScore = config.perfectScore;
                    currentComboStreak++;
                }
                else
                {
                    isGood = true;
                    goodTaps++;
                    earnedScore = config.goodScore;
                    currentComboStreak++;
                }

                if (currentComboStreak >= config.comboThreshold)
                {
                    earnedScore = Mathf.RoundToInt(earnedScore * config.comboMultiplier);
                    Debug.Log($"[KiqqiBarbecueHeroManager] Combo bonus! Streak={currentComboStreak}, Score={earnedScore}");
                }

                sessionScore += earnedScore;
                masterGame.AddScore(earnedScore);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowTapFeedback(meat.instance.transform.position, true, earnedScore, isPerfect);

                app.Levels.NextLevel();
            }
            else
            {
                currentComboStreak = 0;
                badTaps++;

                int penalty = config.wrongPenalty;
                sessionScore = Mathf.Max(0, sessionScore - penalty);
                masterGame.AddScore(-penalty);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowTapFeedback(meat.instance.transform.position, false, penalty, false);

                Debug.Log($"[KiqqiBarbecueHeroManager] Wrong timing! State={meat.currentState}, Penalty={penalty}");
            }

            RemoveMeatItem(meat, true);
        }

        private void RemoveMeatItem(MeatItem meat, bool wasTapped)
        {
            meat.isActive = false;

            if (meat.instance != null)
            {
                if (!wasTapped)
                {
                    StartCoroutine(FadeMeatOut(meat.instance));
                }
                else
                {
                    StartCoroutine(ScaleMeatOut(meat.instance));
                }
            }

            int index = activeMeatItems.IndexOf(meat);
            if (index >= 0)
            {
                activeMeatItems[index] = activeMeatItems[activeMeatItems.Count - 1];
                activeMeatItems.RemoveAt(activeMeatItems.Count - 1);
            }
        }

        private IEnumerator FadeMeatOut(GameObject meatObj)
        {
            var canvasGroup = meatObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = meatObj.AddComponent<CanvasGroup>();

            float fadeDuration = 0.3f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            Destroy(meatObj);
        }

        private IEnumerator ScaleMeatOut(GameObject meatObj)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Transform meatTransform = meatObj.transform;
            Vector3 startScale = meatTransform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(startScale.x, 0f, t);
                meatTransform.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            Destroy(meatObj);
        }

        private void ClearAllMeatItems()
        {
            foreach (var meat in activeMeatItems)
            {
                if (meat.instance != null)
                {
                    Destroy(meat.instance);
                }
            }

            activeMeatItems.Clear();
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

            ClearAllMeatItems();

            Debug.Log($"[KiqqiBarbecueHeroManager] Session ended. Score={sessionScore}, Perfect={perfectTaps}, Good={goodTaps}, Bad={badTaps}");
            CompleteMiniGame(sessionScore, true);
        }

        public void ResumeFromPause(KiqqiBarbecueHeroView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;

            view?.UpdateScoreLabel(masterGame.CurrentScore);

            if (spawnCoroutine == null && !timeExpired)
            {
                spawnCoroutine = StartCoroutine(SpawnMeatLoop());
            }

            Debug.Log("[KiqqiBarbecueHeroManager] Resumed from pause.");
        }

        #endregion

        #region UTILITY

        public int GetCurrentComboStreak() => currentComboStreak;
        public int GetPerfectTaps() => perfectTaps;
        public int GetGoodTaps() => goodTaps;
        public int GetBadTaps() => badTaps;

        #endregion
    }
}
