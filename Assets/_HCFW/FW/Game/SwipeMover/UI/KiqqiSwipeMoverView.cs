using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// View layer for SwipeMover mini-game.
    /// Displays a 3x3 grid with a movable tile (player).
    /// </summary>
    public class KiqqiSwipeMoverView : KiqqiGameViewBase
    {
        [Header("UI")]
        public Transform gridRoot;
        public Text resultLabel;

        private Image[,] cells = new Image[3, 3];

        [Header("Swipe Input Area")]
        public RectTransform swipePanel; // panel that catches swipes


        private void OnEnable()
        {
            if (!gridRoot) return;

            int idx = 0;
            var mgr = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiSwipeMoverManager;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (idx >= gridRoot.childCount) break;
                    var child = gridRoot.GetChild(idx);
                    var img = child.GetComponent<Image>();
                    cells[c, r] = img;
                    idx++;
                }
            }

            if (resultLabel) resultLabel.text = "";
            gridRoot.gameObject.SetActive(false);
        }

        public override void OnShow()
        {
            base.OnShow();
            if (resultLabel) resultLabel.text = "";

            var gm = KiqqiAppManager.Instance.Game;

            // Handle Resume after Pause
            if (gm.ResumeRequested && gm.currentMiniGame is KiqqiSwipeMoverManager moverMgr)
            {
                gridRoot?.gameObject.SetActive(true);
                moverMgr.ResumeFromPause(this);
                if (countdownLabel) countdownLabel.gameObject.SetActive(false);
                gm.ResumeRequested = false;
                Debug.Log("[KiqqiSwipeMoverView] Resuming gameplay without countdown.");
                return;
            }

            // Normal fresh start path
            var input = KiqqiAppManager.Instance.Input;
            if (input != null)
            {
                if (swipePanel != null)
                    input.inputArea = swipePanel;
                else
                    input.inputArea = input.targetCanvas
                        ? input.targetCanvas.GetComponent<RectTransform>()
                        : null;

                input.enabled = true;
            }
        }


        public override void OnHide()
        {
            base.OnHide();
            var input = KiqqiAppManager.Instance.Input;
            if (input != null)
            {
                // Reset to default canvas rect after view is closed
                input.inputArea = input.targetCanvas
                    ? input.targetCanvas.GetComponent<RectTransform>()
                    : null;
            }
        }


        public void RefreshGrid(int[,] grid)
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (cells[c, r])
                        cells[c, r].color = grid[c, r] == 1 ? Color.red : Color.white;
                }
            }
        }

        public void ShowResult(string txt)
        {
            if (resultLabel) resultLabel.text = txt;
        }

        protected override void OnCountdownFinished()
        {
            if (gridRoot) gridRoot.gameObject.SetActive(true);

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiSwipeMoverManager mgr)
            {
                Debug.Log("[KiqqiSwipeMoverView] Countdown finished — starting mini-game via GameManager reference.");
                mgr.StartMiniGame();
            }
            else
            {
                Debug.LogWarning("[KiqqiSwipeMoverView] No active KiqqiSwipeMoverManager found in GameManager.");
            }
        }


        protected override void OnTimeUp()
        {
            base.OnTimeUp();
            Debug.Log($"[{GetType().Name}] Time reached 00:00 - Player wins");

            timerRunning = false;

            // Treat as a win and complete the mini-game
            var gm = KiqqiAppManager.Instance.Game.currentMiniGame;
            if (gm != null)
                gm.CompleteMiniGame(gm.sessionScore, true);

            KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect2");
        }


    }
}
