using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Simple swipe-based tile mover:
    /// - Player (red tile) starts at center of a 3x3 grid.
    /// - Swipe in any direction to move 1 cell.
    /// - Works with global screen swipes (no grid tap).
    /// </summary>
    public class KiqqiSwipeMoverManager : KiqqiMiniGameManagerBase
    {
        protected const int GridSize = 3;

        protected int[,] grid = new int[GridSize, GridSize];
        protected int playerC = 1;
        protected int playerR = 1;
        protected int steps = 0;

        protected KiqqiSwipeMoverView view;
        protected KiqqiInputController input;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiSwipeMoverView);

        // -------------------------------------------------------------
        // Initialization
        // -------------------------------------------------------------
        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiSwipeMoverView>();
            input = context.Input;

            if (input != null)
            {
                // Prevent multiple bindings
                input.OnSwipe -= HandleSwipe;
                input.enableGridMapping = false;
                input.OnSwipe += HandleSwipe;
            }

            Debug.Log("[KiqqiSwipeMoverManager] Initialized with swipe input.");
        }


        public override void StartMiniGame()
        {
            base.StartMiniGame();
            steps = 0;
            InitGrid();
            view?.RefreshGrid(grid);
            Debug.Log("[KiqqiSwipeMoverManager] Game started.");
        }

        protected void InitGrid()
        {
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    grid[c, r] = 0;

            playerC = 1;
            playerR = 1;
            grid[playerC, playerR] = 1;
        }

        // -------------------------------------------------------------
        // Input (global swipe-based)
        // -------------------------------------------------------------
        protected virtual void HandleSwipe(Vector2 start, Vector2 end, KiqqiInputController.SwipeDirection dir)
        {

            if (!isActive || isComplete)
                return;

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            int targetC = playerC;
            int targetR = playerR;

            switch (dir)
            {
                case KiqqiInputController.SwipeDirection.Left: targetC--; break;
                case KiqqiInputController.SwipeDirection.Right: targetC++; break;
                case KiqqiInputController.SwipeDirection.Up: targetR--; break;
                case KiqqiInputController.SwipeDirection.Down: targetR++; break;
                default: return;
            }

            if (targetC < 0 || targetC >= GridSize || targetR < 0 || targetR >= GridSize)
                return;

            grid[playerC, playerR] = 0;
            playerC = targetC;
            playerR = targetR;
            grid[playerC, playerR] = 1;
            steps++;

            sessionScore += 10;
            masterGame.AddScore(10);

            view?.RefreshGrid(grid);
            view?.ShowResult($"Moves: {steps}");

            if (steps >= 10)
                CompleteMiniGame(sessionScore, true);
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();
            if (input != null)
                input.OnSwipe -= HandleSwipe;

            // Reset grid state to avoid ghost position carryover
            InitGrid();
            steps = 0;
            sessionScore = 0;
        }


        public void ResumeFromPause(KiqqiSwipeMoverView v)
        {
            view = v ?? view;
            isActive = true;
            isComplete = false;
            view?.RefreshGrid(grid);
        }
    }
}
