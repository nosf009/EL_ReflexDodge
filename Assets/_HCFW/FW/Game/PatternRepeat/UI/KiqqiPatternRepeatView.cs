using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// View layer for Pattern Repeat.
    /// - Manages 3x3 grid visuals (Images on each child).
    /// - Handles countdown start, resume, and per-cell button clicks.
    /// - Provides API for lighting pattern and confirming taps.
    /// </summary>
    public class KiqqiPatternRepeatView : KiqqiGameViewBase
    {
        [Header("UI References")]
        public Transform gridRoot;        // 9 children, each with Image + Button
        public Text resultLabel;          // optional

        private Image[,] cellImages = new Image[3, 3];
        private Button[,] cellButtons = new Button[3, 3];

        [Header("Colors")]
        public Color idleColor = Color.white;
        public Color litColor = new Color(1f, 0.9f, 0.3f, 1f);
        public Color confirmColor = new Color(0.5f, 1f, 0.5f, 1f);
        public Color wrongColor = new Color(1f, 0.4f, 0.4f, 1f);

        public void EnableGrid(bool value)
        {
            if (!gridRoot) return;
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (cellButtons[c, r]) cellButtons[c, r].interactable = value;
        }

        private void OnEnable()
        {
            if (!gridRoot) return;

            int idx = 0;
            var mgr = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiPatternRepeatManager;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (idx >= gridRoot.childCount) break;
                    var child = gridRoot.GetChild(idx);
                    var img = child.GetComponent<Image>();
                    var btn = child.GetComponent<Button>();
                    if (!btn) btn = child.gameObject.AddComponent<Button>();

                    int cc = c, rr = r;
                    btn.onClick.RemoveAllListeners();
                    if (mgr != null)
                        btn.onClick.AddListener(() => mgr.HandleCellPressed(cc, rr));

                    cellImages[c, r] = img;
                    cellButtons[c, r] = btn;

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
            if (game.ResumeRequested && game.currentMiniGame is KiqqiPatternRepeatManager mgrExisting)
            {
                Debug.Log("[KiqqiPatternRepeatView] Resuming PatternRepeat session.");

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

        public void PrepareGrid(bool fromResume = false)
        {
            // Reset all cells to idle color
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (cellImages[c, r]) cellImages[c, r].color = idleColor;
                }
            }

            if (!fromResume)
                EnableGrid(false);
        }

        public void ShowPattern(List<Vector2Int> pattern)
        {
            PrepareGrid();
            foreach (var p in pattern)
                if (InBounds(p) && cellImages[p.x, p.y]) cellImages[p.x, p.y].color = litColor;
        }

        public void HidePattern()
        {
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (cellImages[c, r]) cellImages[c, r].color = idleColor;
        }

        public void MarkAsConfirmed(int col, int row)
        {
            if (!InBounds(col, row)) return;
            if (cellImages[col, row]) cellImages[col, row].color = confirmColor;
        }

        public void FlashWrong(int col, int row)
        {
            if (!InBounds(col, row)) return;
            if (cellImages[col, row]) cellImages[col, row].color = wrongColor;
        }

        public void ShowResult(string text)
        {
            if (resultLabel) resultLabel.text = text;
        }

        protected override void OnCountdownFinished()
        {
            if (gridRoot) gridRoot.gameObject.SetActive(true);

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiPatternRepeatManager mgr)
            {
                Debug.Log("[KiqqiPatternRepeatView] Countdown finished — starting mini-game via GameManager reference.");
                mgr.StartMiniGame();
            }
            else
            {
                Debug.LogWarning("[KiqqiPatternRepeatView] No active KiqqiPatternRepeatManager found in GameManager.");
            }
        }

        private bool InBounds(Vector2Int p) => InBounds(p.x, p.y);
        private bool InBounds(int c, int r) => c >= 0 && c < 3 && r >= 0 && r < 3;
    }
}
