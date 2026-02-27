using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Main gameplay manager for the "Other Ones" (Poker Face) mini-game.
    /// Flow Summary:
    /// - Spawns several elements per level (mostly identical, one unique)
    /// - Player must tap the unique (different) one before time runs out
    /// - Correct tap > +Score; Wrong tap > penalty
    /// - On completion > triggers result flow via CompleteMiniGame()
    /// </summary>
    public class KiqqiPokerFaceManager : KiqqiMiniGameManagerBase
    {
        #region INSPECTOR CONFIGURATION
        [Header("Gameplay Settings")]
        public int minElements = 3;
        public int maxElements = 12;

        [SerializeField] private KiqqiPokerFaceLevelManager levelLogic;
        #endregion

        #region RUNTIME STATE
        private bool sessionRunning = false;
        private Coroutine nextDealRoutine;

        protected KiqqiPokerFaceView view;
        protected KiqqiInputController input;

        protected int oddIndex = -1;          // Index of the unique card
        protected bool inputEnabled = false;  // Prevents double-taps or mid-animation input
        protected int totalElements = 0;      // Cards in current round
        #endregion

        #region CORE INITIALIZATION (DO NOT MODIFY)
        public override System.Type GetAssociatedViewType() => typeof(KiqqiPokerFaceView);

        public override void Initialize(KiqqiAppManager context)
        {
            base.Initialize(context);

            view = context.UI.GetView<KiqqiPokerFaceView>();
            input = context.Input;

            Debug.Log("[KiqqiPokerFaceManager] Initialized.");
        }

        public override void OnMiniGameExit()
        {
            base.OnMiniGameExit();

            // Stop any ongoing deal or coroutine
            if (nextDealRoutine != null)
            {
                StopCoroutine(nextDealRoutine);
                nextDealRoutine = null;
            }

            // Reset session state
            sessionRunning = false;
            inputEnabled = false;
            isActive = false;
            isComplete = true;

            Debug.Log("[KiqqiPokerFaceManager] OnMiniGameExit -> cleaned up and deactivated.");
        }

        #endregion

        #region GAMEPLAY LIFECYCLE (EDITABLE REGION)

        /// <summary>
        /// Called when the mini-game begins (after countdown & fade).
        /// </summary>
        public override void StartMiniGame()
        {
            base.StartMiniGame();

            sessionScore = 0;
            masterGame.CurrentScore = 0;
            inputEnabled = false;
            sessionRunning = true;

            Debug.Log("[KiqqiPokerFaceManager] Timed session started.");

            int level = app.Levels.currentLevel;
            totalElements = ComputeElementCount(level);
            view?.BuildElements(totalElements);
            inputEnabled = true;
        }


        /// <summary>
        /// Called when the player taps a card.
        /// Validates the choice, updates score/penalty, and triggers feedback.
        /// </summary>
        public virtual void HandleElementTapped(int index)
        {
            if (!isActive || isComplete || !inputEnabled || !sessionRunning)
                return;

            inputEnabled = false;
            KiqqiAppManager.Instance.Audio.PlaySfx("buttonclick");

            bool correct = index == oddIndex;

            if (correct)
            {
                // Visual feedback (particles + animation)
                if (view && index >= 0)
                {
                    var rt = view.GetCardRect(index);
                    if (rt) view.PlayCorrectParticlesAtCard(rt);
                }

                int level = app.Levels.currentLevel;
                int correctScore = levelLogic ? levelLogic.GetScoreForLevel(level) : 100;

                sessionScore += correctScore;
                masterGame.AddScore(correctScore);
                view?.UpdateScoreLabel(masterGame.CurrentScore);
                view?.ShowFeedback(true, index);
                app.Levels.NextLevel();
            }
            else
            {
                int level = app.Levels.currentLevel;
                int penalty = levelLogic ? levelLogic.GetPenaltyForLevel(level) : 10;

                sessionScore = Mathf.Max(0, sessionScore - penalty);
                masterGame.AddScore(-penalty);
                view?.UpdateScoreLabel(masterGame.CurrentScore);

                view?.ShowFeedback(false, index);
                Debug.Log($"[KiqqiPokerFaceManager] Wrong card! Penalty = {penalty}");
            }

            if (timeExpired)
            {
                Debug.Log("[PokerFaceManager] Final pick made � ending session.");
                EndSession();
                return;
            }
        }


        /// <summary>
        /// Called by the view after feedback animation completes.
        /// Rebuilds elements for the next round.
        /// </summary>
        public void TriggerNextDealAfterFeedback()
        {
            if (!sessionRunning) return;

            // NEW: check time left
            float timeLeft = (view != null) ? view.RemainingTime : 999f;
            if (timeLeft <= 2f)
            {
                Debug.Log("[PokerFace] Skipping new deal because time is almost over.");
                return;
            }

            int level = app.Levels.currentLevel;
            totalElements = ComputeElementCount(level);
            view?.BuildElements(totalElements);
            inputEnabled = true;
        }

        private bool timeExpired = false;

        public void NotifyTimeExpired()
        {
            timeExpired = true;
            Debug.Log("[PokerFaceManager] Time expired � one final pick allowed.");
        }


        #endregion

        #region SUPPORT / UTILITY METHODS (SAFE TO EDIT LIGHTLY)

        /// <summary>
        /// Computes number of cards for the given level.
        /// </summary>
        protected int ComputeElementCount(int level)
        {
            return levelLogic ? levelLogic.GetElementCount(level) : 4;
        }


        /// <summary>
        /// Ends the active play session and triggers result flow.
        /// </summary>
        public void EndSession()
        {
            if (!sessionRunning) return;

            sessionRunning = false;
            inputEnabled = false;

            Debug.Log($"[KiqqiPokerFaceManager] Session ended. Final Score={sessionScore}");
            CompleteMiniGame(sessionScore, true);
        }


        /// <summary>
        /// Re-enables gameplay after pause, restoring state.
        /// </summary>
        public void ResumeFromPause(KiqqiPokerFaceView v)
        {
            view = v ?? view;
            if (view.pauseButton) view.pauseButton.interactable = true;

            isActive = true;
            isComplete = false;
            inputEnabled = true;

            view?.UpdateScoreLabel(masterGame.CurrentScore);

            Debug.Log("[KiqqiPokerFaceManager] Resumed from pause.");
        }


        /// <summary>
        /// Assigns which card index is unique for this round.
        /// Called by the view after building elements.
        /// </summary>
        public void SetUniqueIndex(int index)
        {
            oddIndex = index;
        }

        #endregion
    }
}
