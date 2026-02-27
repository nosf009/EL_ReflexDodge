using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial mode for SwipeMover:
    /// Replicates full gameplay but ends after 3 moves.
    /// </summary>
    public class KiqqiSwipeMoverTutorialManager : KiqqiSwipeMoverManager
    {
        [Header("Tutorial Settings")]
        public bool autoStartOnFirstRun = true;
        public bool continueToMainMenu = true;

        private const string TUTORIAL_KEY = "swipemover_tutorial_done";

        [TextArea]
        public string[] stepMessages =
        {
            "Swipe in any direction to move the red tile.",
            "Good! Try a few more swipes.",
            "Each swipe gives points — easy, right?",
            "That's all! You're ready."
        };

        private int stepIndex = 0;
        private const int MAX_TUTORIAL_MOVES = 3;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiSwipeMoverTutorialView);

        public override void StartMiniGame()
        {
            // Ensure correct view before initializing
            var context = KiqqiAppManager.Instance;
            view = context.UI.GetView<KiqqiSwipeMoverTutorialView>();

            base.StartMiniGame(); // now view points to tutorial view
            masterGame.State = KiqqiGameManager.GameState.Tutorial;

            stepIndex = 0;
            ShowStep();
            MarkTutorialShown();

            // make sure player spawns visibly in the middle
            InitGrid();
            view?.RefreshGrid(grid);

            Debug.Log("[KiqqiSwipeMoverTutorialManager] Tutorial started with full gameplay replication.");
        }

        private void ShowStep()
        {
            var tv = KiqqiAppManager.Instance.UI.GetView<KiqqiSwipeMoverTutorialView>();
            if (tv != null && stepIndex >= 0 && stepIndex < stepMessages.Length)
                tv.ShowMessage(stepMessages[stepIndex]);
        }

        protected override void HandleSwipe(Vector2 start, Vector2 end, KiqqiInputController.SwipeDirection dir)
        {
            int prevSteps = steps;

            base.HandleSwipe(start, end, dir);

            // Advance tutorial text
            if (steps > prevSteps)
            {
                stepIndex = Mathf.Min(stepIndex + 1, stepMessages.Length - 1);
                ShowStep();

                // Auto-complete after limited number of moves
                if (steps >= MAX_TUTORIAL_MOVES)
                {
                    Debug.Log("[KiqqiSwipeMoverTutorialManager] Tutorial complete after 3 moves.");
                    CompleteMiniGame(sessionScore, true);
                }
            }
        }

        public bool ShouldAutoStartTutorial()
        {
            if (!autoStartOnFirstRun) return false;
            return PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 0;
        }

        public void MarkTutorialShown()
        {
            PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
            PlayerPrefs.Save();
        }

        public override void CompleteMiniGame(int finalScore, bool playerWon = true)
        {
            if (isComplete) return;
            isComplete = true;
            isActive = false;

            var app = KiqqiAppManager.Instance;
            var input = app.Input;

            // Reset inputArea safely for next normal game session
            if (input != null)
            {
                var gameView = app.UI.GetView<KiqqiSwipeMoverView>();
                if (gameView != null && gameView.swipePanel != null)
                    input.inputArea = gameView.swipePanel;
                else
                    input.inputArea = input.targetCanvas
                        ? input.targetCanvas.GetComponent<RectTransform>()
                        : null;
            }

            app.UI.ShowView<KiqqiTutorialEndView>();
            Debug.Log("[KiqqiSwipeMoverTutorialManager] Completed tutorial — input restored for normal gameplay.");
        }

    }
}
