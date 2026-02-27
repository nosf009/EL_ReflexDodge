// Assets/_HCFW/FW/Games/KiqqiTicTacToeView.cs
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// View layer for Tic-Tac-Toe.
    /// Displays grid cells and visual feedback.
    /// </summary>
    public class KiqqiTicTacToeView : KiqqiGameViewBase
    {
        [Header("UI References")]
        public Transform gridRoot;          // parent container of 3x3 buttons or images
        public Text resultLabel;

        private Image[,] cellImages = new Image[3, 3];
        private KiqqiTicTacToeManager.CellState[,] lastGrid = new KiqqiTicTacToeManager.CellState[3, 3];
        // private Image[,] cellImages = new Image[3, 3];
        private UnityEngine.UI.Button[,] cellButtons = new UnityEngine.UI.Button[3, 3];

        [Header("Cell Sprites")]
        public Sprite emptySprite;
        public Sprite xSprite;
        public Sprite oSprite;

        private void OnEnable()
        {
            gridRoot.gameObject.SetActive(false);

            var mgr = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiTicTacToeManager;
            if (!mgr)
                mgr = FindFirstObjectByType<KiqqiTicTacToeManager>(FindObjectsInactive.Include);

            int index = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (index >= gridRoot.childCount) break;
                    var child = gridRoot.GetChild(index);
                    var img = child.GetComponent<Image>();
                    var cell = child.GetComponent<KiqqiTicTacToeCell>();
                    if (!cell) cell = child.gameObject.AddComponent<KiqqiTicTacToeCell>();

                    var btn = child.GetComponent<UnityEngine.UI.Button>();

                    cell.Init(mgr, col, row);
                    cellImages[row, col] = img;
                    cellButtons[row, col] = btn;
                    index++;
                }
            }

            if (resultLabel) resultLabel.text = "";
        }

        private void OnDisable()
        {
            // Clean up listeners
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (cellButtons[r, c] != null)
                        cellButtons[r, c].onClick.RemoveAllListeners();
        }

        public void SetGridInteractable(bool value)
        {
            if (!gridRoot) return;
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (cellButtons[r, c] != null)
                        cellButtons[r, c].interactable = value;
        }


        public override void OnShow()
        {
            var app = KiqqiAppManager.Instance;
            var game = app.Game;

            // ----- RESUME PATH -----
            if (game.ResumeRequested && game.currentMiniGame is KiqqiTicTacToeManager ttt)
            {
                Debug.Log("[KiqqiTicTacToeView] Resuming existing TicTacToe session (skip base.OnShow + countdown).");

                // DO NOT call base.OnShow() here (it would reset time/score and start countdown)
                // Ensure CanvasGroup is fully active
                var cg = GetComponent<CanvasGroup>();
                if (cg)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                // Make sure UI is visible and clickable
                if (countdownLabel) countdownLabel.gameObject.SetActive(false);
                if (gridRoot) gridRoot.gameObject.SetActive(true);

                // Let the manager restore the board & whose turn it is
                ttt.ResumeFromPause(this);

                // Re-arm the HUD timer to continue ticking
                timerRunning = true;

                // Consume the flag so subsequent shows act normally
                game.ResumeRequested = false;
                return;
            }

            // ----- FRESH START PATH (normal show) -----
            base.OnShow();        // this resets HUD and starts countdown (fresh game)
            ResetBoardVisuals();  // keep your current fresh start visual reset
        }


        public void ResetBoardVisuals()
        {
            if (gridRoot == null) return;

            // Hide grid
            gridRoot.gameObject.SetActive(false);

            // Clear all cell images
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (cellImages[row, col] != null && emptySprite != null)
                        cellImages[row, col].sprite = emptySprite;
                }
            }

            if (resultLabel) resultLabel.text = "";
        }



        public void RefreshGrid(KiqqiTicTacToeManager.CellState[,] grid, bool playerXTurn)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var img = cellImages[row, col];
                    if (!img) continue;

                    var state = grid[col, row]; // grid is still stored as [col,row]
                    lastGrid[row, col] = state;

                    switch (state)
                    {
                        case KiqqiTicTacToeManager.CellState.PlayerX:
                            img.sprite = xSprite;
                            break;
                        case KiqqiTicTacToeManager.CellState.PlayerO:
                            img.sprite = oSprite;
                            break;
                        default:
                            img.sprite = emptySprite;
                            break;
                    }
                }
            }

        }

        public void ShowResult(string text)
        {
            if (resultLabel)
                resultLabel.text = text;
        }

        protected override void OnCountdownFinished()
        {
            // Show grid now that countdown has ended
            if (gridRoot) gridRoot.gameObject.SetActive(true);

            // Countdown done, start actual gameplay logic
            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiTicTacToeManager mgr)
            {
                Debug.Log("[KiqqiTicTacToeView] Countdown finished — starting mini-game via GameManager reference.");
                mgr.StartMiniGame();
            }
            else
            {
                Debug.LogWarning("[KiqqiTicTacToeView] No active KiqqiTicTacToeManager found in GameManager.");
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
