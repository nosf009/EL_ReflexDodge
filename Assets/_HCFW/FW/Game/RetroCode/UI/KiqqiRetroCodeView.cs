using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiRetroCodeView : KiqqiGameViewBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Display")]
        public Text questionNumberText;
        public Image operationTypeImage;

        [Header("Operation Sprites")]
        public Sprite squareOperationSprite;
        public Sprite squareRootOperationSprite;

        [Header("Button Layout Templates")]
        public Transform buttonLayout4;
        public Transform buttonLayout6;
        public Transform buttonLayout8;
        public Transform buttonLayout10;
        public Transform buttonLayout12;

        [Header("Feedback")]
        public Image feedbackCorrect;
        public Image feedbackWrong;
        [Tooltip("Fade in time (seconds)")]
        public float fadeInTime = 0.1f;
        [Tooltip("Hold at full opacity time (seconds)")]
        public float holdTime = 0.25f;
        [Tooltip("Fade out time (seconds)")]
        public float fadeOutTime = 0.1f;

        [Header("Background")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 1f;

        #endregion

        #region RUNTIME STATE

        private readonly List<Button> activeButtons = new();
        private MathQuestion currentQuestion;
        private Transform currentActiveLayout;

        #endregion

        #region VIEW INITIALIZATION

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiRetroCodeManager mgr)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(mgr));
                return;
            }

            DeactivateAllLayouts();
            ResetFeedback();

            if (questionNumberText) questionNumberText.text = "";
            if (operationTypeImage) operationTypeImage.gameObject.SetActive(false);

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

        private IEnumerator HandleResumeFadeIn(KiqqiRetroCodeManager mgr)
        {
            if (currentActiveLayout)
                currentActiveLayout.gameObject.SetActive(true);

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

            yield return new WaitForSecondsRealtime(0.2f);

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiRetroCodeManager mgr)
            {
                mgr.StartMiniGame();
            }

            timerRunning = true;
        }

        #endregion

        #region QUESTION DISPLAY

        public void DisplayQuestion(MathQuestion question, int buttonCount)
        {
            currentQuestion = question;

            if (questionNumberText)
                questionNumberText.text = question.displayNumber.ToString();

            if (operationTypeImage)
            {
                operationTypeImage.sprite = question.operationType == MathOperationType.Square
                    ? squareOperationSprite
                    : squareRootOperationSprite;
                operationTypeImage.gameObject.SetActive(true);
            }

            ActivateButtonLayout(question, buttonCount);
        }

        private void ActivateButtonLayout(MathQuestion question, int buttonCount)
        {
            DeactivateAllLayouts();

            Transform targetLayout = buttonCount switch
            {
                4 => buttonLayout4,
                6 => buttonLayout6,
                8 => buttonLayout8,
                10 => buttonLayout10,
                12 => buttonLayout12,
                _ => buttonLayout4
            };

            if (targetLayout == null)
            {
                Debug.LogError($"[KiqqiRetroCodeView] Button layout for {buttonCount} buttons is not assigned!");
                return;
            }

            currentActiveLayout = targetLayout;
            targetLayout.gameObject.SetActive(true);

            CollectButtonsFromLayout(targetLayout);
            AssignAnswersToButtons(question);

            if (pauseButton)
                pauseButton.interactable = true;
        }

        private void CollectButtonsFromLayout(Transform layout)
        {
            activeButtons.Clear();

            for (int i = 0; i < layout.childCount; i++)
            {
                var btn = layout.GetChild(i).GetComponent<Button>();
                if (btn)
                {
                    activeButtons.Add(btn);
                }
            }
        }

        private void AssignAnswersToButtons(MathQuestion question)
        {
            var allAnswers = new List<int> { question.correctAnswer };
            allAnswers.AddRange(question.wrongAnswers);

            for (int i = allAnswers.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (allAnswers[i], allAnswers[j]) = (allAnswers[j], allAnswers[i]);
            }

            for (int i = 0; i < activeButtons.Count && i < allAnswers.Count; i++)
            {
                int answer = allAnswers[i];
                var btn = activeButtons[i];

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    var mgr = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiRetroCodeManager;
                    mgr?.HandleAnswerSelected(answer);
                });

                var txt = btn.GetComponentInChildren<Text>();
                if (txt)
                    txt.text = answer.ToString();

                btn.interactable = true;
            }
        }

        #endregion

        #region FEEDBACK

        public void ShowFeedback(bool correct, int scoreChange, int comboStreak)
        {
            StartCoroutine(AnimateFeedback(correct));
        }

        private IEnumerator AnimateFeedback(bool correct)
        {
            SetButtonsInteractable(false);

            var img = correct ? feedbackCorrect : feedbackWrong;
            if (img)
            {
                img.gameObject.SetActive(true);
                img.color = new Color(1, 1, 1, 0);

                float t = 0f;
                while (t < fadeInTime)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / fadeInTime);
                    float eased = Mathf.SmoothStep(0f, 1f, n);
                    img.color = new Color(1, 1, 1, eased);
                    yield return null;
                }

                img.color = new Color(1, 1, 1, 1);
                yield return new WaitForSecondsRealtime(holdTime);

                t = 0f;
                while (t < fadeOutTime)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / fadeOutTime);
                    float eased = Mathf.SmoothStep(1f, 0f, n);
                    img.color = new Color(1, 1, 1, eased);
                    yield return null;
                }

                img.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSecondsRealtime(fadeInTime + holdTime + fadeOutTime);
            }

            ResetFeedback();

            var gm = KiqqiAppManager.Instance.Game;
            if (gm.currentMiniGame is KiqqiRetroCodeManager mgr)
            {
                mgr.TriggerNextQuestionAfterFeedback();
            }

            SetButtonsInteractable(true);
        }

        private void ResetFeedback()
        {
            if (feedbackCorrect) feedbackCorrect.gameObject.SetActive(false);
            if (feedbackWrong) feedbackWrong.gameObject.SetActive(false);
        }

        #endregion

        #region BUTTON MANAGEMENT

        private void DeactivateAllLayouts()
        {
            if (buttonLayout4) buttonLayout4.gameObject.SetActive(false);
            if (buttonLayout6) buttonLayout6.gameObject.SetActive(false);
            if (buttonLayout8) buttonLayout8.gameObject.SetActive(false);
            if (buttonLayout10) buttonLayout10.gameObject.SetActive(false);
            if (buttonLayout12) buttonLayout12.gameObject.SetActive(false);

            activeButtons.Clear();
            currentActiveLayout = null;
        }

        private void SetButtonsInteractable(bool state)
        {
            foreach (var btn in activeButtons)
            {
                if (btn) btn.interactable = state;
            }
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

            if (KiqqiAppManager.Instance.Game.currentMiniGame is KiqqiRetroCodeManager mgr)
            {
                Debug.Log("[KiqqiRetroCodeView] Time up - waiting for final answer.");
                mgr.NotifyTimeExpired();
            }
        }

        #endregion
    }
}
