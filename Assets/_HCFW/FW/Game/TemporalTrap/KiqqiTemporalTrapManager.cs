
using System.Collections;
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// TIME SKIP mini-game (core gameplay):
    /// - Clock hand rotates step by step.
    /// - Sometimes skips positions.
    /// - Player presses OK if skipped, NOK if normal.
    /// </summary>
    public class KiqqiTemporalTrapManager : KiqqiMiniGameManagerBase
    {
        [Header("Game Parameters")]
        [Tooltip("Number of discrete clock positions around the circle.")]
        public int totalPositions = 12;

        [Tooltip("Time per tick (seconds).")]
        public float tickInterval = 0.6f;

        [Tooltip("Chance of skip event per tick.")]
        [Range(0f, 1f)] public float skipChance = 0.2f;

        [Tooltip("Maximum skip size in positions (1 = normal move).")]
        public int maxSkipSize = 3;

        [Tooltip("Points for correct answer.")]
        public int pointsCorrect = 100;

        [Tooltip("Points deducted for incorrect answer.")]
        public int pointsWrong = 50;

        [Header("Runtime State (Debug)")]
        public int currentPos;
        public int lastStepSize;
        public bool awaitingAnswer;

        protected KiqqiTemporalTrapView view;
        protected Coroutine tickRoutine;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiTemporalTrapView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);
            view = context.UI.GetView<KiqqiTemporalTrapView>();
        }

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            currentPos = 0;
            awaitingAnswer = false;
            sessionScore = 0;

            AdjustDifficulty();
            view.BindManager(this);
            tickRoutine = StartCoroutine(TickLoop());
        }

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();
            if (tickRoutine != null) StopCoroutine(tickRoutine);
        }

        private void AdjustDifficulty()
        {
            var diff = KiqqiAppManager.Instance.Levels.GetCurrentDifficulty();
            switch (diff)
            {
                case KiqqiLevelManager.KiqqiDifficulty.Beginner:
                    tickInterval = 0.7f; skipChance = 0.10f; maxSkipSize = 2; break;
                case KiqqiLevelManager.KiqqiDifficulty.Easy:
                    tickInterval = 0.6f; skipChance = 0.15f; maxSkipSize = 2; break;
                case KiqqiLevelManager.KiqqiDifficulty.Medium:
                    tickInterval = 0.55f; skipChance = 0.20f; maxSkipSize = 3; break;
                case KiqqiLevelManager.KiqqiDifficulty.Advanced:
                    tickInterval = 0.5f; skipChance = 0.25f; maxSkipSize = 3; break;
                case KiqqiLevelManager.KiqqiDifficulty.Hard:
                    tickInterval = 0.45f; skipChance = 0.33f; maxSkipSize = 4; break;
            }
        }

        public IEnumerator TickLoop()
        {
            yield return new WaitForSeconds(1f); // small delay before start

            while (isActive && !isComplete)
            {
                int step = 1;
                //bool isSkip = false;

                if (Random.value < skipChance)
                {
                    step = Random.Range(2, maxSkipSize + 1);
                    //isSkip = true;
                }

                lastStepSize = step;
                currentPos = (currentPos + step) % totalPositions;
                awaitingAnswer = true;

                view?.UpdateClockVisual(currentPos);
                view?.ShowButtons(true);

                float t = 0f;
                while (t < tickInterval)
                {
                    if (!awaitingAnswer) break;
                    t += Time.deltaTime;
                    yield return null;
                }

                if (awaitingAnswer)
                {
                    // Player didn’t respond — count as miss
                    sessionScore = Mathf.Max(0, sessionScore - 10);
                    awaitingAnswer = false;
                    view?.ShowButtons(false);
                }

                yield return null;
            }
        }

        // ---------------------- Input from buttons
        public void OnOkPressed()
        {
            if (!awaitingAnswer) return;

            bool wasSkip = lastStepSize > 1;
            if (wasSkip)
            {
                sessionScore += pointsCorrect;
                KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect2");
            }
            else
            {
                sessionScore = Mathf.Max(0, sessionScore - pointsWrong);
                KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
            }

            awaitingAnswer = false;
            view?.ShowButtons(false);
            view?.RefreshScoreUI();
        }

        public void OnNokPressed()
        {
            if (!awaitingAnswer) return;

            bool wasSkip = lastStepSize > 1;
            if (!wasSkip)
            {
                sessionScore += pointsCorrect;
                KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect2");
            }
            else
            {
                sessionScore = Mathf.Max(0, sessionScore - pointsWrong);
                KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
            }

            awaitingAnswer = false;
            view?.ShowButtons(false);
            view?.RefreshScoreUI();
        }

        public void NotifyTimeUp()
        {
            if (!isActive || isComplete) return;
            CompleteMiniGame(sessionScore, true);
        }
    }
}
