using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial flavor of Pattern Repeat:
    /// - Uses a short, fixed-length pattern (3 cells).
    /// - Rebinds grid to this tutorial instance.
    /// - Ends by showing KiqqiTutorialEndView (does not post results screen).
    /// </summary>
    public class KiqqiPatternRepeatTutorialManager : KiqqiPatternRepeatManager
    {
        [Header("Tutorial Configuration")]
        public bool autoStartOnFirstRun = true;
        public bool continueToMainMenu = true;

        private const string TUTORIAL_SHOWN_KEY = "patternrepeat_tutorial_shown_once";

        [Header("Tutorial Steps")]
        [TextArea]
        public string[] stepMessages =
        {
            "Watch the pattern carefully.",
            "Now tap the same cells.",
            "Great! That�s how it works."
        };

        private int currentStep = 0;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiPatternRepeatTutorialView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            var tv = context.UI.GetView<KiqqiPatternRepeatTutorialView>();
            if (tv != null)
                view = tv;

            Debug.Log("[KiqqiPatternRepeatTutorialManager] Initialized.");
        }

        public override void StartMiniGame()
        {
            base.StartMiniGame();
            masterGame.State = KiqqiGameManager.GameState.Tutorial;
            currentStep = 0;

            // Force a fixed short pattern (3 cells)
            pattern.Clear();
            remaining.Clear();
            GenerateFixedTutorialPattern(3); // replaces pattern built by base
            view?.ShowPattern(pattern);

            // After small delay, hide and enable input (reuse reveal)
            Debug.Log("[KiqqiPatternRepeatTutorialManager] Tutorial started.");
            MarkTutorialShown();
        }

        private void GenerateFixedTutorialPattern(int count)
        {
            // Deterministic, easy pattern: center + two corners (or random fallback)
            List<Vector2Int> candidates = new()
            {
                new Vector2Int(1,1),
                new Vector2Int(0,0),
                new Vector2Int(2,2),
                new Vector2Int(0,2),
                new Vector2Int(2,0)
            };

            int idx = 0;
            for (int i = 0; i < count; i++)
            {
                var p = candidates[Mathf.Min(idx, candidates.Count - 1)];
                idx++;
                pattern.Add(p);
                remaining.Add(p);
            }
        }

        private void ShowStep()
        {
            var tutorialView = KiqqiAppManager.Instance.UI.GetView<KiqqiPatternRepeatTutorialView>();
            if (tutorialView != null && currentStep >= 0 && currentStep < stepMessages.Length)
                tutorialView.ShowMessage(stepMessages[currentStep]);
        }

        public override void HandleCellPressed(int col, int row)
        {
            if (!isActive || isComplete) return;

            base.HandleCellPressed(col, row);

            // Progress tutorial hint text after each correct tap
            currentStep = Mathf.Min(currentStep + 1, stepMessages.Length - 1);
            ShowStep();
        }

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

        public override void CompleteMiniGame(int finalScore, bool playerWon = true)
        {
            if (isComplete) return;

            isComplete = true;
            isActive = false;
            sessionScore = finalScore;

            // Show tutorial end instead of results
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
            Debug.Log("[KiqqiPatternRepeatTutorialManager] Tutorial ended � showing end view.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();
            Debug.Log("[KiqqiPatternRepeatTutorialManager] Exited tutorial cleanly.");
        }
    }
}
