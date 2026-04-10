using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    // ── Temporary stubs – remove when actual implementations are present ──────
#if !KIQQI_PITSTOP_FULL
    public class KiqqiPitStopView : KiqqiGameViewBase
    {
        public UnityEngine.UI.Button skipTutorialButton;
        public float RemainingTime => 999f;
        public void StopAllAnimations() { }
        public void SetupTutorialUI(bool active) { }
        public void ShowTutorialOverlay() { }
        public void HideTutorialOverlay() { }
        public void ShowTutorialHandPointer(Vector3 pos) { }
        public void HideTutorialHandPointer() { }
        public Vector3 GetFuelButtonPosition(PitStopFuelType type) => Vector3.zero;
        public void SetSemaphoreYellowCallback(GameObject car, System.Action cb) { }
        public void UpdateCarWaitIndicator(GameObject car, float normalized) { }
        public void DisableCarWaitIndicator(GameObject car) { }
        public void SetupFuelButtons(int count) { }
        public void UpdateFuelButtonSelection(PitStopFuelType type) { }
        public void ClearFuelButtonSelection() { }
        public void AnimateFuelCanToCar(Vector3 from, Vector3 to, Sprite sprite, System.Action onComplete) { onComplete?.Invoke(); }
        public void ShowFuelFeedback(Vector3 pos, bool correct) { }
        public void PlaySuccessParticle(Vector3 pos) { }
        public void UpdateScoreLabel(int score) { }
    }

    public class KiqqiPitStopLevelManager : MonoBehaviour
    {
        [System.Serializable]
        public class DifficultyConfig
        {
            public float spawnDelayMin = 2f;
            public float spawnDelayMax = 4f;
            public float waitTimeMin   = 4f;
            public float waitTimeMax   = 8f;
            public int   colorCount    = 2;
            public int   correctScore  = 100;
            public int   wrongPenalty  = 50;
            public int   timeoutPenalty = 30;
            public int   comboThreshold = 3;
            public float comboMultiplier = 1.5f;
        }
        public DifficultyConfig GetDifficultyConfig(int level) => new DifficultyConfig();
        public int GetLaneCount(int level) => 1;
    }
