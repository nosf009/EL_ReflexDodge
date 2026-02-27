using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Kiqqi.Framework
{
    /// <summary>
    /// Central view controller for all KiqqiUIView panels.
    /// Uses GameObject activation + fade for transitions.
    /// </summary>
    public class KiqqiUIManager : MonoBehaviour
    {
        [SerializeField] private List<KiqqiUIView> registeredViews = new();
        [SerializeField] private string defaultViewID = "MainMenuView";

        public KiqqiUIView activeView;

        private bool isTransitioning;
        private Coroutine transitionRoutine;

        public void Initialize(KiqqiAppManager app)
        {
            if (registeredViews.Count == 0)
                DiscoverViews();

            Debug.Log("[KiqqiUIManager] Initialized with " + registeredViews.Count + " views.");

            // Make sure everything starts hidden
            foreach (var v in registeredViews)
                v.gameObject.SetActive(false);
        }

        // Called after all managers are initialized
        public void ShowInitialView()
        {
            if (string.IsNullOrEmpty(defaultViewID))
            {
                // try to auto-pick a view with "Main" in the ID
                var main = registeredViews.Find(v => v.viewID.ToLower().Contains("main"));
                if (main != null)
                    defaultViewID = main.viewID;
            }

            if (!string.IsNullOrEmpty(defaultViewID))
                ShowView(defaultViewID);
            else
                Debug.LogWarning("[KiqqiUIManager] No default viewID set and none auto-detected.");
        }

        // --------------------------------------------------

        public void DiscoverViews()
        {
            registeredViews.Clear();
#if UNITY_2023_1_OR_NEWER
            var found = UnityEngine.Object.FindObjectsByType<KiqqiUIView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var found = UnityEngine.Object.FindObjectsOfType<KiqqiUIView>(true);
#endif
            registeredViews.AddRange(found);
        }

        // ---------------- New API ----------------

        public List<KiqqiUIView> GetAllRegisteredViews()
        {
            return registeredViews;
        }

        public void ShowView<T>() where T : KiqqiUIView
        {
            var target = registeredViews.Find(v => v is T);
            if (!target)
            {
                Debug.LogWarning($"[KiqqiUIManager] No view of type {typeof(T).Name} found.");
                return;
            }

            ShowViewInternal(target);
        }

        public void ShowView(Type type)
        {
            var target = registeredViews.Find(v => v.GetType() == type);
            if (!target)
            {
                Debug.LogWarning($"[KiqqiUIManager] No view found for type: {type.Name}");
                return;
            }

            ShowViewInternal(target);
        }

        public void ShowView(string viewID)
        {
            var target = registeredViews.Find(v => v.viewID == viewID);
            if (!target)
            {
                Debug.LogWarning("[KiqqiUIManager] No view found with ID: " + viewID);
                return;
            }

            ShowViewInternal(target);
        }

        private void ShowViewInternal(KiqqiUIView target)
        {
            if (isTransitioning)
            {
                Debug.Log("isTransitioning"); return;
            }

            if (!target)
            {
                Debug.Log("NO TARGET");
                return;
            }
                
            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);

            transitionRoutine = StartCoroutine(TransitionSequence(target));
        }

        private IEnumerator TransitionSequence(KiqqiUIView target)
        {
            isTransitioning = true;

            // --- hide current ---
            if (activeView && !activeView.isOverlay)
            {
                activeView.HideWithDeactivate(false);
                yield return new WaitForSecondsRealtime(activeView.fadeDuration);
            }

            activeView = null;

            // --- show new ---
            target.gameObject.SetActive(true);
            target.ShowWithActivate();
            target.OnShow();
            activeView = target;

            yield return new WaitForSecondsRealtime(target.fadeDuration);

            isTransitioning = false;
            transitionRoutine = null;
        }

        public void HideAll()
        {
            foreach (var v in registeredViews)
                v.HideWithDeactivate();
            activeView = null;
        }

        public T GetView<T>() where T : KiqqiUIView
        {
            return registeredViews.Find(v => v is T) as T;
        }


    }
}
