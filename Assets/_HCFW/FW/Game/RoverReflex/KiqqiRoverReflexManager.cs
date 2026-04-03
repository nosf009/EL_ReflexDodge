using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Coffee.UIExtensions;

namespace Kiqqi.Framework
{
    public enum DangerZoneBehavior
    {
        Static,
        Expanding,
        Chasing
    }

    /// <summary>
    /// Represents one active meteor threat: shadow ring + flying meteor visual + lifecycle state.
    /// </summary>
    public class DangerZone
    {
        // Root GO (parent of Shadow and Meteor children)
        public GameObject gameObject;
        public RectTransform rectTransform;

        // Shadow ring child - warning indicator on the ground
        public RectTransform shadowRect;
        public Image shadowImage;

        // Meteor child - base rock Image
        public RectTransform meteorRect;
        public Image meteorBaseImage;

        // Lifecycle
        public float warningDuration;
        public float elapsedTime;
        public bool isActive;
        public bool isDangerous;
        public float radius;
        public DangerZoneBehavior behavior;
        public Vector2 chaseTarget;
        public float chaseSpeed;

        // Meteor fly-in data
        public float meteorFlyDuration;
        public float meteorElapsed;
        public bool meteorArrived;

        // Phase tracking
        public bool  isFlashing;    // true during initial warning flash phase
        public float flashElapsed;  // elapsed within flash phase
        public float impactElapsed; // elapsed after impact (for cleanup)

        public void Reset()
        {
            if (gameObject) gameObject.SetActive(false);
            elapsedTime   = 0f;
            meteorElapsed = 0f;
            flashElapsed  = 0f;
            impactElapsed = 0f;
            isActive      = false;
            isDangerous   = false;
            isFlashing    = false;
            meteorArrived = false;
            chaseTarget   = Vector2.zero;
        }
    }

    public class KiqqiRoverReflexManager : KiqqiMiniGameManagerBase
    {
        // ─── Inspector ───────────────────────────────────────────────────────────

        [Header("Level Manager Reference")]
        [Tooltip("Reference to level manager component")]
        public KiqqiRoverReflexLevelManager levelMgr;

        [Header("Scene References")]
        [Tooltip("Player rover GameObject")]
        public GameObject playerObject;

        [Tooltip("Playable area bounds (for zone spawning)")]
        public RectTransform playableArea;

        [Tooltip("Parent transform holding inactive zone GameObjects (each must have Shadow + Meteor children)")]
        public Transform zonePoolParent;

        [Tooltip("Parent transform used as container when spawning impact particles at runtime")]
        public Transform impactPoolParent;

        [Tooltip("Impact particle GO to instantiate on meteor hit - keep it SetActive(false) in the scene as a template")]
        public GameObject impactParticleTemplate;

        [Header("Rover Art")]
        [Tooltip("Sprite for the rover body - assign Astro Madness_rover.png")]
        public Sprite roverSprite;

        [Tooltip("Destination marker RectTransform shown at the tap target (sibling of Player inside playableArea or rrRoverReflexGameView)")]
        public RectTransform destinationMarker;

        [Tooltip("Sprite for the destination marker - assign Astro Madness_rover _ destination.png")]
        public Sprite destinationMarkerSprite;

        [Header("Meteor Art")]
        [Tooltip("Sprite for the asteroid body - assign Astro Madness_asteroid _ base.png")]
        public Sprite meteorBaseSprite;

        [Tooltip("Sprite for the shadow/warning ring - assign Astro Madness_asteroid _ target.png")]
        public Sprite shadowRingSprite;

        [Header("Zone Visual Settings")]
        public Color zoneWarningColor   = new Color(1f, 0.15f, 0.15f, 0.5f);
        public Color zoneDangerousColor = new Color(1f, 0f,    0f,    0.9f);

        [Header("Warning Flash")]
        [Tooltip("Number of on/off flashes before meteor appears")]
        [Range(2, 10)]
        public int warningFlashCount = 5;

        [Tooltip("Total duration of the flash warning sequence (seconds)")]
        public float warningFlashDuration = 0.75f;

        [Header("Movement Settings")]
        [Tooltip("Use smooth movement instead of instant teleport")]
        public bool useSmoothMovement = true;

        [Tooltip("Speed for smooth movement (canvas units/sec)")]
        public float smoothMoveSpeed = 1200f;

