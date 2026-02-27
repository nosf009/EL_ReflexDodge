using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Guided Tic-Tac-Toe tutorial:
    /// - Uses the same grid logic as the main manager (inherits it).
    /// - Shows step messages via the tutorial view.
    /// - Ends with TutorialEndView (no score results, no level progression).
    /// </summary>
    public class KiqqiTicTacToeTutorialManager : KiqqiTicTacToeManager
    {

        [Header("Tutorial Configuration")]
        [Tooltip("If true, tutorial will automatically run the first time the game is launched.")]
        public bool autoStartOnFirstRun = true;

        [Tooltip("If true, tutorial end screen continues to main menu; otherwise it goes to first level.")]
        public bool continueToMainMenu = true;

        private const string TUTORIAL_SHOWN_KEY = "tictactoe_tutorial_shown_once";


        [Header("Tutorial Flow")]
        [TextArea]
        public string[] stepMessages =
        {
            "Welcome! Let's learn Tic-Tac-Toe.",
            "Place your X to start. Try the center or a corner.",
            "Great! Notice how the AI responds.",
            "Always try to create two ways to win.",
            "That's it! Let's finish the tutorial."
        };

        private int currentStep = 0;

        public override System.Type GetAssociatedViewType()
        {
            return typeof(KiqqiTicTacToeTutorialView);
        }

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);
            var tutorialView = context.UI.GetView<KiqqiTicTacToeTutorialView>();
            if (tutorialView != null) SetView(tutorialView);
        }


        public override void StartMiniGame()
        {
            base.StartMiniGame();

            masterGame.State = KiqqiGameManager.GameState.Tutorial;

            // Soften the AI by default for tutorial (feel free to change).
            aiDifficulty = AiDifficulty.Easy;

            currentStep = 0;
            sessionScore = 0;
            isActive = true;
            isComplete = false;
            ShowStep();

            // Mark as shown if auto-started
            MarkTutorialShown();
        }

        private void ShowStep()
        {
            var tutorialView = KiqqiAppManager.Instance.UI.GetView<KiqqiTicTacToeTutorialView>();
            if (tutorialView != null && currentStep >= 0 && currentStep < stepMessages.Length)
            {
                tutorialView.ShowMessage(stepMessages[currentStep]);
            }
        }

        /// <summary>
        /// Called by cells via view piping: this inherits the production HandleCellPressed, so it still plays a real game.
        /// After a player move, we advance the tutorial messaging to keep guidance flowing.
        /// </summary>
        public new void HandleCellPressed(int col, int row)
        {
            base.HandleCellPressed(col, row);

            // If the base logic ended the game (win/draw), don't advance steps.
            if (isComplete) return;

            // Keep tutorial guidance in sync with progress.
            currentStep = Mathf.Min(currentStep + 1, stepMessages.Length - 1);
            ShowStep();
        }

        /// <summary>
        /// Tutorial completion should NOT show the standard Results view or trigger level progression.
        /// </summary>
        public override void CompleteMiniGame(int finalScore, bool playerWon = true)
        {
            // Prevent multiple endings
            if (isComplete) return;

            Debug.Log("[KiqqiTicTacToeTutorialManager] Tutorial finished.");

            // Stop normal game flow (no level progression, no Results view)
            isComplete = true;
            isActive = false;
            sessionScore = finalScore;

            currentStep = 0;

            // Skip masterGame.EndGame() entirely — tutorials should not alter global level state
            // Skip Results view — show the tutorial end panel instead
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }

        /// <summary>
        /// Returns true if the tutorial should auto-start on this app load.
        /// </summary>
        public bool ShouldAutoStartTutorial()
        {
            // Only trigger once if enabled
            if (!autoStartOnFirstRun) return false;

            bool hasShown = PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) == 1;
            return !hasShown;
        }

        /// <summary>
        /// Marks the tutorial as shown so it won't auto-start again.
        /// </summary>
        public void MarkTutorialShown()
        {
            PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 1);
            PlayerPrefs.Save();
        }

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();

            // Fully reset tutorial state
            isComplete = false;
            isActive = false;
            sessionScore = 0;
            currentStep = 0;
        }

        protected override void OnDrawDetected()
        {
            //if (isComplete) return;

            Debug.Log("[KiqqiTicTacToeTutorialManager] Draw detected, ending tutorial.");

            isComplete = true;
            isActive = false;
            sessionScore = 50;

            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }


    }
}
