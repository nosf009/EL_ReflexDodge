using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public enum FuelType
    {
        Red,
        Blue,
        Green,
        Yellow,
        Orange,
        Purple
    }

    public class CarData
    {
        public int id;
        public FuelType requiredFuel;
        public GameObject instance;
        public int laneIndex;
        public Vector3 targetWaitPosition;
        public float moveSpeed;
        public float waitTimeElapsed;
        public float maxWaitTime;
        public bool isInWaitZone;
        public bool isWaiting;
        public CarData carInFront;
    }

    public class KiqqiFuelCarManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Core References")]
        [SerializeField] private KiqqiFuelCarLevelManager levelLogic;

        [Header("Lane Configuration (Per Difficulty)")]
        [SerializeField] private Transform beginnerLanesParent;
        [SerializeField] private Transform easyLanesParent;
        [SerializeField] private Transform mediumLanesParent;
        [SerializeField] private Transform advancedLanesParent;
        [SerializeField] private Transform hardLanesParent;
        
        [Header("Positioning")]
        [SerializeField] private float spawnYPosition = -700f;
        [SerializeField] private float waitZoneYPosition = 0f;
        [SerializeField] private float exitYPosition = 700f;

        [Header("Car Movement")]
        [SerializeField] private float baseCarSpeed = 200f;
        [SerializeField] private float carStopDistance = 150f;

        [Header("Car Prefab")]
        [SerializeField] private GameObject carPrefab;

        [Header("Fuel Colors")]
        [SerializeField] private Color[] fuelColors = new Color[6];

        [Header("Timer Settings")]
        [Tooltip("Don't spawn new cars when this much time is left (seconds)")]
        public float noNewCarsThreshold = 2f;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;

        protected KiqqiFuelCarView view;
        protected KiqqiInputController input;

        private List<CarData> activeCars = new List<CarData>();
        private List<CarData> waitingCars = new List<CarData>();
        private int[] lastSpawnedInLane;
        
        private Transform activeLanesParent;
        private Transform[] activeLanes;
        
        private int carIdCounter = 0;
        private int carsServed = 0;
        private int carsMissed = 0;
        private int currentComboStreak = 0;

        private Coroutine spawnCoroutine;
        private WaitForSeconds spawnCheckInterval;

        private List<int> availableLanesCache = new List<int>();
        private List<FuelType> availableFuelTypesCache = new List<FuelType>();

        private FuelType? selectedFuelType = null;

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiFuelCarView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiFuelCarView>();
            input = context.Input;

            spawnCheckInterval = new WaitForSeconds(0.1f);

            ValidateConfiguration();

            Debug.Log("[KiqqiFuelCarManager] Initialized.");
        }

        private void ValidateConfiguration()
        {
            if (beginnerLanesParent == null || easyLanesParent == null || 
                mediumLanesParent == null || advancedLanesParent == null || 
                hardLanesParent == null)
            {
                Debug.LogError("[KiqqiFuelCarManager] Not all lane parents configured!");
                return;
            }

            if (carPrefab == null)
            {
                Debug.LogError("[KiqqiFuelCarManager] Car prefab not assigned!");
                return;
            }

            if (fuelColors == null || fuelColors.Length < 6)
            {
                Debug.LogWarning("[KiqqiFuelCarManager] Not enough fuel colors defined, using defaults.");
                fuelColors = new Color[6]
                {
                    Color.red,
                    Color.blue,
                    Color.green,
                    Color.yellow,
                    new Color(1f, 0.5f, 0f),
                    new Color(0.5f, 0f, 1f)
                };
            }

            Debug.Log("[KiqqiFuelCarManager] Configuration valid - all lane parents assigned.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            sessionRunning = false;
            timeExpired = false;
            isActive = false;
            isComplete = true;

            ClearAllCars();
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            Debug.Log("[KiqqiFuelCarManager] OnMiniGameExit -> cleaned up.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

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

            view?.SetupFuelButtons(GetActiveLaneCount());

            spawnCoroutine = StartCoroutine(SpawnCarLoop());

            Debug.Log("[KiqqiFuelCarManager] Session started.");
        }

        private void SetupLanesForCurrentDifficulty()
        {
            var difficulty = app.Levels.GetCurrentDifficulty();
            
            HideAllLaneParents();
            
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
                
                Debug.Log($"[KiqqiFuelCarManager] Setup {childCount} lanes for difficulty: {difficulty}");
            }
            else
            {
                Debug.LogError($"[KiqqiFuelCarManager] No lane parent found for difficulty: {difficulty}");
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
            int laneCount = GetActiveLaneCount();
            
            for (int i = 0; i < laneCount && i < 6; i++)
            {
                availableFuelTypesCache.Add((FuelType)i);
            }
        }

        private int GetActiveLaneCount()
        {
            if (activeLanes != null)
                return activeLanes.Length;
            
            return levelLogic.GetLaneCount(app.Levels.currentLevel);
        }

        public void NotifyTimeExpired()
        {
            timeExpired = true;
            Debug.Log("[KiqqiFuelCarManager] Time expired - waiting for final car interactions.");

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            StartCoroutine(WaitForFinalCars());
        }

        private IEnumerator WaitForFinalCars()
        {
            yield return new WaitForSeconds(0.5f);

            while (activeCars.Count > 0)
            {
                yield return null;
            }

            Debug.Log("[KiqqiFuelCarManager] All cars cleared after time expired - ending session.");
            EndSession();
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
                    Debug.Log($"[KiqqiFuelCarManager] Stopping spawn - only {timeLeft:F1}s left.");
                    yield break;
                }

                if (firstCar)
                {
                    firstCar = false;
                    SpawnCar();
                }
                else
                {
                    yield return new WaitForSeconds(GetNextSpawnDelay());

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

        private void SpawnCar()
        {
            if (activeLanes == null || activeLanes.Length == 0)
            {
                Debug.LogWarning("[KiqqiFuelCarManager] No active lanes available for spawning!");
                return;
            }

            int laneCount = GetActiveLaneCount();
            int laneIndex = SelectLane(laneCount);

            if (laneIndex < 0 || laneIndex >= activeLanes.Length)
            {
                Debug.LogWarning($"[KiqqiFuelCarManager] Invalid lane index: {laneIndex}");
                return;
            }

            Transform lane = activeLanes[laneIndex];
            FuelType requiredFuel = SelectRandomFuelType(laneCount);

            GameObject carObj = Instantiate(carPrefab, activeLanesParent);
            carObj.SetActive(true);

            Vector3 spawnPos = lane.position;
            spawnPos.y = spawnYPosition;
            carObj.transform.position = spawnPos;

            var carImage = carObj.GetComponentInChildren<UnityEngine.UI.Image>();
            if (carImage && (int)requiredFuel < fuelColors.Length)
            {
                carImage.color = fuelColors[(int)requiredFuel];
            }

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            float waitTime = Random.Range(config.waitTimeMin, config.waitTimeMax);

            CarData carData = new CarData
            {
                id = carIdCounter++,
                requiredFuel = requiredFuel,
                instance = carObj,
                laneIndex = laneIndex,
                targetWaitPosition = new Vector3(lane.position.x, waitZoneYPosition, 0f),
                moveSpeed = baseCarSpeed,
                maxWaitTime = waitTime,
                waitTimeElapsed = 0f,
                isInWaitZone = false,
                isWaiting = false,
                carInFront = null
            };

            activeCars.Add(carData);
            lastSpawnedInLane[laneIndex] = carData.id;

            var button = carObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                var clickHandler = carObj.AddComponent<CarClickHandler>();
                clickHandler.Initialize(this, carData);
            }

            StartCoroutine(MoveCar(carData));
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

        private FuelType SelectRandomFuelType(int laneCount)
        {
            int maxFuelTypes = Mathf.Min(laneCount, availableFuelTypesCache.Count);
            if (maxFuelTypes <= 0) return FuelType.Red;

            return availableFuelTypesCache[Random.Range(0, maxFuelTypes)];
        }

        private IEnumerator MoveCar(CarData car)
        {
            bool reachedWaitZone = false;

            while (car.instance != null && sessionRunning)
            {
                CarData carAhead = FindCarAhead(car);

                if (carAhead != null)
                {
                    float distance = carAhead.instance.transform.position.y - car.instance.transform.position.y;

                    if (distance <= carStopDistance)
                    {
                        car.carInFront = carAhead;
                        yield return null;
                        continue;
                    }
                }

                float currentY = car.instance.transform.position.y;

                if (!reachedWaitZone && currentY >= car.targetWaitPosition.y)
                {
                    car.isInWaitZone = true;
                    car.isWaiting = true;
                    reachedWaitZone = true;
                    waitingCars.Add(car);

                    car.instance.transform.position = car.targetWaitPosition;
                    
                    StartCoroutine(HandleCarWaiting(car));
                    yield break;
                }

                Vector3 pos = car.instance.transform.position;
                pos.y += car.moveSpeed * Time.deltaTime;
                car.instance.transform.position = pos;

                yield return null;
            }
        }

        private CarData FindCarAhead(CarData currentCar)
        {
            CarData closestAhead = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < activeCars.Count; i++)
            {
                CarData other = activeCars[i];
                
                if (other.id == currentCar.id || other.laneIndex != currentCar.laneIndex)
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

        private IEnumerator HandleCarWaiting(CarData car)
        {
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

        #region PLAYER INTERACTION

        public void OnFuelButtonPressed(FuelType fuelType)
        {
            if (!isActive || isComplete || !sessionRunning)
            {
                Debug.LogWarning("[KiqqiFuelCarManager] Fuel button pressed but session not active.");
                return;
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            selectedFuelType = fuelType;
            view?.UpdateFuelButtonSelection(fuelType);

            Debug.Log($"[KiqqiFuelCarManager] Selected fuel: {fuelType}");
        }

        public void OnCarClicked(CarData car)
        {
            if (!isActive || isComplete || !sessionRunning)
            {
                Debug.LogWarning("[KiqqiFuelCarManager] Car clicked but session not active.");
                return;
            }

            if (!car.isWaiting)
            {
                Debug.Log("[KiqqiFuelCarManager] Car clicked but not in waiting state.");
                return;
            }

            if (!selectedFuelType.HasValue)
            {
                Debug.Log("[KiqqiFuelCarManager] No fuel selected, please select fuel first.");
                KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
                return;
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            bool correct = car.requiredFuel == selectedFuelType.Value;

            if (correct)
            {
                OnCarFueledCorrectly(car);
            }
            else
            {
                OnCarFueledIncorrectly(car, selectedFuelType.Value);
            }
        }

        private CarData FindFirstWaitingCar()
        {
            if (waitingCars.Count == 0) return null;

            CarData lowestCar = waitingCars[0];
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

        private void OnCarFueledCorrectly(CarData car)
        {
            currentComboStreak++;
            carsServed++;

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            int baseScore = config.correctScore;

            int earnedScore = baseScore;
            if (currentComboStreak >= config.comboThreshold)
            {
                earnedScore = Mathf.RoundToInt(baseScore * config.comboMultiplier);
            }

            sessionScore += earnedScore;
            masterGame.AddScore(earnedScore);
            view?.UpdateScoreLabel(masterGame.CurrentScore);
            view?.ShowFuelFeedback(car.instance.transform.position, true);

            app.Levels.NextLevel();

            selectedFuelType = null;
            view?.ClearFuelButtonSelection();

            RemoveCarAndLetNextMove(car);
        }

        private void OnCarFueledIncorrectly(CarData car, FuelType wrongFuel)
        {
            currentComboStreak = 0;

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            int penalty = config.wrongPenalty;

            sessionScore = Mathf.Max(0, sessionScore - penalty);
            masterGame.AddScore(-penalty);
            view?.UpdateScoreLabel(masterGame.CurrentScore);
            view?.ShowFuelFeedback(car.instance.transform.position, false);

            Debug.Log($"[KiqqiFuelCarManager] Wrong fuel! Required={car.requiredFuel}, Given={wrongFuel}, Penalty={penalty}");
        }

        private void OnCarTimedOut(CarData car)
        {
            currentComboStreak = 0;
            carsMissed++;

            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            int penalty = config.timeoutPenalty;

            sessionScore = Mathf.Max(0, sessionScore - penalty);
            masterGame.AddScore(-penalty);
            view?.UpdateScoreLabel(masterGame.CurrentScore);

            Debug.Log($"[KiqqiFuelCarManager] Car timed out! Penalty={penalty}");

            RemoveCarAndLetNextMove(car);
        }

        private void RemoveCarAndLetNextMove(CarData car)
        {
            car.isWaiting = false;
            waitingCars.Remove(car);

            StartCoroutine(MoveCarToExit(car));
        }

        private IEnumerator MoveCarToExit(CarData car)
        {
            if (car.instance == null) yield break;

            float exitSpeed = baseCarSpeed * 1.5f;

            while (car.instance != null && car.instance.transform.position.y < exitYPosition)
            {
                Vector3 pos = car.instance.transform.position;
                pos.y += exitSpeed * Time.deltaTime;
                car.instance.transform.position = pos;

                yield return null;
            }

            RemoveCar(car);
        }

        private void RemoveCar(CarData car)
        {
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

            Debug.Log($"[KiqqiFuelCarManager] Session ended. Final Score={sessionScore}, Cars Served={carsServed}, Missed={carsMissed}");
            CompleteMiniGame(sessionScore, true);
        }

        public void ResumeFromPause(KiqqiFuelCarView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;

            view?.UpdateScoreLabel(masterGame.CurrentScore);

            if (spawnCoroutine == null && !timeExpired)
            {
                spawnCoroutine = StartCoroutine(SpawnCarLoop());
            }

            Debug.Log("[KiqqiFuelCarManager] Resumed from pause.");
        }

        #endregion

        #region UTILITY

        public int GetCurrentComboStreak() => currentComboStreak;
        public int GetCarsServed() => carsServed;
        public int GetCarsMissed() => carsMissed;
        public Color GetFuelColor(FuelType type) => fuelColors[(int)type];

        #endregion
    }

    public class CarClickHandler : MonoBehaviour
    {
        private KiqqiFuelCarManager manager;
        private CarData carData;

        public void Initialize(KiqqiFuelCarManager mgr, CarData data)
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
