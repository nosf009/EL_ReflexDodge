using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Gameplay view: displays time, score, and gameplay buttons.
    /// </summary>
    public class KiqqiGameView : KiqqiUIView
    {
        [Header("UI Elements")]
        public Button pauseButton;
        public Button addScoreButton;
        public Button finishButton;

        [Header("Labels")]
        public Text timeValueText;
        public Text scoreValueText;
        [Header("Countdown")]
        public Text countdownLabel;

        //private float elapsedTime;
        private bool timerRunning;
        private float remainingTime;

        public override void Awake()
        {
            base.Awake();
            if (timeValueText) timeValueText.text = "00:00";
            if (scoreValueText) scoreValueText.text = "00000";
        }

        private IEnumerator CountdownAndStart()
        {
            if (countdownLabel)
            {
                countdownLabel.transform.parent.gameObject.SetActive(true);
                countdownLabel.text = "3";
            }

            SetButtonsInteractable(false);

            for (int i = 3; i > 0; i--)
            {
                if (countdownLabel)
                    countdownLabel.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }

            if (countdownLabel)
            {
                countdownLabel.text = "";
                countdownLabel.transform.parent.gameObject.SetActive(false);
            }

            // Brighten HUD (optional)
            //SetHudDimmed(false);

            SetButtonsInteractable(true);

            // Start the timer now (will count down from full time)
            timerRunning = true;
            Debug.Log("[KiqqiGameView] Countdown finished. Gameplay active.");
        }


        private void SetButtonsInteractable(bool state)
        {
            if (pauseButton) pauseButton.interactable = state;
            if (addScoreButton) addScoreButton.interactable = state;
            if (finishButton) finishButton.interactable = state;
        }


        private void Start()
        {
            var game = KiqqiAppManager.Instance.Game;

            if (pauseButton)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(() =>
                {
                    Debug.Log("[KiqqiGameView] Pause pressed.");
                    KiqqiAppManager.Instance.UI.ShowView<KiqqiPauseView>();
                });
            }

            if (addScoreButton)
            {
                addScoreButton.onClick.RemoveAllListeners();
                addScoreButton.onClick.AddListener(() =>
                {
                    var levels = KiqqiAppManager.Instance.Levels;
                    game.AddScore(100);
                    UpdateScore();
                    //OnFinishPressed();
                });
            }

            if (finishButton)
            {
                finishButton.onClick.RemoveAllListeners();
                finishButton.onClick.AddListener(OnFinishPressed);
            }

            UpdateScore();
            //ResetTimer();
        }

        public override void OnShow()
        {
            base.OnShow();

            var app = KiqqiAppManager.Instance;
            var game = app.Game;
            var levels = app.Levels;

            // 1) Forced fresh start (Restart)
            if (game.RestartRequested)
            {
                game.RestartRequested = false;   // consume
                timerRunning = false;

                remainingTime = levels.GetLevelTime();
                if (timeValueText) timeValueText.text = FormatTime(remainingTime);
                if (scoreValueText) scoreValueText.text = "00000";

                game.CurrentScore = 0;
                game.State = KiqqiGameManager.GameState.Playing;

                StartCoroutine(CountdownAndStart());
                return;
            }

            // 2) Resume from Pause (no countdown, continue time)
            if (game.ResumeRequested || game.State == KiqqiGameManager.GameState.Paused)
            {
                game.ResumeRequested = false;    // consume
                game.State = KiqqiGameManager.GameState.Playing;

                timerRunning = true;
                SetButtonsInteractable(true);
                if (countdownLabel) countdownLabel.gameObject.SetActive(false);

                Debug.Log("[KiqqiGameView] Resumed from pause, continuing timer.");
                return;
            }

            // 3) Fresh entry (from Main Menu or Level Select)
            timerRunning = false;
            remainingTime = levels.GetLevelTime();
            if (timeValueText) timeValueText.text = FormatTime(remainingTime);
            if (scoreValueText) scoreValueText.text = "00000";

            game.CurrentScore = 0;
            game.State = KiqqiGameManager.GameState.Playing;

            var diff = levels.GetCurrentDifficulty();
            Debug.Log($"[KiqqiGameView] Starting Level {levels.currentLevel}, Difficulty={diff}, Time={levels.GetLevelTime()}");

            //var def = levels.GetCurrentLevelDef();
            //Debug.Log($"[KiqqiGameView] Starting Level {levels.currentLevel}, Goal={def.goalScore}, Time={def.timeLimit}");

            StartCoroutine(CountdownAndStart());
        }



        private void Update()
        {
            if (!timerRunning) return;

            if (!timerRunning) return;

            // decrease time
            remainingTime -= Time.deltaTime;

            // clamp at zero
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                timerRunning = false;

                // Optional: auto-end level when time runs out
                Debug.Log("[KiqqiGameView] Time up!");
                // OnFinishPressed(); // uncomment if you want automatic end
            }

            if (timeValueText)
                timeValueText.text = FormatTime(remainingTime);

        }

        private void OnFinishPressed()
        {
            timerRunning = false;

            var app = KiqqiAppManager.Instance;

            // Simulate win for testing (or call from TTT Manager normally)
            app.Game.EndGame(true);

            Debug.Log($"[KiqqiGameView] Level {app.Levels.currentLevel} completed (diff={app.Levels.GetCurrentDifficulty()}).");

            app.UI.ShowView<KiqqiResultsView>();
        }


        private void UpdateScore()
        {
            var game = KiqqiAppManager.Instance.Game;
            if (scoreValueText)
                scoreValueText.text = game.CurrentScore.ToString("00000");
        }

        private void ResetTimer()
        {
            //elapsedTime = 0f;
            timerRunning = false;
            if (timeValueText)
                timeValueText.text = "00:00";
        }

        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
