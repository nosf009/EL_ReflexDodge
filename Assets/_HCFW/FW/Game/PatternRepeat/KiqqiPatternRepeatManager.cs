using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Main gameplay manager for "Repeat the Pattern" (3x3 grid).
    /// Flow:
    /// - On start, generates a pattern (3..9 cells depending on difficulty/level).
    /// - Shows (lights) the pattern for ~2 seconds.
    /// - Hides it; player must tap the same cells. Wrong tap = fail, all correct = win.
    /// Scoring: +10 per correct tap. Calls CompleteMiniGame() on success/failure.
    /// </summary>
    public class KiqqiPatternRepeatManager : KiqqiMiniGameManagerBase
    {
        protected const int GridSize = 3;

        [Header("Pattern Settings")]
        [Tooltip("Base reveal duration before input is enabled.")]
        public float baseRevealSeconds = 2f;

        [Tooltip("Minimum and maximum pattern length (clamped by difficulty).")]
        public int minPattern = 3;
        public int maxPattern = 9;

        protected KiqqiPatternRepeatView view;
        protected KiqqiInputController input;

        // Runtime
        protected List<Vector2Int> pattern = new List<Vector2Int>();
        protected HashSet<Vector2Int> remaining = new HashSet<Vector2Int>();
        protected bool inputEnabled = false;
        protected Coroutine revealRoutine;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiPatternRepeatView);

        // -------------------------------------------------------------
        // Initialization & Lifecycle
        // -------------------------------------------------------------
        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiPatternRepeatView>();
            input = context.Input;

            Debug.Log("[KiqqiPatternRepeatManager] Initialized.");
        }

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            sessionScore = 0;
            inputEnabled = false;

            // Build pattern length from difficulty/level manager
            int level = app.Levels ? app.Levels.currentLevel : 1;
            int len = ComputePatternLength(level);

            GeneratePattern(len);
            view?.PrepareGrid();
            view?.ShowResult("0");

            // Kick off reveal animation then enable input
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = StartCoroutine(RevealThenPlay());
        }

        protected int ComputePatternLength(int level)
        {
            // Simple curve based on difficulty ranges; tweakable
            var diff = app.Levels ? app.Levels.GetCurrentDifficulty() : KiqqiLevelManager.KiqqiDifficulty.Beginner;
            int len = diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 3,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 4,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 5,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 6,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 7,
                _ => 4
            };

            // clamp to configured min/max
            return Mathf.Clamp(len, minPattern, maxPattern);
        }

        protected void GeneratePattern(int count)
        {
            pattern.Clear();
            remaining.Clear();

            // Unique random cells in 3x3
            List<Vector2Int> pool = new List<Vector2Int>(9);
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    pool.Add(new Vector2Int(c, r));

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                var cell = pool[idx];
                pool.RemoveAt(idx);
                pattern.Add(cell);
                remaining.Add(cell);
            }

            Debug.Log($"[KiqqiPatternRepeatManager] Pattern generated: {pattern.Count} cells.");
        }

        private IEnumerator RevealThenPlay()
        {
            // Light up the pattern
            view?.ShowPattern(pattern);

            // Adjust reveal time a bit by difficulty if desired
            float reveal = baseRevealSeconds;
            switch (app.Levels ? app.Levels.GetCurrentDifficulty() : KiqqiLevelManager.KiqqiDifficulty.Beginner)
            {
                case KiqqiLevelManager.KiqqiDifficulty.Advanced: reveal *= 0.9f; break;
                case KiqqiLevelManager.KiqqiDifficulty.Hard: reveal *= 0.8f; break;
            }

            yield return new WaitForSeconds(reveal);

            // Hide pattern and allow taps
            view?.HidePattern();
            inputEnabled = true;
            view?.EnableGrid(true);

            Debug.Log("[KiqqiPatternRepeatManager] Reveal done; input enabled.");
        }

        // -------------------------------------------------------------
        // Core Interaction
        // -------------------------------------------------------------
        public virtual void HandleCellPressed(int col, int row)
        {
            if (!isActive || isComplete || !inputEnabled)
                return;

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            var pos = new Vector2Int(col, row);
            bool isCorrect = remaining.Contains(pos);

            if (!isCorrect)
            {
                // wrong tap ends the game (lose)
                inputEnabled = false;
                view?.FlashWrong(col, row);
                Debug.Log("[KiqqiPatternRepeatManager] Wrong cell tapped � fail.");
                CompleteMiniGame(sessionScore, false);
                return;
            }

            // Correct!
            remaining.Remove(pos);
            sessionScore += 10;
            masterGame.AddScore(10);
            view?.MarkAsConfirmed(col, row);
            view?.ShowResult(sessionScore.ToString());

            if (remaining.Count == 0)
            {
                inputEnabled = false;
                Debug.Log("[KiqqiPatternRepeatManager] All cells matched � success.");
                CompleteMiniGame(sessionScore, true);
            }
        }

        // -------------------------------------------------------------
        // Pause / Resume / Exit
        // -------------------------------------------------------------
        public void ResumeFromPause(KiqqiPatternRepeatView v)
        {
            view = v ?? view;
            isActive = true;
            isComplete = false;
            view?.PrepareGrid(fromResume: true);

            // Resume state (we don't re-reveal; keep current interactivity)
            view?.ShowResult($"{sessionScore}");
            view?.EnableGrid(inputEnabled);

            Debug.Log("[KiqqiPatternRepeatManager] Resumed from pause.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            inputEnabled = false;
            Debug.Log("[KiqqiPatternRepeatManager] Mini-game exited cleanly.");
        }
    }
}
