using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Simple 3x3 grid tap-to-move game:
    /// - Player (red tile) spawns in center
    /// - Tap horizontally or vertically adjacent cell to move
    /// - Adds scoring and completion after fixed number of moves.
    /// </summary>
    public class KiqqiGridSwipeManager : KiqqiMiniGameManagerBase
    {
        protected const int GridSize = 3;
        private const int MAX_SCORE = 100; // Normal gameplay limit

        protected int[,] grid = new int[GridSize, GridSize];
        protected int playerC = 1;
        protected int playerR = 1;
        protected int steps = 0;

        protected KiqqiGridSwipeView view;
        protected KiqqiInputController input;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiGridSwipeView);

        // -------------------------------------------------------------
        // Initialization & Lifecycle
        // -------------------------------------------------------------
        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);
            view = context.UI.GetView<KiqqiGridSwipeView>();
            input = context.Input;

            Debug.Log("[KiqqiGridSwipeManager] Initialized.");
        }

        public override void StartMiniGame()
        {
            base.StartMiniGame();
            steps = 0;
            sessionScore = 0;
            InitGrid();
            view?.RefreshGrid(grid);
            view?.SetGridInteractable(true);
            view?.ShowResult("0");
            Debug.Log("[KiqqiGridSwipeManager] Mini-game started.");
        }

        protected void InitGrid()
        {
            // Clear all
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    grid[c, r] = 0;

            // Spawn player in center
            playerC = 1;
            playerR = 1;
            grid[playerC, playerR] = 1;
            Debug.Log($"[KiqqiGridSwipeManager] Player spawned at center ({playerC},{playerR}).");
        }

        // -------------------------------------------------------------
        // Core Interaction
        // -------------------------------------------------------------
        public virtual void HandleCellPressed(int col, int row)
        {
            if (!isActive || isComplete)
                return;

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            int dc = Mathf.Abs(col - playerC);
            int dr = Mathf.Abs(row - playerR);

            // Must be exactly one step away, no diagonals
            if (dc + dr != 1)
                return;

            // Move player
            grid[playerC, playerR] = 0;
            playerC = col;
            playerR = row;
            grid[playerC, playerR] = 1;
            steps++;

            // Score increment per move
            sessionScore += 10;
            masterGame.AddScore(10);

            // Update view
            view?.RefreshGrid(grid);
            view?.ShowResult($"{sessionScore}");

            // Auto-complete after limit
            if (sessionScore >= MAX_SCORE)
            {
                Debug.Log("[KiqqiGridSwipeManager] Reached max moves — completing mini-game.");
                CompleteMiniGame(sessionScore, true);
            }
        }

        // -------------------------------------------------------------
        // Pause / Resume / Exit
        // -------------------------------------------------------------
        public void ResumeFromPause(KiqqiGridSwipeView v)
        {
            view = v ?? view;
            isActive = true;
            isComplete = false;
            view?.RefreshGrid(grid);
            view?.ShowResult($"{sessionScore}");
            Debug.Log("[KiqqiGridSwipeManager] Resumed from pause.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();
            Debug.Log("[KiqqiGridSwipeManager] Mini-game exited cleanly.");
        }
    }
}