        [Tooltip("Rover rotation speed (degrees/sec)")]
        public float roverRotationSpeed = 720f;

        [Tooltip("Distance threshold to consider rover arrived at destination")]
        public float arrivalThreshold = 8f;

        [Header("Meteor Settings")]
        [Tooltip("Size of the meteor base Image in canvas units")]
        public Vector2 meteorSize = new Vector2(120f, 120f);

        [Header("Impact Particle Scale")]
        [Tooltip("UIParticle scale when zone radius is at minimum")]
        public float impactScaleMin = 50f;

        [Tooltip("UIParticle scale when zone radius is at maximum")]
        public float impactScaleMax = 200f;

        // ─── Runtime State ───────────────────────────────────────────────────────

        protected KiqqiRoverReflexView view;
        protected KiqqiInputController input;

        protected List<DangerZone> zonePool   = new List<DangerZone>();
        protected List<DangerZone> activeZones = new List<DangerZone>();

        protected Vector2 playerPosition;
        protected Vector2 playerTargetPosition;
        protected bool isMoving;
        protected float playerRadius = 30f;

        protected int currentStreak;
        protected bool inputEnabled;
        protected bool sessionEnding;

        protected int currentLevel;
        protected float spawnInterval;
        protected int maxZones;
        protected float warningDuration;
        protected Vector2 zoneRadiusRange;

        protected Coroutine sessionRoutine;

        // Cached player components
        protected RectTransform playerRect;
        protected Image playerImage;

        // ─── Framework ───────────────────────────────────────────────────────────

        public override System.Type GetAssociatedViewType() => typeof(KiqqiRoverReflexView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view  = context.UI.GetView<KiqqiRoverReflexView>();
            input = context.Input;

            if (!levelMgr)
            {
                levelMgr = GetComponent<KiqqiRoverReflexLevelManager>();
                if (!levelMgr)
                    Debug.LogError("[KiqqiRoverReflexManager] KiqqiRoverReflexLevelManager not assigned!");
            }

            InitializeZonePool();
            InitializeImpactPool();
            InitializePlayer();

            Debug.Log("[KiqqiRoverReflexManager] Initialized.");
        }

        // ─── Pool Init ───────────────────────────────────────────────────────────

        protected virtual void InitializeZonePool()
        {
            if (!zonePoolParent)
            {
                Debug.LogError("[KiqqiRoverReflexManager] Zone pool parent missing!");
                return;
            }

            foreach (Transform child in zonePoolParent)
            {
                GameObject zoneGO = child.gameObject;
                zoneGO.SetActive(false);

                DangerZone zone = new DangerZone
                {
                    gameObject    = zoneGO,
                    rectTransform = zoneGO.GetComponent<RectTransform>()
                };

                if (!zone.rectTransform)
                {
                    Debug.LogError($"[KiqqiRoverReflexManager] Zone '{zoneGO.name}' missing RectTransform!");
                    continue;
                }

                // Expect named children: "Shadow" and "Meteor" (Meteor has a "Tail" child)
                Transform shadowT = zoneGO.transform.Find("Shadow");
                Transform meteorT = zoneGO.transform.Find("Meteor");

                if (shadowT)
                {
                    zone.shadowRect  = shadowT.GetComponent<RectTransform>();
                    zone.shadowImage = shadowT.GetComponent<Image>();
                    if (zone.shadowImage && shadowRingSprite)
                        zone.shadowImage.sprite = shadowRingSprite;
                }
                else
                {
                    Debug.LogWarning($"[KiqqiRoverReflexManager] Zone '{zoneGO.name}' has no 'Shadow' child.");
                }

                if (meteorT)
                {
                    zone.meteorRect      = meteorT.GetComponent<RectTransform>();
                    zone.meteorBaseImage = meteorT.GetComponent<Image>();
                    if (zone.meteorBaseImage && meteorBaseSprite)
                    {
                        zone.meteorBaseImage.sprite         = meteorBaseSprite;
                        zone.meteorBaseImage.preserveAspect = true;
                    }
                }
                else
                {
                    Debug.LogWarning($"[KiqqiRoverReflexManager] Zone '{zoneGO.name}' has no 'Meteor' child.");
                }

                zonePool.Add(zone);
            }

            Debug.Log($"[KiqqiRoverReflexManager] Zone pool: {zonePool.Count} zones.");
        }

