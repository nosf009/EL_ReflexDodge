using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiMainMenuView : KiqqiUIView
    {
        [Header("Buttons")]
        public Button playButton;
        public Button tutorialButton;
        public Button quitButton;

        private void OnEnable()
        {
            var app = KiqqiAppManager.Instance;
            var gm = app.Game;

            // --- PLAY BUTTON ---
            if (playButton)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() =>
                {
                    Debug.Log("[KiqqiMainMenuView] Play pressed.");

                    var levels = app.Levels;

                    if (levels != null && levels.hasLevelSelect)
                    {
                        Debug.Log("[KiqqiMainMenuView] Level Select enabled � opening LevelSelectView.");
                        app.UI.ShowView<KiqqiLevelSelectView>();
                        return;
                    }

                    // Directly start the main game via explicit reference
                    gm.StartMainGame();
                });
            }


            // --- TUTORIAL BUTTON ---
            if (tutorialButton)
            {
                tutorialButton.onClick.RemoveAllListeners();
                tutorialButton.onClick.AddListener(() =>
                {
                    Debug.Log("[KiqqiMainMenuView] Tutorial selected.");

                    gm.StartTutorial();
                });
            }


            // --- QUIT BUTTON ---
            if (quitButton)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    Application.OpenURL("about:blank");
#else
                    Application.Quit();
#endif
                });
            }
        }

        private void OnDisable()
        {
            playButton?.onClick.RemoveAllListeners();
            tutorialButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.RemoveAllListeners();
        }

    }
}
