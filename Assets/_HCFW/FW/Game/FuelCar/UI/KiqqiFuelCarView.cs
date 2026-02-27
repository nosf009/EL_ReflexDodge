using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiFuelCarView : KiqqiGameViewBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Fuel Buttons")]
        [Tooltip("Fuel button container that holds all fuel selection buttons")]
        public Transform fuelButtonContainer;
        [Tooltip("Fuel button prefab")]
        public GameObject fuelButtonPrefab;

        [Header("Feedback")]
        [Tooltip("Correct fuel feedback image")]
        public Image feedbackCorrect;
        [Tooltip("Wrong fuel feedback image")]
        public Image feedbackWrong;
        [Tooltip("Feedback fade in time (seconds)")]
        public float feedbackFadeInTime = 0.15f;
        [Tooltip("Feedback hold time (seconds)")]
        public float feedbackHoldTime = 0.25f;
        [Tooltip("Feedback fade out time (seconds)")]
        public float feedbackFadeOutTime = 0.15f;

        [Header("Background")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 0.3f;

        [Header("Wait Indicator")]
        [Tooltip("Wait indicator prefab to show on cars")]
        public GameObject waitIndicatorPrefab;

        #endregion

        #region RUNTIME STATE

        private Button[] fuelButtons;
        private Image[] fuelButtonImages;
        private KiqqiFuelCarManager manager;
        private int selectedButtonIndex = -1;

        private Color normalButtonColor = Color.white;
        private Color selectedButtonColor = new Color(1f, 1f, 0.5f);

        #endregion

        #region VIEW INITIALIZATION

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            manager = game.currentMiniGame as KiqqiFuelCarManager;

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && manager != null)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(manager));
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

        private IEnumerator HandleResumeFadeIn(KiqqiFuelCarManager mgr)
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
            if (gm?.currentMiniGame is KiqqiFuelCarManager mgr)
            {
                mgr.StartMiniGame();
            }

            timerRunning = true;
        }

        #endregion

        #region FUEL BUTTONS SETUP

        public void SetupFuelButtons(int count)
        {
            if (fuelButtonContainer == null || fuelButtonPrefab == null)
            {
                Debug.LogError("[KiqqiFuelCarView] Fuel button container or prefab not assigned!");
                return;
            }

            ClearFuelButtons();

            fuelButtons = new Button[count];
            fuelButtonImages = new Image[count];

            for (int i = 0; i < count; i++)
            {
                GameObject buttonObj = Instantiate(fuelButtonPrefab, fuelButtonContainer);
                buttonObj.SetActive(true);

                Button btn = buttonObj.GetComponent<Button>();
                Image btnImage = buttonObj.GetComponent<Image>();

                if (btn != null)
                {
                    btn.enabled = true;
                    
                    FuelType fuelType = (FuelType)i;
                    
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnFuelButtonPressed(fuelType));

                    if (btnImage != null && manager != null)
                    {
                        btnImage.enabled = true;
                        btnImage.color = manager.GetFuelColor(fuelType);
                    }

                    fuelButtons[i] = btn;
                    fuelButtonImages[i] = btnImage;
                }
            }

            selectedButtonIndex = -1;

            Debug.Log($"[KiqqiFuelCarView] Setup {count} fuel buttons.");
        }

        private void ClearFuelButtons()
        {
            if (fuelButtonContainer == null) return;

            for (int i = fuelButtonContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = fuelButtonContainer.GetChild(i);
                if (child.gameObject != fuelButtonPrefab)
                {
                    Destroy(child.gameObject);
                }
            }

            fuelButtons = null;
            fuelButtonImages = null;
            selectedButtonIndex = -1;
        }

        private void OnFuelButtonPressed(FuelType fuelType)
        {
            if (manager != null)
            {
                manager.OnFuelButtonPressed(fuelType);
            }
        }

        public void UpdateFuelButtonSelection(FuelType selectedFuel)
        {
            int selectedIndex = (int)selectedFuel;

            for (int i = 0; i < fuelButtons.Length; i++)
            {
                if (fuelButtonImages[i] != null)
                {
                    if (i == selectedIndex)
                    {
                        fuelButtonImages[i].transform.localScale = Vector3.one * 1.2f;
                        selectedButtonIndex = i;
                    }
                    else
                    {
                        fuelButtonImages[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        public void ClearFuelButtonSelection()
        {
            for (int i = 0; i < fuelButtons.Length; i++)
            {
                if (fuelButtonImages[i] != null)
                {
                    fuelButtonImages[i].transform.localScale = Vector3.one;
                }
            }
            selectedButtonIndex = -1;
        }

        #endregion

        #region FEEDBACK DISPLAY

        public void ShowFuelFeedback(Vector3 position, bool correct)
        {
            StartCoroutine(AnimateFuelFeedback(position, correct));
        }

        private IEnumerator AnimateFuelFeedback(Vector3 position, bool correct)
        {
            Image feedbackImg = correct ? feedbackCorrect : feedbackWrong;

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
            if (feedbackCorrect) feedbackCorrect.gameObject.SetActive(false);
            if (feedbackWrong) feedbackWrong.gameObject.SetActive(false);
        }

        #endregion

        #region CAR WAIT INDICATOR

        public void UpdateCarWaitIndicator(GameObject carInstance, float normalizedWaitTime)
        {
            if (carInstance == null) return;

            Transform indicatorTransform = carInstance.transform.Find("WaitIndicator");
            
            if (indicatorTransform == null && waitIndicatorPrefab != null)
            {
                GameObject indicator = Instantiate(waitIndicatorPrefab, carInstance.transform);
                indicator.name = "WaitIndicator";
                indicatorTransform = indicator.transform;
            }

            if (indicatorTransform != null)
            {
                var fillImage = indicatorTransform.GetComponentInChildren<Image>();
                if (fillImage != null)
                {
                    fillImage.fillAmount = normalizedWaitTime;
                    
                    if (normalizedWaitTime < 0.5f)
                        fillImage.color = Color.green;
                    else if (normalizedWaitTime < 0.75f)
                        fillImage.color = Color.yellow;
                    else
                        fillImage.color = Color.red;
                }
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

            if (KiqqiAppManager.Instance.Game.currentMiniGame is KiqqiFuelCarManager mgr)
            {
                Debug.Log("[KiqqiFuelCarView] Time up - waiting for final cars.");
                mgr.NotifyTimeExpired();
            }
        }

        #endregion
    }
}
