using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Kiqqi.Framework
{
    public struct Fraction
    {
        public int numerator;
        public int denominator;

        public Fraction(int num, int denom)
        {
            numerator = num;
            denominator = denom;
        }

        public float Value => denominator != 0 ? (float)numerator / denominator : 0f;

        public override string ToString() => $"{numerator}/{denominator}";

        public static bool operator <(Fraction a, Fraction b) => a.Value < b.Value;
        public static bool operator >(Fraction a, Fraction b) => a.Value > b.Value;
        public static bool operator ==(Fraction a, Fraction b) => Mathf.Approximately(a.Value, b.Value);
        public static bool operator !=(Fraction a, Fraction b) => !Mathf.Approximately(a.Value, b.Value);

        public override bool Equals(object obj)
        {
            if (obj is Fraction other)
                return this == other;
            return false;
        }

        public override int GetHashCode() => (numerator, denominator).GetHashCode();
    }

    public class KiqqiFractionRushManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR

        [Header("Core References")]
        [SerializeField] private KiqqiFractionRushLevelManager levelLogic;

        #endregion

        #region STATE

        private FractionRushDifficultyConfig currentConfig;
        private List<Fraction> currentSequence;
        private List<int> playerSelectionOrder;
        private bool sessionRunning = false;
        private bool sequenceActive = false;
        private float sequenceTimer = 0f;
        private int consecutiveCorrect = 0;

        protected KiqqiFractionRushView view;

        #endregion

        #region INITIALIZATION

        public override System.Type GetAssociatedViewType() => typeof(KiqqiFractionRushView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiFractionRushView>();

            if (view != null)
            {
                view.SetManager(this);
            }

            Debug.Log("[KiqqiFractionRushManager] Initialized.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE

        public override void StartMiniGame()
        {
            base.StartMiniGame();

            if (view == null)
            {
                Debug.LogError("[FractionRush] View reference is null!");
                return;
            }

            if (levelLogic == null)
            {
                Debug.LogError("[FractionRush] LevelLogic reference is null! Assign it in the inspector.");
                return;
            }

            KiqqiAppManager app = KiqqiAppManager.Instance;
            int currentLevel = app.Levels.currentLevel;
            Debug.Log($"[FractionRush] Current level from app.Levels: {currentLevel}");

            currentConfig = levelLogic.GetDifficultyConfig(currentLevel);

            Debug.Log($"[FractionRush] Config loaded - {currentConfig.fractionCount} fractions, {currentConfig.sequenceTimeLimit}s per sequence");

            sessionRunning = false;
            sequenceActive = false;
            consecutiveCorrect = 0;
            sessionScore = 0;
            app.Game.CurrentScore = 0;
            currentSequence = new List<Fraction>();
            playerSelectionOrder = new List<int>();

            Debug.Log($"[FractionRush] StartMiniGame() - Level {currentLevel}");
        }

        public void OnCountdownFinished()
        {
            if (playerSelectionOrder == null)
            {
                playerSelectionOrder = new List<int>();
                Debug.LogWarning("[OnCountdownFinished] playerSelectionOrder was null, initialized");
            }

            if (currentSequence == null)
            {
                currentSequence = new List<Fraction>();
                Debug.LogWarning("[OnCountdownFinished] currentSequence was null, initialized");
            }

            sessionRunning = true;
            GenerateNewSequence();
        }

        public override void ResetMiniGame()
        {
            base.ResetMiniGame();

            sessionRunning = false;
            sequenceActive = false;
            sequenceTimer = 0f;
            consecutiveCorrect = 0;

            if (currentSequence != null)
            {
                currentSequence.Clear();
            }

            if (playerSelectionOrder != null)
            {
                playerSelectionOrder.Clear();
            }

            if (view != null)
            {
                view.ClearSequence();
            }

            Debug.Log("[KiqqiFractionRushManager] ResetMiniGame - state cleared");
        }

        public void ResumeFromPause(KiqqiFractionRushView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;
            sessionRunning = true;

            KiqqiAppManager.Instance.Game.CurrentScore = sessionScore;
            view?.AddScore(0);

            Debug.Log("[KiqqiFractionRushManager] Resumed from pause");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            sessionRunning = false;
            isActive = false;
            isComplete = true;

            if (view != null)
            {
                view.ClearSequence();
            }

            if (currentSequence != null)
            {
                currentSequence.Clear();
            }

            Debug.Log("[KiqqiFractionRushManager] OnMiniGameExit -> cleaned up.");
        }

        #endregion

        #region GAME LOOP

        public override void TickMiniGame()
        {
            if (!sessionRunning) return;

            if (sequenceActive)
            {
                sequenceTimer -= Time.deltaTime;

                view?.UpdateSequenceTimer(sequenceTimer, currentConfig.sequenceTimeLimit);

                if (sequenceTimer <= 0f)
                {
                    OnSequenceTimeout();
                }
            }

            if (view != null && view.RemainingTime <= 0f)
            {
                if (sequenceActive)
                {
                    return;
                }

                sessionRunning = false;
                EndSession();
            }
        }

        #endregion

        #region SEQUENCE GENERATION

        private void GenerateNewSequence()
        {
            if (view.RemainingTime <= 3f)
            {
                Debug.Log("[GenerateNewSequence] Less than 3 seconds remaining, waiting for timer to expire");
                return;
            }

            currentSequence = GenerateFractionSequence(currentConfig.fractionCount, currentConfig);
            playerSelectionOrder.Clear();
            sequenceTimer = currentConfig.sequenceTimeLimit;
            sequenceActive = true;

            view.DisplaySequence(currentSequence);

            Debug.Log($"[GenerateNewSequence] Created sequence: {string.Join(", ", currentSequence.Select(f => f.ToString()))}");
        }

        private List<Fraction> GenerateFractionSequence(int count, FractionRushDifficultyConfig config)
        {
            List<Fraction> fractions = new List<Fraction>();
            HashSet<float> usedValues = new HashSet<float>();

            int attempts = 0;
            int maxAttempts = 100;

            while (fractions.Count < count && attempts < maxAttempts)
            {
                attempts++;

                int denominator = Random.Range(config.minDenominator, config.maxDenominator + 1);
                int maxNumerator = config.allowImproperFractions ? denominator * 2 : denominator - 1;
                int numerator = Random.Range(1, maxNumerator + 1);

                Fraction newFraction = new Fraction(numerator, denominator);
                float value = newFraction.Value;

                if (usedValues.Contains(value))
                {
                    continue;
                }

                bool tooSimilar = false;
                foreach (float existingValue in usedValues)
                {
                    if (Mathf.Abs(value - existingValue) < config.minimumDifference)
                    {
                        tooSimilar = true;
                        break;
                    }
                }

                if (!tooSimilar)
                {
                    fractions.Add(newFraction);
                    usedValues.Add(value);
                }
            }

            if (fractions.Count < count)
            {
                Debug.LogWarning($"[GenerateFractionSequence] Could only generate {fractions.Count}/{count} unique fractions");
            }

            return fractions;
        }

        #endregion

        #region GAME LOGIC

        public void OnFractionClicked(int fractionIndex)
        {
            Debug.Log($"[OnFractionClicked] Fraction {fractionIndex} clicked. SequenceActive: {sequenceActive}");

            if (!sequenceActive) return;

            if (playerSelectionOrder.Contains(fractionIndex))
            {
                Debug.Log($"[FractionClicked] Fraction {fractionIndex} already selected, ignoring");
                return;
            }

            int expectedIndex = GetExpectedNextIndex();
            bool isCorrect = (fractionIndex == expectedIndex);

            Fraction clickedFraction = currentSequence[fractionIndex];
            Debug.Log($"[FractionClicked] Clicked {clickedFraction} at index {fractionIndex}, expected index {expectedIndex}, correct: {isCorrect}");

            playerSelectionOrder.Add(fractionIndex);

            if (!isCorrect)
            {
                OnWrongSelection(fractionIndex);
            }
            else
            {
                OnCorrectSelection(fractionIndex);

                if (playerSelectionOrder.Count == currentSequence.Count)
                {
                    OnSequenceCompleted();
                }
            }
        }

        private int GetExpectedNextIndex()
        {
            List<Fraction> sortedFractions = currentSequence.OrderBy(f => f.Value).ToList();

            int nextPositionInSorted = playerSelectionOrder.Count;

            if (nextPositionInSorted >= sortedFractions.Count)
                return -1;

            Fraction expectedFraction = sortedFractions[nextPositionInSorted];

            for (int i = 0; i < currentSequence.Count; i++)
            {
                if (!playerSelectionOrder.Contains(i))
                {
                    Fraction currentFraction = currentSequence[i];
                    if (currentFraction.numerator == expectedFraction.numerator && 
                        currentFraction.denominator == expectedFraction.denominator)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void OnCorrectSelection(int fractionIndex)
        {
            view?.ShowFractionCorrect(fractionIndex);

            KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect");

            Debug.Log($"[CorrectSelection] Fraction {fractionIndex} correct");
        }

        private void OnWrongSelection(int fractionIndex)
        {
            sessionScore -= currentConfig.wrongPenalty;
            sessionScore = Mathf.Max(0, sessionScore);
            KiqqiAppManager.Instance.Game.CurrentScore = sessionScore;

            consecutiveCorrect = 0;

            view?.ShowFractionWrong(fractionIndex);
            view?.AddScore(0);

            KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");

            Debug.Log($"[WrongSelection] Fraction {fractionIndex} wrong, penalty: {currentConfig.wrongPenalty}, new score: {sessionScore}");

            playerSelectionOrder.Remove(fractionIndex);
        }

        private void OnSequenceCompleted()
        {
            sequenceActive = false;

            consecutiveCorrect++;

            int baseScore = currentConfig.correctScore;
            float multiplier = 1f;

            if (consecutiveCorrect >= currentConfig.comboThreshold)
            {
                multiplier = currentConfig.comboMultiplier;
            }

            int scoreGain = Mathf.RoundToInt(baseScore * multiplier);
            sessionScore += scoreGain;
            KiqqiAppManager.Instance.Game.CurrentScore = sessionScore;

            view?.ShowSequenceComplete(consecutiveCorrect >= currentConfig.comboThreshold);
            view?.AddScore(0);

            KiqqiAppManager.Instance.Levels.NextLevel();

            Debug.Log($"[SequenceCompleted] Score +{scoreGain} (base {baseScore} x {multiplier}), streak: {consecutiveCorrect}, total: {sessionScore}");

            StartCoroutine(TransitionToNextSequence());
        }

        private void OnSequenceTimeout()
        {
            sequenceActive = false;

            sessionScore -= currentConfig.timeoutPenalty;
            sessionScore = Mathf.Max(0, sessionScore);
            KiqqiAppManager.Instance.Game.CurrentScore = sessionScore;

            consecutiveCorrect = 0;

            view?.ShowSequenceTimeout();
            view?.AddScore(0);

            Debug.Log($"[SequenceTimeout] Penalty: {currentConfig.timeoutPenalty}, new score: {sessionScore}");

            StartCoroutine(TransitionToNextSequence());
        }

        private IEnumerator TransitionToNextSequence()
        {
            yield return new WaitForSeconds(1f);

            view.ClearSequence();

            yield return new WaitForSeconds(0.3f);

            int currentLevel = app.Levels.currentLevel;
            currentConfig = levelLogic.GetDifficultyConfig(currentLevel);

            GenerateNewSequence();
        }

        #endregion

        #region SESSION END

        private void EndSession()
        {
            view.ClearSequence();

            CompleteMiniGame(sessionScore, true);

            Debug.Log($"[EndSession] Time up. Final score: {sessionScore}");
        }

        #endregion
    }
}
