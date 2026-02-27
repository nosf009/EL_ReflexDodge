using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Kiqqi.Framework
{
    public enum DangerZoneBehavior
    {
        Static,
        Expanding,
        Chasing
    }

    public class DangerZone
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image image;
        public float warningDuration;
        public float elapsedTime;
        public bool isActive;
        public bool isDangerous;
        public float radius;
        public DangerZoneBehavior behavior;
        public Vector2 chaseTarget;
        public float chaseSpeed;

        public void Reset()
        {
            if (gameObject) gameObject.SetActive(false);
            elapsedTime = 0f;
            isActive = false;
            isDangerous = false;
            chaseTarget = Vector2.zero;
        }
    }

    public class KiqqiReflexDodgeManager : KiqqiMiniGameManagerBase
    {
        [Header("Level Manager Reference")]
        [Tooltip("Reference to level manager component")]
        public KiqqiReflexDodgeLevelManager levelMgr;

        [Header("Scene References")]
        [Tooltip("Player circle GameObject")]
        public GameObject playerObject;

        [Tooltip("Playable area bounds (for zone spawning)")]
        public RectTransform playableArea;

        [Tooltip("Parent transform holding inactive zone GameObjects")]
        public Transform zonePoolParent;

        [Header("Visual Settings")]
        public Color zoneWarningColor = new Color(1f, 0f, 0f, 0.3f);
        public Color zoneDangerousColor = new Color(1f, 0f, 0f, 0.8f);
        public Color playerColor = Color.green;

        [Header("Movement Settings")]
        [Tooltip("Use smooth movement instead of instant teleport")]
        public bool useSmoothMovement = true;
        [Tooltip("Speed for smooth movement (units per second)")]
        public float smoothMoveSpeed = 1200f;

        protected KiqqiReflexDodgeView view;
        protected KiqqiInputController input;

        protected List<DangerZone> zonePool = new List<DangerZone>();
        protected List<DangerZone> activeZones = new List<DangerZone>();

        protected Vector2 playerPosition;
        protected Vector2 playerTargetPosition;
        protected float playerRadius = 30f;

        protected float nextSpawnTime;
        protected int currentStreak;
        protected bool inputEnabled;
        protected bool sessionEnding;

        protected int currentLevel;
        protected float spawnInterval;
        protected int maxZones;
        protected float warningDuration;
        protected Vector2 zoneRadiusRange;

        protected Coroutine sessionRoutine;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiReflexDodgeView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiReflexDodgeView>();
            input = context.Input;

            if (!levelMgr)
            {
                levelMgr = GetComponent<KiqqiReflexDodgeLevelManager>();
                if (!levelMgr)
                {
                    Debug.LogError("[KiqqiReflexDodgeManager] KiqqiReflexDodgeLevelManager not assigned and not found on same GameObject!");
                }
            }

            InitializeZonePool();
            InitializePlayer();

            Debug.Log("[KiqqiReflexDodgeManager] Initialized.");
        }

        protected virtual void InitializeZonePool()
        {
            if (!zonePoolParent)
            {
                Debug.LogError("[KiqqiReflexDodgeManager] Zone pool parent missing!");
                return;
            }

            foreach (Transform child in zonePoolParent)
            {
                GameObject zoneGO = child.gameObject;
                zoneGO.SetActive(false);

                DangerZone zone = new DangerZone
                {
                    gameObject = zoneGO,
                    rectTransform = zoneGO.GetComponent<RectTransform>(),
                    image = zoneGO.GetComponent<Image>()
                };

                if (!zone.rectTransform)
                {
                    Debug.LogError($"[KiqqiReflexDodgeManager] Zone '{zoneGO.name}' missing RectTransform! All UI elements must have RectTransform.");
                }

                if (!zone.image)
                {
                    Debug.LogWarning($"[KiqqiReflexDodgeManager] Zone '{zoneGO.name}' missing Image component! Adding one...");
                    zone.image = zoneGO.AddComponent<Image>();
                }

                zonePool.Add(zone);
            }

            Debug.Log($"[KiqqiReflexDodgeManager] Zone pool initialized with {zonePool.Count} zones from hierarchy.");
        }

        protected virtual void InitializePlayer()
        {
            if (playerObject)
            {
                Image playerImage = playerObject.GetComponent<Image>();
                if (playerImage)
                {
                    playerImage.color = playerColor;
                }

                RectTransform playerRect = playerObject.GetComponent<RectTransform>();
                if (playerRect)
                {
                    playerPosition = playerRect.anchoredPosition;
                }
            }
        }

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            if (sessionRoutine != null)
            {
                StopCoroutine(sessionRoutine);
                sessionRoutine = null;
            }

            sessionScore = 0;
            currentStreak = 0;
            inputEnabled = false;
            sessionEnding = false;
            nextSpawnTime = 0f;

            currentLevel = app.Levels ? app.Levels.currentLevel : 1;

            LoadLevelSettings();

            foreach (var zone in activeZones)
            {
                zone.Reset();
            }
            activeZones.Clear();

            foreach (var zone in zonePool)
            {
                zone.Reset();
            }

            if (playerObject)
            {
                RectTransform playerRect = playerObject.GetComponent<RectTransform>();
                if (playerRect)
                {
                    playerRect.anchoredPosition = Vector2.zero;
                    playerPosition = Vector2.zero;
                    playerTargetPosition = Vector2.zero;
                }
                playerObject.SetActive(true);
            }

            if (view)
            {
                view.UpdateStreakDisplay(0, 1f);
            }

            sessionRoutine = StartCoroutine(RunSession());

            Debug.Log($"[KiqqiReflexDodgeManager] Session started - Level {currentLevel}");
        }

        protected virtual void LoadLevelSettings()
        {
            if (!levelMgr) return;

            spawnInterval = levelMgr.GetSpawnInterval(currentLevel);
            maxZones = levelMgr.GetMaxZones(currentLevel);
            warningDuration = levelMgr.GetWarningDuration(currentLevel);
            zoneRadiusRange = levelMgr.GetZoneRadiusRange(currentLevel);

            Debug.Log($"[KiqqiReflexDodgeManager] Level {currentLevel}: spawnInterval={spawnInterval}s, maxZones={maxZones}, warning={warningDuration}s");
        }

        protected virtual IEnumerator RunSession()
        {
            yield return new WaitForSeconds(0.5f);

            inputEnabled = true;

            float sessionTime = levelMgr ? levelMgr.GetLevelTime(currentLevel) : 60f;
            float elapsed = 0f;
            float lastSpawnTime = -spawnInterval;

            Debug.Log($"[KiqqiReflexDodgeManager] RunSession started: sessionTime={sessionTime}s, spawnInterval={spawnInterval}s, initial spawn at t=0");

            SpawnDangerZone();
            lastSpawnTime = 0f;

            while (elapsed < sessionTime && !sessionEnding)
            {
                elapsed += Time.deltaTime;

                if (elapsed - lastSpawnTime >= spawnInterval && activeZones.Count < maxZones)
                {
                    SpawnDangerZone();
                    lastSpawnTime = elapsed;
                }

                UpdatePlayerMovement();
                UpdateActiveZones();

                yield return null;
            }

            yield return new WaitForSeconds(0.3f);

            while (activeZones.Count > 0)
            {
                UpdatePlayerMovement();
                UpdateActiveZones();
                yield return null;
            }

            EndSession();
        }

        protected virtual void SpawnDangerZone()
        {
            if (!playableArea || !levelMgr)
            {
                Debug.LogWarning($"[KiqqiReflexDodgeManager] Cannot spawn: playableArea={playableArea != null}, levelMgr={levelMgr != null}");
                return;
            }

            DangerZone zone = GetAvailableZone();
            if (zone == null)
            {
                Debug.LogWarning("[KiqqiReflexDodgeManager] No available zone from pool!");
                return;
            }

            Rect bounds = playableArea.rect;
            Vector2 randomPos = new Vector2(
                Random.Range(bounds.xMin + 50f, bounds.xMax - 50f),
                Random.Range(bounds.yMin + 50f, bounds.yMax - 50f)
            );

            float radius = Random.Range(zoneRadiusRange.x, zoneRadiusRange.y);

            zone.rectTransform.anchoredPosition = randomPos;
            zone.rectTransform.sizeDelta = Vector2.one * radius * 2f;
            zone.radius = radius;
            zone.warningDuration = warningDuration;
            zone.elapsedTime = 0f;
            zone.isActive = true;
            zone.isDangerous = false;

            DetermineBehavior(zone);

            if (zone.image)
            {
                zone.image.color = zoneWarningColor;
            }

            zone.gameObject.SetActive(true);
            zone.rectTransform.SetAsLastSibling();
            
            activeZones.Add(zone);

            Debug.Log($"[KiqqiReflexDodgeManager] Spawned zone '{zone.gameObject.name}' at {randomPos}, radius={radius}, behavior={zone.behavior}, active={zone.gameObject.activeSelf}, parent={zone.gameObject.transform.parent.name}");
        }

        protected virtual void DetermineBehavior(DangerZone zone)
        {
            if (!levelMgr) return;

            float expandChance = levelMgr.GetExpandingChance(currentLevel);
            float chaseChance = levelMgr.GetChasingChance(currentLevel);

            float roll = Random.value;

            if (roll < chaseChance)
            {
                zone.behavior = DangerZoneBehavior.Chasing;
                zone.chaseSpeed = levelMgr.GetChaseSpeed(currentLevel);
                zone.chaseTarget = playerPosition;

                if (zone.image)
                {
                    zone.image.color = new Color(0.5f, 0f, 0.5f, 0.3f);
                }
            }
            else if (roll < chaseChance + expandChance)
            {
                zone.behavior = DangerZoneBehavior.Expanding;
            }
            else
            {
                zone.behavior = DangerZoneBehavior.Static;
            }
        }

        protected virtual DangerZone GetAvailableZone()
        {
            foreach (var zone in zonePool)
            {
                if (!zone.isActive)
                {
                    return zone;
                }
            }

            Debug.LogWarning("[KiqqiReflexDodgeManager] No available zones in pool! Add more zone GameObjects to hierarchy.");
            return null;
        }

        protected virtual void UpdateActiveZones()
        {
            for (int i = activeZones.Count - 1; i >= 0; i--)
            {
                DangerZone zone = activeZones[i];
                zone.elapsedTime += Time.deltaTime;

                if (!zone.isDangerous)
                {
                    float pulseSpeed = 3f;
                    float alpha = Mathf.Lerp(0.2f, 0.5f, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
                    Color pulseColor = zoneWarningColor;
                    pulseColor.a = alpha;
                    if (zone.image)
                    {
                        zone.image.color = pulseColor;
                    }
                }

                if (!zone.isDangerous && zone.elapsedTime >= zone.warningDuration)
                {
                    zone.isDangerous = true;

                    if (zone.image)
                    {
                        zone.image.color = zoneDangerousColor;
                    }

                    CheckPlayerHit(zone);
                }

                if (zone.behavior == DangerZoneBehavior.Chasing && zone.isDangerous)
                {
                    Vector2 currentPos = zone.rectTransform.anchoredPosition;
                    Vector2 direction = (playerPosition - currentPos).normalized;
                    zone.rectTransform.anchoredPosition = Vector2.MoveTowards(currentPos, playerPosition, zone.chaseSpeed * Time.deltaTime);
                }
                else if (zone.behavior == DangerZoneBehavior.Expanding && !zone.isDangerous)
                {
                    float scale = Mathf.Lerp(zone.radius * 0.5f, zone.radius, zone.elapsedTime / zone.warningDuration);
                    zone.rectTransform.sizeDelta = Vector2.one * scale * 2f;
                }

                if (zone.isDangerous && zone.elapsedTime >= zone.warningDuration + 0.5f)
                {
                    OnZoneExplode(zone);
                    zone.Reset();
                    activeZones.RemoveAt(i);
                }
            }
        }

        protected virtual void CheckPlayerHit(DangerZone zone)
        {
            float distance = Vector2.Distance(playerPosition, zone.rectTransform.anchoredPosition);

            if (distance <= zone.radius + playerRadius)
            {
                OnPlayerHit(zone);
            }
            else
            {
                OnSuccessfulDodge(zone);
            }
        }

        protected virtual void OnZoneExplode(DangerZone zone)
        {
        }

        protected virtual void OnPlayerHit(DangerZone zone)
        {
            if (!levelMgr) return;

            int penalty = levelMgr.GetHitPenalty(currentLevel);
            sessionScore -= penalty;
            masterGame.AddScore(-penalty);

            currentStreak = 0;

            if (view)
            {
                view.UpdateStreakDisplay(currentStreak, 1f);
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");

            Debug.Log($"[KiqqiReflexDodgeManager] Player HIT! Penalty: -{penalty}");
        }

        protected virtual void OnSuccessfulDodge(DangerZone zone)
        {
            if (!levelMgr) return;

            int baseScore = levelMgr.GetDodgeScore(currentLevel);
            currentStreak++;

            int threshold = levelMgr.GetStreakThreshold(currentLevel);
            float multiplier = currentStreak >= threshold ? levelMgr.GetStreakMultiplier(currentLevel) : 1f;

            int earnedScore = Mathf.RoundToInt(baseScore * multiplier);
            sessionScore += earnedScore;
            masterGame.AddScore(earnedScore);

            if (view)
            {
                view.UpdateStreakDisplay(currentStreak, multiplier);
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect");

            Debug.Log($"[KiqqiReflexDodgeManager] Dodge SUCCESS! +{earnedScore} (streak: {currentStreak}, mult: {multiplier}x)");
        }

        public virtual void HandlePlayerTap(Vector2 screenPosition)
        {
            if (!isActive || !inputEnabled || sessionEnding) return;

            if (playerObject && playableArea)
            {
                RectTransform playerRect = playerObject.GetComponent<RectTransform>();
                if (playerRect)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        playableArea,
                        screenPosition,
                        null,
                        out localPoint
                    );

                    if (useSmoothMovement)
                    {
                        playerTargetPosition = localPoint;
                    }
                    else
                    {
                        playerRect.anchoredPosition = localPoint;
                        playerPosition = localPoint;
                    }
                }
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");
        }

        protected virtual void UpdatePlayerMovement()
        {
            if (!playerObject || !useSmoothMovement) return;

            RectTransform playerRect = playerObject.GetComponent<RectTransform>();
            if (!playerRect) return;

            if (Vector2.Distance(playerPosition, playerTargetPosition) > 1f)
            {
                playerPosition = Vector2.MoveTowards(
                    playerPosition,
                    playerTargetPosition,
                    smoothMoveSpeed * Time.deltaTime
                );
                playerRect.anchoredPosition = playerPosition;
            }
        }

        protected virtual void EndSession()
        {
            if (sessionEnding) return;

            sessionEnding = true;
            inputEnabled = false;

            Debug.Log($"[KiqqiReflexDodgeManager] Session ended. Final Score: {sessionScore}");

            CompleteMiniGame(sessionScore, sessionScore > 0);
        }

        public void NotifyTimeUp()
        {
            if (sessionEnding) return;
            sessionEnding = true;
        }

        public void ResumeFromPause(KiqqiReflexDodgeView v)
        {
            view = v ?? view;
            isActive = true;
            isComplete = false;

            if (view && levelMgr)
            {
                float multiplier = currentStreak >= levelMgr.GetStreakThreshold(currentLevel) ? levelMgr.GetStreakMultiplier(currentLevel) : 1f;
                view.UpdateStreakDisplay(currentStreak, multiplier);
            }

            Debug.Log("[KiqqiReflexDodgeManager] Resumed from pause.");
        }

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();

            if (sessionRoutine != null)
            {
                StopCoroutine(sessionRoutine);
                sessionRoutine = null;
            }

            currentStreak = 0;
            sessionEnding = false;
            inputEnabled = false;

            foreach (var zone in activeZones)
            {
                zone.Reset();
            }
            activeZones.Clear();

            foreach (var zone in zonePool)
            {
                zone.Reset();
            }

            if (playerObject)
            {
                RectTransform playerRect = playerObject.GetComponent<RectTransform>();
                if (playerRect)
                {
                    playerRect.anchoredPosition = Vector2.zero;
                    playerPosition = Vector2.zero;
                    playerTargetPosition = Vector2.zero;
                }
            }

            Debug.Log("[KiqqiReflexDodgeManager] ResetMiniGame - state cleared.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            if (sessionRoutine != null)
            {
                StopCoroutine(sessionRoutine);
                sessionRoutine = null;
            }

            inputEnabled = false;
            sessionEnding = false;
            isActive = false;
            isComplete = true;

            foreach (var zone in activeZones)
            {
                zone.Reset();
            }
            activeZones.Clear();

            if (playerObject)
            {
                playerObject.SetActive(false);
            }

            Debug.Log("[KiqqiReflexDodgeManager] OnMiniGameExit - cleaned up.");
        }
    }
}
