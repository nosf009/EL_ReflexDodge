using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Handles the tutorial flow for the "Poker Face" mini-game.
    /// - Runs a simplified, guided version of the main gameplay.
    /// - Only one round is played, with instructions overlayed on-screen.
    /// - Automatically shows once on first launch (configurable).
    /// </summary>
    public class KiqqiPokerFaceTutorialManager : KiqqiPokerFaceManager
    {
        #region INSPECTOR CONFIGURATION
        [Header("Tutorial Settings")]
        [TextArea]
        public string tutorialMessage = "Find the odd one out and tap it!";
        public bool continueToMainMenu = true;

        [Header("Auto-Start Settings")]
        [Tooltip("If true, tutorial runs automatically once on first app launch.")]
        public bool autoStartOnFirstRun = true;
        #endregion

        #region RUNTIME STATE
        private bool tutorialActive;
        public bool skipNextDeal = false;

        private const string TUTORIAL_SHOWN_KEY = "pokerface_tutorial_shown_once";
        #endregion

        #region CORE INITIALIZATION (DO NOT MODIFY)
        public override System.Type GetAssociatedViewType() => typeof(KiqqiPokerFaceTutorialView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);
            view = context.UI.GetView<KiqqiPokerFaceTutorialView>();

            Debug.Log("[KiqqiPokerFaceTutorialManager] Initialized.");
        }
        #endregion

        #region AUTO-START LOGIC (SAFE TO EDIT)
        /// <summary>
        /// Checks if tutorial should auto-start based on player prefs.
        /// </summary>
        public bool ShouldAutoStartTutorial()
        {
            if (!autoStartOnFirstRun)
                return false;

            bool hasShown = PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) == 1;
            return !hasShown;
        }

        /// <summary>
        /// Marks tutorial as completed so it won't auto-run next time.
        /// </summary>
        public void MarkTutorialShown()
        {
            PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Utility for testing - allows tutorial to re-run.
        /// </summary>
        public void ResetTutorialFlagForTesting()
        {
            PlayerPrefs.DeleteKey(TUTORIAL_SHOWN_KEY);
        }
        #endregion

        #region TUTORIAL GAMEPLAY FLOW (EDITABLE REGION)

        /// <summary>
        /// Starts the tutorial mini-game with a fixed element count.
        /// </summary>
        public override void StartMiniGame()
        {
            if (isActive) return;

            if (view is KiqqiPokerFaceTutorialView tView)
                tView.ClearGridSafe();

            base.StartMiniGame();
            tutorialActive = true;
            masterGame.State = KiqqiGameManager.GameState.Tutorial;

            MarkTutorialShown();

            totalElements = 4;
            oddIndex = Random.Range(0, totalElements);
            view?.BuildElements(totalElements);

            if (view is KiqqiPokerFaceTutorialView tutorialView)
                tutorialView.StartCoroutine(
                    tutorialView.WaitUntilDealingCompleteThenLock(oddIndex, tutorialMessage)
                );

            Debug.Log("[KiqqiPokerFaceTutorialManager] Tutorial started.");
        }


        /// <summary>
        /// Handles the correct tap by ending tutorial after feedback.
        /// </summary>
        public void HandleCorrectTap()
        {
            if (!tutorialActive) return;
            tutorialActive = false;

            StartCoroutine(FinishTutorialAfterFeedback());
        }


        /// <summary>
        /// Handles card tap events specifically for the tutorial.
        /// No level progression or next-deal logic here.
        /// </summary>
        public override void HandleElementTapped(int index)
        {
            if (!isActive || isComplete)
                return;

            inputEnabled = false;
            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            bool correct = index == oddIndex;

            if (correct)
            {
                if (view && index >= 0)
                {
                    var rt = view.GetCardRect(index);
                    if (rt) view.PlayCorrectParticlesAtCard(rt);
                }

                int correctScore = 100;
                sessionScore += correctScore;
                masterGame.AddScore(correctScore);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowFeedback(true, index);

                HandleCorrectTap(); // triggers delayed completion
            }
            else
            {
                view?.ShowFeedback(false, index);
                Debug.Log("[KiqqiPokerFaceTutorialManager] Wrong tap in tutorial � retry possible.");
            }
        }

        #endregion

        #region COMPLETION & TRANSITION LOGIC (DO NOT MODIFY)

        /// <summary>
        /// Delays tutorial completion slightly to allow feedback animation.
        /// </summary>
        private IEnumerator FinishTutorialAfterFeedback()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            CompleteMiniGame(100, true);
        }

        /// <summary>
        /// Handles cleanup and transitions to TutorialEndView.
        /// </summary>
        public override void CompleteMiniGame(int finalScore, bool playerWon = true)
        {
            if (isComplete) return;

            isComplete = true;
            isActive = false;
            sessionScore = finalScore;

            if (view is KiqqiPokerFaceTutorialView tView)
            {
                tView.SetAllCardsInteractable(false);
                tView.overlayRoot?.SetActive(false);
                tView.ClearGridSafe();
                tView.HideWithDeactivate(forceImmediate: true);
                tView.OnHide();
            }

            KiqqiAppManager.Instance.StartCoroutine(ShowTutorialEndDelayed());
        }

        /// <summary>
        /// Coroutine that transitions to the TutorialEndView.
        /// </summary>
        private IEnumerator ShowTutorialEndDelayed()
        {
            yield return new WaitForEndOfFrame();
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
            Debug.Log("[KiqqiPokerFaceTutorialManager] Tutorial completed and transitioned cleanly to end view.");
        }

        #endregion
    }
}
