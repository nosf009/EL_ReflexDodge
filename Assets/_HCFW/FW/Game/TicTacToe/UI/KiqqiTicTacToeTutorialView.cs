using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial-flavored view:
    /// - Inherits the production view HUD & grid.
    /// - Adds a tutorial label + overlay.
    /// - Safe resume: skip restart when resuming from Pause.
    /// - Safely starts (or continues) tutorial without double-starting.
    /// </summary>
    public class KiqqiTicTacToeTutorialView : KiqqiTicTacToeView
    {
        [Header("Tutorial UI")]
        public Text tutorialLabel;
        public GameObject overlay;

        [Header("Tutorial UI")]
        public Button skipButton;

        private void OnSkipPressed()
        {
            Debug.Log("[KiqqiTicTacToeTutorialView] Tutorial skipped.");
            skipButton.interactable = false;
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }


        public void ShowMessage(string msg)
        {
            if (tutorialLabel) tutorialLabel.text = msg;
            if (overlay) overlay.SetActive(true);
        }

        public override void OnShow()
        {
            var app = KiqqiAppManager.Instance;
            var game = app.Game;
            remainingTime = 20f;

            if (skipButton)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipPressed);
                skipButton.interactable = false;
            }

            // ----- RESUME PATH -----
            if (game.ResumeRequested && game.currentMiniGame is KiqqiTicTacToeTutorialManager tttTut)
            {
                Debug.Log("[KiqqiTicTacToeTutorialView] Resume tutorial (skip base reset).");

                // Ensure CanvasGroup is visible & interactive
                var cg = GetComponent<CanvasGroup>();
                if (cg)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                if (countdownLabel) countdownLabel.gameObject.SetActive(false);
                if (overlay) overlay.SetActive(true);
                if (gridRoot) gridRoot.gameObject.SetActive(true);

                // Ask manager to restore interactivity & visuals
                tttTut.ResumeFromPause(this);

                timerRunning = true;
                game.ResumeRequested = false;
                return;
            }

            // ----- FRESH START PATH -----
            base.OnShow(); // production view sets up HUD, countdown, etc.
            overlay?.SetActive(true);
            tutorialLabel?.gameObject.SetActive(true);
        }

        protected override void OnCountdownFinished()
        {
            // After countdown we want to start the tutorial only if it isn't already running.
            if (gridRoot) gridRoot.gameObject.SetActive(true);

            var gm = KiqqiAppManager.Instance.Game;
            if (gm.currentMiniGame is KiqqiTicTacToeTutorialManager tutMgr)
            {
                // If not active yet (fresh entry), start it. If already active (resume), do nothing.
                if (!tutMgr.isActive && !tutMgr.isComplete)
                    tutMgr.StartMiniGame();

                skipButton.interactable = true;
            }
        }

        protected override void OnTimeUp()
        {
            base.OnTimeUp();
            Debug.Log($"[{GetType().Name}] Time reached 00:00 - Tutorial end");

            timerRunning = false;
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();

            // Optional: play SFX
            KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
        }

    }
}
