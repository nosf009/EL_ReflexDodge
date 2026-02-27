using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiPrizePickView : KiqqiGameViewBase
    {
        #region INSPECTOR

        [Header("Prize Pick UI Elements")]
        [SerializeField] private GameObject prizeOptionsContainer;
        [SerializeField] private GameObject prizeOptionTemplate;
        
        [Header("Top Prize Display")]
        [SerializeField] private GameObject topPrizePanel;
        [SerializeField] private Text topPrizeValueText;
        
        [Header("Choice Timer")]
        [SerializeField] private GameObject choiceTimerPanel;
        [SerializeField] private Image choiceTimerFillImage;
        [SerializeField] private Text choiceTimerText;
        
        [Header("Feedback")]
        [SerializeField] private GameObject correctFeedbackIcon;
        [SerializeField] private GameObject wrongFeedbackIcon;
        [SerializeField] private Text feedbackScoreText;
        [SerializeField] private Text comboStreakText;
        
        [Header("Animation Settings")]
        [SerializeField] private float choiceAppearDuration = 1.2f;
        [SerializeField] private float choiceAppearDelay = 0.15f;
        [SerializeField] private float feedbackDuration = 0.8f;

        #endregion

        #region STATE

        private List<GameObject> activeChoiceButtons = new List<GameObject>();
        private KiqqiPrizePickManager prizePickManager;
        private bool choicesActive = false;
        private float maxChoiceTime = 0f;

        public event Action<PrizeOption> OnPrizeSelected;

        #endregion

        #region LIFECYCLE

        public void SetManager(KiqqiPrizePickManager manager)
        {
            this.prizePickManager = manager;
        }

        public override void OnShow()
        {
            base.OnShow();

            if (prizeOptionTemplate != null)
            {
                prizeOptionTemplate.SetActive(false);
            }

            if (topPrizePanel != null)
            {
                topPrizePanel.SetActive(false);
            }

            if (choiceTimerPanel != null)
            {
                choiceTimerPanel.SetActive(false);
            }

            HideFeedback();
            ClearChoices();

            Debug.Log("[KiqqiPrizePickView] OnShow complete.");
        }

        protected override void OnCountdownFinished()
        {
            base.OnCountdownFinished();
            
            if (prizePickManager != null)
            {
                prizePickManager.StartMiniGame();
                prizePickManager.NotifyCountdownFinished();
            }
            else
            {
                Debug.LogError("[KiqqiPrizePickView] prizePickManager is null in OnCountdownFinished!");
            }

            if (pauseButton)
            {
                pauseButton.interactable = true;
            }
        }

        protected override void OnTimeUp()
        {
            base.OnTimeUp();
            prizePickManager?.NotifyTimeExpired();
        }

        public override void OnHide()
        {
            base.OnHide();
            ClearChoices();
            HideFeedback();
        }

        #endregion

        #region CHOICE DISPLAY

        public void ShowChoices(List<PrizeOption> options, int currentTopPrize, float timeLimit)
        {
            ClearChoices();

            maxChoiceTime = timeLimit;
            choicesActive = true;

            if (choiceTimerPanel != null)
            {
                choiceTimerPanel.SetActive(true);
            }

            if (currentTopPrize > 0 && topPrizePanel != null)
            {
                topPrizePanel.SetActive(true);
                UpdateTopPrize(currentTopPrize);
            }

            StartCoroutine(AnimateChoicesAppearance(options));
        }

        private IEnumerator AnimateChoicesAppearance(List<PrizeOption> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                CreateChoiceButton(options[i], i);
                yield return new WaitForSeconds(choiceAppearDelay);
            }

            Debug.Log($"[KiqqiPrizePickView] All {options.Count} choices displayed.");
        }

        private void CreateChoiceButton(PrizeOption option, int index)
        {
            if (prizeOptionTemplate == null || prizeOptionsContainer == null) return;

            GameObject choiceButton = Instantiate(prizeOptionTemplate, prizeOptionsContainer.transform);
            choiceButton.SetActive(true);

            Text valueText = choiceButton.GetComponentInChildren<Text>();
            if (valueText != null)
            {
                valueText.text = option.GetDisplayText();
            }

            Button button = choiceButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnChoiceButtonClicked(option));
            }

            StartCoroutine(AnimateChoiceAppear(choiceButton));

            activeChoiceButtons.Add(choiceButton);
        }

        private IEnumerator AnimateChoiceAppear(GameObject choiceObject)
        {
            CanvasGroup canvasGroup = choiceObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = choiceObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            choiceObject.transform.localScale = Vector3.one * 0.5f;

            float elapsed = 0f;

            while (elapsed < choiceAppearDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / choiceAppearDuration;

                canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                choiceObject.transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, progress);

                yield return null;
            }

            canvasGroup.alpha = 1f;
            choiceObject.transform.localScale = Vector3.one;
        }

        public void HideChoices()
        {
            choicesActive = false;

            if (choiceTimerPanel != null)
            {
                choiceTimerPanel.SetActive(false);
            }

            ClearChoices();
        }

        private void ClearChoices()
        {
            foreach (var choiceButton in activeChoiceButtons)
            {
                if (choiceButton != null)
                {
                    Destroy(choiceButton);
                }
            }

            activeChoiceButtons.Clear();
        }

        private void OnChoiceButtonClicked(PrizeOption selectedOption)
        {
            if (!choicesActive) return;

            choicesActive = false;

            Debug.Log($"[KiqqiPrizePickView] Choice clicked: {selectedOption.GetDisplayText()} = {selectedOption.calculatedValue}");

            SetChoicesInteractable(false);

            OnPrizeSelected?.Invoke(selectedOption);
        }

        private void SetChoicesInteractable(bool interactable)
        {
            foreach (var choiceButton in activeChoiceButtons)
            {
                Button button = choiceButton.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }

        #endregion

        #region CHOICE TIMER

        public void UpdateChoiceTimer(float remainingTime)
        {
            if (choiceTimerPanel == null || !choiceTimerPanel.activeSelf) return;

            if (choiceTimerText != null)
            {
                choiceTimerText.text = Mathf.CeilToInt(remainingTime).ToString();
            }

            if (choiceTimerFillImage != null)
            {
                choiceTimerFillImage.fillAmount = remainingTime / maxChoiceTime;
            }
        }

        #endregion

        #region TOP PRIZE

        public void UpdateTopPrize(int prizeValue)
        {
            if (topPrizeValueText != null)
            {
                topPrizeValueText.text = prizeValue.ToString();
            }
        }

        #endregion

        #region FEEDBACK

        public void ShowFeedback(bool isCorrect, int scoreChange, int comboStreak)
        {
            StartCoroutine(ShowFeedbackCoroutine(isCorrect, scoreChange, comboStreak));
        }

        private IEnumerator ShowFeedbackCoroutine(bool isCorrect, int scoreChange, int comboStreak)
        {
            if (correctFeedbackIcon != null)
            {
                correctFeedbackIcon.SetActive(isCorrect);
            }

            if (wrongFeedbackIcon != null)
            {
                wrongFeedbackIcon.SetActive(!isCorrect);
            }

            if (feedbackScoreText != null)
            {
                string sign = scoreChange >= 0 ? "+" : "";
                feedbackScoreText.text = $"{sign}{scoreChange}";
                feedbackScoreText.gameObject.SetActive(true);
            }

            if (comboStreakText != null)
            {
                if (comboStreak > 0)
                {
                    comboStreakText.text = $"COMBO x{comboStreak}";
                    comboStreakText.gameObject.SetActive(true);
                }
                else
                {
                    comboStreakText.gameObject.SetActive(false);
                }
            }

            if (isCorrect)
            {
                KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect");
            }
            else
            {
                KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
            }

            yield return new WaitForSeconds(feedbackDuration);

            HideFeedback();
        }

        private void HideFeedback()
        {
            if (correctFeedbackIcon != null)
            {
                correctFeedbackIcon.SetActive(false);
            }

            if (wrongFeedbackIcon != null)
            {
                wrongFeedbackIcon.SetActive(false);
            }

            if (feedbackScoreText != null)
            {
                feedbackScoreText.gameObject.SetActive(false);
            }

            if (comboStreakText != null)
            {
                comboStreakText.gameObject.SetActive(false);
            }
        }

        #endregion

        #region SCORE UPDATE

        public void UpdateScore(int score)
        {
            if (scoreValueText != null)
            {
                scoreValueText.text = score.ToString("00000");
            }
        }

        #endregion
    }
}
