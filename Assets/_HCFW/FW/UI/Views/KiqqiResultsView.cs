using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Results screen showing the final score and navigation buttons.
    /// </summary>
    public class KiqqiResultsView : KiqqiUIView
    {
        [Header("UI Elements")]
        public Text scoreValueText;   // dynamic value (raw number only)
        public Button playAgainButton;
        public Button menuButton;

        private void OnEnable()
        {
            if (playAgainButton)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(() =>
                {
                    var app = KiqqiAppManager.Instance;
                    var levels = app.Levels;
                    var game = app.Game;

                    levels.LoadCurrentLevel();     // refresh any level data

                    // Switch game state back to Playing and start the main game again
                    game.State = KiqqiGameManager.GameState.Playing;
                    game.StartMainGame();
                });
            }

            if (menuButton)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    var app = KiqqiAppManager.Instance;
                    app.Game.ReturnToMenu();
                    app.UI.ShowView<KiqqiMainMenuView>();
#else
                    Application.Quit();
#endif
                });
            }
        }

        private void OnDisable()
        {
            if (playAgainButton)
            {
                playAgainButton.onClick.RemoveAllListeners();
            }
            if (menuButton)
            {
                menuButton.onClick.RemoveAllListeners();
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            var finalScore = KiqqiAppManager.Instance.Game.CurrentScore;

            // Label is static, localized via KiqqiLocalizedText component (no need to modify)
            if (scoreValueText)
                scoreValueText.text = finalScore.ToString("00000"); // raw numeric format, e.g., 00045
        }
    }
}
