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

        // Drop shadow (spawned at impact position during meteor fall)
        public GameObject meteorDropShadow;
        public Image      meteorDropShadowImage;

        public void Reset()
        {
            if (gameObject) gameObject.SetActive(false);
            if (meteorDropShadow) { UnityEngine.Object.Destroy(meteorDropShadow); meteorDropShadow = null; meteorDropShadowImage = null; }
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
        // ─── Tutorial Mode ───────────────────────────────────────────────────────

        private const string TutorialShownKey = "roverreflex_tutorial_shown_once";

        [Header("Tutorial Mode")]
        [Tooltip("When true this manager instance runs tutorial flow instead of normal gameplay.")]
        public bool isTutorialMode;

        [Tooltip("Auto-start tutorial on first-ever launch via PlayerPrefs flag.")]
        public bool tutAutoStartOnFirstRun = true;

        [Tooltip("Min anchored distance from player the first tutorial meteor must land.")]
        public float tutMeteorMinDist = 220f;

        [Tooltip("Min anchored distance from player the second (hint) meteor must land.")]
        public float tutSecondMeteorMinDist = 180f;

        [Tooltip("Seconds after the second impact before ending the tutorial.")]
        public float tutEndDelay = 2.5f;

        [Tooltip("Canvas-unit Y offset applied to the hand icon below the ore spawn.")]
        public float tutHandIconOffsetY = -70f;

        [Tooltip("Minimum canvas-unit clearance from the bottom edge of the playable area for tutorial meteor landing positions (keeps them above the instruction overlay).")]
        public float tutMeteorBottomMargin = 300f;

        [Tooltip("Maximum danger zone radius for tutorial meteors (caps the normal level-driven radius so the tutorial zone never overlaps the rover by accident).")]
        public float tutMeteorMaxRadius = 90f;

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

        [Header("Meteor Drop Shadow")]
        [Tooltip("Circular shadow Image GO - keep inactive in scene as template")]
        public GameObject meteorDropShadowTemplate;

        [Tooltip("Parent for spawned drop shadows (defaults to zonePoolParent's parent if not set)")]
        public Transform meteorDropShadowParent;

        [Tooltip("Shadow size (canvas units diameter) when meteor is at maximum height (t=0)")]
        public float meteorDropShadowSizeMax = 180f;

        [Tooltip("Shadow size (canvas units diameter) when meteor is about to impact (t=1)")]
        public float meteorDropShadowSizeMin = 60f;

        [Tooltip("Shadow alpha when meteor is at maximum height (t=0) - dim and wide")]
        public float meteorDropShadowAlphaStart = 0.15f;

        [Tooltip("Shadow alpha just before impact (t=1) - tight and dark")]
        public float meteorDropShadowAlphaEnd = 0.65f;

        [Header("Mineral Pickups")]
        [Tooltip("MineralPickup GO kept inactive in scene as template")]
        public GameObject mineralPickupTemplate;

        [Tooltip("Parent for spawned minerals — leave empty to use template's own parent")]
        public Transform mineralPickupParent;

        [Tooltip("Sprite variants for minerals — one will be chosen at random per drop")]
        public Sprite[] mineralSprites;

        [Tooltip("Pickup detection radius in world-space screen pixels")]
        public float mineralPickupRadius = 60f;

        [Tooltip("Randomise mineral Z rotation on spawn")]
        public bool randomizeMineralRotation = false;

        [Header("Screen Shake")]
        [Tooltip("Enable/disable screen shake on every meteor impact")]
        public bool enableScreenShake = true;

        [Tooltip("RectTransform to shake — assign rrRoverReflexGameView")]
        public RectTransform gameViewRect;

        [Tooltip("Total shake duration (seconds)")]
        public float shakeDuration = 0.22f;

        [Tooltip("Peak shake magnitude in canvas units")]
        public float shakeMagnitude = 11f;

        [Tooltip("How fast the shake decays — higher = snappier falloff")]
        public float shakeDamping = 9f;

        [Header("Skidmarks")]
        [Tooltip("UI Image prefab for a single skidmark segment - keep inactive in scene")]
        public GameObject skidmarkPrefab;

        [Tooltip("Parent transform for spawned skidmark GOs (defaults to playableArea if not set)")]
        public Transform skidmarkParent;

        [Tooltip("Names of wheel child transforms on the Player GO (emission points)")]
        public string[] wheelTransformNames = new string[] { "WheelLeft", "WheelRight" };

        [Tooltip("Minimum distance the rover must travel between skidmark spawns")]
        public float skidmarkSpawnDistance = 20f;

        [Tooltip("Seconds the mark stays fully visible before fading")]
        public float skidmarkFadeDelay = 0.4f;

        [Tooltip("Seconds the fade-out takes")]
        public float skidmarkFadeDuration = 0.6f;

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

        // Skidmark state
        private Vector2 _lastSkidmarkPosition;
        private RectTransform[] _wheelRects;

        // Mineral pickup state
        private readonly List<GameObject> _activeMinerals = new List<GameObject>();
        private Coroutine _roverPulseRoutine;
        private Coroutine _shakeRoutine;

        // Tracks all live impact particle instances so we can clean them up between rounds
        private readonly List<GameObject> _liveImpactParticles = new List<GameObject>();

        // ─── Tutorial Runtime State ──────────────────────────────────────────────

        private bool _tutorialActive;
        private bool _firstMineralPickedUp;
        private bool _tutorialEnding;
        private bool _tutForceNextMineralDrop;
        private bool _tutSuppressNextMineralDrop;

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

            if (isTutorialMode && view != null)
            {
                view.IsTutorialMode = true;

                // Wire skip button once
                if (view.tutSkipBtn != null)
                {
                    var btn = view.tutSkipBtn.GetComponent<UnityEngine.UI.Button>();
                    if (btn)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(OnSkipPressed);
                    }
                }
            }

            Debug.Log($"[KiqqiRoverReflexManager] Initialized (tutorialMode={isTutorialMode}).");
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

            //Debug.Log($"[KiqqiRoverReflexManager] Zone pool: {zonePool.Count} zones.");
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

            // Always reset to center and hide until gameplay begins
            if (playerRect)
            {
                playerRect.anchoredPosition = Vector2.zero;
                playerRect.localEulerAngles = Vector3.zero;
            }
            playerPosition       = Vector2.zero;
            playerTargetPosition = Vector2.zero;

            playerObject.SetActive(false);

            // Cache wheel emission point RectTransforms
            if (wheelTransformNames != null && wheelTransformNames.Length > 0)
            {
                var found = new List<RectTransform>();
                foreach (string wName in wheelTransformNames)
                {
                    Transform t = playerObject.transform.Find(wName);
                    if (t)
                    {
                        RectTransform rt = t.GetComponent<RectTransform>();
                        if (rt) found.Add(rt);
                    }
                    else
                    {
                        Debug.LogWarning($"[KiqqiRoverReflexManager] Wheel transform '{wName}' not found on Player.");
                    }
                }
                _wheelRects = found.ToArray();
            }

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

            if (isTutorialMode)
            {
                _tutorialActive       = true;
                _firstMineralPickedUp = false;
                _tutorialEnding       = false;
                if (app?.Levels != null) app.Levels.currentLevel = 1;
                masterGame.State = KiqqiGameManager.GameState.Tutorial;
                MarkTutorialShown();
            }

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

            _lastSkidmarkPosition = Vector2.zero;
            ClearAllMinerals();

            if (playerObject) playerObject.SetActive(true);

            if (destinationMarker) destinationMarker.gameObject.SetActive(false);

            if (view) view.UpdateStreakDisplay(0, 1f);

            sessionRoutine = StartCoroutine(RunSession());

            //Debug.Log($"[KiqqiRoverReflexManager] Session started - Level {currentLevel}, tutorial={isTutorialMode}");
        }

        protected virtual void LoadLevelSettings()
        {
            if (!levelMgr) return;

            spawnInterval   = levelMgr.GetSpawnInterval(currentLevel);
            maxZones        = levelMgr.GetMaxZones(currentLevel);
            warningDuration = levelMgr.GetWarningDuration(currentLevel);
            zoneRadiusRange = levelMgr.GetZoneRadiusRange(currentLevel);

            //Debug.Log($"[KiqqiRoverReflexManager] Level {currentLevel}: interval={spawnInterval}s, maxZones={maxZones}, warning={warningDuration}s");
        }

        // ─── Session Loop ────────────────────────────────────────────────────────

        protected virtual IEnumerator RunSession()
        {
            if (isTutorialMode)
            {
                yield return StartCoroutine(RunTutorialSession());
                yield break;
            }

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
                UpdateMineralPickups();

                yield return null;
            }

            yield return new WaitForSeconds(0.3f);

            foreach (var zone in activeZones) zone.Reset();
            activeZones.Clear();

            EndSession();
        }

        // ─── Tutorial Session ────────────────────────────────────────────────────

        private IEnumerator RunTutorialSession()
        {
            yield return new WaitForSeconds(0.5f);
            inputEnabled = true;

            // ── 1. Spawn first meteor with guaranteed mineral drop ─────────────────
            _tutForceNextMineralDrop    = true;
            _tutSuppressNextMineralDrop = false;
            DangerZone firstZone = SpawnTutorialMeteor(tutMeteorMinDist);

            if (firstZone == null)
            {
                Debug.LogError("[KiqqiRoverReflexManager] Tutorial: could not spawn first meteor.");
                EndTutorial();
                yield break;
            }

            while (!firstZone.meteorArrived && firstZone.isActive && _tutorialActive)
            {
                UpdatePlayerMovement();
                UpdateActiveZones();
                UpdateMineralPickups();
                yield return null;
            }

            yield return new WaitForSeconds(0.15f);
            if (!_tutorialActive) yield break;

            // ── 2. Show step-1 overlay + hand icon, gameplay continues freely ─────
            if (view != null)
            {
                Vector2 orePos = firstZone.rectTransform != null
                    ? firstZone.rectTransform.anchoredPosition
                    : Vector2.zero;
                view.ShowHandIcon(orePos + new Vector2(0f, tutHandIconOffsetY));
                view.ShowTutorialStep1();
            }

            // ── 3. Wait for mineral pickup — game runs normally ────────────────────
            while (!_firstMineralPickedUp && _tutorialActive)
            {
                UpdatePlayerMovement();
                UpdateActiveZones();
                UpdateMineralPickups();
                yield return null;
            }

            if (!_tutorialActive) yield break;

            // ── 4. Show step-2 overlay ────────────────────────────────────────────
            if (view != null)
            {
                view.HideHandIcon();
                view.ShowTutorialStep2();
            }

            if (!_tutorialActive) yield break;

            // ── 5. Spawn second meteor – no mineral, hint only ────────────────────
            _tutForceNextMineralDrop    = false;
            _tutSuppressNextMineralDrop = true;
            SpawnTutorialMeteor(tutSecondMeteorMinDist);

            // ── 6. Keep game running for tutEndDelay then end ─────────────────────
            // We don't gate on meteorArrived — we simply let it play out and end
            // after a fixed window. This avoids any zone-state edge cases.
            float waited = 0f;
            while (waited < tutEndDelay && _tutorialActive)
            {
                waited += Time.deltaTime;
                UpdatePlayerMovement();
                UpdateActiveZones();
                UpdateMineralPickups();
                yield return null;
            }

            if (!_tutorialActive) yield break;
            EndTutorial();
        }

        private DangerZone SpawnTutorialMeteor(float minDistFromPlayer)
        {
            if (!playableArea || !levelMgr) return null;

            DangerZone zone = GetAvailableZone();
            if (zone == null) return null;

            Rect    bounds = playableArea.rect;
            Vector2 pos    = Vector2.zero;

            for (int i = 0; i < 30; i++)
            {
                pos = new Vector2(
                    Random.Range(bounds.xMin + 100f, bounds.xMax - 100f),
                    Random.Range(bounds.yMin + tutMeteorBottomMargin, bounds.yMax - 100f));
                if (Vector2.Distance(pos, playerPosition) >= minDistFromPlayer) break;
            }

            // Cap radius for tutorial so the danger zone edge never reaches the rover
            float radius = Mathf.Min(
                Random.Range(zoneRadiusRange.x, zoneRadiusRange.y),
                tutMeteorMaxRadius);

            zone.rectTransform.anchoredPosition = pos;

            if (zone.shadowRect) { zone.shadowRect.anchoredPosition = Vector2.zero; zone.shadowRect.sizeDelta = Vector2.one * radius * 2f; }
            if (zone.shadowImage) { Color c = zoneWarningColor; c.a = 0f; zone.shadowImage.color = c; }
            if (zone.meteorRect)
            {
                zone.meteorRect.anchoredPosition = Vector2.zero;
                zone.meteorRect.sizeDelta        = meteorSize;
                zone.meteorRect.localScale       = new Vector3(0.1f, 0.1f, 1f);
                zone.meteorRect.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
                zone.meteorRect.gameObject.SetActive(false);
            }

            zone.meteorFlyDuration = warningDuration;
            zone.meteorElapsed     = 0f;
            zone.meteorArrived     = false;
            zone.isFlashing        = true;
            zone.flashElapsed      = 0f;
            zone.impactElapsed     = 0f;
            zone.radius            = radius;
            zone.warningDuration   = warningDuration;
            zone.elapsedTime       = 0f;
            zone.isActive          = true;
            zone.isDangerous       = false;
            zone.behavior          = DangerZoneBehavior.Static;

            zone.gameObject.SetActive(true);
            zone.rectTransform.SetAsLastSibling();
            activeZones.Add(zone);

            return zone;
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

            //Debug.Log($"[KiqqiRoverReflexManager] Spawned '{zone.gameObject.name}' at {shadowPos}, r={radius}, behavior={zone.behavior}");
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

                    // Flash complete → hide shadow, show meteor, spawn drop shadow
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

                        SpawnMeteorDropShadow(zone);
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

                    UpdateMeteorDropShadow(zone, t);

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
            if (zone.isDangerous) return;
            zone.isDangerous = true;

            if (zone.meteorRect)
            {
                zone.meteorRect.localScale = Vector3.one;
                zone.meteorRect.gameObject.SetActive(false);
            }

            if (zone.meteorDropShadow) { Destroy(zone.meteorDropShadow); zone.meteorDropShadow = null; zone.meteorDropShadowImage = null; }

            PlayImpactParticle(zone.rectTransform.anchoredPosition, zone.radius);

            if (zone.shadowImage)
                zone.shadowImage.color = zoneDangerousColor;

            bool roverWasHit = Vector2.Distance(playerPosition, zone.rectTransform.anchoredPosition)
                               <= zone.radius + playerRadius;

            // Always attempt mineral spawn in tutorial regardless of hit,
            // so the pickup-wait step can never get stuck.
            if (isTutorialMode)
                TrySpawnTutorialMineral(zone);
            else if (!roverWasHit)
                TrySpawnMineral(zone);

            TriggerScreenShake(zone.radius);
            CheckPlayerHit(zone);
        }

        /// <summary>Tutorial mineral spawn: obeys _tutForceNextMineralDrop / _tutSuppressNextMineralDrop flags.</summary>
        private void TrySpawnTutorialMineral(DangerZone zone)
        {
            if (!mineralPickupTemplate || mineralSprites == null || mineralSprites.Length == 0) return;
            if (_tutSuppressNextMineralDrop) { _tutSuppressNextMineralDrop = false; return; }

            bool drop = _tutForceNextMineralDrop
                || (levelMgr != null && Random.value <= levelMgr.GetMineralDropChance(currentLevel));

            _tutForceNextMineralDrop = false;

            if (!drop) return;
            SpawnMineralAt(zone);
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
            KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
            PulseRoverColor(Color.red);

            if (isTutorialMode)
            {
                // Tutorial: NOK feedback only — no penalty, no streak reset
                Debug.Log("[KiqqiRoverReflexManager] Tutorial hit – no penalty.");
                return;
            }

            if (!levelMgr) return;

            int penalty   = levelMgr.GetHitPenalty(currentLevel);
            sessionScore  = Mathf.Max(0, sessionScore - penalty);
            currentStreak = 0;

            if (view)
            {
                view.UpdateStreakDisplay(currentStreak, 1f);
                RefreshScore();
            }

            //Debug.Log($"[KiqqiRoverReflexManager] HIT! Penalty -{penalty}, score={sessionScore}");
        }

        protected virtual void OnSuccessfulDodge(DangerZone zone)
        {
            currentStreak++;
            if (view) view.UpdateStreakDisplay(currentStreak, 1f);
            //Debug.Log($"[KiqqiRoverReflexManager] Dodge (streak:{currentStreak})");
        }

        // ─── Meteor Drop Shadow ──────────────────────────────────────────────────

        private void SpawnMeteorDropShadow(DangerZone zone)
        {
            if (!meteorDropShadowTemplate) return;

            Transform parent = meteorDropShadowParent
                ? meteorDropShadowParent
                : meteorDropShadowTemplate.transform.parent;

            GameObject shadow = Instantiate(meteorDropShadowTemplate, parent);
            RectTransform rt  = shadow.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchoredPosition = zone.rectTransform.anchoredPosition;
                float size = meteorDropShadowSizeMax;
                rt.sizeDelta = new Vector2(size, size);
            }

            Image img = shadow.GetComponent<Image>();
            if (img)
            {
                Color c = img.color;
                c.a     = meteorDropShadowAlphaStart;
                img.color = c;
            }

            shadow.SetActive(true);

            zone.meteorDropShadow      = shadow;
            zone.meteorDropShadowImage = img;
        }

        /// <summary>Called every frame during meteor fall. t is 0 (just spawned) → 1 (impact).</summary>
        private void UpdateMeteorDropShadow(DangerZone zone, float t)
        {
            if (!zone.meteorDropShadow || !zone.meteorDropShadowImage) return;

            float size  = Mathf.Lerp(meteorDropShadowSizeMax, meteorDropShadowSizeMin, t);
            float alpha = Mathf.Lerp(meteorDropShadowAlphaStart, meteorDropShadowAlphaEnd, t);

            RectTransform rt = zone.meteorDropShadow.GetComponent<RectTransform>();
            if (rt) rt.sizeDelta = new Vector2(size, size);

            Color c = zone.meteorDropShadowImage.color;
            c.a = alpha;
            zone.meteorDropShadowImage.color = c;
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
            _liveImpactParticles.Add(instance);
            Destroy(instance, destroyDelay);
        }

        private void ClearAllImpactParticles()
        {
            foreach (GameObject p in _liveImpactParticles)
                if (p) Destroy(p);
            _liveImpactParticles.Clear();
        }

        // ─── Mineral Pickups ─────────────────────────────────────────────────────

        /// <summary>Chance-based mineral drop at the meteor impact position.</summary>
        private void TrySpawnMineral(DangerZone zone)
        {
            if (!mineralPickupTemplate || mineralSprites == null || mineralSprites.Length == 0) return;
            if (!levelMgr) return;
            if (Random.value > levelMgr.GetMineralDropChance(currentLevel)) return;
            SpawnMineralAt(zone);
        }

        private void SpawnMineralAt(DangerZone zone)
        {
            if (!mineralPickupTemplate) return;

            Transform parent = mineralPickupParent
                ? mineralPickupParent
                : mineralPickupTemplate.transform.parent;

            GameObject mineral = Instantiate(mineralPickupTemplate, parent);

            RectTransform rt = mineral.GetComponent<RectTransform>();
            if (rt)
            {
                RectTransform parentRt = parent.GetComponent<RectTransform>();
                if (parentRt != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentRt, zone.rectTransform.position, null, out Vector2 localPos);
                    rt.anchoredPosition = localPos;
                }
                rt.localEulerAngles = randomizeMineralRotation
                    ? new Vector3(0f, 0f, Random.Range(0f, 360f))
                    : Vector3.zero;
            }

            Image img = mineral.GetComponent<Image>();
            if (img) img.sprite = mineralSprites[Random.Range(0, mineralSprites.Length)];

            mineral.SetActive(true);
            _activeMinerals.Add(mineral);
        }

        /// <summary>Checks rover proximity to each active mineral every frame.</summary>
        private void UpdateMineralPickups()
        {
            if (_activeMinerals.Count == 0 || !playerRect) return;

            Vector2 roverWorldPos = playerRect.position;

            for (int i = _activeMinerals.Count - 1; i >= 0; i--)
            {
                GameObject mineral = _activeMinerals[i];
                if (!mineral) { _activeMinerals.RemoveAt(i); continue; }

                RectTransform mineralRt = mineral.GetComponent<RectTransform>();
                if (!mineralRt) continue;

                float dist = Vector2.Distance(roverWorldPos, (Vector2)mineralRt.position);
                if (dist <= mineralPickupRadius)
                {
                    _activeMinerals.RemoveAt(i);
                    OnMineralPickup(mineral);
                }
            }
        }

        protected virtual void OnMineralPickup(GameObject mineral)
        {
            Destroy(mineral);

            KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect");
            PulseRoverColor(Color.green);

            if (isTutorialMode)
            {
                _firstMineralPickedUp = true;
                //Debug.Log("[KiqqiRoverReflexManager] Tutorial mineral pickup – unfreezing.");
                return;
            }

            if (!levelMgr) return;

            int earned    = levelMgr.GetPickupScore(currentLevel);
            sessionScore += earned;
            currentStreak++;

            if (view)
            {
                view.UpdateStreakDisplay(currentStreak,
                    currentStreak >= levelMgr.GetStreakThreshold(currentLevel)
                        ? levelMgr.GetStreakMultiplier(currentLevel) : 1f);
                RefreshScore();
            }

            //Debug.Log($"[KiqqiRoverReflexManager] Mineral pickup +{earned}, score={sessionScore}");
        }

        private void ClearAllMinerals()
        {
            foreach (GameObject m in _activeMinerals)
                if (m) Destroy(m);
            _activeMinerals.Clear();
        }

        /// <summary>Pushes sessionScore into the shared GameManager slot then refreshes the HUD label.</summary>
        private void RefreshScore()
        {
            KiqqiAppManager.Instance.Game.CurrentScore = sessionScore;
            if (view) view.RefreshScoreUI();
        }

        // ─── Screen Shake ────────────────────────────────────────────────────────

        private void TriggerScreenShake(float zoneRadius)
        {
            if (!enableScreenShake || !gameViewRect) return;

            Vector2 radiusRange = levelMgr
                ? levelMgr.GetZoneRadiusRange(currentLevel)
                : new Vector2(zoneRadius, zoneRadius);

            float t              = Mathf.InverseLerp(radiusRange.x, radiusRange.y, zoneRadius);
            float scaledMagnitude = Mathf.Lerp(shakeMagnitude, shakeMagnitude * 2f, t);

            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ScreenShakeRoutine(scaledMagnitude));
        }

        private IEnumerator ScreenShakeRoutine(float magnitude)
        {
            Vector2 origin = gameViewRect.anchoredPosition;
            float elapsed  = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float progress     = elapsed / shakeDuration;
                float currentMag   = magnitude * (1f - Mathf.Pow(progress, 1f / shakeDamping));
                gameViewRect.anchoredPosition = origin + Random.insideUnitCircle * currentMag;
                yield return null;
            }

            gameViewRect.anchoredPosition = origin;
            _shakeRoutine = null;
        }

        // ─── Rover Color Pulse ───────────────────────────────────────────────────

        private void PulseRoverColor(Color pulseColor)
        {
            if (_roverPulseRoutine != null) StopCoroutine(_roverPulseRoutine);
            _roverPulseRoutine = StartCoroutine(RoverColorPulseRoutine(pulseColor));
        }

        private IEnumerator RoverColorPulseRoutine(Color pulseColor)
        {
            if (!playerImage) yield break;

            const float halfDuration = 0.12f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                playerImage.color = Color.Lerp(Color.white, pulseColor, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                playerImage.color = Color.Lerp(pulseColor, Color.white, elapsed / halfDuration);
                yield return null;
            }

            playerImage.color = Color.white;
            _roverPulseRoutine = null;
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

                    TrySpawnSkidmarks(newAngle);
                }
            }
            else if (isMoving)
            {
                isMoving = false;
                HideDestinationMarker();
            }
        }

        // ─── Skidmarks ───────────────────────────────────────────────────────────

        /// <summary>Spawns a mark at each wheel transform when the rover has moved far enough.</summary>
        private void TrySpawnSkidmarks(float roverAngleDeg)
        {
            if (!skidmarkPrefab || _wheelRects == null || _wheelRects.Length == 0) return;

            float distanceSinceLast = Vector2.Distance(playerPosition, _lastSkidmarkPosition);
            if (distanceSinceLast < skidmarkSpawnDistance) return;

            _lastSkidmarkPosition = playerPosition;

            foreach (RectTransform wheel in _wheelRects)
            {
                if (!wheel) continue;

                // Convert the wheel's world position into playableArea local (anchored) space
                Vector2 worldPos = wheel.position;
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    playableArea, worldPos, null, out localPos);

                SpawnSkidmark(localPos, roverAngleDeg);
            }
        }

        private void SpawnSkidmark(Vector2 anchoredPos, float angleDeg)
        {
            Transform parent = skidmarkParent ? skidmarkParent : playableArea;
            GameObject mark  = Instantiate(skidmarkPrefab, parent);

            RectTransform rt = mark.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchoredPosition = anchoredPos;
                rt.localEulerAngles = new Vector3(0f, 0f, angleDeg);
            }

            mark.SetActive(true);
            StartCoroutine(FadeOutSkidmark(mark));
        }

        private IEnumerator FadeOutSkidmark(GameObject mark)
        {
            yield return new WaitForSeconds(skidmarkFadeDelay);

            if (mark == null) yield break;

            CanvasGroup cg = mark.GetComponent<CanvasGroup>();
            if (!cg) cg = mark.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < skidmarkFadeDuration)
            {
                if (mark == null) yield break;
                elapsed    += Time.deltaTime;
                cg.alpha    = Mathf.Lerp(1f, 0f, elapsed / skidmarkFadeDuration);
                yield return null;
            }

            if (mark != null) Destroy(mark);
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
            if (isComplete) return;

            sessionEnding = true;
            inputEnabled  = false;
            HideDestinationMarker();
            ClearAllMinerals();
            ClearAllImpactParticles();

            if (_roverPulseRoutine != null) { StopCoroutine(_roverPulseRoutine); _roverPulseRoutine = null; }
            if (playerImage) playerImage.color = Color.white;

            if (_shakeRoutine != null) { StopCoroutine(_shakeRoutine); _shakeRoutine = null; }
            if (gameViewRect) gameViewRect.anchoredPosition = Vector2.zero;

            if (playerObject) playerObject.SetActive(false);

            //Debug.Log($"[KiqqiRoverReflexManager] Session ended. Score: {sessionScore}");

            CompleteMiniGame(sessionScore, sessionScore > 0);
        }

        public void NotifyTimeUp()
        {
            if (isTutorialMode) return; // no timer end in tutorial
            if (sessionEnding) return;
            sessionEnding = true;
        }

        // ─── Tutorial End ─────────────────────────────────────────────────────────

        /// <summary>Auto-start support: returns true if tutorial should launch on this session.</summary>
        public bool ShouldAutoStartTutorial()
        {
            if (!tutAutoStartOnFirstRun) return false;
            return PlayerPrefs.GetInt(TutorialShownKey, 0) == 0;
        }

        /// <summary>Marks tutorial as done — won't auto-start again.</summary>
        public void MarkTutorialShown()
        {
            PlayerPrefs.SetInt(TutorialShownKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Resets shown flag for testing.</summary>
        public void ResetTutorialFlagForTesting()
        {
            PlayerPrefs.DeleteKey(TutorialShownKey);
        }

        private void OnSkipPressed()
        {
            if (_tutorialEnding) return;
            //Debug.Log("[KiqqiRoverReflexManager] Tutorial skipped.");
            EndTutorial();
        }

        /// <summary>Cleans up tutorial state and transitions to KiqqiTutorialEndView.</summary>
        public void EndTutorial()
        {
            if (_tutorialEnding) return;
            _tutorialEnding = true;
            _tutorialActive = false;

            Time.timeScale = 1f;

            if (sessionRoutine != null) { StopCoroutine(sessionRoutine); sessionRoutine = null; }

            sessionEnding = true;
            inputEnabled  = false;

            HideDestinationMarker();
            ClearAllMinerals();
            ClearAllImpactParticles();

            if (_roverPulseRoutine != null) { StopCoroutine(_roverPulseRoutine); _roverPulseRoutine = null; }
            if (playerImage) playerImage.color = Color.white;

            if (_shakeRoutine != null) { StopCoroutine(_shakeRoutine); _shakeRoutine = null; }
            if (gameViewRect) gameViewRect.anchoredPosition = Vector2.zero;

            foreach (var zone in activeZones) zone.Reset();
            activeZones.Clear();

            if (playerObject) playerObject.SetActive(false);

            if (view != null) view.ResetTutorialUI();

            isComplete = true;
            isActive   = false;

            StartCoroutine(ShowTutorialEndDelayed());
        }

        private System.Collections.IEnumerator ShowTutorialEndDelayed()
        {
            yield return new WaitForEndOfFrame();

            // Reset mode flag before transitioning so normal play is clean afterwards
            isTutorialMode = false;

            // Let UIManager drive the transition — it will hide the active view and show the end view
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
            //Debug.Log("[KiqqiRoverReflexManager] Tutorial ended – showing KiqqiTutorialEndView.");
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

            Time.timeScale    = 1f;
            _tutorialActive   = false;
            _tutorialEnding   = false;
            _firstMineralPickedUp = false;

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
            ClearAllMinerals();
            ClearAllImpactParticles();

            if (view != null) view.ResetTutorialUI();

            //Debug.Log("[KiqqiRoverReflexManager] Reset complete.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            Time.timeScale  = 1f;
            _tutorialActive = false;
            _tutorialEnding = true;

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
            ClearAllMinerals();
            ClearAllImpactParticles();

            if (view != null) view.ResetTutorialUI();

            //Debug.Log("[KiqqiRoverReflexManager] OnMiniGameExit – cleaned up.");
        }
    }
}
