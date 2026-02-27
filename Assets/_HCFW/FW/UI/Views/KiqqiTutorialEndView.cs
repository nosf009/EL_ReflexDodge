using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// End-of-tutorial screen. Leads player into normal gameplay.
    /// Uses the queued transition system in KiqqiUIManager.
    /// </summary>
    public class KiqqiTutorialEndView : KiqqiUIView
    {
        [Header("UI Elements")]
        public Button continueButton;

        private void OnEnable()
        {
            if (continueButton)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.interactable = false;
                continueButton.onClick.AddListener(OnContinuePressed);
                StopCoroutine(EnableContinue());
                StartCoroutine(EnableContinue());
            }
        }

        IEnumerator EnableContinue()
        {
            yield return new WaitForSeconds(0.35f);
            continueButton.interactable = true;
        }

        private void OnDisable()
        {
            if (continueButton)
            {
                continueButton.interactable = false;
                continueButton.onClick.RemoveAllListeners();
            }
                
        }

        private void OnContinuePressed()
        {
            var app = KiqqiAppManager.Instance;
            var gm = app.Game;
            var ui = app.UI;

            Debug.Log("[KiqqiTutorialEndView] Continue pressed - returning to Main Menu.");

            // ---- clean up active tutorial ----
            var activeMini = gm.currentMiniGame;
            if (activeMini is KiqqiPokerFaceTutorialManager pfTut)
                pfTut.MarkTutorialShown();
            else if (activeMini is KiqqiTicTacToeTutorialManager ttTut)
                ttTut.MarkTutorialShown();

            // stop tutorial logic completely
            if (activeMini != null)
            {
                try { activeMini.OnMiniGameExit(); } catch { }
                gm.currentMiniGame = null;
            }

            if (app.Input) app.Input.enabled = false;
            gm.RestartRequested = gm.ResumeRequested = false;
            gm.State = KiqqiGameManager.GameState.MainMenu;

            // ---- hand off to UIManager ----
            //StartCoroutine(ShowMainMenuQueued(app));
            app.UI.HideAll();
            KiqqiAppManager.Instance.Game.ReturnToMenu();
            app.UI.ShowView<KiqqiMainMenuView>();

            var menuView = app.UI.GetView<KiqqiMainMenuView>();
            menuView.gameObject.SetActive(true);
            menuView.OnShow();
            this.gameObject.SetActive(false);
        }

        private IEnumerator ShowMainMenuQueued(KiqqiAppManager app)
        {
            var ui = app.UI;

            // --- safety cleanup ---
            if (ui.activeView != null)
            {
                ui.activeView.HideWithDeactivate(true);
                ui.activeView = null;
            }

            ui.HideAll();

            // --- sanity check ---
            var menuView = ui.GetView<KiqqiMainMenuView>();
            if (!menuView)
            {
                Debug.LogError("[KiqqiTutorialEndView] MainMenuView not found in UIManager registry!");
                yield break;
            }

            // --- switch game state ---
            app.Game.ReturnToMenu();

            // --- call ONLY the framework transition ---
            ui.ShowView<KiqqiMainMenuView>();

            Debug.Log("[KiqqiTutorialEndView] Main Menu transition triggered (framework slide).");
            yield break;
        }


    }
}