        protected virtual void InitializeImpactPool()
        {
            // Template is kept inactive in the scene - nothing to pre-initialize
            if (!impactParticleTemplate)
                Debug.LogWarning("[KiqqiRoverReflexManager] Impact particle template not assigned.");
        }

        protected virtual void InitializePlayer()
        {
            if (!playerObject) return;

            playerRect  = playerObject.GetComponent<RectTransform>();
            playerImage = playerObject.GetComponent<Image>();

            if (playerImage && roverSprite)
                playerImage.sprite = roverSprite;

            if (playerRect)
                playerPosition = playerRect.anchoredPosition;

            playerTargetPosition = playerPosition;

            // Set up destination marker sprite
            if (destinationMarker)
            {
                Image markerImage = destinationMarker.GetComponent<Image>();
                if (markerImage && destinationMarkerSprite)
                    markerImage.sprite = destinationMarkerSprite;

                destinationMarker.gameObject.SetActive(false);
            }
        }

        // ─── Game Lifecycle ──────────────────────────────────────────────────────

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            if (sessionRoutine != null)
            {
                StopCoroutine(sessionRoutine);
                sessionRoutine = null;
            }

            sessionScore  = 0;
            currentStreak = 0;
            inputEnabled  = false;
            sessionEnding = false;
            isMoving      = false;

            currentLevel = app.Levels ? app.Levels.currentLevel : 1;
            LoadLevelSettings();

            foreach (var zone in activeZones) zone.Reset();
            activeZones.Clear();
            foreach (var zone in zonePool) zone.Reset();

            if (playerRect)
            {
                playerRect.anchoredPosition = Vector2.zero;
                playerRect.localEulerAngles = Vector3.zero;
                playerPosition              = Vector2.zero;
                playerTargetPosition        = Vector2.zero;
            }

            if (playerObject) playerObject.SetActive(true);

            if (destinationMarker) destinationMarker.gameObject.SetActive(false);

            if (view) view.UpdateStreakDisplay(0, 1f);

            sessionRoutine = StartCoroutine(RunSession());

            Debug.Log($"[KiqqiRoverReflexManager] Session started - Level {currentLevel}");
        }

        protected virtual void LoadLevelSettings()
        {
            if (!levelMgr) return;

            spawnInterval   = levelMgr.GetSpawnInterval(currentLevel);
            maxZones        = levelMgr.GetMaxZones(currentLevel);
            warningDuration = levelMgr.GetWarningDuration(currentLevel);
            zoneRadiusRange = levelMgr.GetZoneRadiusRange(currentLevel);

            Debug.Log($"[KiqqiRoverReflexManager] Level {currentLevel}: interval={spawnInterval}s, maxZones={maxZones}, warning={warningDuration}s");
        }

        // ─── Session Loop ────────────────────────────────────────────────────────

