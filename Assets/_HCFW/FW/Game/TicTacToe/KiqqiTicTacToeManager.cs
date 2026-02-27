// Assets/_HCFW/FW/Games/KiqqiTicTacToeManager.cs
using UnityEngine;
using System;
using System.Collections;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Simple Tic-Tac-Toe logic manager (3x3 grid).
    /// Demonstrates grid tap interaction using KiqqiInputController.
    /// </summary>
    public class KiqqiTicTacToeManager : KiqqiMiniGameManagerBase
    {
        public enum AiDifficulty { Easy, Medium, Hard }

        [Header("AI Difficulty")]
        public AiDifficulty aiDifficulty = AiDifficulty.Hard;

        private const int GridSize = 3;

        //[Header("Tutorial Reference")]
        //[Tooltip("Optional reference to the tutorial manager, used for auto-start logic.")]
        //public KiqqiTicTacToeTutorialManager tutorialManager;

        //private string lastWinner = "None";

        public enum CellState { Empty, PlayerX, PlayerO }
        private CellState[,] grid = new CellState[GridSize, GridSize];

        // --- NEW TURN LOGIC ---
        public enum PlayerType { Human, AI }

        private PlayerType currentTurn;
        private CellState humanMark = CellState.PlayerX;
        private CellState aiMark = CellState.PlayerO;

        private int moveCount = 0;

        protected KiqqiTicTacToeView view;
        public void SetView(KiqqiTicTacToeView v) { view = v; }


        public bool IsHumanTurn => currentTurn == PlayerType.Human;


        protected virtual void OnDrawDetected()
        {
            CompleteMiniGame(50, false);
        }


        public void ResumeFromPause(KiqqiTicTacToeView v)
        {
            view = v ?? view; // refresh link if needed
            isActive = true;      // ensure manager thinks game is running
            isComplete = false;

            // Restore visuals + clickability based on turn
            if (view != null)
            {
                view.RefreshGrid(grid, humanMark == CellState.PlayerX);
                view.SetGridInteractable(IsHumanTurn);
            }
        }


        public override System.Type GetAssociatedViewType()
        {
            return typeof(KiqqiTicTacToeView);
        }

        public void HandleCellPressed(int col, int row)
        {
            if (!isActive || isComplete) return;
            if (currentTurn != PlayerType.Human) return;
            if (grid[col, row] != CellState.Empty) return;

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            grid[col, row] = humanMark;
            moveCount++;

            view.RefreshGrid(grid, true);

            if (CheckForWin(humanMark))
            {
                Debug.Log("[KiqqiTicTacToeManager] Game Over - Human (Player X) Wins!");
                if (view != null) view.ShowResult("You Win! (Player X)");

                // Single unified end point:
                CompleteMiniGame(100, true);
                return;
            }

            else if (moveCount >= GridSize * GridSize)
            {
                Debug.Log("[KiqqiTicTacToeManager] Game Over - Draw! No winner.");
                if (view != null) view.ShowResult("It's a Draw!");

                // Unifies through the draw hook > CompleteMiniGame(50, false)
                OnDrawDetected();
                return;
            }

            currentTurn = PlayerType.AI;
            if (view != null) view.SetGridInteractable(false);
            KiqqiAppManager.Instance.StartCoroutine(AI_MoveDelayed());
        }

        private void Awake()
        {

        }

        private IEnumerator Start()
        {
            // Wait one frame so KiqqiGameManager & UI are initialized
            yield return null;

            Initialize(KiqqiAppManager.Instance);

            var app = KiqqiAppManager.Instance;

            /*
            // --- AUTO-START TUTORIAL CHECK ---
            if (app.Game.tutorialGameManager is KiqqiTicTacToeTutorialManager tutMgr &&
                tutMgr.autoStartOnFirstRun &&
                tutMgr.ShouldAutoStartTutorial())
            {
                Debug.Log("[KiqqiTicTacToeManager] Auto-starting tutorial (first-time user).");

                yield return new WaitForSecondsRealtime(0.1f);

                app.Game.StartTutorial();
                tutMgr.MarkTutorialShown();
                yield break;
            }
            */


            // --- NORMAL START PATH ---
            //StartMiniGame();
        }


        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);
            view = context.UI.GetView<KiqqiTicTacToeView>();

            Debug.Log("[KiqqiTicTacToeManager] Initialized (view linked from UI Manager).");
        }

        public override void StartMiniGame()
        {
            base.StartMiniGame();
            // Clear board
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    grid[c, r] = CellState.Empty;

            moveCount = 0;
            currentTurn = PlayerType.Human; // Human always starts first

            // ----------------------------------------------------
            // Determine AI difficulty based on current game level
            // ----------------------------------------------------
            var levelMgr = KiqqiAppManager.Instance.Levels;
            if (levelMgr != null)
            {
                var diff = levelMgr.GetCurrentDifficulty();
                switch (diff)
                {
                    case KiqqiLevelManager.KiqqiDifficulty.Beginner:
                        aiDifficulty = AiDifficulty.Easy; // totally random
                        break;
                    case KiqqiLevelManager.KiqqiDifficulty.Easy:
                        aiDifficulty = AiDifficulty.Easy; // slightly smarter
                        break;
                    case KiqqiLevelManager.KiqqiDifficulty.Medium:
                        aiDifficulty = AiDifficulty.Medium;
                        break;
                    case KiqqiLevelManager.KiqqiDifficulty.Advanced:
                        aiDifficulty = AiDifficulty.Hard; // smart blocking + some randomness
                        break;
                    case KiqqiLevelManager.KiqqiDifficulty.Hard:
                        aiDifficulty = AiDifficulty.Hard; // full logic
                        break;
                }

                Debug.Log($"[KiqqiTicTacToeManager] Difficulty set from LevelManager: {aiDifficulty}");
            }
            else
            {
                Debug.LogWarning("[KiqqiTicTacToeManager] LevelManager not found; keeping default difficulty.");
            }

            if (view != null)
            {
                view.RefreshGrid(grid, humanMark == CellState.PlayerX);
                view.SetGridInteractable(true);
            }

            Debug.Log("[KiqqiTicTacToeManager] Game started. Human is X, AI is O.");
        }

        private bool CheckForWin(CellState player)
        {
            // Rows
            for (int r = 0; r < GridSize; r++)
                if (grid[0, r] == player && grid[1, r] == player && grid[2, r] == player)
                    return true;

            // Columns
            for (int c = 0; c < GridSize; c++)
                if (grid[c, 0] == player && grid[c, 1] == player && grid[c, 2] == player)
                    return true;

            // Diagonals
            if (grid[0, 0] == player && grid[1, 1] == player && grid[2, 2] == player) return true;
            if (grid[2, 0] == player && grid[1, 1] == player && grid[0, 2] == player) return true;

            return false;
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();
        }

        private System.Collections.IEnumerator AI_MoveDelayed()
        {
            yield return new WaitForSeconds(0.5f); // short pause for realism

            Vector2Int? bestMove = FindBestMove();

            if (bestMove.HasValue)
            {
                int c = bestMove.Value.x;
                int r = bestMove.Value.y;
                grid[c, r] = aiMark;
                moveCount++;

                view.RefreshGrid(grid, false);

                if (CheckForWin(aiMark))
                {
                    Debug.Log("[KiqqiTicTacToeManager] Game Over - AI (Player O) Wins!");
                    if (view != null) view.ShowResult("AI Wins! (Player O)");

                    // Single unified end point:
                    CompleteMiniGame(50, false);
                    yield break;
                }
                else if (moveCount >= GridSize * GridSize)
                {
                    Debug.Log("[KiqqiTicTacToeManager] Game Over - Draw! No winner.");
                    if (view != null) view.ShowResult("It's a Draw!");

                    // Unifies through the draw hook > CompleteMiniGame(50, false)
                    OnDrawDetected();
                    yield break;
                }
            }

            // Back to player
            currentTurn = PlayerType.Human;
            if (view != null) view.SetGridInteractable(true);
        }

        public void SetDifficulty(string difficultyString)
        {
            if (System.Enum.TryParse(difficultyString, true, out AiDifficulty diff))
            {
                aiDifficulty = diff;
                Debug.Log($"[TicTacToe] Difficulty set to {aiDifficulty}");
            }
            else
            {
                Debug.LogWarning($"[TicTacToe] Unknown difficulty '{difficultyString}', keeping {aiDifficulty}");
            }
        }


        private Vector2Int? FindBestMove()
        {
            // --- HARD difficulty (full logic) ---
            if (aiDifficulty == AiDifficulty.Hard)
                return FindSmartMove(true, true, true, true);

            // --- MEDIUM difficulty (only win/block) ---
            if (aiDifficulty == AiDifficulty.Medium)
                return FindSmartMove(true, true, false, false);

            // --- EASY difficulty (random) ---
            return FindSmartMove(false, false, false, false);
        }

        private Vector2Int? FindSmartMove(
        bool tryWin,
        bool tryBlock,
        bool takeCenter,
        bool takeCorner)
            {
            
                if (tryWin)
                {
                    for (int r = 0; r < GridSize; r++)
                    {
                        for (int c = 0; c < GridSize; c++)
                        {
                            if (grid[c, r] == CellState.Empty)
                            {
                                grid[c, r] = aiMark;
                                if (CheckForWin(aiMark))
                                {
                                    grid[c, r] = CellState.Empty;
                                    return new Vector2Int(c, r);
                                }
                                grid[c, r] = CellState.Empty;
                            }
                        }
                    }
                }

            
                if (tryBlock)
                {
                    for (int r = 0; r < GridSize; r++)
                    {
                        for (int c = 0; c < GridSize; c++)
                        {
                            if (grid[c, r] == CellState.Empty)
                            {
                                grid[c, r] = humanMark;
                                if (CheckForWin(humanMark))
                                {
                                    grid[c, r] = CellState.Empty;
                                    return new Vector2Int(c, r);
                                }
                                grid[c, r] = CellState.Empty;
                            }
                        }
                    }
                }

            
                if (takeCenter && grid[1, 1] == CellState.Empty)
                    return new Vector2Int(1, 1);

            
                if (takeCorner)
                {
                    var corners = new System.Collections.Generic.List<Vector2Int>();
                    foreach (var pos in new[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(2, 2) })
                        if (grid[pos.x, pos.y] == CellState.Empty) corners.Add(pos);
                    if (corners.Count > 0)
                        return corners[UnityEngine.Random.Range(0, corners.Count)];
                }

            
                var empty = new System.Collections.Generic.List<Vector2Int>();
                for (int r = 0; r < GridSize; r++)
                    for (int c = 0; c < GridSize; c++)
                        if (grid[c, r] == CellState.Empty)
                            empty.Add(new Vector2Int(c, r));

                if (empty.Count > 0)
                    return empty[UnityEngine.Random.Range(0, empty.Count)];

                return null;
            }

    }
}
