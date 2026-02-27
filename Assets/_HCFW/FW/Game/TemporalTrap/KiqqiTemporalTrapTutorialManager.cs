// Assets/_HCFW/FW/Game/TemporalTrap/KiqqiTemporalTrapTutorialManager.cs
using System.Collections;
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Tutorial flavor of Temporal Trap:
    /// - Inherits full gameplay logic from main manager.
    /// - Runs slower and ends with KiqqiTutorialEndView (no scoring API).
    /// </summary>
    public class KiqqiTemporalTrapTutorialManager : KiqqiTemporalTrapManager
    {
        [Header("Tutorial Configuration")]
        public bool autoStartOnFirstRun = true;
        public bool continueToMainMenu = true;

        private const string TUTORIAL_KEY = "temporaltrap_tutorial_done";
        private Coroutine tutorialTickRoutine;

        public override System.Type GetAssociatedViewType() => typeof(KiqqiTemporalTrapTutorialView);

        public override void StartMiniGame()
        {
            // Bind tutorial view explicitly
            var context = KiqqiAppManager.Instance;
            view = context.UI.GetView<KiqqiTemporalTrapTutorialView>();

            base.StartMiniGame(); // ensures base setup (score reset, flags, etc.)
            masterGame.State = KiqqiGameManager.GameState.Tutorial;

            // Adjust difficulty for tutorial
            tickInterval = 1.0f;
            skipChance = 0.35f;
            maxSkipSize = 2;

            // Restart coroutine independently for tutorial
            if (tickRoutine != null)
                StopCoroutine(tickRoutine);
            tutorialTickRoutine = StartCoroutine(TutorialTickLoop());

            MarkTutorialShown();
            Debug.Log("[KiqqiTemporalTrapTutorialManager] Tutorial started with independent TickLoop.");
        }

        private IEnumerator TutorialTickLoop()
        {
            // Simply reuse gameplay logic but run under tutorial pacing
            yield return new WaitForSeconds(1f);

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
                    sessionScore = Mathf.Max(0, sessionScore - 10);
                    awaitingAnswer = false;
                    view?.ShowButtons(false);
                }

                yield return null;
            }
        }

        public override void CompleteMiniGame(int finalScore, bool playerWon = true)
        {
            if (isComplete) return;

            isComplete = true;
            isActive = false;
            sessionScore = finalScore;

            KiqqiAppManager.Instance.UI.ShowView<KiqqiTutorialEndView>();
            Debug.Log("[KiqqiTemporalTrapTutorialManager] Tutorial complete — showing end screen.");
        }

        public bool ShouldAutoStartTutorial()
        {
            if (!autoStartOnFirstRun) return false;
            return PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 0;
        }

        public void MarkTutorialShown()
        {
            PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
            PlayerPrefs.Save();
        }
    }
}
