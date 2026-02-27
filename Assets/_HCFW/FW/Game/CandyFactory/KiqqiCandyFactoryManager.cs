using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public class CandyItem
    {
        public int itemType;
        public Sprite sprite;
    }

    public class BagConfig
    {
        public List<int> requiredItemTypes = new();
        public Color bagColor = Color.white;
    }

    public class KiqqiCandyFactoryManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Gameplay Settings")]
        [SerializeField] private KiqqiCandyFactoryLevelManager levelLogic;

        [Header("Timer Settings")]
        [Tooltip("Don't start new rounds when this much time is left (seconds)")]
        public float noNewRoundThreshold = 3f;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;
        private bool waitingForPlacement = false;
        private float timeExpiredTimestamp = -1f;
        private const float TIME_EXPIRED_GRACE_PERIOD = 1f;

        protected KiqqiCandyFactoryView view;
        protected KiqqiInputController input;

        private List<BagConfig> currentBags = new();
        private List<CandyItem> currentRoundItems = new();
        private HashSet<int> usedItemIndices = new();
        private HashSet<int> filledBags = new();
        private HashSet<int> correctlyFilledBags = new();
        private CandyItem selectedItem = null;
        private int selectedItemIndex = -1;
        private int currentComboStreak = 0;

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiCandyFactoryView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiCandyFactoryView>();
            input = context.Input;

            Debug.Log("[KiqqiCandyFactoryManager] Initialized.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            sessionRunning = false;
            timeExpired = false;
            waitingForPlacement = false;
            isActive = false;
            isComplete = true;

            Debug.Log("[KiqqiCandyFactoryManager] OnMiniGameExit -> cleaned up and deactivated.");
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
            waitingForPlacement = false;

            Debug.Log("[KiqqiCandyFactoryManager] Session started.");

            StartNewRound();
        }

        public void NotifyTimeExpired()
        {
            timeExpired = true;
            timeExpiredTimestamp = Time.time;
            Debug.Log("[KiqqiCandyFactoryManager] Time expired - waiting for current round to complete.");
        }

        protected virtual void Update()
        {
            if (timeExpired && timeExpiredTimestamp > 0)
            {
                if (Time.time - timeExpiredTimestamp > TIME_EXPIRED_GRACE_PERIOD)
                {
                    Debug.Log("[CandyFactory] Grace period expired - forcing session end.");
                    EndSession();
                }
            }
        }

        private void StartNewRound()
        {
            if (!sessionRunning || timeExpired) return;

            float timeLeft = view != null ? view.RemainingTime : 999f;
            if (timeLeft <= noNewRoundThreshold)
            {
                Debug.Log("[CandyFactory] Not starting new round - time too low.");
                return;
            }

            int level = app.Levels.currentLevel;
            int bagCount = levelLogic ? levelLogic.GetBagCount(level) : 2;
            int itemsPerRound = levelLogic ? levelLogic.GetItemsPerRound(level) : 4;
            float previewDuration = levelLogic ? levelLogic.GetPreviewDuration(level) : 3f;

            GenerateRoundData(bagCount, itemsPerRound);

            selectedItem = null;
            filledBags.Clear();
            correctlyFilledBags.Clear();
            waitingForPlacement = true;

            view?.ShowPreviewPhase(currentBags, currentRoundItems, previewDuration);

            Debug.Log($"[CandyFactory] Started round: {bagCount} bags, {itemsPerRound} items, {previewDuration}s preview");
        }

        private void GenerateRoundData(int bagCount, int itemCount)
        {
            currentBags.Clear();
            currentRoundItems.Clear();

            int level = app.Levels.currentLevel;
            int[] itemPool = levelLogic ? levelLogic.GetItemPoolForLevel(level) : new int[] { 0, 1, 2, 3 };

            if (itemPool == null || itemPool.Length == 0)
            {
                Debug.LogError("[CandyFactory] Item pool is empty for current level!");
                return;
            }

            int availableItemTypes = Mathf.Min(itemCount, itemPool.Length);
            int[] selectedItemIDs = new int[availableItemTypes];

            for (int i = 0; i < availableItemTypes; i++)
            {
                selectedItemIDs[i] = itemPool[i % itemPool.Length];
            }

            ShuffleArray(selectedItemIDs);

            for (int i = 0; i < bagCount; i++)
            {
                BagConfig bag = new BagConfig();
                int itemID = selectedItemIDs[i % availableItemTypes];
                bag.requiredItemTypes.Add(itemID);
                currentBags.Add(bag);
            }

            for (int i = 0; i < itemCount; i++)
            {
                int itemID = selectedItemIDs[i % availableItemTypes];

                CandyItemDefinition itemDef = levelLogic ? levelLogic.GetItemDefinition(itemID) : null;
                Sprite itemSprite = itemDef?.sprite;

                CandyItem item = new CandyItem 
                { 
                    itemType = itemID,
                    sprite = itemSprite
                };
                currentRoundItems.Add(item);
            }

            ShuffleList(currentRoundItems);
        }

        private void ShuffleArray(int[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void OnPreviewComplete()
        {
            Debug.Log("[CandyFactory] Preview complete - gameplay active.");
            waitingForPlacement = true;
        }

        public void HandleItemSelected(int itemIndex)
        {
            if (!isActive || isComplete || !waitingForPlacement || !sessionRunning)
                return;

            if (itemIndex < 0 || itemIndex >= currentRoundItems.Count)
                return;

            if (usedItemIndices.Contains(itemIndex))
            {
                Debug.Log($"[CandyFactory] Item {itemIndex} already used - ignoring.");
                return;
            }

            selectedItem = currentRoundItems[itemIndex];
            selectedItemIndex = itemIndex;
            view?.HighlightSelectedItem(itemIndex);

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            Debug.Log($"[CandyFactory] Item {itemIndex} selected (type {selectedItem.itemType})");
        }

        public void HandleBagSelected(int bagIndex)
        {
            if (!isActive || isComplete || !waitingForPlacement || !sessionRunning)
                return;

            if (bagIndex < 0 || bagIndex >= currentBags.Count)
                return;

            if (filledBags.Contains(bagIndex))
            {
                Debug.Log($"[CandyFactory] Bag {bagIndex} already filled - ignoring.");
                return;
            }

            if (selectedItem == null)
            {
                Debug.Log("[CandyFactory] No item selected - ignoring bag tap.");
                return;
            }

            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            bool correct = currentBags[bagIndex].requiredItemTypes.Contains(selectedItem.itemType);
            filledBags.Add(bagIndex);

            ProcessPlacement(correct, bagIndex);
        }

        private void ProcessPlacement(bool correct, int bagIndex)
        {
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
                    Debug.Log($"[CandyFactory] Combo! Streak={currentComboStreak}, Score x{comboMultiplier}");
                }

                sessionScore += correctScore;
                masterGame.AddScore(correctScore);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowFeedback(true, bagIndex);

                correctlyFilledBags.Add(bagIndex);
                usedItemIndices.Add(selectedItemIndex);
                view?.RemoveItemFromShelf(selectedItemIndex);

                Debug.Log($"[CandyFactory] Correct placement! +{correctScore} points (bag {bagIndex})");
            }
            else
            {
                currentComboStreak = 0;

                int penalty = levelLogic ? levelLogic.GetPenaltyForLevel(level) : 10;

                sessionScore = Mathf.Max(0, sessionScore - penalty);
                masterGame.AddScore(-penalty);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowFeedback(false, bagIndex);

                Debug.Log($"[CandyFactory] Wrong placement! -{penalty} points (bag {bagIndex})");
            }

            selectedItem = null;
            selectedItemIndex = -1;
            view?.HighlightSelectedItem(-1);

            int bagCount = levelLogic ? levelLogic.GetBagCount(level) : 2;
            if (filledBags.Count >= bagCount)
            {
                EvaluateRound();
            }
        }

        private void EvaluateRound()
        {
            int bagCount = levelLogic ? levelLogic.GetBagCount(app.Levels.currentLevel) : 2;

            if (correctlyFilledBags.Count >= bagCount)
            {
                app.Levels.NextLevel();
                Debug.Log("[CandyFactory] Round SUCCESS - all bags correct! Level increased.");
                OnRoundComplete();
            }
            else
            {
                Debug.Log($"[CandyFactory] Round FAILED - only {correctlyFilledBags.Count}/{bagCount} correct. Restarting round...");
                StartCoroutine(RestartRound());
            }
        }

        private IEnumerator RestartRound()
        {
            waitingForPlacement = false;
            yield return new WaitForSeconds(1f);

            usedItemIndices.Clear();
            filledBags.Clear();
            correctlyFilledBags.Clear();

            if (timeExpired)
            {
                Debug.Log("[CandyFactory] Time expired during round restart - ending session.");
                EndSession();
            }
            else
            {
                StartNewRound();
            }
        }

        private void OnRoundComplete()
        {
            waitingForPlacement = false;
            usedItemIndices.Clear();
            filledBags.Clear();
            correctlyFilledBags.Clear();
            timeExpiredTimestamp = -1f;

            if (timeExpired)
            {
                Debug.Log("[CandyFactory] Session ending after round completion.");
                EndSession();
            }
            else
            {
                StartCoroutine(DelayedNextRound());
            }
        }

        private IEnumerator DelayedNextRound()
        {
            yield return new WaitForSeconds(0.5f);
            StartNewRound();
        }

        public void EndSession()
        {
            if (!sessionRunning) return;

            sessionRunning = false;
            waitingForPlacement = false;

            Debug.Log($"[KiqqiCandyFactoryManager] Session ended. Final Score={sessionScore}");
            CompleteMiniGame(sessionScore, true);
        }

        public void ResumeFromPause(KiqqiCandyFactoryView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;
            waitingForPlacement = true;

            view?.UpdateScoreLabel(masterGame.CurrentScore);

            Debug.Log("[KiqqiCandyFactoryManager] Resumed from pause.");
        }

        #endregion
    }
}