#endif
    // ─────────────────────────────────────────────────────────────────────────
    public enum PitStopFuelType
    {
        Red,
        Blue,
        Green,
        Yellow,
        Orange,
        Purple
    }

    [System.Serializable]
    public class PitStopFormulaData
    {
        public Sprite carSprite;
        public Sprite canSprite;
    }

    public class PitStopCarData
    {
        public int id;
        public PitStopFuelType requiredFuel;
        public GameObject instance;
        public int laneIndex;
        public Vector3 targetWaitPosition;
        public float moveSpeed;
        public float waitTimeElapsed;
        public float maxWaitTime;
        public bool isInWaitZone;
        public bool isWaiting;
        public PitStopCarData carInFront;
        public bool isBraking;
        public List<GameObject> skidmarkSegments;
        public Vector3 lastSkidmarkPosition;
        public bool isExiting;
        public bool wasBlocked;
        public float timeSinceUnblocked;
    }

    public class KiqqiPitStopManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Core References")]
        [SerializeField] private KiqqiPitStopLevelManager levelLogic;

        [Header("Lane Configuration (Per Difficulty)")]
        [SerializeField] private Transform beginnerLanesParent;
        [SerializeField] private Transform easyLanesParent;
        [SerializeField] private Transform mediumLanesParent;
        [SerializeField] private Transform advancedLanesParent;
        [SerializeField] private Transform hardLanesParent;
        
        [Header("Positioning")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform waitZonePoint;
        [SerializeField] private Transform exitPoint;

        [Header("Car Movement")]
        [SerializeField] private float baseCarSpeed = 200f;
        [SerializeField] private float carStopDistance = 150f;
        [Tooltip("Distance from stop position where braking/deceleration begins")]
        [SerializeField] private float brakingDistance = 400f;
        [Tooltip("Speed multiplier during approach (before braking)")]
        [SerializeField] private float approachSpeedMultiplier = 1.8f;
        [Tooltip("Extra clearance distance before rear car starts moving (prevents stutter)")]
        [SerializeField] private float queueAdvanceClearance = 100f;
        [Tooltip("Time for queued car to accelerate from stop (seconds)")]
        [SerializeField] private float queueResumeAccelerationTime = 0.4f;

        [Header("Skidmarks (UGUI)")]
        [Tooltip("UI Image prefab for skidmark segments")]
        [SerializeField] private GameObject skidmarkSegmentPrefab;
        [Tooltip("Names of wheel transform children in car prefab (e.g., 'WheelLeft', 'WheelRight')")]
        [SerializeField] private string[] wheelTransformNames = new string[] { "WheelLeft", "WheelRight" };
        [Tooltip("Distance between spawned skidmark segments")]
        [SerializeField] private float skidmarkSpawnDistance = 30f;
        [Tooltip("Delay before oldest segments start fading (seconds)")]
        [SerializeField] private float skidmarkFadeDelay = 1f;
        [Tooltip("How long each skidmark takes to fade out (seconds)")]
        [SerializeField] private float skidmarkFadeDuration = 1f;

        [Header("Car Prefab")]
        [SerializeField] private GameObject carPrefab;

        [Header("Formula Sprite Pairs (Car + Can)")]
        [SerializeField] private PitStopFormulaData[] formulaPairs = new PitStopFormulaData[6];

        [Header("Timer Settings")]
        [Tooltip("Don't spawn new cars when this much time is left (seconds)")]
        public float noNewCarsThreshold = 2f;

        [Header("Tutorial Configuration")]
        [SerializeField] private Transform tutorialLanesParent;
        [Tooltip("If true, tutorial runs automatically once on first app launch")]
        public bool autoStartTutorialOnFirstRun = true;
        [Tooltip("Y offset for hand pointer when pointing at gas can")]
        public float handPointerCanYOffset = 0f;
        [Tooltip("Y offset for hand pointer when pointing at formula car")]
        public float handPointerCarYOffset = 50f;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;

        protected KiqqiPitStopView view;
        protected KiqqiInputController input;

        private List<PitStopCarData> activeCars = new List<PitStopCarData>();
        private List<PitStopCarData> waitingCars = new List<PitStopCarData>();
        private int[] lastSpawnedInLane;
        
        private Transform activeLanesParent;
        private Transform[] activeLanes;
        
        private int carIdCounter = 0;
        private int carsServed = 0;
        private int carsMissed = 0;
        private int currentComboStreak = 0;

        private Coroutine spawnCoroutine;
        private List<Coroutine> carMoveCoroutines = new List<Coroutine>();
        private List<Coroutine> carWaitCoroutines = new List<Coroutine>();
        private List<Coroutine> carExitCoroutines = new List<Coroutine>();

        private WaitForSeconds spawnCheckInterval;

        private List<int> availableLanesCache = new List<int>();
        private List<PitStopFuelType> availableFuelTypesCache = new List<PitStopFuelType>();

        private PitStopFuelType? selectedFuelType = null;
        private PitStopFuelType? lastUsedFuelType = null;

        public bool isTutorialMode = false;
        private bool tutorialCarSpawned = false;
        private PitStopCarData tutorialCar = null;
        private bool tutorialPhase1Shown = false;
        private bool tutorialPhase2Shown = false;
        private bool tutorialCanSelected = false;

        private const string TUTORIAL_SHOWN_KEY = "pitstop_tutorial_shown_once";

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiPitStopView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiPitStopView>();
            input = context.Input;

            spawnCheckInterval = new WaitForSeconds(0.1f);

            ValidateConfiguration();

            //Debug.Log("[KiqqiPitStopManager] Initialized.");
        }

        private void ValidateConfiguration()
        {
            if (beginnerLanesParent == null || easyLanesParent == null || 
                mediumLanesParent == null || advancedLanesParent == null || 
                hardLanesParent == null)
            {
                Debug.LogError("[KiqqiPitStopManager] Not all lane parents configured!");
                return;
            }

            if (carPrefab == null)
            {
                Debug.LogError("[KiqqiPitStopManager] Car prefab not assigned!");
                return;
            }

            if (formulaPairs == null || formulaPairs.Length == 0)
            {
                Debug.LogError("[KiqqiPitStopManager] No formula sprite pairs defined!");
            }

            //Debug.Log("[KiqqiPitStopManager] Configuration valid - all lane parents assigned.");
        }

        #endregion

        #region TUTORIAL MODE MANAGEMENT

        // ShouldAutoStartTutorial() commented out - base class doesn't declare this virtual method yet.
        // public override bool ShouldAutoStartTutorial()
        // {
        //     if (!autoStartTutorialOnFirstRun)
        //         return false;
        //
        //     bool hasShown = PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) == 1;
        //     return !hasShown;
        // }

        public void MarkTutorialShown()
        {
            PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 1);
            PlayerPrefs.Save();
        }

        public void ResetTutorialFlagForTesting()
        {
            PlayerPrefs.DeleteKey(TUTORIAL_SHOWN_KEY);
            PlayerPrefs.Save();
        }

        public void StartTutorial()
        {
            isTutorialMode = true;
            MarkTutorialShown();
            
            Debug.Log("[KiqqiPitStopManager] Tutorial mode flag set.");
        }

        private void OnSkipTutorialPressed()
        {
            Debug.Log("[KiqqiPitStopManager] Skip tutorial button pressed.");
            EndTutorial(true);
        }

        private void EndTutorial(bool showEndView)
        {
            if (!isTutorialMode)
                return;

            sessionRunning = false;
            isComplete = true;
            isActive = false;

            ResetTutorialState();

            if (view != null)
            {
                view.HideTutorialOverlay();
                view.SetupTutorialUI(false);
            }

            ClearAllCars();

            if (showEndView)
            {
                StartCoroutine(ShowTutorialEndViewDelayed());
            }

            Debug.Log("[KiqqiPitStopManager] Tutorial ended.");
        }

        private IEnumerator ShowTutorialEndViewDelayed()
        {
            yield return new WaitForEndOfFrame();
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }

        private void ResetTutorialState()
        {
            isTutorialMode = false;
            tutorialCarSpawned = false;
            tutorialCar = null;
            tutorialPhase1Shown = false;
            tutorialPhase2Shown = false;
            tutorialCanSelected = false;
        }

        #endregion

        #region CORE INITIALIZATION (CONTINUED)

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            StopAllActiveCoroutines();

            if (view != null)
            {
                view.StopAllAnimations();
            }

            sessionRunning = false;
            timeExpired = false;
            isActive = false;
            isComplete = true;

            ResetTutorialState();

            ClearAllCars();

            //Debug.Log("[KiqqiPitStopManager] OnMiniGameExit -> cleaned up.");
        }

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();

            StopAllActiveCoroutines();

            if (view != null)
            {
                view.StopAllAnimations();
            }

            sessionRunning = false;
            timeExpired = false;
            carsServed = 0;
            carsMissed = 0;
            currentComboStreak = 0;
            carIdCounter = 0;
            selectedFuelType = null;
            lastUsedFuelType = null;

            ResetTutorialState();

            ClearAllCars();

            //Debug.Log("[KiqqiPitStopManager] ResetMiniGame -> cleaned up and ready for restart.");
        }

        private void StopAllActiveCoroutines()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            foreach (var coroutine in carMoveCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            carMoveCoroutines.Clear();

            foreach (var coroutine in carWaitCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            carWaitCoroutines.Clear();

            foreach (var coroutine in carExitCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            carExitCoroutines.Clear();
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            StopAllActiveCoroutines();

            if (view != null)
            {
                view.StopAllAnimations();
                
                if (isTutorialMode)
                {
                    view.SetupTutorialUI(true);
                    if (view.skipTutorialButton)
                    {
                        view.skipTutorialButton.onClick.RemoveAllListeners();
                        view.skipTutorialButton.onClick.AddListener(OnSkipTutorialPressed);
                    }
                }
            }

            sessionScore = 0;
            masterGame.CurrentScore = 0;
            carsServed = 0;
            carsMissed = 0;
            currentComboStreak = 0;
            sessionRunning = true;
            timeExpired = false;
            carIdCounter = 0;
            selectedFuelType = null;

            ClearAllCars();
            SetupLanesForCurrentDifficulty();
            InitializeLaneTracking();
            InitializeAvailableFuelTypes();

            view?.SetupFuelButtons(GetActiveColorCount());

            if (isTutorialMode)
            {
                spawnCoroutine = StartCoroutine(SpawnTutorialCar());
            }
            else
            {
                spawnCoroutine = StartCoroutine(SpawnCarLoop());
            }

            //Debug.Log($"[KiqqiPitStopManager] Session started. Tutorial mode: {isTutorialMode}");
        }

        private void SetupLanesForCurrentDifficulty()
        {
            HideAllLaneParents();

            if (isTutorialMode)
            {
                if (tutorialLanesParent != null)
                {
                    activeLanesParent = tutorialLanesParent;
                    activeLanesParent.gameObject.SetActive(true);

                    int childCount = activeLanesParent.childCount;
                    activeLanes = new Transform[childCount];

                    for (int i = 0; i < childCount; i++)
                    {
                        activeLanes[i] = activeLanesParent.GetChild(i);
                    }

                    //Debug.Log($"[KiqqiPitStopManager] Setup tutorial lane (1 lane)");
                }
                else
                {
                    Debug.LogError("[KiqqiPitStopManager] Tutorial lanes parent not assigned!");
                    activeLanes = new Transform[0];
                }
                return;
            }

            var difficulty = app.Levels.GetCurrentDifficulty();
            
            activeLanesParent = difficulty switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerLanesParent,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyLanesParent,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumLanesParent,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedLanesParent,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardLanesParent,
                _ => beginnerLanesParent
            };

            if (activeLanesParent != null)
            {
                activeLanesParent.gameObject.SetActive(true);
                
                int childCount = activeLanesParent.childCount;
                activeLanes = new Transform[childCount];
                
                for (int i = 0; i < childCount; i++)
                {
                    activeLanes[i] = activeLanesParent.GetChild(i);
                }
                
                //Debug.Log($"[KiqqiPitStopManager] Setup {childCount} lanes for difficulty: {difficulty}");
            }
            else
            {
                Debug.LogError($"[KiqqiPitStopManager] No lane parent found for difficulty: {difficulty}");
                activeLanes = new Transform[0];
            }
        }

        private void HideAllLaneParents()
        {
            if (beginnerLanesParent) beginnerLanesParent.gameObject.SetActive(false);
            if (easyLanesParent) easyLanesParent.gameObject.SetActive(false);
            if (mediumLanesParent) mediumLanesParent.gameObject.SetActive(false);
            if (advancedLanesParent) advancedLanesParent.gameObject.SetActive(false);
            if (hardLanesParent) hardLanesParent.gameObject.SetActive(false);
            if (tutorialLanesParent) tutorialLanesParent.gameObject.SetActive(false);
        }

        private void InitializeLaneTracking()
        {
            int laneCount = GetActiveLaneCount();
            lastSpawnedInLane = new int[laneCount];
            
            for (int i = 0; i < laneCount; i++)
            {
                lastSpawnedInLane[i] = -999;
            }
        }

        private void InitializeAvailableFuelTypes()
        {
            availableFuelTypesCache.Clear();
            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            int colorCount = Mathf.Clamp(config.colorCount, 1, 6);
            
            for (int i = 0; i < colorCount; i++)
            {
                availableFuelTypesCache.Add((PitStopFuelType)i);
            }
        }

        private int GetActiveColorCount()
        {
            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            return Mathf.Clamp(config.colorCount, 1, 6);
        }

        private int GetActiveLaneCount()
        {
            if (activeLanes != null)
                return activeLanes.Length;
            
            return levelLogic.GetLaneCount(app.Levels.currentLevel);
        }

        public void NotifyTimeExpired()
        {
            if (!sessionRunning) return;

            timeExpired = true;
            sessionRunning = false;

            Debug.Log("[KiqqiPitStopManager] Time expired - ending session immediately.");

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            ClearAllCars();

            masterGame.CurrentScore = sessionScore;

            Debug.Log($"[KiqqiPitStopManager] Session ended by timer. Final Score={sessionScore}, Cars Served={carsServed}, Missed={carsMissed}");
            CompleteMiniGame(sessionScore, true);
        }

        #endregion

        #region CAR SPAWNING & MANAGEMENT

        private IEnumerator SpawnCarLoop()
        {
            bool firstCar = true;

            while (sessionRunning && !timeExpired)
            {
                float timeLeft = view?.RemainingTime ?? 999f;
                if (timeLeft <= noNewCarsThreshold)
                {
                    //Debug.Log($"[KiqqiPitStopManager] Stopping spawn - only {timeLeft:F1}s left.");
                    yield break;
                }

                if (firstCar)
                {
                    firstCar = false;
                    SpawnCar();
                }
                else
                {
                    float delay = GetNextSpawnDelay();
                    yield return new WaitForSeconds(delay);

                    if (sessionRunning && !timeExpired)
                    {
                        SpawnCar();
                    }
                }
            }
        }

        private float GetNextSpawnDelay()
        {
            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            return Random.Range(config.spawnDelayMin, config.spawnDelayMax);
        }

        private IEnumerator SpawnTutorialCar()
        {
            if (tutorialCarSpawned)
                yield break;

            yield return new WaitForSeconds(0.5f);

            if (activeLanes == null || activeLanes.Length == 0)
            {
                Debug.LogWarning("[KiqqiPitStopManager] No active lanes for tutorial!");
                yield break;
            }

            int laneIndex = 0;
            Transform lane = activeLanes[laneIndex];
            PitStopFuelType requiredFuel = PitStopFuelType.Red;

            GameObject carObj = Instantiate(carPrefab, activeLanesParent);
            carObj.SetActive(true);

            Vector3 spawnPos = lane.position;
            spawnPos.y = spawnPoint.position.y;
            carObj.transform.position = spawnPos;

            var carImage = carObj.GetComponentInChildren<UnityEngine.UI.Image>();
            if (carImage && formulaPairs.Length > 0 && formulaPairs[0] != null)
            {
                carImage.sprite = formulaPairs[0].carSprite;
            }

            tutorialCar = new PitStopCarData
            {
                id = carIdCounter++,
                requiredFuel = requiredFuel,
                instance = carObj,
                laneIndex = laneIndex,
                targetWaitPosition = new Vector3(lane.position.x, waitZonePoint.position.y, 0f),
                moveSpeed = baseCarSpeed,
                maxWaitTime = 6f, // Yellow appears at 50% = 3 seconds after reaching wait zone
                waitTimeElapsed = 0f,
                isInWaitZone = false,
                isWaiting = false,
                carInFront = null,
                isBraking = false,
                skidmarkSegments = new List<GameObject>(),
                lastSkidmarkPosition = Vector3.zero,
                isExiting = false,
                wasBlocked = false,
                timeSinceUnblocked = 0f
            };

            activeCars.Add(tutorialCar);
            tutorialCarSpawned = true;

            var button = carObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                var clickHandler = carObj.AddComponent<CarClickHandler>();
                clickHandler.Initialize(this, tutorialCar);
            }

            var moveCoro = StartCoroutine(MoveCar(tutorialCar));
            carMoveCoroutines.Add(moveCoro);

            //Debug.Log("[KiqqiPitStopManager] Tutorial car spawned with 20s wait time.");
        }

        #endregion

        #region TUTORIAL FLOW

        public void OnTutorialCarReachedWaitZone()
        {
            if (!isTutorialMode || tutorialPhase1Shown)
                return;

            tutorialPhase1Shown = true;
            if (view != null)
            {
                // Show the main instruction text
                view.ShowTutorialOverlay();
                
                // Show hand pointer immediately on the correct can
                if (tutorialCar != null && tutorialCar.requiredFuel == PitStopFuelType.Red)
                {
                    Vector3 canButtonPos = view.GetFuelButtonPosition(PitStopFuelType.Red);
                    canButtonPos.y += handPointerCanYOffset;
                    view.ShowTutorialHandPointer(canButtonPos);
                }
            }

            //Debug.Log("[KiqqiPitStopManager] Tutorial Phase 1 - Car stopped, showing instruction and hand pointer.");
        }

        public void OnTutorialSemaphoreYellow()
        {
            if (!isTutorialMode || tutorialPhase2Shown)
                return;

            tutorialPhase2Shown = true;
            
            Debug.Log("[KiqqiPitStopManager] Tutorial Phase 2 - Semaphore yellow (no additional action needed).");
        }

        private void OnTutorialCorrectAction()
        {
            if (!isTutorialMode)
                return;

            if (view != null)
            {
                view.HideTutorialHandPointer();
            }

            StartCoroutine(CompleteTutorialAfterFeedback());
        }

        private IEnumerator CompleteTutorialAfterFeedback()
        {
            yield return new WaitForSeconds(1.5f);
            EndTutorial(true);
        }

        #endregion

        #region CAR SPAWNING (CONTINUED)

        private void SpawnCar()
        {
            if (activeLanes == null || activeLanes.Length == 0)
            {
                Debug.LogWarning("[KiqqiPitStopManager] No active lanes available for spawning!");
                return;
            }

            int laneCount = GetActiveLaneCount();
            int laneIndex = SelectLane(laneCount);

            if (laneIndex < 0 || laneIndex >= activeLanes.Length)
            {
                Debug.LogWarning($"[KiqqiPitStopManager] Invalid lane index: {laneIndex}");
                return;
            }

            Transform lane = activeLanes[laneIndex];
            PitStopFuelType requiredFuel = SelectRandomFuelType(laneCount);

            GameObject carObj = Instantiate(carPrefab, activeLanesParent);
            carObj.SetActive(true);

            Vector3 spawnPos = lane.position;
            spawnPos.y = spawnPoint.position.y;
            carObj.transform.position = spawnPos;

            var carImage = carObj.GetComponentInChildren<UnityEngine.UI.Image>();
            if (carImage && (int)requiredFuel < formulaPairs.Length && formulaPairs[(int)requiredFuel] != null)
            {
                carImage.sprite = formulaPairs[(int)requiredFuel].carSprite;
            }

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            float waitTime = Random.Range(config.waitTimeMin, config.waitTimeMax);

            PitStopCarData carData = new PitStopCarData
            {
                id = carIdCounter++,
                requiredFuel = requiredFuel,
                instance = carObj,
                laneIndex = laneIndex,
                targetWaitPosition = new Vector3(lane.position.x, waitZonePoint.position.y, 0f),
                moveSpeed = baseCarSpeed,
                maxWaitTime = waitTime,
                waitTimeElapsed = 0f,
                isInWaitZone = false,
                isWaiting = false,
                carInFront = null,
                isBraking = false,
                skidmarkSegments = new List<GameObject>(),
                lastSkidmarkPosition = Vector3.zero,
                isExiting = false,
                wasBlocked = false,
                timeSinceUnblocked = 0f
            };

            activeCars.Add(carData);
            lastSpawnedInLane[laneIndex] = carData.id;

            var button = carObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                var clickHandler = carObj.AddComponent<CarClickHandler>();
                clickHandler.Initialize(this, carData);
            }

            Coroutine moveCoroutine = StartCoroutine(MoveCar(carData));
            carMoveCoroutines.Add(moveCoroutine);
        }

        private int SelectLane(int laneCount)
        {
            if (laneCount <= 0) return 0;

            availableLanesCache.Clear();

            for (int i = 0; i < laneCount; i++)
            {
                int framesSinceLastSpawn = carIdCounter - lastSpawnedInLane[i];
                
                if (framesSinceLastSpawn >= 3)
                {
                    availableLanesCache.Add(i);
                }
            }

            if (availableLanesCache.Count == 0)
            {
                for (int i = 0; i < laneCount; i++)
                {
                    availableLanesCache.Add(i);
                }
            }

            return availableLanesCache[Random.Range(0, availableLanesCache.Count)];
        }

        private PitStopFuelType SelectRandomFuelType(int laneCount)
        {
            int count = availableFuelTypesCache.Count;
            if (count <= 0) return PitStopFuelType.Red;
            if (count == 1 || !lastUsedFuelType.HasValue) 
            {
                lastUsedFuelType = availableFuelTypesCache[Random.Range(0, count)];
                return lastUsedFuelType.Value;
            }

            // 50% chance: avoid last used (pick from the rest)
            // 50% chance: pick from all (same type can appear again)
            PitStopFuelType chosen;
            if (Random.value < 0.5f)
            {
                availableLanesCache.Clear(); // reuse cache as temp int list
                for (int i = 0; i < count; i++)
                {
                    if (availableFuelTypesCache[i] != lastUsedFuelType.Value)
                        availableLanesCache.Add(i);
                }
                int idx = availableLanesCache[Random.Range(0, availableLanesCache.Count)];
                chosen = availableFuelTypesCache[idx];
            }
            else
            {
                chosen = availableFuelTypesCache[Random.Range(0, count)];
            }

            lastUsedFuelType = chosen;
            return chosen;
        }

        private IEnumerator MoveCar(PitStopCarData car)
        {
            bool reachedWaitZone = false;

            while (car.instance != null && sessionRunning)
            {
                PitStopCarData carAhead = FindCarAhead(car);
                bool isBlocked = false;

                if (carAhead != null && !carAhead.isExiting)
                {
                    float distance = carAhead.instance.transform.position.y - car.instance.transform.position.y;

                    if (distance <= carStopDistance + queueAdvanceClearance)
                    {
                        car.carInFront = carAhead;
                        car.wasBlocked = true;
                        car.timeSinceUnblocked = 0f;
                        isBlocked = true;
                        yield return null;
                        continue;
                    }
                }

                if (!isBlocked && car.wasBlocked)
                {
                    car.timeSinceUnblocked += Time.deltaTime;
                    
                    if (car.timeSinceUnblocked >= queueResumeAccelerationTime)
                    {
                        car.wasBlocked = false;
                        car.timeSinceUnblocked = 0f;
                    }
                }

                car.carInFront = null;

                float currentY = car.instance.transform.position.y;
                float distanceToTarget = car.targetWaitPosition.y - currentY;

                if (!reachedWaitZone && currentY >= car.targetWaitPosition.y)
                {
                    car.isInWaitZone = true;
                    car.isWaiting = true;
                    car.isBraking = false;
                    reachedWaitZone = true;
                    waitingCars.Add(car);

                    car.instance.transform.position = car.targetWaitPosition;
                    
                    FadeOutSkidmarks(car);
                    
                    Coroutine waitCoroutine = StartCoroutine(HandleCarWaiting(car));
                    carWaitCoroutines.Add(waitCoroutine);

                    if (isTutorialMode && car == tutorialCar)
                    {
                        OnTutorialCarReachedWaitZone();
                    }

                    yield break;
                }

                float currentSpeed;

                if (distanceToTarget <= brakingDistance)
                {
                    if (!car.isBraking)
                    {
                        car.isBraking = true;
                    }

                    SpawnSkidmarkSegment(car);

                    float brakingProgress = 1f - (distanceToTarget / brakingDistance);
                    float decelerationCurve = 1f - Mathf.Pow(brakingProgress, 2f);
                    currentSpeed = car.moveSpeed * approachSpeedMultiplier * decelerationCurve;
                    currentSpeed = Mathf.Max(currentSpeed, car.moveSpeed * 0.2f);
                }
                else
                {
                    currentSpeed = car.moveSpeed * approachSpeedMultiplier;
                }

                if (car.wasBlocked && car.timeSinceUnblocked < queueResumeAccelerationTime)
                {
                    float accelerationProgress = car.timeSinceUnblocked / queueResumeAccelerationTime;
                    float accelerationCurve = Mathf.SmoothStep(0f, 1f, accelerationProgress);
                    currentSpeed *= accelerationCurve;
                }

                Vector3 pos = car.instance.transform.position;
                pos.y += currentSpeed * Time.deltaTime;
                car.instance.transform.position = pos;

                yield return null;
            }
        }

        private PitStopCarData FindCarAhead(PitStopCarData currentCar)
        {
            PitStopCarData closestAhead = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < activeCars.Count; i++)
            {
                PitStopCarData other = activeCars[i];
                
                if (other.id == currentCar.id || other.laneIndex != currentCar.laneIndex)
                    continue;

                if (other.instance == null)
                    continue;

                if (other.instance.transform.position.y > currentCar.instance.transform.position.y)
                {
                    float distance = other.instance.transform.position.y - currentCar.instance.transform.position.y;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestAhead = other;
                    }
                }
            }

            return closestAhead;
        }

        private IEnumerator HandleCarWaiting(PitStopCarData car)
        {
            if (isTutorialMode && car == tutorialCar)
            {
                view?.SetSemaphoreYellowCallback(car.instance, OnTutorialSemaphoreYellow);
            }

            while (car.instance != null && car.isWaiting && sessionRunning)
            {
                car.waitTimeElapsed += Time.deltaTime;

                float normalizedWait = Mathf.Clamp01(car.waitTimeElapsed / car.maxWaitTime);
                view?.UpdateCarWaitIndicator(car.instance, normalizedWait);

                if (car.waitTimeElapsed >= car.maxWaitTime)
                {
                    OnCarTimedOut(car);
                    yield break;
                }

                yield return null;
            }
        }

        #endregion

        #region SKIDMARK MANAGEMENT

        private void SpawnSkidmarkSegment(PitStopCarData car)
        {
            if (car.instance == null || skidmarkSegmentPrefab == null)
                return;

            Vector3 currentPos = car.instance.transform.position;

            if (car.skidmarkSegments.Count > 0)
            {
                float distanceSinceLastMark = Vector3.Distance(currentPos, car.lastSkidmarkPosition);
                if (distanceSinceLastMark < skidmarkSpawnDistance)
                    return;
            }

            int segmentIndex = car.skidmarkSegments.Count;
            float delayBeforeFade = skidmarkFadeDelay + (segmentIndex * 0.05f);

            foreach (string wheelName in wheelTransformNames)
            {
                Transform wheelTransform = FindChildRecursive(car.instance.transform, wheelName);
                if (wheelTransform != null)
                {
                    GameObject skidmark = Instantiate(skidmarkSegmentPrefab, activeLanesParent);
                    skidmark.transform.position = wheelTransform.position;
                    skidmark.SetActive(true);

                    car.skidmarkSegments.Add(skidmark);

                    StartCoroutine(FadeOutSkidmarkDelayed(skidmark, delayBeforeFade));
                }
            }

            car.lastSkidmarkPosition = currentPos;
        }

        private void FadeOutSkidmarks(PitStopCarData car)
        {
            if (car.skidmarkSegments == null || car.skidmarkSegments.Count == 0)
                return;

            foreach (GameObject skidmark in car.skidmarkSegments)
            {
                if (skidmark != null)
                {
                    StartCoroutine(FadeOutSkidmark(skidmark));
                }
            }
        }

        private IEnumerator FadeOutSkidmarkDelayed(GameObject skidmark, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (skidmark != null)
            {
                yield return StartCoroutine(FadeOutSkidmark(skidmark));
            }
        }

        private IEnumerator FadeOutSkidmark(GameObject skidmark)
        {
            if (skidmark == null) yield break;

            var canvasGroup = skidmark.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = skidmark.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            while (elapsed < skidmarkFadeDuration)
            {
                if (skidmark == null || canvasGroup == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / skidmarkFadeDuration;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            if (skidmark != null)
            {
                Destroy(skidmark);
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        #endregion

        #region PLAYER INTERACTION

        public void OnFuelButtonPressed(PitStopFuelType fuelType)
        {
            if (!isActive || isComplete || !sessionRunning)
            {
                Debug.LogWarning("[KiqqiPitStopManager] Fuel button pressed but session not active.");
                return;
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            selectedFuelType = fuelType;
            view?.UpdateFuelButtonSelection(fuelType);

            if (isTutorialMode && !tutorialCanSelected)
            {
                tutorialCanSelected = true;

                if (tutorialCar != null && tutorialCar.instance != null)
                {
                    if (view != null)
                    {
                        view.HideTutorialHandPointer();
                        Vector3 carPos = tutorialCar.instance.transform.position;
                        carPos.y += handPointerCarYOffset;
                        view.ShowTutorialHandPointer(carPos);
                    }
                }
            }

            //Debug.Log($"[KiqqiPitStopManager] Selected fuel: {fuelType}");
        }

        public void OnCarClicked(PitStopCarData car)
        {
            if (!isActive || isComplete || !sessionRunning)
            {
                Debug.LogWarning("[KiqqiPitStopManager] Car clicked but session not active.");
                return;
            }

            if (!car.isWaiting)
            {
                Debug.Log("[KiqqiPitStopManager] Car clicked but not in waiting state.");
                return;
            }

            if (!selectedFuelType.HasValue)
            {
                Debug.Log("[KiqqiPitStopManager] No fuel selected, please select fuel first.");
                KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
                return;
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            bool correct = car.requiredFuel == selectedFuelType.Value;
            PitStopFuelType selectedFuel = selectedFuelType.Value;

            Vector3 fuelButtonPos = view.GetFuelButtonPosition(selectedFuel);
            Vector3 carPos = car.instance.transform.position;
            Sprite canSprite = GetCanSprite(selectedFuel);

            view?.AnimateFuelCanToCar(fuelButtonPos, carPos, canSprite, () => 
            {
                if (car == null || car.instance == null)
                {
                    Debug.LogWarning("[KiqqiPitStopManager] Car was destroyed before animation completed.");
                    return;
                }

                if (correct)
                {
                    OnCarFueledCorrectly(car);
                }
                else
                {
                    OnCarFueledIncorrectly(car, selectedFuel);
                }
            });
        }

        private PitStopCarData FindFirstWaitingCar()
        {
            if (waitingCars.Count == 0) return null;

            PitStopCarData lowestCar = waitingCars[0];
            float lowestY = lowestCar.instance.transform.position.y;

            for (int i = 1; i < waitingCars.Count; i++)
            {
                float y = waitingCars[i].instance.transform.position.y;
                if (y < lowestY)
                {
                    lowestY = y;
                    lowestCar = waitingCars[i];
                }
            }

            return lowestCar;
        }

        private void OnCarFueledCorrectly(PitStopCarData car)
        {
            if (car == null || car.instance == null)
            {
                Debug.LogWarning("[KiqqiPitStopManager] Car is null in OnCarFueledCorrectly");
                return;
            }

            currentComboStreak++;
            carsServed++;

            var config = isTutorialMode ? null : levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            int baseScore = isTutorialMode ? 100 : config.correctScore;

            int earnedScore = baseScore;
            if (!isTutorialMode && currentComboStreak >= config.comboThreshold)
            {
                earnedScore = Mathf.RoundToInt(baseScore * config.comboMultiplier);
            }

            sessionScore += earnedScore;
            masterGame.CurrentScore = sessionScore;
            view?.UpdateScoreLabel(masterGame.CurrentScore);
            
            Vector3 carPosition = car.instance.transform.position;
            view?.ShowFuelFeedback(carPosition, true);
            view?.PlaySuccessParticle(carPosition);

            selectedFuelType = null;
            view?.ClearFuelButtonSelection();

            view?.DisableCarWaitIndicator(car.instance);

            RemoveCarAndLetNextMove(car);

            if (isTutorialMode && car == tutorialCar)
            {
                OnTutorialCorrectAction();
            }
        }

        private void OnCarFueledIncorrectly(PitStopCarData car, PitStopFuelType wrongFuel)
        {
            if (car == null || car.instance == null)
            {
                Debug.LogWarning("[KiqqiPitStopManager] Car is null in OnCarFueledIncorrectly");
                return;
            }

            currentComboStreak = 0;

            if (!isTutorialMode)
            {
                var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
                int penalty = config.wrongPenalty;

                sessionScore = Mathf.Max(0, sessionScore - penalty);
                masterGame.CurrentScore = sessionScore;
                view?.UpdateScoreLabel(masterGame.CurrentScore);
            }

            view?.ShowFuelFeedback(car.instance.transform.position, false);

            //Debug.Log($"[KiqqiPitStopManager] Wrong fuel! Required={car.requiredFuel}, Given={wrongFuel}");

            if (isTutorialMode && car == tutorialCar)
            {
                RemoveCarAndLetNextMove(car);
                OnTutorialCorrectAction();
            }
        }

        private void OnCarTimedOut(PitStopCarData car)
        {
            currentComboStreak = 0;
            carsMissed++;

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            int penalty = config.timeoutPenalty;

            sessionScore = Mathf.Max(0, sessionScore - penalty);
            masterGame.CurrentScore = sessionScore;
            view?.UpdateScoreLabel(masterGame.CurrentScore);

            view?.DisableCarWaitIndicator(car.instance);

            //Debug.Log($"[KiqqiPitStopManager] Car timed out! Penalty={penalty}");

            RemoveCarAndLetNextMove(car);

            // End tutorial after car exits if the tutorial car times out
            if (isTutorialMode && car == tutorialCar)
            {
                Debug.Log("[KiqqiPitStopManager] Tutorial car timed out - will end tutorial after car exits.");
                StartCoroutine(EndTutorialAfterCarExits());
            }
        }

        private IEnumerator EndTutorialAfterCarExits()
        {
            // Wait for car to exit (2 seconds should be enough for the exit animation)
            yield return new WaitForSeconds(2f);
            EndTutorial(true);
        }

        private void RemoveCarAndLetNextMove(PitStopCarData car)
        {
            car.isWaiting = false;
            car.isExiting = true;
            waitingCars.Remove(car);

            Coroutine exitCoroutine = StartCoroutine(MoveCarToExit(car));
            carExitCoroutines.Add(exitCoroutine);
        }

        private IEnumerator MoveCarToExit(PitStopCarData car)
        {
            if (car.instance == null) yield break;

            float targetExitSpeed = baseCarSpeed * approachSpeedMultiplier;
            float currentSpeed = 0f;
            float accelerationTime = 0.5f;
            float elapsed = 0f;

            while (car.instance != null && car.instance.transform.position.y < exitPoint.position.y)
            {
                elapsed += Time.deltaTime;
                float accelerationProgress = Mathf.Clamp01(elapsed / accelerationTime);
                currentSpeed = Mathf.Lerp(0f, targetExitSpeed, accelerationProgress);

                if (accelerationProgress < 1f)
                {
                    SpawnSkidmarkSegment(car);
                }

                Vector3 pos = car.instance.transform.position;
                pos.y += currentSpeed * Time.deltaTime;
                car.instance.transform.position = pos;

                yield return null;
            }

            RemoveCar(car);
        }

        private void RemoveCar(PitStopCarData car)
        {
            if (car.skidmarkSegments != null)
            {
                foreach (var skidmark in car.skidmarkSegments)
                {
                    if (skidmark != null)
                    {
                        Destroy(skidmark);
                    }
                }
                car.skidmarkSegments.Clear();
            }

            if (car.instance != null)
            {
                Destroy(car.instance);
            }

            activeCars.Remove(car);
            waitingCars.Remove(car);
        }

        private void ClearAllCars()
        {
            for (int i = activeCars.Count - 1; i >= 0; i--)
            {
                if (activeCars[i].skidmarkSegments != null)
                {
                    foreach (var skidmark in activeCars[i].skidmarkSegments)
                    {
                        if (skidmark != null)
                        {
                            Destroy(skidmark);
                        }
                    }
                    activeCars[i].skidmarkSegments.Clear();
                }

                if (activeCars[i].instance != null)
                {
                    Destroy(activeCars[i].instance);
                }
            }

            activeCars.Clear();
            waitingCars.Clear();
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

            ClearAllCars();

            masterGame.CurrentScore = sessionScore;

            Debug.Log($"[KiqqiPitStopManager] Session ended. Final Score={sessionScore}, Cars Served={carsServed}, Missed={carsMissed}");
            CompleteMiniGame(sessionScore, true);
        }

        public void ResumeFromPause(KiqqiPitStopView v)
        {
            view = v ?? view;

            isActive = true;
            isComplete = false;

            view?.UpdateScoreLabel(masterGame.CurrentScore);

            if (spawnCoroutine == null && !timeExpired)
            {
                spawnCoroutine = StartCoroutine(SpawnCarLoop());
            }

            Debug.Log("[KiqqiPitStopManager] Resumed from pause.");
        }

        #endregion

        #region UTILITY

        public int GetCurrentComboStreak() => currentComboStreak;
        public int GetCarsServed() => carsServed;
        public int GetCarsMissed() => carsMissed;
        public Sprite GetCanSprite(PitStopFuelType type)
        {
            int index = (int)type;
            if (index >= 0 && index < formulaPairs.Length && formulaPairs[index] != null)
                return formulaPairs[index].canSprite;
            return null;
        }

        #endregion
    }

    public class CarClickHandler : MonoBehaviour
    {
        private KiqqiPitStopManager manager;
        private PitStopCarData carData;

        public void Initialize(KiqqiPitStopManager mgr, PitStopCarData data)
        {
            manager = mgr;
            carData = data;

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
                manager.OnCarClicked(carData);
            }
        }
    }
}
