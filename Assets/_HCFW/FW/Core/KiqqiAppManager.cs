using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Kiqqi.Localization;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Root bootstrap and lifetime controller for Kiqqi-based Eleverse templates.
    /// Initializes and connects all core managers.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class KiqqiAppManager : MonoBehaviour
    {
        public static KiqqiAppManager Instance { get; private set; }

        [Header("Scene References (optional)")]
        [SerializeField] private KiqqiUIManager uiManager;
        [SerializeField] private KiqqiGameManager gameManager;
        [SerializeField] private KiqqiLevelManager levelManager;
        [SerializeField] private KiqqiAudioManager audioManager;
        [SerializeField] private KiqqiDataManager dataManager;
        [SerializeField] private KiqqiScoringApi scoringApi;
        [SerializeField] private KiqqiInputController inputController;

        public KiqqiUIManager UI => uiManager;
        public KiqqiGameManager Game => gameManager;
        public KiqqiLevelManager Levels => levelManager;
        public KiqqiAudioManager Audio => audioManager;
        public KiqqiDataManager Data => dataManager;
        public KiqqiScoringApi Scoring => scoringApi;
        public KiqqiInputController Input => inputController;

        private async void Awake()
        {
            Application.targetFrameRate = 60;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // --- Validate core managers ---
            if (!uiManager || !gameManager || !levelManager || !audioManager || !dataManager)
            {
                Debug.LogError("[KiqqiAppManager] Missing one or more manager references in inspector.");
                return;
            }

            // --- Localization (async safe) ---
            await EnsureLocalizationAsync();

            // --- Sequential init order ---
            dataManager.Initialize();
            levelManager.Initialize(dataManager);
            audioManager.Initialize(dataManager);
            gameManager.Initialize(this);
            scoringApi?.Initialize(dataManager);

            uiManager.Initialize(this);

            ApplySharedGameDefinition();

            bool shouldStartTutorial = false;

            // check for any tutorial manager that should auto-start
            if (gameManager != null && gameManager.tutorialGameManager != null)
            {
                if (gameManager.tutorialGameManager is KiqqiPokerFaceTutorialManager pfTut &&
                    pfTut.ShouldAutoStartTutorial())
                {
                    shouldStartTutorial = true;
                    Debug.Log("[KiqqiAppManager] Poker Face tutorial will auto-start (skipping main menu).");
                }
                else if (gameManager.tutorialGameManager is KiqqiTicTacToeTutorialManager ttTut &&
                         ttTut.ShouldAutoStartTutorial())
                {
                    shouldStartTutorial = true;
                    Debug.Log("[KiqqiAppManager] Tic-Tac-Toe tutorial will auto-start (skipping main menu).");
                }
            }

            if (shouldStartTutorial)
            {
                // Defer showing tutorial until all managers finish initializing
                StartCoroutine(ShowTutorialAfterInit());
            }
            else
            {
                // normal main menu path
                uiManager.ShowView<KiqqiMainMenuView>();
            }

        }

        private void ApplySharedGameDefinition()
        {
            if (Data == null || Data.gameDefinition == null)
            {
                Debug.LogWarning("[KiqqiAppManager] No GameDefinition found in DataManager — skipping propagation.");
                return;
            }

            var def = Data.gameDefinition;
            Debug.Log($"[KiqqiAppManager] Applying GameDefinition '{def.gameId}' to systems.");

            // Scoring API
            if (Scoring)
            {
                Scoring.gameId = def.gameId;
                Scoring.apiRoot = def.apiRoot;
            }

            // Game Manager (and all its minigame managers)
            if (Game)
            {
                if (Game.tutorialGameManager is KiqqiMiniGameManagerBase tutMgr)
                {
                    tutMgr.gameID = def.gameId + "_tutorial";
                    tutMgr.displayName = def.displayName + " Tutorial";
                }

                if (Game.currentMiniGame is KiqqiMiniGameManagerBase mainMgr)
                {
                    mainMgr.gameID = def.gameId;
                    mainMgr.displayName = def.displayName;
                }
            }
        }

        private System.Collections.IEnumerator ShowTutorialAfterInit()
        {
            // wait a couple of frames so UI system and localization finish
            yield return null;
            yield return null;

            var gm = Game;
            if (gm != null)
            {
                Debug.Log("[KiqqiAppManager] Auto-launching tutorial directly.");
                gm.StartTutorial(); // this will show the tutorial view as first screen
            }
        }


        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

        }


        // ---------------- INTERNALS ----------------

        private async Task EnsureLocalizationAsync()
        {
            if (!KiqqiLocalizationManager.IsInitialized)
            {
                Debug.Log("[KiqqiAppManager] Initializing localization...");
                await KiqqiLocalizationManager.InitAsync();
            }
        }

    }
}
