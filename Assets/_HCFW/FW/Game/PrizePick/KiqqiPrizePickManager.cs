using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public enum MathOperation
    {
        None,
        Add,
        Multiply,
        Combined
    }

    public class PrizeOption
    {
        public int calculatedValue;
        public int baseValue;
        public MathOperation operation;
        public int operand1;
        public int operand2;
        public int addValue;

        public string GetDisplayText()
        {
            return operation switch
            {
                MathOperation.None => calculatedValue.ToString(),
                MathOperation.Add => $"{baseValue} + {addValue}",
                MathOperation.Multiply => $"{operand1} × {operand2}",
                MathOperation.Combined => $"{operand1} × {operand2} + {addValue}",
                _ => calculatedValue.ToString()
            };
        }
    }

    public class KiqqiPrizePickManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Core References")]
        [SerializeField] private KiqqiPrizePickLevelManager levelLogic;

        [Header("Gameplay Settings")]
        [Tooltip("Don't show new choices when this much time is left (seconds)")]
        public float noNewChoicesThreshold = 2.5f;

        [Header("Value Generation")]
        [SerializeField] private int minValueGap = 5;
        [SerializeField] private int maxValueProximity = 10;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;
        private bool waitingForChoice = false;

        protected KiqqiPrizePickView view;

        private PrizePickDifficultyConfig currentConfig;
        private List<PrizeOption> currentOptions;
        private int currentTopPrize = 0;
        private int currentComboStreak = 0;
        private int challengesCompleted = 0;
        private float choiceTimer = 0f;

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiPrizePickView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiPrizePickView>();

            if (view != null)
            {
                view.SetManager(this);
                view.OnPrizeSelected += HandlePrizeSelected;
            }
            else
            {
                Debug.LogError("[KiqqiPrizePickManager] View not found!");
            }

            Debug.Log("[KiqqiPrizePickManager] Initialized.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            if (view != null)
            {
                view.OnPrizeSelected -= HandlePrizeSelected;
            }

            sessionRunning = false;
            timeExpired = false;
            waitingForChoice = false;
            isActive = false;
            isComplete = true;

            Debug.Log("[KiqqiPrizePickManager] OnMiniGameExit -> cleaned up and deactivated.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            int currentLevel = app.Levels.currentLevel;
            currentConfig = levelLogic.GetDifficultyConfig(currentLevel);

            sessionScore = 0;
            masterGame.CurrentScore = 0;
            currentTopPrize = 0;
            currentComboStreak = 0;
            challengesCompleted = 0;
            sessionRunning = true;
            timeExpired = false;
            waitingForChoice = false;

            Debug.Log($"[KiqqiPrizePickManager] Session started. Level {currentLevel}, Config: {currentConfig.optionCount} options, {currentConfig.challengesPerLevel} challenges, {currentConfig.timePerChoice}s per choice");
        }

        public void NotifyCountdownFinished()
        {
            Debug.Log("[KiqqiPrizePickManager] Countdown finished, starting first challenge.");
            GenerateAndShowChoices();
        }

        public void NotifyTimeExpired()
        {
            if (timeExpired) return;

            timeExpired = true;
            Debug.Log("[KiqqiPrizePickManager] Time expired.");

            if (waitingForChoice)
            {
                Debug.Log("[KiqqiPrizePickManager] Waiting for final choice before ending session.");
            }
            else
            {
                EndSession();
            }
        }

        private void Update()
        {
            if (!sessionRunning || !waitingForChoice || timeExpired) return;

            choiceTimer -= Time.deltaTime;

            view?.UpdateChoiceTimer(choiceTimer);

            if (choiceTimer <= 0f)
            {
                HandleChoiceTimeout();
            }
        }

        #endregion

        #region CHOICE GENERATION

        private void GenerateAndShowChoices()
        {
            if (timeExpired) return;

            if (view != null && view.RemainingTime <= noNewChoicesThreshold)
            {
                Debug.Log($"[KiqqiPrizePickManager] Not showing new choices - only {view.RemainingTime:F1}s remaining.");
                return;
            }

            Debug.Log($"[KiqqiPrizePickManager] Config check: optionCount={currentConfig.optionCount}, challengesPerLevel={currentConfig.challengesPerLevel}, timePerChoice={currentConfig.timePerChoice}");

            currentOptions = GenerateOptions();

            waitingForChoice = true;
            choiceTimer = currentConfig.timePerChoice;

            view?.ShowChoices(currentOptions, currentTopPrize, choiceTimer);

            Debug.Log($"[KiqqiPrizePickManager] Generated {currentOptions.Count} choices. Top prize: {currentTopPrize}");
        }

        private List<PrizeOption> GenerateOptions()
        {
            List<PrizeOption> options = new List<PrizeOption>();

            int minValue = Mathf.Max(currentTopPrize + minValueGap, 10);
            int maxValue = currentTopPrize + currentConfig.maxBaseValue;

            int highestValue = Random.Range(minValue, maxValue + 1);

            PrizeOption correctOption = GeneratePrizeOption(highestValue, true);
            options.Add(correctOption);

            for (int i = 1; i < currentConfig.optionCount; i++)
            {
                int wrongValue = Random.Range(Mathf.Max(10, currentTopPrize), highestValue - minValueGap);
                PrizeOption wrongOption = GeneratePrizeOption(wrongValue, false);
                options.Add(wrongOption);
            }

            Shuffle(options);

            return options;
        }

        private PrizeOption GeneratePrizeOption(int targetValue, bool isHighest)
        {
            PrizeOption option = new PrizeOption { calculatedValue = targetValue };

            bool useMathExpression = Random.value < currentConfig.mathExpressionProbability;

            if (!useMathExpression)
            {
                option.operation = MathOperation.None;
                option.baseValue = targetValue;
                return option;
            }

            bool useMultiply = Random.value < 0.5f;
            bool useCombined = Random.value < 0.3f && currentConfig.mathExpressionProbability > 0.5f;

            if (useCombined)
            {
                option.operation = MathOperation.Combined;
                option.operand1 = Random.Range(2, 6);
                int productLimit = Mathf.Min(targetValue - 5, currentConfig.maxBaseValue / 2);
                option.operand2 = Random.Range(5, productLimit / option.operand1);
                int product = option.operand1 * option.operand2;
                option.addValue = targetValue - product;
                option.calculatedValue = product + option.addValue;
            }
            else if (useMultiply)
            {
                option.operation = MathOperation.Multiply;
                option.operand1 = Random.Range(2, 6);
                option.operand2 = Mathf.Max(1, targetValue / option.operand1);
                option.calculatedValue = option.operand1 * option.operand2;
            }
            else
            {
                option.operation = MathOperation.Add;
                int split = Random.Range((int)(targetValue * 0.4f), (int)(targetValue * 0.8f));
                option.baseValue = split;
                option.addValue = targetValue - split;
                option.calculatedValue = targetValue;
            }

            return option;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        #endregion

        #region CHOICE HANDLING

        private void HandlePrizeSelected(PrizeOption selectedOption)
        {
            if (!waitingForChoice) return;

            waitingForChoice = false;

            int highestValue = GetHighestValue(currentOptions);
            bool isCorrect = selectedOption.calculatedValue == highestValue;

            Debug.Log($"[KiqqiPrizePickManager] Prize selected: {selectedOption.calculatedValue}, Highest: {highestValue}, Correct: {isCorrect}");

            ProcessChoice(isCorrect, selectedOption.calculatedValue);

            if (timeExpired)
            {
                EndSession();
            }
        }

        private void HandleChoiceTimeout()
        {
            if (!waitingForChoice) return;

            Debug.Log("[KiqqiPrizePickManager] Choice timed out - applying penalty.");

            waitingForChoice = false;
            ProcessChoice(false, 0);

            if (timeExpired)
            {
                EndSession();
            }
        }

        private void ProcessChoice(bool isCorrect, int selectedValue)
        {
            if (isCorrect)
            {
                currentTopPrize = selectedValue;
                currentComboStreak++;

                int scoreToAdd = currentConfig.correctScore;

                if (currentComboStreak >= currentConfig.comboThreshold)
                {
                    scoreToAdd = Mathf.RoundToInt(scoreToAdd * currentConfig.comboMultiplier);
                    Debug.Log($"[KiqqiPrizePickManager] Combo active! Streak: {currentComboStreak}, Multiplier: {currentConfig.comboMultiplier}x");
                }

                sessionScore += scoreToAdd;
                masterGame.AddScore(scoreToAdd);

                view?.ShowFeedback(true, scoreToAdd, currentComboStreak);
                view?.UpdateTopPrize(currentTopPrize);

                challengesCompleted++;

                Debug.Log($"[KiqqiPrizePickManager] Correct! Score +{scoreToAdd}, Total: {sessionScore}, Combo: {currentComboStreak}, Challenges completed: {challengesCompleted}");
            }
            else
            {
                currentComboStreak = 0;

                int penalty = currentConfig.wrongPenalty;
                sessionScore = Mathf.Max(0, sessionScore - penalty);
                masterGame.AddScore(-penalty);

                view?.ShowFeedback(false, -penalty, 0);

                Debug.Log($"[KiqqiPrizePickManager] Wrong! Penalty: -{penalty}, Total: {sessionScore}, Combo reset");
            }

            view?.UpdateScore(sessionScore);

            StartCoroutine(ShowNextChoicesAfterDelay());
        }

        private IEnumerator ShowNextChoicesAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);

            if (!timeExpired)
            {
                GenerateAndShowChoices();
            }
            else
            {
                EndSession();
            }
        }

        private int GetHighestValue(List<PrizeOption> options)
        {
            int highest = 0;
            foreach (var option in options)
            {
                if (option.calculatedValue > highest)
                {
                    highest = option.calculatedValue;
                }
            }
            return highest;
        }

        #endregion

        #region SESSION END

        private void EndSession()
        {
            if (isComplete) return;

            sessionRunning = false;
            waitingForChoice = false;

            Debug.Log($"[KiqqiPrizePickManager] Session ended. Final score: {sessionScore}, Challenges: {challengesCompleted}/{currentConfig.challengesPerLevel}");

            bool playerWon = challengesCompleted >= currentConfig.challengesPerLevel;

            view?.StopTimer();
            view?.HideChoices();

            CompleteMiniGame(sessionScore, playerWon);
        }

        #endregion

        #region RESET

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();

            sessionRunning = false;
            timeExpired = false;
            waitingForChoice = false;
            currentTopPrize = 0;
            currentComboStreak = 0;
            challengesCompleted = 0;
            currentOptions = null;

            Debug.Log("[KiqqiPrizePickManager] Reset complete.");
        }

        #endregion
    }
}
