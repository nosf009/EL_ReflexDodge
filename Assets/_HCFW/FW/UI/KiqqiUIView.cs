using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Base component for all UI views (MainMenu, Game, Results, etc.).
    /// Controls visibility and provides hooks for setup / cleanup.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class KiqqiUIView : MonoBehaviour
    {
        public enum ViewState { Hidden, Showing }

        [Header("View Settings")]
        public string viewID = "UnnamedView";
        public bool isOverlay = false; // If true, doesn't hide others when shown.

        public enum TransitionType { Fade, Slide }
        public enum SlideDirection { Left, Right, Up, Down }

        [Header("Transition")]
        public TransitionType transitionType = TransitionType.Fade;
        public SlideDirection slideInFrom = SlideDirection.Right;
        public float fadeDuration = 0.25f;
        public float slideDuration = 0.35f;
        public bool slideAlsoFade = true;


        private CanvasGroup cg;
        private Coroutine fadeRoutine;
        public ViewState State { get; private set; } = ViewState.Hidden;

        public virtual void Awake()
        {
            cg = GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            State = ViewState.Hidden;
        }

        public virtual void OnShow() { }
        public virtual void OnHide() { }

        public void SetVisible(bool visible, bool instant = false)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(visible, instant));
        }

        private IEnumerator FadeRoutine(bool visible, bool instant)
        {
            State = visible ? ViewState.Showing : ViewState.Hidden;

            // Pre-activation prep
            if (visible)
            {
                cg.alpha = 0f; // ensure consistent start (no old alpha flashes)
                cg.interactable = false;   // avoid interaction before visible
                cg.blocksRaycasts = false;
                yield return null; // allow 1 frame for Unity to process activation
            }

            float startAlpha = cg.alpha;
            float targetAlpha = visible ? 1f : 0f;
            float duration = Mathf.Max(0.01f, fadeDuration);
            float startTime = Time.unscaledTime;

            if (instant)
            {
                cg.alpha = targetAlpha;
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
                yield break;
            }

            // Use an eased interpolation with *fixedDeltaTime* time step to avoid flicker variance
            while (true)
            {
                float elapsed = Time.unscaledTime - startTime;
                float normalized = Mathf.Clamp01(elapsed / duration);

                // super-smooth ease-in-out cubic
                float eased = normalized * normalized * (3f - 2f * normalized);

                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);

                if (normalized >= 1f)
                    break;

                yield return new WaitForEndOfFrame(); // ensures consistent visual update
            }

            cg.alpha = targetAlpha;

            // Apply interactivity only after fully visible
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }

        public void ShowWithActivate()
        {
            gameObject.SetActive(true);

            if (transitionType == TransitionType.Slide)
                StartCoroutine(SlideRoutine(true));
            else
                SetVisible(true, false);
        }

        public void HideWithDeactivate(bool forceImmediate = false)
        {
            if (!gameObject.activeSelf) return;

            if (forceImmediate || fadeDuration <= 0f)
            {
                SetVisible(false, true);
                gameObject.SetActive(false);
                return;
            }

            SetVisible(false, false);
            StartCoroutine(DisableAfterFadeSafe());
        }

        private IEnumerator SlideRoutine(bool entering)
        {
            var rt = GetComponent<RectTransform>();
            if (!rt)
            {
                cg.alpha = 1f;
                cg.interactable = entering;
                cg.blocksRaycasts = entering;
                yield break;
            }

            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas ? canvas.GetComponent<RectTransform>() : null;
            Vector2 canvasSize = canvasRect ? canvasRect.rect.size : new Vector2(Screen.width, Screen.height);

            Vector2 from, to;
            if (entering)
            {
                from = GetSlideOffset(canvasSize, slideInFrom);
                to = Vector2.zero;
            }
            else
            {
                from = Vector2.zero;
                to = -GetSlideOffset(canvasSize, slideInFrom);
            }

            rt.anchoredPosition = from;
            cg.alpha = slideAlsoFade ? (entering ? 0f : 1f) : 1f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / slideDuration;
                float eased = t * t * (3f - 2f * t);
                rt.anchoredPosition = Vector2.Lerp(from, to, eased);

                if (slideAlsoFade)
                    cg.alpha = Mathf.Lerp(entering ? 0f : 1f, entering ? 1f : 0f, eased);

                yield return null;
            }

            rt.anchoredPosition = to;
            cg.alpha = entering ? 1f : 0f;
            cg.interactable = entering;
            cg.blocksRaycasts = entering;

            if (!entering)
                gameObject.SetActive(false);
        }

        private Vector2 GetSlideOffset(Vector2 canvasSize, SlideDirection dir)
        {
            switch (dir)
            {
                case SlideDirection.Left: return new Vector2(-canvasSize.x, 0);
                case SlideDirection.Right: return new Vector2(canvasSize.x, 0);
                case SlideDirection.Up: return new Vector2(0, canvasSize.y);
                case SlideDirection.Down: return new Vector2(0, -canvasSize.y);
                default: return Vector2.zero;
            }
        }


        private IEnumerator DisableAfterFadeSafe()
        {
            float wait = Mathf.Max(0.01f, fadeDuration);
            yield return new WaitForSecondsRealtime(wait);

            // Only deactivate if still hidden (avoid conflicts)
            if (cg.alpha <= 0.01f && State == ViewState.Hidden)
                gameObject.SetActive(false);
        }

    }
}
