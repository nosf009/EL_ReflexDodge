using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial gameplay view — simplified version of the main gameplay UI.
    /// </summary>
    public class KiqqiTutorialGameView : KiqqiUIView
    {
        [Header("UI Elements")]
        //public Text timeLabelText;
        public Text timeValueText;
        //public Text scoreLabelText;
        public Text scoreValueText;
        public Button skipButton;

        private float elapsedTime;
        private bool running;

        private void OnEnable()
        {
            if (skipButton)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(() =>
                {
                    Debug.Log("[KiqqiTutorialGameView] Tutorial skipped.");
                    running = false;
                    KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
                });
            }

            ResetTutorialUI();
        }

        private void OnDisable()
        {
            if (skipButton)
            {
                skipButton.onClick.RemoveAllListeners();
            }
        }

        public override void OnShow()
        {
            base.OnShow();
            ResetTutorialUI();
            running = true;
        }

        private void Update()
        {
            if (!running) return;

            elapsedTime += Time.deltaTime;

            if (timeValueText)
                timeValueText.text = FormatTime(elapsedTime);

            if (scoreValueText)
                scoreValueText.text = Mathf.FloorToInt(elapsedTime * 5).ToString("00000"); // simulated score increment
        }

        private void ResetTutorialUI()
        {
            elapsedTime = 0f;
            running = false;

            if (timeValueText) timeValueText.text = "00:00";
            if (scoreValueText) scoreValueText.text = "00000";
        }

        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
