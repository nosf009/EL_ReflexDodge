using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Pause menu overlay for gameplay.
    /// </summary>
    public class KiqqiPauseView : KiqqiUIView
    {

        [Header("Mute Button Visuals")]
        public Image muteIconImage;
        public Sprite soundOnSprite;
        public Sprite soundOffSprite;

        [Header("Buttons")]
        public Button resumeButton;
        public Button levelSelectButton;
        public Button restartButton;
        public Button muteButton;
        public Button tutorialButton;
        public Button exitButton;

        //[Header("Optional")]
        //public bool hasLevelSelect = true;

        private void OnEnable()
        {
            var app = KiqqiAppManager.Instance;

            // ---------- RESUME ----------
            if (resumeButton)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(() =>
                {
                    var app = KiqqiAppManager.Instance;
                    var game = app.Game;

                    Time.timeScale = 1f;
                    game.ResumeRequested = true;
                    game.State = KiqqiGameManager.GameState.Playing;

                    // Set transition type to Fade before showing
                    var pokerFaceView = app.UI.GetView<KiqqiPokerFaceView>();
                    if (pokerFaceView != null)
                    {
                        pokerFaceView.transitionType = TransitionType.Fade;
                        Debug.Log($"[PauseView] PokerFaceView transitionType set to: {pokerFaceView.transitionType}");
                    }

                    if (game.currentMiniGame != null)
                    {
                        var viewType = game.currentMiniGame.GetAssociatedViewType();
                        Debug.Log($"[KiqqiPauseView] Resuming mini-game '{game.currentMiniGame.displayName}'");
                        app.UI.ShowView(viewType);
                    }
                    else
                    {
                        Debug.LogWarning("[KiqqiPauseView] No current mini-game found. Falling back to KiqqiGameView.");
                        app.UI.ShowView<KiqqiGameView>();
                    }
                });
            }

            // ---------- LEVEL SELECT ----------
            if (levelSelectButton)
            {
                levelSelectButton.onClick.RemoveAllListeners();
                levelSelectButton.onClick.AddListener(() =>
                {
                    var app = KiqqiAppManager.Instance;
                    var pokerFaceView = app.UI.GetView<KiqqiPokerFaceView>();
                    if (pokerFaceView != null)
                    {
                        pokerFaceView.transitionType = KiqqiUIView.TransitionType.Fade;
                        Debug.Log($"[PauseView] PokerFaceView transitionType set to: {pokerFaceView.transitionType}");
                    }
                    var levels = app.Levels;

                    if (!levels.hasLevelSelect)
                    {
                        Debug.Log("[KiqqiPauseView] Level select disabled for this game.");
                        return;
                    }

                    Time.timeScale = 1f;
                    app.Game.ResumeRequested = false;
                    app.Game.RestartRequested = false;
                    app.Game.State = KiqqiGameManager.GameState.LevelSelect;

                    app.UI.ShowView<KiqqiLevelSelectView>();
                });

                // Show/hide based on LevelManager flag
                bool visible = KiqqiAppManager.Instance.Levels.hasLevelSelect;
                levelSelectButton.gameObject.SetActive(visible);
            }

            // ---------- RESTART ----------
            if (restartButton)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() =>
                {
                    var app = KiqqiAppManager.Instance;
                    var game = app.Game;

                    Time.timeScale = 1f;
                    game.ResumeRequested = false;
                    game.RestartRequested = true;

                    if (game.mainGameManager != null)
                    {
                        Debug.Log("[KiqqiPauseView] Restarting main mini-game.");
                        game.mainGameManager.ResetMiniGame();
                        game.StartMainGame();
                    }
                    else
                    {
                        Debug.LogWarning("[KiqqiPauseView] No main mini-game assigned!");
                    }

                });
            }


            if (muteButton)
            {
                muteButton.onClick.RemoveAllListeners();
                muteButton.onClick.AddListener(() =>
                {
                    var app = KiqqiAppManager.Instance;
                    app.Audio.ToggleMute();

                    UpdateMuteButtonVisual();

                    Debug.Log("[KiqqiPauseView] Audio mute toggled.");
                });

                // Set the initial image on start
                UpdateMuteButtonVisual();
            }



            // ---------- TUTORIAL ----------
            if (tutorialButton)
            {
                tutorialButton.onClick.RemoveAllListeners();
                tutorialButton.onClick.AddListener(() =>
                {
                    var app = KiqqiAppManager.Instance;
                    var gm = app.Game;

                    // Always resume time first
                    Time.timeScale = 1f;
                    gm.ResumeRequested = false;
                    gm.RestartRequested = false;

                    Debug.Log("[KiqqiPauseView] Tutorial selected from Pause menu.");

                    gm.StartTutorial();

                });
            }


            if (exitButton)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    ResumeGame(true);
                    KiqqiAppManager.Instance.Game.ReturnToMenu();
                    KiqqiAppManager.Instance.UI.ShowView<KiqqiMainMenuView>();
#else
                    Application.Quit();
#endif
                });
            }

        }

        private void OnDisable()
        {
            resumeButton?.onClick.RemoveAllListeners();
            levelSelectButton?.onClick.RemoveAllListeners();
            restartButton?.onClick.RemoveAllListeners();
            muteButton?.onClick.RemoveAllListeners();
            tutorialButton?.onClick.RemoveAllListeners();
            exitButton?.onClick.RemoveAllListeners();
        }

        private void UpdateMuteButtonVisual()
        {
            if (!muteIconImage)
                return;

            bool isMuted = PlayerPrefs.GetInt("audio_muted", 0) == 1;

            if (isMuted)
            {
                if (soundOffSprite)
                    muteIconImage.sprite = soundOffSprite;
            }
            else
            {
                if (soundOnSprite)
                    muteIconImage.sprite = soundOnSprite;
            }
        }

        private void ResumeTime()
        {
            if (Time.timeScale < 1f)
                Time.timeScale = 1f;
        }

        private IEnumerator SetStateAfterFrame()
        {
            yield return null;
            var app = KiqqiAppManager.Instance;
            app.Game.StartGame();
            Debug.Log("[KiqqiPauseView] Game state set to Playing after view activation.");
        }


        public override void OnShow()
        {
            base.OnShow();
            PauseGame();
            KiqqiAppManager.Instance.Game.State = KiqqiGameManager.GameState.Paused;
        }


        private void PauseGame()
        {
            Time.timeScale = 0.0001f;
            Debug.Log("[KiqqiPauseView] Game paused.");
        }

        private void ResumeGame(bool silent = false)
        {
            Time.timeScale = 1f;
            if (!silent)
                KiqqiAppManager.Instance.UI.ShowView<KiqqiGameView>();
            Debug.Log("[KiqqiPauseView] Game resumed.");
        }


        private IEnumerator ResumeAfterDelay(bool silent)
        {
            yield return new WaitForSecondsRealtime(0.25f); // small delay for fade transition

            if (!silent)
                KiqqiAppManager.Instance.UI.ShowView<KiqqiGameView>();

            Debug.Log("[KiqqiPauseView] Game resumed.");
        }
    }
}
