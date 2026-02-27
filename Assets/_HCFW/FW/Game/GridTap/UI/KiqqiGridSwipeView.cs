using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// View layer for Grid Swipe (8-puzzle).
    /// Shows numbered tiles and an empty cell sprite.
    /// </summary>
    public class KiqqiGridSwipeView : KiqqiGameViewBase
    {
        [Header("UI References")]
        public Transform gridRoot;          // 9 children (row-major or any consistent order)
        public Text resultLabel;            // optional

        private Image[,] cellImages = new Image[3, 3];
        private Text[,] cellTexts = new Text[3, 3];

        [Header("Cell Sprites")]
        public Sprite tileSprite;   // for numbered tiles (1..8)
        public Sprite emptySprite;  // for the 0 cell (empty)

        public void SetGridInteractable(bool value)
        {
            if (!gridRoot) return;
            // We don't use per-cell Buttons for sliding — swipes come from KiqqiInputController.
            // If you decide to use buttons later, wire interactables here.
        }

        private void OnEnable()
        {
            if (!gridRoot) return;

            int idx = 0;
            var mgr = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiGridSwipeManager;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (idx >= gridRoot.childCount) break;
                    var child = gridRoot.GetChild(idx);

                    var img = child.GetComponent<Image>();
                    var txt = child.GetComponentInChildren<Text>();
                    var cell = child.GetComponent<KiqqiGridSwipeCell>();
                    if (!cell) cell = child.gameObject.AddComponent<KiqqiGridSwipeCell>();

                    cell.Init(mgr, c, r);

                    cellImages[c, r] = img;
                    cellTexts[c, r] = txt;
                    idx++;
                }
            }

            if (resultLabel) resultLabel.text = "";
            gridRoot.gameObject.SetActive(false); // shown after countdown
        }


        public override void OnShow()
        {
            var app = KiqqiAppManager.Instance;
            var game = app.Game;

            // ----- RESUME PATH -----
            if (game.ResumeRequested && game.currentMiniGame is KiqqiGridSwipeManager mgrExisting)
            {
                Debug.Log("[KiqqiGridSwipeView] Resuming GridSwipe session.");

                // Ensure CanvasGroup fully active
                var cg = GetComponent<CanvasGroup>();
                if (cg)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                if (countdownLabel) countdownLabel.gameObject.SetActive(false);
                if (gridRoot) gridRoot.gameObject.SetActive(true);

                mgrExisting.ResumeFromPause(this);
                timerRunning = true;

                game.ResumeRequested = false;
                return;
            }

            // ----- FRESH START PATH -----
            base.OnShow(); // starts countdown
            if (resultLabel) resultLabel.text = "";
            if (gridRoot) gridRoot.gameObject.SetActive(false);
        }

        public void RefreshGrid(int[,] grid)
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int val = grid[c, r];
                    var img = cellImages[c, r];
                    if (!img) continue;

                    img.color = (val == 1) ? Color.red : Color.white;
                }
            }
        }


        public void ShowResult(string text)
        {
            if (resultLabel) resultLabel.text = text;
        }

        protected override void OnCountdownFinished()
        {
            if (gridRoot) gridRoot.gameObject.SetActive(true);
            
            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiGridSwipeManager mgr)
            {
                Debug.Log("[KiqqiGridSwipeView] Countdown finished — starting mini-game via GameManager reference.");
                mgr.StartMiniGame();
            }
            else
            {
                Debug.LogWarning("[KiqqiGridSwipeView] No active KiqqiGridSwipeManager found in GameManager.");
            }
        }

        protected override void OnTimeUp()
        {
            base.OnTimeUp();
            Debug.Log($"[{GetType().Name}] Time reached 00:00 - Player wins");

            timerRunning = false;

            // Treat as a win and complete the mini-game
            var gm = KiqqiAppManager.Instance.Game.currentMiniGame;
            if (gm != null)
                gm.CompleteMiniGame(gm.sessionScore, true);

            KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect2");
        }


    }
}