        protected virtual IEnumerator RunSession()
        {
            yield return new WaitForSeconds(0.5f);

            inputEnabled = true;

            float sessionTime   = levelMgr ? levelMgr.GetLevelTime(currentLevel) : 60f;
            float elapsed       = 0f;
            float lastSpawnTime = 0f;

            SpawnDangerZone();

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

        // ─── Zone Spawning ───────────────────────────────────────────────────────

        protected virtual void SpawnDangerZone()
        {
            if (!playableArea || !levelMgr) return;

            DangerZone zone = GetAvailableZone();
            if (zone == null)
            {
                Debug.LogWarning("[KiqqiRoverReflexManager] No available zone from pool!");
                return;
            }

            Rect bounds = playableArea.rect;
            Vector2 shadowPos = new Vector2(
                Random.Range(bounds.xMin + 80f, bounds.xMax - 80f),
                Random.Range(bounds.yMin + 80f, bounds.yMax - 80f)
            );

            float radius = Random.Range(zoneRadiusRange.x, zoneRadiusRange.y);

            // Root positioned at shadow world location; all children use local coords from here
            zone.rectTransform.anchoredPosition = shadowPos;

            if (zone.shadowRect)
            {
                zone.shadowRect.anchoredPosition = Vector2.zero;
                zone.shadowRect.sizeDelta        = Vector2.one * radius * 2f;
            }

            // Shadow starts transparent - it will flash during warning phase
            if (zone.shadowImage)
            {
                Color c = zoneWarningColor;
                c.a = 0f;
                zone.shadowImage.color = c;
            }

            // Meteor hidden until flash phase completes
            if (zone.meteorRect)
            {
                zone.meteorRect.anchoredPosition = Vector2.zero;
                zone.meteorRect.sizeDelta        = meteorSize;
                zone.meteorRect.localScale       = new Vector3(0.1f, 0.1f, 1f);
                zone.meteorRect.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
                zone.meteorRect.gameObject.SetActive(false);
            }

            // warningDuration from level manager is the meteor fall duration
            zone.meteorFlyDuration = warningDuration;
            zone.meteorElapsed     = 0f;
            zone.meteorArrived     = false;
            zone.isFlashing        = true;
            zone.flashElapsed      = 0f;
            zone.impactElapsed     = 0f;

            zone.radius          = radius;
            zone.warningDuration = warningDuration;
            zone.elapsedTime     = 0f;
            zone.isActive        = true;
            zone.isDangerous     = false;

            DetermineBehavior(zone);

            zone.gameObject.SetActive(true);
            zone.rectTransform.SetAsLastSibling();
            activeZones.Add(zone);

            Debug.Log($"[KiqqiRoverReflexManager] Spawned '{zone.gameObject.name}' at {shadowPos}, r={radius}, behavior={zone.behavior}");
        }

        protected virtual void DetermineBehavior(DangerZone zone)
        {
            if (!levelMgr) return;

            float expandChance = levelMgr.GetExpandingChance(currentLevel);
            float chaseChance  = levelMgr.GetChasingChance(currentLevel);
            float roll         = Random.value;

            if (roll < chaseChance)
            {
                zone.behavior    = DangerZoneBehavior.Chasing;
                zone.chaseSpeed  = levelMgr.GetChaseSpeed(currentLevel);
                zone.chaseTarget = playerPosition;

                if (zone.shadowImage)
                    zone.shadowImage.color = new Color(0.6f, 0f, 0.6f, 0.5f);
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
                if (!zone.isActive) return zone;

            Debug.LogWarning("[KiqqiRoverReflexManager] Zone pool exhausted!");
            return null;
        }

        // ─── Zone Update ─────────────────────────────────────────────────────────

        protected virtual void UpdateActiveZones()
        {
            for (int i = activeZones.Count - 1; i >= 0; i--)
            {
                DangerZone zone = activeZones[i];
                zone.elapsedTime += Time.deltaTime;

                // ── Phase 1: Warning flash ────────────────────────────────────────
                if (zone.isFlashing)
                {
                    zone.flashElapsed += Time.deltaTime;

                    // Strobe shadow on/off
                    if (zone.shadowImage)
                    {
                        float cycle   = warningFlashDuration / warningFlashCount;
                        float inCycle = zone.flashElapsed % cycle;
                        bool  on      = inCycle < cycle * 0.5f;

                        // Expanding behavior scales the ring during flash phase
                        if (zone.behavior == DangerZoneBehavior.Expanding && zone.shadowRect)
                        {
                            float expandT = zone.flashElapsed / warningFlashDuration;
                            zone.shadowRect.sizeDelta = Vector2.one * Mathf.Lerp(zone.radius * 0.5f, zone.radius, expandT) * 2f;
                        }

                        Color c = zoneWarningColor;
                        c.a = on ? 0.8f : 0f;
                        zone.shadowImage.color = c;
                    }

                    // Flash complete → hide shadow, show meteor
                    if (zone.flashElapsed >= warningFlashDuration)
                    {
                        zone.isFlashing = false;

                        if (zone.shadowImage)
                        {
                            Color c = zoneWarningColor;
                            c.a = 0f;
                            zone.shadowImage.color = c;
                        }

                        if (zone.meteorRect)
                            zone.meteorRect.gameObject.SetActive(true);
                    }

                    continue; // nothing else runs during flash phase
                }

                // ── Phase 2: Meteor fall ──────────────────────────────────────────
                if (!zone.meteorArrived && zone.meteorRect)
                {
                    zone.meteorElapsed += Time.deltaTime;
                    float t     = Mathf.Clamp01(zone.meteorElapsed / zone.meteorFlyDuration);
                    float eased = t * t * t; // cubic ease-in - accelerates into impact
                    float scale = Mathf.Lerp(0.1f, 1f, eased);
                    zone.meteorRect.localScale = new Vector3(scale, scale, 1f);

                    if (t >= 1f)
                    {
                        zone.meteorArrived = true;
                        TriggerImpact(zone);
                    }
                }

                // Chasing behavior: shadow tracks player after impact
                if (zone.behavior == DangerZoneBehavior.Chasing && zone.isDangerous)
                {
                    zone.rectTransform.anchoredPosition = Vector2.MoveTowards(
                        zone.rectTransform.anchoredPosition, playerPosition, zone.chaseSpeed * Time.deltaTime);
                }

                // ── Phase 3: Cleanup 0.5s after impact ───────────────────────────
                if (zone.isDangerous)
                {
                    zone.impactElapsed += Time.deltaTime;
                    if (zone.impactElapsed >= 0.5f)
                    {
                        OnZoneExplode(zone);
                        zone.Reset();
                        activeZones.RemoveAt(i);
                    }
                }
            }
        }

        // ─── Hit Detection ───────────────────────────────────────────────────────

        /// <summary>Called the frame the meteor arrives at its target. Fires particle, hides meteor, checks hit.</summary>
        protected virtual void TriggerImpact(DangerZone zone)
        {
            if (zone.isDangerous) return; // already triggered (safety guard)
            zone.isDangerous = true;

            if (zone.meteorRect)
            {
                zone.meteorRect.localScale = Vector3.one;
                zone.meteorRect.gameObject.SetActive(false);
            }

            PlayImpactParticle(zone.rectTransform.anchoredPosition, zone.radius);

            if (zone.shadowImage)
                zone.shadowImage.color = zoneDangerousColor;

            CheckPlayerHit(zone);
        }

        protected virtual void CheckPlayerHit(DangerZone zone)
        {
            float distance = Vector2.Distance(playerPosition, zone.rectTransform.anchoredPosition);

            if (distance <= zone.radius + playerRadius)
                OnPlayerHit(zone);
            else
                OnSuccessfulDodge(zone);
        }

        // ─── Game Events ─────────────────────────────────────────────────────────

        protected virtual void OnZoneExplode(DangerZone zone)
        {
            // Particle was already fired at impact moment - nothing more needed here
        }

        protected virtual void OnPlayerHit(DangerZone zone)
        {
            if (!levelMgr) return;

            int penalty   = levelMgr.GetHitPenalty(currentLevel);
            sessionScore  = Mathf.Max(0, sessionScore - penalty);
            currentStreak = 0;

            if (view) view.UpdateStreakDisplay(currentStreak, 1f);

            KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");

            Debug.Log($"[KiqqiRoverReflexManager] HIT! Penalty -{penalty}, score={sessionScore}");
        }

        protected virtual void OnSuccessfulDodge(DangerZone zone)
        {
            if (!levelMgr) return;

            int baseScore    = levelMgr.GetDodgeScore(currentLevel);
            currentStreak++;

            int   threshold  = levelMgr.GetStreakThreshold(currentLevel);
            float multiplier = currentStreak >= threshold ? levelMgr.GetStreakMultiplier(currentLevel) : 1f;
            int   earned     = Mathf.RoundToInt(baseScore * multiplier);
            sessionScore    += earned;

            if (view) view.UpdateStreakDisplay(currentStreak, multiplier);

            KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect");

            Debug.Log($"[KiqqiRoverReflexManager] Dodge +{earned} (streak:{currentStreak} x{multiplier}), score={sessionScore}");
        }

        // ─── Impact Particles ────────────────────────────────────────────────────

        protected virtual void PlayImpactParticle(Vector2 anchoredPos, float radius)
        {
            if (!impactParticleTemplate) return;

            Transform parent = impactPoolParent ? impactPoolParent : impactParticleTemplate.transform.parent;
            GameObject instance = Instantiate(impactParticleTemplate, parent);

            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt) rt.anchoredPosition = anchoredPos;

            // Scale UIParticle proportionally to the zone radius
            UIParticle uiParticle = instance.GetComponent<UIParticle>();
            if (uiParticle)
            {
                float t = Mathf.InverseLerp(zoneRadiusRange.x, zoneRadiusRange.y, radius);
                uiParticle.scale = Mathf.Lerp(impactScaleMin, impactScaleMax, t);
            }

            instance.SetActive(true);

            ParticleSystem ps = instance.GetComponent<ParticleSystem>();
            if (ps) ps.Play();

            float destroyDelay = ps ? ps.main.duration + ps.main.startLifetime.constantMax + 0.1f : 2f;
            Destroy(instance, destroyDelay);
        }

        // ─── Player Input & Movement ─────────────────────────────────────────────

        /// <summary>Called by the view on tap/click.</summary>
        public virtual void HandlePlayerTap(Vector2 screenPosition)
        {
            if (!isActive || !inputEnabled || sessionEnding) return;
            if (!playableArea) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    playableArea, screenPosition, null, out localPoint))
            {
                playerTargetPosition = localPoint;
                isMoving = true;
                ShowDestinationMarker(localPoint);
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");
        }

