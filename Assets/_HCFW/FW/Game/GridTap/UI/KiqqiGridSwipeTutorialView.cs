using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial view for Grid Swipe:
    /// - Inherits production view HUD + grid.
    /// - Adds guidance label + overlay + skip button.
    /// - Safe resume behavior.
    /// </summary>
    public class KiqqiGridSwipeTutorialView : KiqqiGridSwipeView
    {
        [Header("Tutorial UI")]
        public Text tutorialLabel;
        public GameObject overlay;
        public Button skipButton;

        public void ShowMessage(string msg)
        {
            if (tutorialLabel) tutorialLabel.text = msg;
            if (overlay) overlay.SetActive(true);
        }

        private void OnSkipPressed()
        {
            Debug.Log("[KiqqiGridSwipeTutorialView] Tutorial skipped.");
            skipButton.interactable = false;
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }

        public override void OnShow()
        {
            var app = KiqqiAppManager.Instance;
            var game = app.Game;

            if (skipButton)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipPressed);
                skipButton.interactable = false;
            }

            // ----- RESUME PATH -----
            if (game.ResumeRequested && game.currentMiniGame is KiqqiGridSwipeTutorialManager tutMgr)
            {
                Debug.Log("[KiqqiGridSwipeTutorialView] Resume tutorial (skip base reset).");

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

                tutMgr.ResumeFromPause(this);

                timerRunning = true;
                game.ResumeRequested = false;
                return;
            }

            // ----- FRESH START PATH -----
            base.OnShow();
            remainingTime = 20f;
            overlay?.SetActive(true);
            tutorialLabel?.gameObject.SetActive(true);
        }

        protected override void OnCountdownFinished()
        {
            if (gridRoot) gridRoot.gameObject.SetActive(true);

            var gm = KiqqiAppManager.Instance.Game;
            if (gm.currentMiniGame is KiqqiGridSwipeTutorialManager tutMgr)
            {
                // --- Force rebind of all grid cells to the tutorial manager ---
                int idx = 0;
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        if (idx >= gridRoot.childCount) break;
                        var cell = gridRoot.GetChild(idx).GetComponent<KiqqiGridSwipeCell>();
                        if (cell) cell.Init(tutMgr, c, r);
                        idx++;
                    }
                }
                // ---------------------------------------------------------------

                if (!tutMgr.isActive && !tutMgr.isComplete)
                    tutMgr.StartMiniGame();

                if (skipButton) skipButton.interactable = true;
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
