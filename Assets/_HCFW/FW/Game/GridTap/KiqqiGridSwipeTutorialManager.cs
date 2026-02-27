using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial flavor of Grid Swipe:
    /// - Fully replicates normal gameplay.
    /// - Ends automatically after 3 moves (30 score).
    /// - Shows step messages and final tutorial end screen.
    /// </summary>
    public class KiqqiGridSwipeTutorialManager : KiqqiGridSwipeManager
    {
        [Header("Tutorial Configuration")]
        public bool autoStartOnFirstRun = true;
        public bool continueToMainMenu = true;

        private const string TUTORIAL_SHOWN_KEY = "gridswipe_tutorial_shown_once";
        private const int MAX_TUTORIAL_SCORE = 30; // 3 moves × 10 points

        [Header("Tutorial Steps")]
        [TextArea]
        public string[] stepMessages =
        {
            "Welcome! Tap a tile next to you to move there.",
            "Nice! Try moving around a bit.",
            "That's it — you've got it!"
        };

        private int currentStep = 0;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiGridSwipeTutorialView);

        // -------------------------------------------------------------
        // Initialization
        // -------------------------------------------------------------
        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            var tv = context.UI.GetView<KiqqiGridSwipeTutorialView>();
            if (tv != null)
                view = tv;

            Debug.Log("[KiqqiGridSwipeTutorialManager] Initialized (button-based input).");
        }

        // -------------------------------------------------------------
        // Start MiniGame
        // -------------------------------------------------------------
        public override void StartMiniGame()
        {
            base.StartMiniGame();
            masterGame.State = KiqqiGameManager.GameState.Tutorial;

            currentStep = 0;

            // Rebind grid cells to this tutorial manager
            RebindTutorialCells();

            ShowStep();
            MarkTutorialShown();

            view?.ShowResult("0");
            Debug.Log("[KiqqiGridSwipeTutorialManager] Tutorial started and cells rebound.");
        }

        // -------------------------------------------------------------
        // Rebind Cells to Tutorial Manager
        // -------------------------------------------------------------
        private void RebindTutorialCells()
        {
            var viewObj = KiqqiAppManager.Instance.UI.GetView<KiqqiGridSwipeTutorialView>();
            if (viewObj == null || viewObj.gridRoot == null)
                return;

            int idx = 0;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (idx >= viewObj.gridRoot.childCount) break;

                    var child = viewObj.gridRoot.GetChild(idx);
                    var cell = child.GetComponent<KiqqiGridSwipeCell>();
                    if (cell)
                        cell.Init(this, c, r); // rebind to THIS tutorial manager

                    idx++;
                }
            }

            Debug.Log("[KiqqiGridSwipeTutorialManager] Rebound all grid cells to tutorial instance.");
        }

        // -------------------------------------------------------------
        // Tutorial Steps
        // -------------------------------------------------------------
        private void ShowStep()
        {
            var tutorialView = KiqqiAppManager.Instance.UI.GetView<KiqqiGridSwipeTutorialView>();
            if (tutorialView != null && currentStep >= 0 && currentStep < stepMessages.Length)
                tutorialView.ShowMessage(stepMessages[currentStep]);
        }

        // -------------------------------------------------------------
        // Handle Cell Press (tutorial variant)
        // -------------------------------------------------------------
        public override void HandleCellPressed(int col, int row)
        {
            if (!isActive || isComplete)
                return;

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            int dc = Mathf.Abs(col - playerC);
            int dr = Mathf.Abs(row - playerR);
            if (dc + dr != 1)
                return;

            // Move player
            grid[playerC, playerR] = 0;
            playerC = col;
            playerR = row;
            grid[playerC, playerR] = 1;
            steps++;

            // Scoring
            sessionScore += 10;
            masterGame.AddScore(10);

            // Update visuals
            view?.RefreshGrid(grid);
            view?.ShowResult($"{sessionScore}");

            // Tutorial message progression
            currentStep = Mathf.Min(currentStep + 1, stepMessages.Length - 1);
            ShowStep();

            // End tutorial after reaching 30 score (3 moves)
            if (sessionScore >= MAX_TUTORIAL_SCORE)
            {
                Debug.Log("[KiqqiGridSwipeTutorialManager] Tutorial complete after 3 moves.");
                CompleteMiniGame(sessionScore, true);
            }
        }

        // -------------------------------------------------------------
        // Utility / Persistence
        // -------------------------------------------------------------
        public bool ShouldAutoStartTutorial()
        {
            if (!autoStartOnFirstRun) return false;
            bool hasShown = PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) == 1;
            return !hasShown;
        }

        public void MarkTutorialShown()
        {
            PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 1);
            PlayerPrefs.Save();
        }

        // -------------------------------------------------------------
        // Completion / Exit
        // -------------------------------------------------------------
        public override void CompleteMiniGame(int finalScore, bool playerWon = true)
        {
            if (isComplete) return;

            isComplete = true;
            isActive = false;
            sessionScore = finalScore;

            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
            Debug.Log("[KiqqiGridSwipeTutorialManager] Tutorial ended — showing end view.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();
            Debug.Log("[KiqqiGridSwipeTutorialManager] Exited tutorial cleanly.");
        }
    }
}
