using System.ComponentModel;
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Base class for all Kiqqi mini-game managers.
    /// Encapsulates per-game logic, while KiqqiGameManager handles global flow.
    /// </summary>
    public abstract class KiqqiMiniGameManagerBase : MonoBehaviour
    {
        [HideInInspector]
        public string gameID = "undefined";

        [HideInInspector]
        public string displayName = "Unnamed MiniGame";

        [Header("State")]
        public bool isActive;
        public bool isComplete;
        public int sessionScore;

        protected KiqqiAppManager app;
        protected KiqqiGameManager masterGame;

        public virtual System.Type GetAssociatedViewType()
        {
            return typeof(KiqqiGameViewBase);
        }

        public virtual void Initialize(KiqqiAppManager context)
        {
            app = context;
            masterGame = app.Game;

            if (app.Data != null && app.Data.gameDefinition != null)
            {
                gameID = app.Data.gameDefinition.gameId;
                displayName = app.Data.gameDefinition.displayName;
                Debug.Log($"[KiqqiMiniGameManagerBase] Auto-populated from GameDefinition: ID={gameID}, Name={displayName}");
            }

            Debug.Log($"[KiqqiMiniGameManagerBase] Initialized for {gameID}");
        }

        /// <summary>Called when the mini-game becomes active (after UI shown, etc.).</summary>
        public virtual void StartMiniGame()
        {
            if (isActive)
            {
                Debug.LogWarning($"[KiqqiMiniGameManagerBase] StartMiniGame called while already active ({displayName})");
                return;
            }

            isActive = true;
            isComplete = false;
            sessionScore = 0;
            masterGame.State = KiqqiGameManager.GameState.Playing;
            Debug.Log($"[KiqqiMiniGameManagerBase] StartMiniGame: {displayName}");
        }

        /// <summary>Called every frame if you want to update the game loop manually.</summary>
        public virtual void TickMiniGame()
        {
            // Derived classes can override.
        }

        public event System.Action<int, bool> OnCompleted;
        
        /// <summary>Mark game as finished and report to master manager.</summary>
        public virtual void CompleteMiniGame(int finalScore, bool playerWon = true)
        {
            if (isComplete) return;         // guard: already ended once
            isComplete = true;
            isActive = false;
            sessionScore = finalScore;

            Debug.Log($"[MiniGameBase] CompleteMiniGame() won={playerWon}, finalScore={finalScore}");

            OnCompleted?.Invoke(finalScore, playerWon);

            if (playerWon)
                KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect");
            else
                KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");

            Debug.Log($"[KiqqiMiniGameManagerBase] CompleteMiniGame ({displayName}) score={finalScore} won={playerWon}");

            // Inform GameManager once
            masterGame.AddScore(finalScore);
            masterGame.EndGame(playerWon);

            // Show results exactly once
            app.UI.ShowView<KiqqiResultsView>();
        }


        /// <summary>Reset to initial state (used for restart or replay).</summary>
        public virtual void ResetMiniGame()
        {
            isActive = false;
            isComplete = false;
            sessionScore = 0;
            Debug.Log($"[KiqqiMiniGameManagerBase] ResetMiniGame ({displayName})");
        }

        /// <summary>Optional: called when leaving or unloading the mini-game scene.</summary>
        public virtual void OnMiniGameExit()
        {
            Debug.Log($"[KiqqiMiniGameManagerBase] OnMiniGameExit ({displayName})");
        }

    }
}
