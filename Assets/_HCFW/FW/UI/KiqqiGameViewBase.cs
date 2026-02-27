// Assets/_HCFW/FW/Core/KiqqiGameViewBase.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Base class for all gameplay UI views (shared HUD: timer, score, pause).
    /// </summary>
    public class KiqqiGameViewBase : KiqqiUIView
    {
        [Header("Shared HUD Elements")]
        public Text timeValueText;
        public Text scoreValueText;
        public Button pauseButton;

        [Header("Countdown")]
        public Text countdownLabel;
        public float countdownSeconds = 3f;

        protected bool timerRunning;
        protected float remainingTime;
        public float RemainingTime => remainingTime;
        protected KiqqiGameManager gameManager;
        protected KiqqiLevelManager levelManager;

        public override void OnShow()
        {
            base.OnShow();

            gameManager = KiqqiAppManager.Instance.Game;
            levelManager = KiqqiAppManager.Instance.Levels;

            SetupPauseButton();

            // Reset time and score
            remainingTime = levelManager.GetLevelTime();
            gameManager.CurrentScore = 0;

            UpdateScoreUI();
            UpdateTimeUI();
            StartCoroutine(StartCountdown());
        }

        private void SetupPauseButton()
        {
            if (pauseButton)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(() =>
                {
                    KiqqiAppManager.Instance.UI.ShowView<KiqqiPauseView>();
                });
            }
        }

        private IEnumerator StartCountdown()
        {
            if (countdownLabel)
            {
                countdownLabel.transform.parent.gameObject.SetActive(true);

                for (int i = (int)countdownSeconds; i > 0; i--)
                {
                    countdownLabel.text = i.ToString();

                    // Play countdown tick sound
                    KiqqiAppManager.Instance.Audio.PlaySfx("countdown");

                    yield return new WaitForSecondsRealtime(1f);
                }

                // Final cue - start!
                KiqqiAppManager.Instance.Audio.PlaySfx("levelstart");

                countdownLabel.text = "";
                countdownLabel.transform.parent.gameObject.SetActive(false);
            }

            timerRunning = true;
            OnCountdownFinished();
        }


        protected virtual void OnCountdownFinished()
        {
            // Override in child view if you need to trigger logic start
        }

        protected void UpdateScoreUI()
        {
            if (scoreValueText)
                scoreValueText.text = gameManager.CurrentScore.ToString("0000");
        }

        protected void UpdateTimeUI()
        {
            if (timeValueText)
                timeValueText.text = FormatTime(remainingTime);
        }

        private void Update()
        {
            if (!timerRunning) return;

            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                timerRunning = false;
                OnTimeUp();
            }

            UpdateTimeUI();
        }

        protected virtual void OnTimeUp()
        {
            // Optional override per mini-game
        }

        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        public void AddScore(int value)
        {
            gameManager.AddScore(value);
            UpdateScoreUI();
        }

        public void StopTimer()
        {
            timerRunning = false;
        }

        public void ResumeTimer()
        {
            timerRunning = true;
        }
    }
}
