// Assets/_HCFW/FW/Game/TemporalTrap/UI/KiqqiTemporalTrapTutorialView.cs
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial view for Temporal Trap.
    /// Extends gameplay HUD with overlay text and skip button.
    /// </summary>
    public class KiqqiTemporalTrapTutorialView : KiqqiTemporalTrapView
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
            Debug.Log("[KiqqiTemporalTrapTutorialView] Tutorial skipped.");
            if (skipButton) skipButton.interactable = false;
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (skipButton)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipPressed);
                skipButton.interactable = false;
            }

            if (overlay) overlay.SetActive(true);
            if (tutorialLabel)
                tutorialLabel.text = "Watch the hand carefully.\nPress OK if it skips, NOK if it moves normally.";
        }

        protected override void OnCountdownFinished()
        {
            base.OnCountdownFinished();

            if (skipButton) skipButton.interactable = true;

            var gm = KiqqiAppManager.Instance.Game;
            if (gm.currentMiniGame is KiqqiTemporalTrapTutorialManager tutMgr)
            {
                if (!tutMgr.isActive && !tutMgr.isComplete)
                {
                    Debug.Log("[KiqqiTemporalTrapTutorialView] Countdown done — starting tutorial.");
                    tutMgr.StartMiniGame();
                }
            }
        }

        protected override void OnTimeUp()
        {
            base.OnTimeUp();
            var gm = KiqqiAppManager.Instance.Game.currentMiniGame;
            if (gm is KiqqiTemporalTrapTutorialManager tutMgr)
                tutMgr.CompleteMiniGame(tutMgr.sessionScore, true);
            else
                KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }
    }
}
