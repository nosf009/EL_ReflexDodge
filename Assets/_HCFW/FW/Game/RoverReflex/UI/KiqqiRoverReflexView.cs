using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Kiqqi.Framework
{
    public class KiqqiRoverReflexView : KiqqiGameViewBase, IPointerDownHandler
    {
        [Header("Background Fade")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 1f;

        [Header("Streak Display")]
        public Text streakText;
        public GameObject streakContainer;

        [Header("Playable Area")]
        public RectTransform playableAreaRect;

        [Header("Tutorial UI")]
        public GameObject tutTimePanelToHide;
        public GameObject tutScorePanelToHide;
        public GameObject tutPauseBtnToHide;
        public GameObject tutSkipBtn;
        public GameObject tutOverlay;
        public GameObject tutInfoText1;
        public GameObject tutInfoText2;
        public RectTransform handIconObjectForTut;

        // Set by KiqqiRoverReflexTutorialManager before OnShow is called
        public bool IsTutorialMode { get; set; }

        private KiqqiRoverReflexManager manager;

        public void BindManager(KiqqiRoverReflexManager m)
        {
            manager = m;
        }

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiRoverReflexManager rdm)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(rdm));
                return;
            }

            if (streakContainer)
                streakContainer.SetActive(false);

            if (streakText)
                streakText.text = "";

            if (gameplayBackground)
            {
                var c = gameplayBackground.color;
                c.a = 0f;
                gameplayBackground.color = c;
                gameplayBackground.gameObject.SetActive(false);
            }

            // Always restore HUD panels first so they're in a clean state,
            // regardless of whether the previous session was a tutorial.
            if (tutTimePanelToHide)  tutTimePanelToHide.SetActive(true);
            if (tutScorePanelToHide) tutScorePanelToHide.SetActive(true);
            if (tutPauseBtnToHide)   tutPauseBtnToHide.SetActive(true);

            // Apply tutorial mode UI overrides before base.OnShow() starts the countdown
            if (IsTutorialMode)
                ApplyTutorialUIState();

            base.OnShow();
        }

        /// <summary>Configures the HUD for tutorial mode: hides score/time/pause, shows skip (non-interactable).</summary>
        private void ApplyTutorialUIState()
        {
            if (tutTimePanelToHide)  tutTimePanelToHide.SetActive(false);
            if (tutScorePanelToHide) tutScorePanelToHide.SetActive(false);
            if (tutPauseBtnToHide)   tutPauseBtnToHide.SetActive(false);

            if (tutSkipBtn)
            {
                tutSkipBtn.SetActive(true);
                var btn = tutSkipBtn.GetComponent<Button>();
                if (btn) btn.interactable = false; // enabled after countdown
            }

            if (tutOverlay)   tutOverlay.SetActive(false);
            if (tutInfoText1) tutInfoText1.SetActive(false);
            if (tutInfoText2) tutInfoText2.SetActive(false);
            if (handIconObjectForTut) handIconObjectForTut.gameObject.SetActive(false);
        }

        /// <summary>Called by tutorial manager after countdown: enables skip button.</summary>
        public void EnableSkipButton()
        {
            if (!tutSkipBtn) return;
            var btn = tutSkipBtn.GetComponent<Button>();
            if (btn) btn.interactable = true;
        }

        /// <summary>Shows/hides the tutorial instruction overlay panel.</summary>
        public void SetTutorialOverlayActive(bool active)
        {
            if (tutOverlay) tutOverlay.SetActive(active);
        }

        /// <summary>Shows step 1 text, hides step 2. Call after countdown when paused.</summary>
        public void ShowTutorialStep1()
        {
            if (tutInfoText1) tutInfoText1.SetActive(true);
            if (tutInfoText2) tutInfoText2.SetActive(false);
            SetTutorialOverlayActive(true);
        }

        /// <summary>Shows step 2 text, hides step 1. Call after mineral pickup.</summary>
        public void ShowTutorialStep2()
        {
            if (tutInfoText1) tutInfoText1.SetActive(false);
            if (tutInfoText2) tutInfoText2.SetActive(true);
            SetTutorialOverlayActive(true);
        }

        /// <summary>Hides both instruction texts and overlay panel.</summary>
        public void HideTutorialOverlay()
        {
            SetTutorialOverlayActive(false);
            if (tutInfoText1) tutInfoText1.SetActive(false);
            if (tutInfoText2) tutInfoText2.SetActive(false);
        }

        /// <summary>Positions and shows the hand icon at the given anchored position within the view.</summary>
        public void ShowHandIcon(Vector2 anchoredPos)
        {
            if (!handIconObjectForTut) return;
            handIconObjectForTut.anchoredPosition = anchoredPos;
            handIconObjectForTut.SetAsLastSibling();
            handIconObjectForTut.gameObject.SetActive(true);
        }

        /// <summary>Hides the tutorial hand icon.</summary>
        public void HideHandIcon()
        {
            if (handIconObjectForTut)
                handIconObjectForTut.gameObject.SetActive(false);
        }

        /// <summary>Full cleanup of all tutorial UI — call before EndSession to leave view in a clean state.</summary>
        public void ResetTutorialUI()
        {
            IsTutorialMode = false;

            // HUD panels (time, score, pause) are intentionally NOT restored here.
            // OnShow() always resets them to active before ApplyTutorialUIState() runs,
            // so restoring here would cause a visible flash when the view deactivates.

            if (tutSkipBtn)
            {
                var btn = tutSkipBtn.GetComponent<Button>();
                if (btn) btn.interactable = false;
                tutSkipBtn.SetActive(false);
            }

            HideTutorialOverlay();
            HideHandIcon();
        }

        public override void OnHide()
        {
            base.OnHide();

            if (streakContainer)
                streakContainer.SetActive(false);

            if (IsTutorialMode)
                ResetTutorialUI();

            if (manager != null)
            {
                manager.isActive = false;
                manager = null;
            }

            Debug.Log("[KiqqiReflexDodgeView] OnHide");
        }

        private IEnumerator HandleResumeFadeIn(KiqqiRoverReflexManager rdm)
        {
            if (gameplayBackground)
            {
                gameplayBackground.gameObject.SetActive(true);
                float t = 0f;
                Color bgCol = gameplayBackground.color;
                bgCol.a = 0f;
                gameplayBackground.color = bgCol;

                while (t < backgroundFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / backgroundFadeDuration);
                    float eased = 1f - Mathf.Pow(1f - n, 4f);

                    bgCol.a = Mathf.Lerp(0f, 1f, eased);
                    gameplayBackground.color = bgCol;

                    yield return null;
                }

                bgCol.a = 1f;
                gameplayBackground.color = bgCol;
            }

            manager = rdm;
            rdm.ResumeFromPause(this);
            timerRunning = true;
            if (pauseButton)
                pauseButton.interactable = true;
        }

        protected override void OnCountdownFinished()
        {
            StartCoroutine(HandleBackgroundFadeThenStart());
        }

        private IEnumerator HandleBackgroundFadeThenStart()
        {
            if (gameplayBackground)
            {
                gameplayBackground.gameObject.SetActive(true);
                float t = 0f;
                Color col = gameplayBackground.color;
                while (t < backgroundFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / backgroundFadeDuration);
                    float eased = 1f - Mathf.Pow(1f - n, 5f);
                    col.a = Mathf.Lerp(0f, 1f, eased);
                    gameplayBackground.color = col;
                    yield return null;
                }

                col.a = 1f;
                gameplayBackground.color = col;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            if (IsTutorialMode)
            {
                // Tutorial: stop the base timer immediately, enable skip, let manager drive from here
                StopTimer();
                EnableSkipButton();

                if (KiqqiAppManager.Instance.Game?.currentMiniGame is KiqqiRoverReflexManager tutMgr
                    && tutMgr.isTutorialMode)
                {
                    manager = tutMgr;
                    tutMgr.StartMiniGame();
                }
            }
            else
            {
                if (pauseButton)
                    pauseButton.interactable = true;

                var gm = KiqqiAppManager.Instance.Game;
                if (gm?.currentMiniGame is KiqqiRoverReflexManager rdm)
                {
                    manager = rdm;
                    rdm.StartMiniGame();
                }

                timerRunning = true;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (manager != null && playableAreaRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    playableAreaRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
                {
                    Vector2 worldPoint = playableAreaRect.TransformPoint(localPoint);
                    manager.HandlePlayerTap(eventData.position);
                }
            }
        }

        public void UpdateStreakDisplay(int streak, float multiplier)
        {
            if (!streakText || !streakContainer) return;

            if (streak >= 2)
            {
                streakContainer.SetActive(true);
                streakText.text = $"STREAK x{streak}\n{multiplier:F1}x BONUS";
            }
            else
            {
                streakContainer.SetActive(false);
            }
        }

        protected override void OnTimeUp()
        {
            base.OnTimeUp();

            if (manager != null)
            {
                manager.NotifyTimeUp();
            }
        }

        public void RefreshScoreUI()
        {
            UpdateScoreUI();
        }
    }
}
