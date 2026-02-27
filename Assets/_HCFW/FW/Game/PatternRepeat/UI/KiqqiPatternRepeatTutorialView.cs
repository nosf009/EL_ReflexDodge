using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial view for Pattern Repeat:
    /// - Inherits production view HUD + grid.
    /// - Adds guidance label + overlay + skip button.
    /// - Safe resume behavior.
    /// </summary>
    public class KiqqiPatternRepeatTutorialView : KiqqiPatternRepeatView
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
            Debug.Log("[KiqqiPatternRepeatTutorialView] Tutorial skipped.");
            if (skipButton) skipButton.interactable = false;
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
            if (game.ResumeRequested && game.currentMiniGame is KiqqiPatternRepeatTutorialManager tutMgr)
            {
                Debug.Log("[KiqqiPatternRepeatTutorialView] Resume tutorial (skip base reset).");

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
            overlay?.SetActive(true);
            tutorialLabel?.gameObject.SetActive(true);
        }

        protected override void OnCountdownFinished()
        {
            if (gridRoot) gridRoot.gameObject.SetActive(true);

            var gm = KiqqiAppManager.Instance.Game;
            if (gm.currentMiniGame is KiqqiPatternRepeatTutorialManager tutMgr)
            {
                if (!tutMgr.isActive && !tutMgr.isComplete)
                    tutMgr.StartMiniGame();

                if (skipButton) skipButton.interactable = true;
            }
        }
    }
}
