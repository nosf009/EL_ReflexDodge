using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Kiqqi.Framework
{
    public class KiqqiReflexDodgeView : KiqqiGameViewBase, IPointerDownHandler
    {
        [Header("Background Fade")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 1f;

        [Header("Streak Display")]
        public Text streakText;
        public GameObject streakContainer;

        [Header("Playable Area")]
        public RectTransform playableAreaRect;

        private KiqqiReflexDodgeManager manager;

        public void BindManager(KiqqiReflexDodgeManager m)
        {
            manager = m;
        }

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiReflexDodgeManager rdm)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(rdm));
                return;
            }

            if (streakContainer)
            {
                streakContainer.SetActive(false);
            }

            if (streakText)
            {
                streakText.text = "";
            }

            if (gameplayBackground)
            {
                var c = gameplayBackground.color;
                c.a = 0f;
                gameplayBackground.color = c;
                gameplayBackground.gameObject.SetActive(false);
            }

            base.OnShow();
        }

        public override void OnHide()
        {
            base.OnHide();

            if (streakContainer)
            {
                streakContainer.SetActive(false);
            }

            if (manager != null)
            {
                manager.isActive = false;
                manager = null;
            }

            Debug.Log("[KiqqiReflexDodgeView] OnHide");
        }

        private IEnumerator HandleResumeFadeIn(KiqqiReflexDodgeManager rdm)
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

            if (pauseButton)
                pauseButton.interactable = true;

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiReflexDodgeManager rdm)
            {
                manager = rdm;
                rdm.StartMiniGame();
            }

            timerRunning = true;
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
