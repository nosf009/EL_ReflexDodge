using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Kiqqi.Framework
{
    public class KiqqiFractionRushView : KiqqiGameViewBase
    {
        #region INSPECTOR

        [Header("Fraction Rush UI")]
        [SerializeField] private GameObject fractionButtonTemplate;
        [SerializeField] private Text sequenceTimerText;
        [SerializeField] private Image sequenceTimerFill;
        [SerializeField] private GameObject comboIndicator;
        [SerializeField] private Text comboText;

        [Header("Feedback")]
        [SerializeField] private GameObject correctFeedback;
        [SerializeField] private GameObject wrongFeedback;
        [SerializeField] private GameObject timeoutFeedback;
        [SerializeField] private GameObject completeFeedback;

        #endregion

        #region STATE

        private KiqqiFractionRushManager manager;
        private List<GameObject> fractionButtons = new List<GameObject>();
        private Transform currentContainer;

        #endregion

        #region INITIALIZATION

        public void SetManager(KiqqiFractionRushManager m)
        {
            manager = m;
        }

        protected override void OnCountdownFinished()
        {
            base.OnCountdownFinished();

            if (sequenceTimerText) sequenceTimerText.text = "";
            if (sequenceTimerFill) sequenceTimerFill.fillAmount = 1f;
            if (comboIndicator) comboIndicator.SetActive(false);

            HideAllFeedback();

            if (manager != null)
            {
                manager.StartMiniGame();
                manager.OnCountdownFinished();
            }

            if (pauseButton)
            {
                pauseButton.interactable = true;
            }

            Debug.Log("[KiqqiFractionRushView] Countdown finished, game starting");
        }

        #endregion

        #region VIEW LIFECYCLE

        private void LateUpdate()
        {
            if (manager != null)
            {
                manager.TickMiniGame();
            }
        }

        public override void OnShow()
        {
            if (pauseButton)
                pauseButton.interactable = false;

            KiqqiGameManager game = KiqqiAppManager.Instance?.Game;
            if (game != null && game.ResumeRequested && game.currentMiniGame is KiqqiFractionRushManager mgr)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(mgr));
                return;
            }

            base.OnShow();
        }

        private IEnumerator HandleResumeFadeIn(KiqqiFractionRushManager mgr)
        {
            yield return new WaitForSeconds(0.1f);

            mgr.ResumeFromPause(this);
            ResumeTimer();

            if (pauseButton)
                pauseButton.interactable = true;

            Debug.Log("[KiqqiFractionRushView] Resumed from pause");
        }

        public override void OnHide()
        {
            base.OnHide();
            ClearSequence();
        }

        #endregion

        #region SEQUENCE DISPLAY

        public void DisplaySequence(List<Fraction> fractions)
        {
            ClearSequence();

            if (fractions == null || fractions.Count == 0)
            {
                Debug.LogWarning("[FractionRushView] DisplaySequence called with empty/null fractions");
                return;
            }

            FractionRushDifficultyConfig config = GetCurrentConfig();
            currentContainer = config.fractionContainer;

            if (currentContainer == null)
            {
                Debug.LogError("[FractionRushView] No fraction container assigned for current difficulty!");
                return;
            }

            int childCount = currentContainer.childCount;
            if (childCount < fractions.Count)
            {
                Debug.LogWarning($"[FractionRushView] Container has {childCount} positions but need {fractions.Count}. Using available positions.");
            }

            for (int i = 0; i < fractions.Count; i++)
            {
                GameObject buttonObj = CreateFractionButton(fractions[i], i);

                if (buttonObj == null)
                {
                    Debug.LogError($"[DisplaySequence] Failed to create button {i}");
                    continue;
                }

                if (i < childCount)
                {
                    Transform positionTransform = currentContainer.GetChild(i);
                    buttonObj.transform.SetParent(positionTransform, false);
                    buttonObj.transform.localPosition = Vector3.zero;
                    buttonObj.transform.localScale = Vector3.one;
                }
                else
                {
                    buttonObj.transform.SetParent(currentContainer, false);
                }

                fractionButtons.Add(buttonObj);
            }

            Debug.Log($"[DisplaySequence] Created {fractionButtons.Count} fraction buttons");
        }

        private GameObject CreateFractionButton(Fraction fraction, int index)
        {
            if (fractionButtonTemplate == null)
            {
                Debug.LogError("[CreateFractionButton] fractionButtonTemplate is not assigned! Please assign a button template in the inspector.");
                return null;
            }

            GameObject buttonObj = Instantiate(fractionButtonTemplate);
            buttonObj.name = $"FractionButton_{index}";
            buttonObj.SetActive(true);

            Text text = buttonObj.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = fraction.ToString();
            }
            else
            {
                Debug.LogWarning($"[CreateFractionButton] No Text component found in template children for button {index}");
            }

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                int capturedIndex = index;
                button.onClick.AddListener(() => OnFractionButtonClicked(capturedIndex));
            }
            else
            {
                Debug.LogWarning($"[CreateFractionButton] No Button component found on template for button {index}");
            }

            return buttonObj;
        }

        public void ClearSequence()
        {
            foreach (GameObject button in fractionButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }

            fractionButtons.Clear();
            currentContainer = null;

            HideAllFeedback();

            Debug.Log("[ClearSequence] Cleared all fraction buttons");
        }

        #endregion

        #region TIMER DISPLAY

        public void UpdateSequenceTimer(float remaining, float total)
        {
            if (sequenceTimerText != null)
            {
                sequenceTimerText.text = Mathf.CeilToInt(remaining).ToString();
            }

            if (sequenceTimerFill != null && total > 0f)
            {
                sequenceTimerFill.fillAmount = Mathf.Clamp01(remaining / total);
            }
        }

        #endregion

        #region FEEDBACK

        public void ShowFractionCorrect(int fractionIndex)
        {
            if (fractionIndex >= 0 && fractionIndex < fractionButtons.Count)
            {
                GameObject button = fractionButtons[fractionIndex];
                StartCoroutine(PlayFlipEffect(button, true));
            }
        }

        public void ShowFractionWrong(int fractionIndex)
        {
            if (fractionIndex >= 0 && fractionIndex < fractionButtons.Count)
            {
                GameObject button = fractionButtons[fractionIndex];
                StartCoroutine(PlayShakeEffect(button));
            }

            StartCoroutine(ShowFeedbackTemporary(wrongFeedback, 0.5f));
        }

        public void ShowSequenceComplete(bool hasCombo)
        {
            if (hasCombo && comboIndicator != null)
            {
                StartCoroutine(ShowFeedbackTemporary(comboIndicator, 1f));
            }

            StartCoroutine(ShowFeedbackTemporary(completeFeedback, 0.8f));
        }

        public void ShowSequenceTimeout()
        {
            StartCoroutine(ShowFeedbackTemporary(timeoutFeedback, 1f));
        }

        private void HideAllFeedback()
        {
            if (correctFeedback) correctFeedback.SetActive(false);
            if (wrongFeedback) wrongFeedback.SetActive(false);
            if (timeoutFeedback) timeoutFeedback.SetActive(false);
            if (completeFeedback) completeFeedback.SetActive(false);
            if (comboIndicator) comboIndicator.SetActive(false);
        }

        private IEnumerator ShowFeedbackTemporary(GameObject feedbackObj, float duration)
        {
            if (feedbackObj == null) yield break;

            feedbackObj.SetActive(true);
            yield return new WaitForSeconds(duration);
            feedbackObj.SetActive(false);
        }

        #endregion

        #region EFFECTS

        private IEnumerator PlayFlipEffect(GameObject target, bool correct)
        {
            if (target == null) yield break;

            Transform t = target.transform;
            Vector3 originalScale = t.localScale;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration / 2f);
                t.localScale = new Vector3(originalScale.x * (1f - progress), originalScale.y, originalScale.z);
                yield return null;
            }

            Image img = target.GetComponent<Image>();
            if (img != null)
            {
                img.color = correct ? new Color(0.5f, 1f, 0.5f) : Color.white;
            }

            Button btn = target.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = false;
            }

            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration / 2f);
                t.localScale = new Vector3(originalScale.x * progress, originalScale.y, originalScale.z);
                yield return null;
            }

            t.localScale = originalScale;
        }

        private IEnumerator PlayShakeEffect(GameObject target)
        {
            if (target == null) yield break;

            Transform t = target.transform;
            Vector3 originalPos = t.localPosition;
            float duration = 0.3f;
            float magnitude = 10f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = originalPos.x + Random.Range(-magnitude, magnitude);
                float y = originalPos.y + Random.Range(-magnitude, magnitude);
                t.localPosition = new Vector3(x, y, originalPos.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            t.localPosition = originalPos;
        }

        #endregion

        #region INPUT HANDLING

        private void OnFractionButtonClicked(int index)
        {
            Debug.Log($"[FractionRushView] Button {index} clicked");

            if (manager != null)
            {
                manager.OnFractionClicked(index);
            }
        }

        #endregion

        #region HELPERS

        private FractionRushDifficultyConfig GetCurrentConfig()
        {
            if (manager == null)
            {
                Debug.LogError("[GetCurrentConfig] Manager is null!");
                return default;
            }

            KiqqiAppManager app = KiqqiAppManager.Instance;
            if (app == null || app.Levels == null)
            {
                Debug.LogError("[GetCurrentConfig] KiqqiAppManager or Levels is null!");
                return default;
            }

            KiqqiFractionRushLevelManager levelManager = FindFirstObjectByType<KiqqiFractionRushLevelManager>();
            if (levelManager == null)
            {
                Debug.LogError("[GetCurrentConfig] Could not find KiqqiFractionRushLevelManager!");
                return default;
            }

            return levelManager.GetDifficultyConfig(app.Levels.currentLevel);
        }

        #endregion
    }
}
