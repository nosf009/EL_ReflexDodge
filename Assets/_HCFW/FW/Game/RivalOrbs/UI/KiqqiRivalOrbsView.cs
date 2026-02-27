using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiRivalOrbsView : KiqqiGameViewBase
    {
        [Header("Rival Orbs Specific")]
        [SerializeField] private RectTransform interactionArea;

        public event Action<Vector2> OnBarrierSwipe;

        private KiqqiRivalOrbsManager manager;
        private bool isGameActive = false;

        private Vector2 lastPointerPosition;
        private bool isDragging = false;
        private Canvas parentCanvas;

        #region VIEW LIFECYCLE

        public override void OnShow()
        {
            base.OnShow();

            manager = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiRivalOrbsManager;
            if (manager == null)
            {
                Debug.LogError("[RivalOrbsView] Manager not found!");
                return;
            }

            parentCanvas = GetComponentInParent<Canvas>();

            if (interactionArea == null)
            {
                Debug.LogWarning("[RivalOrbsView] interactionArea not assigned in inspector!");
            }

            isGameActive = false;
        }

        protected override void OnCountdownFinished()
        {
            base.OnCountdownFinished();

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiRivalOrbsManager mgr)
            {
                mgr.StartMiniGame();
            }

            isGameActive = true;
            timerRunning = true;
        }

        protected override void OnTimeUp()
        {
            base.OnTimeUp();

            isGameActive = false;

            if (manager != null)
            {
                manager.OnTimeExpired();
            }

            Debug.Log("[RivalOrbsView] Time's up!");
        }

        #endregion

        #region INPUT HANDLING

        private void LateUpdate()
        {
            if (!isGameActive) return;

            manager?.TickMiniGame();
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 localPoint;
                if (IsPointerOverInteractionArea(Input.mousePosition, out localPoint))
                {
                    lastPointerPosition = Input.mousePosition;
                    isDragging = true;
                }
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector2 currentPos = Input.mousePosition;
                Vector2 delta = currentPos - lastPointerPosition;

                if (delta.magnitude > 0.1f)
                {
                    OnBarrierSwipe?.Invoke(delta);
                }

                lastPointerPosition = currentPos;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }

        private bool IsPointerOverInteractionArea(Vector2 screenPosition, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;

            if (interactionArea == null || parentCanvas == null)
                return true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                interactionArea,
                screenPosition,
                parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                out localPoint
            );

            return interactionArea.rect.Contains(localPoint);
        }

        #endregion

        #region SCORE UPDATE

        public void UpdateScore(int newScore)
        {
            if (scoreValueText != null)
            {
                scoreValueText.text = newScore.ToString("00000");
            }
        }

        #endregion
    }
}
