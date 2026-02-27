using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public class LillyItem
    {
        public int itemID;
        public Sprite sprite;
        public Color color;
    }

    public class KiqqiWaterLilliesManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Gameplay Settings")]
        [SerializeField] private KiqqiWaterLilliesLevelManager levelLogic;

        [Header("Timer Settings")]
        [Tooltip("Don't start new rounds when this much time is left (seconds)")]
        public float noNewRoundThreshold = 1f;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;
        private bool waitingForPlayerInput = false;
        private bool inHighlightPhase = false;
        private float timeExpiredTimestamp = -1f;
        private const float TIME_EXPIRED_GRACE_PERIOD = 1f;

        protected KiqqiWaterLilliesView view;
        protected KiqqiInputController input;

        private List<LillyItem> currentRoundItems = new();
        private List<int> highlightSequence = new();
        private List<int> playerSequence = new();
        private int currentComboStreak = 0;

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiWaterLilliesView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiWaterLilliesView>();
            input = context.Input;

            Debug.Log("[KiqqiWaterLilliesManager] Initialized.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            sessionRunning = false;
            timeExpired = false;
            waitingForPlayerInput = false;
            inHighlightPhase = false;
            isActive = false;
            isComplete = true;

            Debug.Log("[KiqqiWaterLilliesManager] OnMiniGameExit -> cleaned up and deactivated.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            sessionScore = 0;
            masterGame.CurrentScore = 0;
            currentComboStreak = 0;
            sessionRunning = true;
            timeExpired = false;
            waitingForPlayerInput = false;
            inHighlightPhase = false;

            Debug.Log("[KiqqiWaterLilliesManager] Session started.");

            StartNewRound();
        }

        public void NotifyTimeExpired()
        {
            timeExpired = true;
            timeExpiredTimestamp = Time.time;
            Debug.Log("[KiqqiWaterLilliesManager] Time expired - waiting for current round to complete.");
        }

        protected virtual void Update()
        {
            if (timeExpired && timeExpiredTimestamp > 0)
            {
                if (Time.time - timeExpiredTimestamp > TIME_EXPIRED_GRACE_PERIOD)
                {
                    if (!waitingForPlayerInput && !inHighlightPhase)
                    {
                        Debug.Log("[WaterLillies] Grace period expired - forcing session end.");
                        EndSession();
                    }
                }
            }
        }

        private void StartNewRound()
        {
            if (!sessionRunning || timeExpired) return;

            float timeLeft = view != null ? view.RemainingTime : 999f;
            if (timeLeft <= noNewRoundThreshold)
            {
                Debug.Log("[WaterLillies] Not starting new round - time too low.");
                return;
            }

            int level = app.Levels.currentLevel;
            int sequenceLength = levelLogic ? levelLogic.GetSequenceLength(level) : 3;
            int totalItems = levelLogic ? levelLogic.GetTotalItemsOnScreen(level) : 6;

            GenerateRoundData(totalItems, sequenceLength);

            playerSequence.Clear();
            waitingForPlayerInput = false;
            inHighlightPhase = true;

            view?.ShowRoundItems(currentRoundItems);

            Debug.Log($"[WaterLillies] Started round: {totalItems} items, {sequenceLength} sequence length");
        }

        public void OnGrowAnimationComplete()
        {
            StartCoroutine(PlayHighlightSequence());
        }

        private void GenerateRoundData(int totalItems, int sequenceLength)
        {
            currentRoundItems.Clear();
            highlightSequence.Clear();

            int level = app.Levels.currentLevel;
            LillyItemDefinition[] itemPool = levelLogic ? levelLogic.GetItemPoolForLevel(level) : null;

            if (itemPool == null || itemPool.Length == 0)
            {
                Debug.LogError("[WaterLillies] Item pool is empty for current level!");
                return;
            }

            for (int i = 0; i < totalItems; i++)
            {
                LillyItemDefinition itemDef = itemPool[i % itemPool.Length];
                LillyItem item = new LillyItem
                {
                    itemID = itemDef.itemID,
                    sprite = itemDef.sprite,
                    color = itemDef.color
                };
                currentRoundItems.Add(item);
            }

            List<int> availableIndices = new();
            for (int i = 0; i < totalItems; i++)
            {
                availableIndices.Add(i);
            }

            for (int i = 0; i < sequenceLength; i++)
            {
                int randomListIndex = Random.Range(0, availableIndices.Count);
                int selectedIndex = availableIndices[randomListIndex];
                highlightSequence.Add(selectedIndex);
                availableIndices.RemoveAt(randomListIndex);
            }
        }

        private IEnumerator PlayHighlightSequence()
        {
            inHighlightPhase = true;
            float highlightDelay = levelLogic ? levelLogic.GetHighlightDelay(app.Levels.currentLevel) : 0.8f;
            float highlightDuration = levelLogic ? levelLogic.GetHighlightDuration(app.Levels.currentLevel) : 0.4f;

            yield return new WaitForSeconds(0.5f);

            for (int i = 0; i < highlightSequence.Count; i++)
            {
                int itemIndex = highlightSequence[i];
                view?.HighlightItem(itemIndex, true);
                KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

                yield return new WaitForSeconds(highlightDuration);

                view?.HighlightItem(itemIndex, false);

                yield return new WaitForSeconds(highlightDelay);
            }

            inHighlightPhase = false;
            waitingForPlayerInput = true;

            view?.OnHighlightPhaseComplete();

            Debug.Log("[WaterLillies] Highlight phase complete - player's turn.");
        }

        public void HandleItemClicked(int itemIndex)
        {
            if (!isActive || isComplete || !waitingForPlayerInput || !sessionRunning || inHighlightPhase)
                return;

            if (itemIndex < 0 || itemIndex >= currentRoundItems.Count)
                return;

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            playerSequence.Add(itemIndex);

            view?.ShowPlayerClickFeedback(itemIndex);

            Debug.Log($"[WaterLillies] Player clicked item {itemIndex}. Progress: {playerSequence.Count}/{highlightSequence.Count}");

            if (playerSequence.Count >= highlightSequence.Count)
            {
                EvaluatePlayerSequence();
            }
        }

        private void EvaluatePlayerSequence()
        {
            waitingForPlayerInput = false;

            bool correct = true;
            for (int i = 0; i < playerSequence.Count; i++)
            {
                if (playerSequence[i] != highlightSequence[i])
                {
                    correct = false;
                    break;
                }
            }

            int level = app.Levels.currentLevel;

            if (correct)
            {
                int correctScore = levelLogic ? levelLogic.GetScoreForLevel(level) : 100;
                currentComboStreak++;

                int comboThreshold = levelLogic ? levelLogic.GetComboThreshold(level) : 3;
                float comboMultiplier = levelLogic ? levelLogic.GetComboMultiplier(level) : 1.5f;

                if (currentComboStreak >= comboThreshold)
                {
                    correctScore = Mathf.RoundToInt(correctScore * comboMultiplier);
                    Debug.Log($"[WaterLillies] Combo! Streak={currentComboStreak}, Score x{comboMultiplier}");
                }

                sessionScore += correctScore;
                masterGame.AddScore(correctScore);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowRoundResult(true, correctScore);

                app.Levels.NextLevel();

                Debug.Log($"[WaterLillies] Round SUCCESS! +{correctScore} points. Level increased.");

                StartCoroutine(DelayedNextRound());
            }
            else
            {
                currentComboStreak = 0;

                int penalty = levelLogic ? levelLogic.GetPenaltyForLevel(level) : 0;

                if (penalty > 0)
                {
                    sessionScore = Mathf.Max(0, sessionScore - penalty);
                    masterGame.AddScore(-penalty);
                    view?.UpdateScoreLabel(masterGame.CurrentScore);
                }

                view?.ShowRoundResult(false, penalty);

                Debug.Log($"[WaterLillies] Round FAILED. Penalty={penalty}. Retrying same level...");

                StartCoroutine(RestartRound());
            }
        }

        private IEnumerator RestartRound()
        {
            yield return new WaitForSeconds(1.5f);

            if (timeExpired)
            {
                Debug.Log("[WaterLillies] Time expired during round restart - ending session.");
                EndSession();
            }
            else
            {
                StartNewRound();
            }
        }

        private IEnumerator DelayedNextRound()
        {
            yield return new WaitForSeconds(1f);

            timeExpiredTimestamp = -1f;

            if (timeExpired)
            {
                Debug.Log("[WaterLillies] Session ending after round completion.");
                EndSession();
            }
            else
            {
                StartNewRound();
            }
        }

        public void EndSession()
        {
            if (!sessionRunning) return;

            sessionRunning = false;
            waitingForPlayerInput = false;
            inHighlightPhase = false;

            Debug.Log($"[KiqqiWaterLilliesManager] Session ended. Final Score={sessionScore}");
            CompleteMiniGame(sessionScore, true);
        }

        public void ResumeFromPause(KiqqiWaterLilliesView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;

            if (!inHighlightPhase)
            {
                waitingForPlayerInput = true;
            }

            view?.UpdateScoreLabel(masterGame.CurrentScore);

            Debug.Log("[KiqqiWaterLilliesManager] Resumed from pause.");
        }

        #endregion
    }
}
