// Assets/_HCFW/FW/Core/KiqqiInputController.cs
using System;
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Universal UI-space input for Kiqqi games.
    /// - Tap / Hold / Release
    /// - Drag / Swipe (direction + magnitude)
    /// - Grid mapping (cell taps + cell-to-cell swipe)
    ///
    /// Works over a target RectTransform (inputArea) in the main Canvas.
    /// Designer-friendly: configure grid size, thresholds, debug.
    /// </summary>
    public class KiqqiInputController : MonoBehaviour
    {
        public enum SwipeDirection { None, Left, Right, Up, Down }

        [Header("Target Area")]
        [Tooltip("UI RectTransform that receives input (e.g., your game board area). If empty, uses the root Canvas rect.")]
        public RectTransform inputArea;
        [Tooltip("Canvas used for coordinate mapping. If empty, tries to find the root canvas.")]
        public Canvas targetCanvas;

        [Header("Grid Mapping")]
        public bool enableGridMapping = true;
        [Min(1)] public int gridColumns = 3;
        [Min(1)] public int gridRows = 3;
        [Tooltip("Optional inner padding (px) inside the input area before mapping to grid.")]
        public Vector2 gridPadding = new Vector2(0, 0);

        [Header("Gestures")]
        [Tooltip("Maximum pointer movement (px) and time (s) to still call it a Tap.")]
        public float tapMaxMovement = 12f;
        public float tapMaxDuration = 0.25f;

        [Tooltip("Min swipe distance in pixels inside input area rect.")]
        public float swipeMinDistance = 50f;

        [Tooltip("Hold starts after this time (s) if pointer is down and not moving much.")]
        public float holdStartTime = 0.35f;
        [Tooltip("Max movement (px) allowed while holding before canceling hold.")]
        public float holdMaxWobble = 14f;

        [Header("Debug")]
        public bool drawGizmos = true;
        public bool logEventsInEditor = true;

        // EVENTS ----------
        public event Action<Vector2> OnTapScreen;                       // screen position
        public event Action<Vector2, float> OnHoldStart;                // screen pos, elapsed
        public event Action<Vector2, float> OnHoldEnd;                  // screen pos, total
        public event Action<Vector2, Vector2, SwipeDirection> OnSwipe;  // startScreen, endScreen, dir

        // Grid-aware
        public event Action<int, int> OnGridTap; // col,row
        public event Action<int, int, int, int, SwipeDirection> OnGridSwipe; // fromC,fromR,toC,toR,dir

        // INTERNAL ----------
        private bool pointerDown;
        private Vector2 downScreenPos;
        private Vector2 lastScreenPos;
        private float downTime;
        private bool holdFired;
        private bool holdCanceled;

        private Camera UICamera => targetCanvas && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;

        private void Awake()
        {
            if (!targetCanvas)
                targetCanvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
#if UNITY_EDITOR
            // mouse primary
            HandlePointer(Input.GetMouseButtonDown(0), Input.GetMouseButton(0), Input.GetMouseButtonUp(0), (Vector2)Input.mousePosition);
#else
            // touch or mouse fallback
            if (Input.touchSupported && Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                bool d = t.phase == TouchPhase.Began;
                bool h = (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary);
                bool u = (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
                HandlePointer(d, h, u, t.position);
            }
            else
            {
                HandlePointer(Input.GetMouseButtonDown(0), Input.GetMouseButton(0), Input.GetMouseButtonUp(0), (Vector2)Input.mousePosition);
            }
#endif
        }

        /// <summary>
        /// Processes all pointer (mouse/touch) states for this frame
        /// and emits appropriate gesture events.
        /// </summary>
        private void HandlePointer(bool down, bool held, bool up, Vector2 screenPos)
        {
            // filter: only react if inside inputArea (if set)
            if (!IsWithinInputArea(screenPos))
            {
                if (up && pointerDown)
                    ResetPointerState(); // in case pointer released outside
                return;
            }

            if (down && !pointerDown)
            {
                pointerDown = true;
                holdFired = false;
                holdCanceled = false;
                downScreenPos = screenPos;
                lastScreenPos = screenPos;
                downTime = Time.unscaledTime;
            }

            if (held && pointerDown)
            {
                lastScreenPos = screenPos;

                // hold logic
                if (!holdFired && !holdCanceled)
                {
                    float elapsed = Time.unscaledTime - downTime;
                    float move = Vector2.Distance(downScreenPos, lastScreenPos);
                    if (elapsed >= holdStartTime && move <= holdMaxWobble)
                    {
                        holdFired = true;
                        if (logEventsInEditor) Debug.Log("[KiqqiInput] HoldStart");
                        OnHoldStart?.Invoke(lastScreenPos, elapsed);
                    }
                    else if (move > holdMaxWobble)
                    {
                        holdCanceled = true; // user moved too much
                    }
                }
            }

            if (up && pointerDown)
            {
                float totalTime = Time.unscaledTime - downTime;
                float totalMove = Vector2.Distance(downScreenPos, screenPos);

                SwipeDirection dir = SwipeDirection.None;
                bool isSwipe = totalMove >= swipeMinDistance;
                if (isSwipe)
                    dir = DetectSwipeDirection(downScreenPos, screenPos);

                if (isSwipe)
                {
                    // Swipe (screen)
                    if (logEventsInEditor) Debug.Log($"[KiqqiInput] Swipe {dir}");
                    OnSwipe?.Invoke(downScreenPos, screenPos, dir);

                    // Grid swipe (cell-to-cell)
                    if (enableGridMapping && TryMapToGrid(downScreenPos, out int c0, out int r0)
                                          && TryMapToGrid(screenPos, out int c1, out int r1))
                    {
                        // For strict 4-dir swipe, snap the target to neighbor in that dir if same start cell
                        if (c0 == c1 && r0 == r1)
                            (c1, r1) = NeighborFrom(c0, r0, dir);

                        // Clamp to grid
                        c1 = Mathf.Clamp(c1, 0, gridColumns - 1);
                        r1 = Mathf.Clamp(r1, 0, gridRows - 1);

                        if (!(c0 == c1 && r0 == r1))
                        {
                            if (logEventsInEditor) Debug.Log($"[KiqqiInput] GridSwipe {c0},{r0} -> {c1},{r1} ({dir})");
                            OnGridSwipe?.Invoke(c0, r0, c1, r1, dir);
                        }
                    }
                }
                else
                {
                    // Tap vs HoldEnd
                    if (totalTime <= tapMaxDuration && totalMove <= tapMaxMovement)
                    {
                        // Tap
                        if (logEventsInEditor) Debug.Log("[KiqqiInput] Tap");
                        OnTapScreen?.Invoke(screenPos);

                        if (enableGridMapping && TryMapToGrid(screenPos, out int col, out int row))
                        {
                            if (logEventsInEditor) Debug.Log($"[KiqqiInput] GridTap {col},{row}");
                            OnGridTap?.Invoke(col, row);
                        }
                    }
                    else
                    {
                        // End hold if it fired
                        if (holdFired)
                        {
                            if (logEventsInEditor) Debug.Log("[KiqqiInput] HoldEnd");
                            OnHoldEnd?.Invoke(screenPos, totalTime);
                        }
                        // else just a long press with move—no explicit event
                    }
                }

                ResetPointerState();
            }
        }

        private void ResetPointerState()
        {
            pointerDown = false;
            holdFired = false;
            holdCanceled = false;
        }

        /// <summary>
        /// Returns true if a screen position is inside the active input area.
        /// </summary>
        private bool IsWithinInputArea(Vector2 screenPos)
        {
            RectTransform r = inputArea ? inputArea : (targetCanvas ? targetCanvas.GetComponent<RectTransform>() : null);
            if (!r) return true; // if nothing specified, accept anywhere
            return RectTransformUtility.RectangleContainsScreenPoint(r, screenPos, UICamera);
        }

        /// <summary>
        /// Converts a screen position to grid cell indices (col,row)
        /// based on configured columns, rows, and padding.
        /// </summary>
        private bool TryMapToGrid(Vector2 screenPos, out int col, out int row)
        {
            col = row = -1;

            RectTransform r = inputArea ? inputArea : (targetCanvas ? targetCanvas.GetComponent<RectTransform>() : null);
            if (!r) return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, screenPos, UICamera, out var local))
                return false;

            // local is centered rect space; convert to 0..1 inside rect
            var rect = r.rect;
            float x01 = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
            float y01 = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);

            // apply inner padding
            float px = gridPadding.x;
            float py = gridPadding.y;

            float innerXMin = rect.xMin + px;
            float innerXMax = rect.xMax - px;
            float innerYMin = rect.yMin + py;
            float innerYMax = rect.yMax - py;

            if (innerXMin >= innerXMax || innerYMin >= innerYMax)
                return false;

            float x01Inner = Mathf.InverseLerp(innerXMin, innerXMax, local.x);
            float y01Inner = Mathf.InverseLerp(innerYMin, innerYMax, local.y);

            if (x01Inner < 0f || x01Inner > 1f || y01Inner < 0f || y01Inner > 1f)
                return false;

            col = Mathf.Clamp(Mathf.FloorToInt(x01Inner * gridColumns), 0, gridColumns - 1);
            row = Mathf.Clamp(Mathf.FloorToInt(y01Inner * gridRows), 0, gridRows - 1);
            return true;
        }

        private SwipeDirection DetectSwipeDirection(Vector2 start, Vector2 end)
        {
            var delta = end - start;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            else
                return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }

        private (int c, int r) NeighborFrom(int c, int r, SwipeDirection dir)
        {
            switch (dir)
            {
                case SwipeDirection.Left: return (c - 1, r);
                case SwipeDirection.Right: return (c + 1, r);
                case SwipeDirection.Up: return (c, r + 1);
                case SwipeDirection.Down: return (c, r - 1);
                default: return (c, r);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            var r = inputArea ? inputArea : (targetCanvas ? targetCanvas.GetComponent<RectTransform>() : null);
            if (!r) return;

            // Draw grid lines (editor-only)
            Vector3[] wc = new Vector3[4];
            r.GetWorldCorners(wc);

            // Pad in world by translating local padding to world—approx with rect local to world scale on axes.
            var rect = r.rect;
            var size = rect.size;
            var sx = r.lossyScale.x;
            var sy = r.lossyScale.y;

            // Outer
            DrawRectGizmo(wc, new Color(1, 1, 1, 0.35f));

            // Inner (with padding)
            if (gridPadding.sqrMagnitude > 0.001f)
            {
                // approximate inner corners by lerping from center
                Vector3 center = (wc[0] + wc[2]) * 0.5f;
                var inner = new Vector3[4];
                float px = gridPadding.x * sx;
                float py = gridPadding.y * sy;

                inner[0] = new Vector3(wc[0].x + px, wc[0].y + py, wc[0].z);
                inner[2] = new Vector3(wc[2].x - px, wc[2].y - py, wc[2].z);
                inner[1] = new Vector3(inner[2].x, inner[0].y, inner[0].z);
                inner[3] = new Vector3(inner[0].x, inner[2].y, inner[0].z);

                DrawRectGizmo(inner, new Color(0, 1, 0, 0.35f));
                wc = inner; // draw grid inside padded rect
            }

            if (enableGridMapping && gridColumns > 0 && gridRows > 0)
            {
                // Vertical lines
                for (int c = 1; c < gridColumns; c++)
                {
                    float t = c / (float)gridColumns;
                    Vector3 a = Vector3.Lerp(wc[0], wc[1], t);
                    Vector3 b = Vector3.Lerp(wc[3], wc[2], t);
                    Gizmos.color = new Color(1, 1, 0, 0.7f);
                    Gizmos.DrawLine(a, b);
                }
                // Horizontal lines
                for (int rI = 1; rI < gridRows; rI++)
                {
                    float t = rI / (float)gridRows;
                    Vector3 a = Vector3.Lerp(wc[0], wc[3], t);
                    Vector3 b = Vector3.Lerp(wc[1], wc[2], t);
                    Gizmos.color = new Color(1, 1, 0, 0.7f);
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        private void DrawRectGizmo(Vector3[] corners, Color col)
        {
            Gizmos.color = col;
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
        }
#endif
    }
}
