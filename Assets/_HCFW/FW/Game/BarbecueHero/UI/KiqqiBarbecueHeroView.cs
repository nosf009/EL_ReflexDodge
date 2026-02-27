using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiBarbecueHeroView : KiqqiGameViewBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Tap Feedback")]
        [Tooltip("Perfect tap feedback image")]
        public Image feedbackPerfect;
        [Tooltip("Good tap feedback image")]
        public Image feedbackGood;
        [Tooltip("Wrong tap feedback image")]
        public Image feedbackWrong;
        [Tooltip("Fade in time (seconds)")]
        public float feedbackFadeInTime = 0.15f;
        [Tooltip("Hold at full opacity time (seconds)")]
        public float feedbackHoldTime = 0.25f;
        [Tooltip("Fade out time (seconds)")]
        public float feedbackFadeOutTime = 0.15f;

        [Header("Background")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 0.3f;

        #endregion

        #region VIEW INITIALIZATION

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiBarbecueHeroManager mgr)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(mgr));
                return;
            }

            ResetFeedback();

            if (gameplayBackground)
            {
                var c = gameplayBackground.color;
                c.a = 0f;
                gameplayBackground.color = c;
                gameplayBackground.gameObject.SetActive(false);
            }

            base.OnShow();
        }

        #endregion

        #region BACKGROUND FADE & RESUME

        private IEnumerator HandleResumeFadeIn(KiqqiBarbecueHeroManager mgr)
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

            mgr.ResumeFromPause(this);
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

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiBarbecueHeroManager mgr)
            {
                mgr.StartMiniGame();
            }

            timerRunning = true;
        }

        #endregion

        #region TAP FEEDBACK

        public void ShowTapFeedback(Vector3 position, bool correct, int scoreChange, bool isPerfect)
        {
            StartCoroutine(AnimateTapFeedback(position, correct, scoreChange, isPerfect));
        }

        private IEnumerator AnimateTapFeedback(Vector3 position, bool correct, int scoreChange, bool isPerfect)
        {
            Image feedbackImg = null;

            if (correct)
            {
                if (isPerfect && feedbackPerfect != null)
                {
                    feedbackImg = feedbackPerfect;
                }
                else if (feedbackGood != null)
                {
                    feedbackImg = feedbackGood;
                }
            }
            else if (feedbackWrong != null)
            {
                feedbackImg = feedbackWrong;
            }

            if (feedbackImg != null)
            {
                feedbackImg.gameObject.SetActive(true);
                feedbackImg.color = new Color(1, 1, 1, 0);

                float t = 0f;
                while (t < feedbackFadeInTime)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / feedbackFadeInTime);
                    float eased = Mathf.SmoothStep(0f, 1f, n);
                    feedbackImg.color = new Color(1, 1, 1, eased);
                    yield return null;
                }

                feedbackImg.color = new Color(1, 1, 1, 1);
                yield return new WaitForSecondsRealtime(feedbackHoldTime);

                t = 0f;
                while (t < feedbackFadeOutTime)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / feedbackFadeOutTime);
                    float eased = Mathf.SmoothStep(1f, 0f, n);
                    feedbackImg.color = new Color(1, 1, 1, eased);
                    yield return null;
                }

                feedbackImg.gameObject.SetActive(false);
            }

            ResetFeedback();
        }

        private void ResetFeedback()
        {
            if (feedbackPerfect) feedbackPerfect.gameObject.SetActive(false);
            if (feedbackGood) feedbackGood.gameObject.SetActive(false);
            if (feedbackWrong) feedbackWrong.gameObject.SetActive(false);
        }

        #endregion

        #region SCORE & TIME

        public void UpdateScoreLabel(int score)
        {
            if (scoreValueText)
                scoreValueText.text = score.ToString("00000");
        }

        protected override void OnTimeUp()
        {
            timerRunning = false;

            if (KiqqiAppManager.Instance.Game.currentMiniGame is KiqqiBarbecueHeroManager mgr)
            {
                Debug.Log("[KiqqiBarbecueHeroView] Time up - waiting for final meat items.");
                mgr.NotifyTimeExpired();
            }
        }

        #endregion
    }
}
