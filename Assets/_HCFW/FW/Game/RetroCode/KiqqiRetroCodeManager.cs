using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public enum MathOperationType
    {
        Square,
        SquareRoot
    }

    public class MathQuestion
    {
        public int displayNumber;
        public int correctAnswer;
        public MathOperationType operationType;
        public List<int> wrongAnswers;
    }

    public class KiqqiRetroCodeManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Gameplay Settings")]
        [SerializeField] private KiqqiRetroCodeLevelManager levelLogic;

        [Header("Timer Settings")]
        [Tooltip("Don't show new questions when this much time is left (seconds)")]
        public float noNewQuestionsThreshold = 2f;

        #endregion

        #region RUNTIME STATE

        private bool sessionRunning = false;
        private bool timeExpired = false;
        private bool waitingForAnswer = false;

        protected KiqqiRetroCodeView view;
        protected KiqqiInputController input;

        private MathQuestion currentQuestion;
        private int currentComboStreak = 0;
        private int questionsAnswered = 0;
        
        private readonly List<int> recentQuestionNumbers = new();
        private const int QUESTION_HISTORY_SIZE = 2;

        #endregion

        #region CORE INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiRetroCodeView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiRetroCodeView>();
            input = context.Input;

            Debug.Log("[KiqqiRetroCodeManager] Initialized.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            sessionRunning = false;
            timeExpired = false;
            waitingForAnswer = false;
            isActive = false;
            isComplete = true;

            Debug.Log("[KiqqiRetroCodeManager] OnMiniGameExit -> cleaned up and deactivated.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            sessionScore = 0;
            masterGame.CurrentScore = 0;
            currentComboStreak = 0;
            questionsAnswered = 0;
            sessionRunning = true;
            timeExpired = false;
            waitingForAnswer = false;
            recentQuestionNumbers.Clear();

            Debug.Log("[KiqqiRetroCodeManager] Session started.");

            GenerateAndShowQuestion();
        }

        public void NotifyTimeExpired()
        {
            timeExpired = true;
            Debug.Log("[KiqqiRetroCodeManager] Time expired - waiting for final answer.");
        }

        public void HandleAnswerSelected(int selectedAnswer)
        {
            if (!isActive || isComplete || !sessionRunning)
            {
                Debug.LogWarning("[RetroCode] HandleAnswerSelected called but session not active.");
                return;
            }

            if (!waitingForAnswer)
            {
                Debug.LogWarning("[RetroCode] Answer received but not waiting for one.");
                return;
            }

            waitingForAnswer = false;
            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            bool correct = selectedAnswer == currentQuestion.correctAnswer;

            if (correct)
            {
                currentComboStreak++;
                questionsAnswered++;

                var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
                int baseScore = config.correctScore;
                
                int earnedScore = baseScore;
                if (currentComboStreak >= config.comboThreshold)
                {
                    earnedScore = Mathf.RoundToInt(baseScore * config.comboMultiplier);
                    Debug.Log($"[RetroCode] Combo bonus! Streak={currentComboStreak}, Score={earnedScore}");
                }

                sessionScore += earnedScore;
                masterGame.AddScore(earnedScore);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowFeedback(true, earnedScore, currentComboStreak);

                app.Levels.NextLevel();
            }
            else
            {
                currentComboStreak = 0;

                var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
                int penalty = config.wrongPenalty;

                sessionScore = Mathf.Max(0, sessionScore - penalty);
                masterGame.AddScore(-penalty);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowFeedback(false, penalty, 0);

                Debug.Log($"[KiqqiRetroCodeManager] Wrong answer! Penalty = {penalty}");
            }

            if (timeExpired)
            {
                Debug.Log("[RetroCodeManager] Final answer submitted - ending session.");
                EndSession();
                return;
            }
        }

        public void TriggerNextQuestionAfterFeedback()
        {
            if (!sessionRunning || timeExpired)
                return;

            float timeLeft = view?.RemainingTime ?? 999f;
            if (timeLeft <= noNewQuestionsThreshold)
            {
                Debug.Log($"[RetroCode] Skipping new question - only {timeLeft:F1}s left.");
                return;
            }

            GenerateAndShowQuestion();
        }

        #endregion

        #region QUESTION GENERATION

        private void GenerateAndShowQuestion()
        {
            var config = levelLogic.GetDifficultyConfig(app.Levels.currentLevel);
            
            MathOperationType opType = Random.value <= config.squareOperationChance 
                ? MathOperationType.Square 
                : MathOperationType.SquareRoot;

            int attempts = 0;
            const int maxAttempts = 20;
            
            do
            {
                currentQuestion = GenerateQuestion(opType, config.minNumber, config.maxNumber, config.buttonCount);
                attempts++;
                
                if (!recentQuestionNumbers.Contains(currentQuestion.displayNumber))
                    break;
                    
            } while (attempts < maxAttempts);
            
            recentQuestionNumbers.Add(currentQuestion.displayNumber);
            if (recentQuestionNumbers.Count > QUESTION_HISTORY_SIZE)
            {
                recentQuestionNumbers.RemoveAt(0);
            }
            
            waitingForAnswer = true;
            view?.DisplayQuestion(currentQuestion, config.buttonCount);
        }

        private MathQuestion GenerateQuestion(MathOperationType opType, int minNum, int maxNum, int buttonCount)
        {
            var question = new MathQuestion
            {
                operationType = opType,
                wrongAnswers = new List<int>()
            };

            if (opType == MathOperationType.Square)
            {
                question.displayNumber = Random.Range(minNum, maxNum + 1);
                question.correctAnswer = question.displayNumber * question.displayNumber;
            }
            else
            {
                int root = Random.Range(minNum, maxNum + 1);
                question.displayNumber = root * root;
                question.correctAnswer = root;
            }

            GenerateWrongAnswers(question, buttonCount - 1, minNum, maxNum);

            return question;
        }

        private void GenerateWrongAnswers(MathQuestion question, int count, int minNum, int maxNum)
        {
            var wrongAnswers = new HashSet<int>();
            int correctAnswer = question.correctAnswer;

            int rangeSize = question.operationType == MathOperationType.Square 
                ? (maxNum * maxNum) - (minNum * minNum)
                : maxNum - minNum;

            int maxAttempts = count * 10;
            int attempts = 0;

            while (wrongAnswers.Count < count && attempts < maxAttempts)
            {
                attempts++;

                int offset = Random.Range(1, Mathf.Max(2, rangeSize / 4));
                if (Random.value > 0.5f) offset = -offset;

                int wrongAnswer = correctAnswer + offset;

                if (question.operationType == MathOperationType.Square)
                {
                    wrongAnswer = Mathf.Max(minNum * minNum, wrongAnswer);
                }
                else
                {
                    wrongAnswer = Mathf.Max(minNum, wrongAnswer);
                }

                if (wrongAnswer != correctAnswer && !wrongAnswers.Contains(wrongAnswer) && wrongAnswer > 0)
                {
                    wrongAnswers.Add(wrongAnswer);
                }
            }

            while (wrongAnswers.Count < count)
            {
                int fallback = correctAnswer + Random.Range(-10, 11);
                if (fallback > 0 && fallback != correctAnswer && !wrongAnswers.Contains(fallback))
                {
                    wrongAnswers.Add(fallback);
                }
            }

            question.wrongAnswers.AddRange(wrongAnswers);
        }

        #endregion

        #region SESSION MANAGEMENT

        public void EndSession()
        {
            if (!sessionRunning) return;

            sessionRunning = false;
            waitingForAnswer = false;

            Debug.Log($"[KiqqiRetroCodeManager] Session ended. Final Score={sessionScore}, Questions Answered={questionsAnswered}");
            CompleteMiniGame(sessionScore, true);
        }

        public void ResumeFromPause(KiqqiRetroCodeView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;
            waitingForAnswer = true;

            view?.UpdateScoreLabel(masterGame.CurrentScore);

            Debug.Log("[KiqqiRetroCodeManager] Resumed from pause.");
        }

        #endregion

        #region UTILITY

        public int GetCurrentComboStreak() => currentComboStreak;
        public int GetQuestionsAnswered() => questionsAnswered;

        #endregion
    }
}
