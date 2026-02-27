using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Specialized view for the Poker Face tutorial.
    /// Adds instructional overlay, skip handling, and limited interactivity.
    /// </summary>
    public class KiqqiPokerFaceTutorialView : KiqqiPokerFaceView
    {
        #region INSPECTOR CONFIGURATION
        [Header("Tutorial UI")]
        public GameObject overlayRoot;
        public CanvasGroup overlayCanvas;
        public Text tutorialLabel;
        public Button skipButton;
        #endregion

        #region RUNTIME STATE
        private KiqqiPokerFaceTutorialManager tutMgr;
        #endregion

        #region VIEW INITIALIZATION (DO NOT MODIFY)

        public override void OnShow()
        {
            base.OnShow();

            timerRunning = false;
            remainingTime = 20f;
            UpdateTimeUI();
            ResetFeedback();

            if (skipButton)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipPressed);
                skipButton.interactable = false;
            }

            if (overlayRoot) overlayRoot.SetActive(false);
            if (overlayCanvas) overlayCanvas.alpha = 0f;

            var gm = KiqqiAppManager.Instance.Game;
            if (gm.currentMiniGame is KiqqiPokerFaceTutorialManager m)
                tutMgr = m;
        }

        protected override void OnCountdownFinished()
        {
            base.OnCountdownFinished();

            if (skipButton)
                skipButton.interactable = true;

            if (tutMgr != null && !tutMgr.isActive)
                tutMgr.StartMiniGame();

            timerRunning = true;
        }

        #endregion

        #region CARD INTERACTIVITY & TUTORIAL FLOW

        public void SetAllCardsInteractable(bool state)
        {
            if (Cards == null) return;

            foreach (var btn in Cards)
                if (btn)
                    btn.interactable = state;
        }

        /// <summary>
        /// Waits until dealing is finished, then locks all cards except the allowed one.
        /// </summary>
        public IEnumerator WaitUntilDealingCompleteThenLock(int allowedIndex, string message)
        {
            SetAllCardsInteractable(false);

            yield return new WaitUntil(() =>
            {
                if (Cards == null) return false;
                foreach (var b in Cards)
                    if (b && b.interactable)
                        return true;
                return false;
            });

            SetAllCardsInteractable(false);
            if (allowedIndex >= 0 && allowedIndex < Cards.Count)
                Cards[allowedIndex].interactable = true;

            StartCoroutine(ShowTutorialOverlayAfterDealing(message));
        }

        /// <summary>
        /// Fades in the tutorial overlay after dealing is done.
        /// </summary>
        public IEnumerator ShowTutorialOverlayAfterDealing(string msg)
        {
            yield return new WaitForSecondsRealtime(0.5f);

            if (overlayRoot) overlayRoot.SetActive(true);
            //if (tutorialLabel) tutorialLabel.text = msg;

            if (overlayCanvas)
            {
                overlayCanvas.alpha = 0f;
                float t = 0f;
                while (t < 0.5f)
                {
                    t += Time.unscaledDeltaTime;
                    overlayCanvas.alpha = Mathf.Lerp(0f, 1f, t / 0.5f);
                    yield return null;
                }
                overlayCanvas.alpha = 1f;
            }
        }

        /// <summary>
        /// Assigns click handlers, locking interactivity to only the allowed card.
        /// </summary>
        public void SetCardInteractivity(int allowedIndex)
        {
            for (int i = 0; i < elementRoot.childCount; i++)
            {
                var btn = elementRoot.GetChild(i).GetComponent<Button>();
                if (!btn) continue;

                int captured = i;
                btn.interactable = captured == allowedIndex;
                btn.onClick.RemoveAllListeners();

                if (captured == allowedIndex)
                {
                    btn.onClick.AddListener(() =>
                    {
                        HideTutorialOverlay();
                        if (tutMgr != null)
                            tutMgr.HandleCorrectTap();
                    });
                }
                else
                {
                    btn.onClick.AddListener(() =>
                    {
                        KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
                    });
                }
            }
        }

        private void HideTutorialOverlay()
        {
            if (overlayRoot) overlayRoot.SetActive(false);
        }

        private void OnSkipPressed()
        {
            Debug.Log("[KiqqiPokerFaceTutorialView] Skip pressed.");
            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
        }

        #endregion

        #region FEEDBACK HANDLING & COMPLETION

        public override void ShowFeedback(bool correct, int tappedIndex)
        {
            if (correct)
                StartCoroutine(ShowFeedbackAndEndTutorial(tappedIndex));
            else
                base.ShowFeedback(false, tappedIndex);
        }

        private IEnumerator ShowFeedbackAndEndTutorial(int tappedIndex)
        {
            base.ShowFeedback(true, tappedIndex);
            yield return new WaitForSecondsRealtime(1f);

            var gm = KiqqiAppManager.Instance.Game;

            if (gm.currentMiniGame is KiqqiPokerFaceTutorialManager mgr)
            {
                mgr.HandleCorrectTap();
            }
        }

        #endregion

        #region CLEANUP

        public void ClearGridSafe()
        {
            if (elementRoot)
            {
                foreach (Transform child in elementRoot)
                {
                    if (child)
                        Destroy(child.gameObject);
                }
            }

            SetAllCardsInteractable(false);
            ResetFeedback();

            Debug.Log("[KiqqiPokerFaceTutorialView] Cleared grid before new tutorial start.");
        }

        #endregion
    }
}