        protected virtual void UpdatePlayerMovement()
        {
            if (!playerObject) return;
            if (!playerRect) playerRect = playerObject.GetComponent<RectTransform>();
            if (!playerRect) return;

            float dist = Vector2.Distance(playerPosition, playerTargetPosition);

            if (dist > arrivalThreshold)
            {
                isMoving = true;

                Vector2 prevPos = playerPosition;

                playerPosition = useSmoothMovement
                    ? Vector2.MoveTowards(playerPosition, playerTargetPosition, smoothMoveSpeed * Time.deltaTime)
                    : playerTargetPosition;

                playerRect.anchoredPosition = playerPosition;

                // Rotate rover to face travel direction
                Vector2 moveDir = playerPosition - prevPos;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    float targetAngle  = Mathf.Atan2(moveDir.x, moveDir.y) * Mathf.Rad2Deg;
                    float currentAngle = playerRect.localEulerAngles.z;
                    float delta        = Mathf.DeltaAngle(currentAngle, -targetAngle);
                    float step         = roverRotationSpeed * Time.deltaTime;
                    float newAngle     = currentAngle + Mathf.Clamp(delta, -step, step);
                    playerRect.localEulerAngles = new Vector3(0f, 0f, newAngle);
                }
            }
            else if (isMoving)
            {
                isMoving = false;
                HideDestinationMarker();
            }
        }

        // ─── Destination Marker ───────────────────────────────────────────────────

        private void ShowDestinationMarker(Vector2 localPosInPlayableArea)
        {
            if (!destinationMarker) return;
            destinationMarker.anchoredPosition = localPosInPlayableArea;
            destinationMarker.gameObject.SetActive(true);
        }

        private void HideDestinationMarker()
        {
            if (destinationMarker)
                destinationMarker.gameObject.SetActive(false);
        }

        // ─── Session End ─────────────────────────────────────────────────────────

        protected virtual void EndSession()
        {
            if (sessionEnding) return;

            sessionEnding = true;
            inputEnabled  = false;
            HideDestinationMarker();

            Debug.Log($"[KiqqiRoverReflexManager] Session ended. Score: {sessionScore}");

            CompleteMiniGame(sessionScore, sessionScore > 0);
        }

        public void NotifyTimeUp()
        {
            if (sessionEnding) return;
            sessionEnding = true;
        }

        public void ResumeFromPause(KiqqiRoverReflexView v)
        {
            view       = v ?? view;
            isActive   = true;
            isComplete = false;

            if (view && levelMgr)
            {
                float mult = currentStreak >= levelMgr.GetStreakThreshold(currentLevel)
                    ? levelMgr.GetStreakMultiplier(currentLevel) : 1f;
                view.UpdateStreakDisplay(currentStreak, mult);
            }

            Debug.Log("[KiqqiRoverReflexManager] Resumed from pause.");
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
            inputEnabled  = false;
            isMoving      = false;

            foreach (var zone in activeZones) zone.Reset();
            activeZones.Clear();
            foreach (var zone in zonePool) zone.Reset();

            if (playerRect)
            {
                playerRect.anchoredPosition = Vector2.zero;
                playerRect.localEulerAngles = Vector3.zero;
                playerPosition              = Vector2.zero;
                playerTargetPosition        = Vector2.zero;
            }

            HideDestinationMarker();
            Debug.Log("[KiqqiRoverReflexManager] Reset complete.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            if (sessionRoutine != null)
            {
                StopCoroutine(sessionRoutine);
                sessionRoutine = null;
            }

            inputEnabled  = false;
            sessionEnding = false;
            isActive      = false;
            isComplete    = true;

            foreach (var zone in activeZones) zone.Reset();
            activeZones.Clear();

            if (playerObject) playerObject.SetActive(false);
            HideDestinationMarker();

            Debug.Log("[KiqqiRoverReflexManager] OnMiniGameExit - cleaned up.");
        }
    }
}
