using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial view for SwipeMover.
    /// Extends base game view with overlay + tutorial text.
    /// </summary>
    public class KiqqiSwipeMoverTutorialView : KiqqiSwipeMoverView
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
            Debug.Log("[KiqqiSwipeMoverTutorialView] Tutorial skipped.");
            skipButton.interactable = false;
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }

        public override void OnShow()
        {
            var app = KiqqiAppManager.Instance;
            var gm = app.Game;

            // Assign this view's swipe panel as the active input area
            var input = app.Input;
            if (input != null)
            {
                if (swipePanel != null)
                    input.inputArea = swipePanel;
                else
                    input.inputArea = input.targetCanvas
                        ? input.targetCanvas.GetComponent<RectTransform>()
                        : null;

                input.enabled = true;
            }

            if (skipButton)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipPressed);
                skipButton.interactable = false;
            }

            if (gm.ResumeRequested && gm.currentMiniGame is KiqqiSwipeMoverTutorialManager tutMgr)
            {
                var cg = GetComponent<CanvasGroup>();
                if (cg)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                overlay?.SetActive(true);
                if (countdownLabel) countdownLabel.gameObject.SetActive(false);
                gridRoot?.gameObject.SetActive(true);
                tutMgr.ResumeFromPause(this);
                gm.ResumeRequested = false;
                return;
            }

            base.OnShow();
            remainingTime = 20f;
            overlay?.SetActive(true);
            tutorialLabel?.gameObject.SetActive(true);
        }

        public override void OnHide()
        {
            base.OnHide();

            // Reset input area to default root when tutorial view is hidden
            var input = KiqqiAppManager.Instance.Input;
            if (input != null)
            {
                input.inputArea = input.targetCanvas
                    ? input.targetCanvas.GetComponent<RectTransform>()
                    : null;
            }
        }

        protected override void OnCountdownFinished()
        {
            if (gridRoot) gridRoot.gameObject.SetActive(true);
            var gm = KiqqiAppManager.Instance.Game;
            if (gm.currentMiniGame is KiqqiSwipeMoverTutorialManager tutMgr)
            {
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
