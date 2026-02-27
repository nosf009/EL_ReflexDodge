using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Core game state controller.
    /// Handles lifecycle, score, and mini-game / tutorial activation.
    /// </summary>
    public class KiqqiGameManager : MonoBehaviour
    {
        public enum GameState
        {
            None,
            MainMenu,
            LevelSelect,
            Playing,
            Paused,
            Results,
            Tutorial
        }

        [Header("Flags")]
        public bool RestartRequested;
        public bool ResumeRequested;

        private bool _isTransitioning = false;
        public bool IsTransitioning => _isTransitioning;

        [Header("Runtime State (debug)")]
        public GameState State = GameState.None;
        public int CurrentScore;

        private KiqqiAppManager app;

        [Header("Mini-Game References")]
        [Tooltip("Main gameplay manager for this scene.")]
        public KiqqiMiniGameManagerBase mainGameManager;

        [Tooltip("Tutorial manager for this scene.")]
        public KiqqiMiniGameManagerBase tutorialGameManager;

        [HideInInspector] public KiqqiMiniGameManagerBase currentMiniGame;

        // ----------------------------------------------------
        // Initialization
        // ----------------------------------------------------

        public void Initialize(KiqqiAppManager appContext)
        {
            app = appContext;
            if (app == null)
                app = KiqqiAppManager.Instance;
            State = GameState.MainMenu;
            CurrentScore = 0;

            Debug.Log("[KiqqiGameManager] Initialized (explicit refs).");
        }

        // ----------------------------------------------------
        // Mini-Game Control
        // ----------------------------------------------------

        /// <summary>
        /// Starts the main gameplay mini-game.
        /// </summary>
        public void StartMainGame()
        {
            if (mainGameManager == null)
            {
                Debug.LogWarning("[KiqqiGameManager] MainGameManager not assigned!");
                return;
            }
            _alreadyEnded = false;
            StartMiniGame(mainGameManager);
        }

        private bool tutorialAlreadyStarted = false;

        /// <summary>
        /// Starts the tutorial mini-game.
        /// </summary>
        public void StartTutorial()
        {
            if (tutorialAlreadyStarted)
            {
                Debug.Log("[KiqqiGameManager] Tutorial start called, skip...");
                return;
            }
            tutorialAlreadyStarted = true;

            if (tutorialGameManager == null)
            {
                Debug.LogWarning("[KiqqiGameManager] TutorialGameManager not assigned!");
                return;
            }

            Debug.Log("[KiqqiGameManager] Starting tutorial...");

            tutorialGameManager.ResetMiniGame();
            _alreadyEnded = false;

            ForceHideAll(app.UI);
            State = GameState.Tutorial;
            StartMiniGame(tutorialGameManager);
        }

        private void ForceHideAll(KiqqiUIManager ui)
        {
            foreach (var v in ui.GetAllRegisteredViews())
            {
                if (v.gameObject.activeSelf)
                {
                    v.HideWithDeactivate(true);
                    v.gameObject.SetActive(false);
                }
            }
        }



        /// <summary>
        /// Centralized launch routine for any mini-game manager.
        /// </summary>
        public void StartMiniGame(KiqqiMiniGameManagerBase target)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            if (target == null)
            {
                Debug.LogWarning("[KiqqiGameManager] StartMiniGame called with null target!");
                _isTransitioning = false;
                return;
            }

            Debug.Log($"[KiqqiGameManager] Starting mini-game: {target.displayName}");

            target.Initialize(KiqqiAppManager.Instance);
            currentMiniGame = target;

            app.UI.ShowView(target.GetAssociatedViewType());
            _isTransitioning = false;
        }

        // ----------------------------------------------------
        // Core Gameplay
        // ----------------------------------------------------

        public void StartGame()
        {
            CurrentScore = 0;
            State = GameState.Playing;
            Debug.Log("[KiqqiGameManager] Game started.");
        }

        public void AddScore(int amount)
        {
            CurrentScore += amount;
            //Debug.Log($"[KiqqiGameManager] Score updated: {CurrentScore}");
        }


        private bool _alreadyEnded = false;
        public void EndGame(bool playerWon = false)
        {

            if (_alreadyEnded)
            {
                Debug.LogWarning("[KiqqiGameManager] EndGame called again — ignored.");
                return;
            }
            _alreadyEnded = true;

            Debug.Log($"[GameManager] EndGame() called. playerWon={playerWon}, CurrentScore={CurrentScore}");

            State = GameState.Results;
            Debug.Log($"[KiqqiGameManager] Game ended. Final score: {CurrentScore}");

            if (playerWon)
                app.Levels.RegisterWin();

            app.Scoring?.PostScore(CurrentScore);
        }

        public void ReturnToMenu()
        {
            if (currentMiniGame != null)
            {
                currentMiniGame.OnMiniGameExit();
            }
            ResetSession();
            State = GameState.MainMenu;
            Debug.Log("[KiqqiGameManager] Returned to Main Menu.");
        }

        public void ResetSession()
        {
            CurrentScore = 0;
            _alreadyEnded = false;
            RestartRequested = false;
            ResumeRequested = false;
            currentMiniGame = null;
            tutorialAlreadyStarted = false;
            State = GameState.None;
        }

    }
}
