using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiNeuronGraphView : KiqqiGameViewBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Feedback Elements")]
        [SerializeField] private GameObject feedbackCorrect;
        [SerializeField] private GameObject feedbackWrong;
        [SerializeField] private float feedbackDuration = 0.5f;

        [Header("Background")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 0.3f;

        #endregion

        #region RUNTIME STATE

        private KiqqiNeuronGraphManager neuronGraphManager;

        #endregion

        #region VIEW INITIALIZATION

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiNeuronGraphManager mgr)
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

        public override void OnHide()
        {
            base.OnHide();

            neuronGraphManager = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiNeuronGraphManager;
            if (neuronGraphManager != null)
            {
                neuronGraphManager.ResetMiniGame();
            }
        }

        #endregion

        #region BACKGROUND FADE & RESUME

        private IEnumerator HandleResumeFadeIn(KiqqiNeuronGraphManager mgr)
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
                    bgCol.a = eased;
                    gameplayBackground.color = bgCol;
                    yield return null;
                }

                bgCol.a = 1f;
                gameplayBackground.color = bgCol;
            }

            timerRunning = true;
            if (pauseButton) pauseButton.interactable = true;
        }

        protected override void OnCountdownFinished()
        {
            base.OnCountdownFinished();

            if (gameplayBackground)
            {
                gameplayBackground.gameObject.SetActive(true);
                StartCoroutine(FadeInBackground());
            }

            neuronGraphManager = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiNeuronGraphManager;
            if (neuronGraphManager != null)
            {
                neuronGraphManager.StartMiniGame();
                neuronGraphManager.OnCountdownFinished();
            }

            if (pauseButton)
                pauseButton.interactable = true;
        }

        private IEnumerator FadeInBackground()
        {
            float t = 0f;
            Color bgCol = gameplayBackground.color;
            bgCol.a = 0f;
            gameplayBackground.color = bgCol;

            while (t < backgroundFadeDuration)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / backgroundFadeDuration);
                float eased = 1f - Mathf.Pow(1f - n, 4f);
                bgCol.a = eased;
                gameplayBackground.color = bgCol;
                yield return null;
            }

            bgCol.a = 1f;
            gameplayBackground.color = bgCol;
        }

        #endregion

        #region FEEDBACK

        public void ShowFeedback(bool correct)
        {
            if (correct && feedbackCorrect)
            {
                StartCoroutine(ShowFeedbackCoroutine(feedbackCorrect));
            }
            else if (!correct && feedbackWrong)
            {
                StartCoroutine(ShowFeedbackCoroutine(feedbackWrong));
            }
        }

        private IEnumerator ShowFeedbackCoroutine(GameObject feedbackObject)
        {
            feedbackObject.SetActive(true);

            CanvasGroup cg = feedbackObject.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = feedbackObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;

            float halfDuration = feedbackDuration * 0.5f;
            float t = 0f;

            while (t < halfDuration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(t / halfDuration);
                yield return null;
            }

            cg.alpha = 1f;

            t = 0f;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / halfDuration);
                yield return null;
            }

            cg.alpha = 0f;
            feedbackObject.SetActive(false);
        }

        private void ResetFeedback()
        {
            if (feedbackCorrect) feedbackCorrect.SetActive(false);
            if (feedbackWrong) feedbackWrong.SetActive(false);
        }

        #endregion

        #region TIMER OVERRIDE

        protected override void OnTimeUp()
        {
            base.OnTimeUp();

            neuronGraphManager = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiNeuronGraphManager;
            if (neuronGraphManager != null)
            {
                neuronGraphManager.OnTimeUp();
            }
        }

        #endregion
    }
}
