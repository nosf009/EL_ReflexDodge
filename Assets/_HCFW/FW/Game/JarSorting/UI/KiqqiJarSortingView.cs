using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Kiqqi.Framework
{
    public class KiqqiJarSortingView : KiqqiGameViewBase
    {
        #region INSPECTOR

        [Header("Game-Specific UI")]
        [SerializeField] private Text movesText;

        [Header("Jar Container")]
        [SerializeField] private Transform jarsContainer;
        [SerializeField] private GameObject jarPrefab;
        [SerializeField] private GameObject jarItemTpl;

        [Header("Feedback")]
        [SerializeField] private Image feedbackCorrect;
        [SerializeField] private Image feedbackWrong;
        [SerializeField] private float feedbackDuration = 0.35f;

        [Header("Transition")]
        [SerializeField] private CanvasGroup jarsCanvasGroup;
        [SerializeField] private float puzzleFadeDuration = 0.33f;

        #endregion

        #region STATE

        private KiqqiJarSortingManager manager;
        private List<JarVisual> jarVisuals = new List<JarVisual>();
        private GameObject floatingItem;
        private int selectedJarIndex = -1;

        #endregion

        #region INITIALIZATION

        public void SetManager(KiqqiJarSortingManager manager)
        {
            this.manager = manager;
        }

        protected override void OnCountdownFinished()
        {
            base.OnCountdownFinished();
            
            if (manager != null)
            {
                manager.StartMiniGame();
                manager.OnCountdownFinished();
            }

            if (pauseButton)
            {
                pauseButton.interactable = true;
            }
        }

        #endregion

        #region UI UPDATES

        public void UpdateScore(int score)
        {
            AddScore(0);
        }

        public void UpdateMoves(int moves)
        {
            if (movesText != null)
            {
                movesText.text = $"Moves: {moves}";
            }
        }

        #endregion

        #region JAR VISUALIZATION

        public void UpdateJars(List<Jar> jars)
        {
            ClearJars();

            for (int i = 0; i < jars.Count; i++)
            {
                CreateJarVisual(jars[i], i);
            }

            Debug.Log($"[JarSortingView] Updated {jarVisuals.Count} jar visuals");
        }

        public void ClearJars()
        {
            foreach (var jarVisual in jarVisuals)
            {
                if (jarVisual != null && jarVisual.gameObject != null)
                {
                    Destroy(jarVisual.gameObject);
                }
            }
            jarVisuals.Clear();
        }

        private void CreateJarVisual(Jar jar, int index)
        {
            if (jarPrefab == null || jarsContainer == null)
            {
                Debug.LogError("[JarSortingView] Missing jarPrefab or jarsContainer!");
                return;
            }

            GameObject jarObj = Instantiate(jarPrefab, jarsContainer);
            jarObj.SetActive(true);

            JarVisual jarVisual = jarObj.GetComponent<JarVisual>();
            if (jarVisual == null)
            {
                jarVisual = jarObj.AddComponent<JarVisual>();
            }

            jarVisual.Initialize(jar, index, manager, jarItemTpl);

            jarVisuals.Add(jarVisual);
        }

        #endregion

        #region SELECTION FEEDBACK

        public void ShowJarSelected(int jarIndex)
        {
            if (jarIndex >= 0 && jarIndex < jarVisuals.Count)
            {
                selectedJarIndex = jarIndex;
                jarVisuals[jarIndex].SetSelected(true);

                floatingItem = jarVisuals[jarIndex].ExtractTopItem();
            }
        }

        public void ClearSelection()
        {
            if (floatingItem != null && selectedJarIndex >= 0 && selectedJarIndex < jarVisuals.Count)
            {
                jarVisuals[selectedJarIndex].ReturnTopItem(floatingItem);
                floatingItem = null;
            }

            foreach (var jarVisual in jarVisuals)
            {
                jarVisual.SetSelected(false);
            }
            selectedJarIndex = -1;
        }

        #endregion

        #region ANIMATIONS

        public void AnimatePour(int fromIndex, int toIndex, int itemType, System.Action onComplete)
        {
            StartCoroutine(AnimatePourCoroutine(fromIndex, toIndex, itemType, onComplete));
        }

        private IEnumerator AnimatePourCoroutine(int fromIndex, int toIndex, int itemType, System.Action onComplete)
        {
            if (floatingItem == null || fromIndex < 0 || fromIndex >= jarVisuals.Count || toIndex < 0 || toIndex >= jarVisuals.Count)
            {
                Debug.LogWarning("[AnimatePour] Invalid state - aborting animation");
                onComplete?.Invoke();
                yield break;
            }

            RectTransform floatingRect = floatingItem.GetComponent<RectTransform>();
            if (floatingRect == null)
            {
                Debug.LogWarning("[AnimatePour] FloatingItem has no RectTransform - aborting");
                onComplete?.Invoke();
                yield break;
            }

            Vector3 startPos = floatingRect.position;
            Vector3 targetPos = jarVisuals[toIndex].GetFloatingItemPosition();
            
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (floatingRect == null || floatingItem == null)
                {
                    Debug.LogWarning("[AnimatePour] FloatingItem destroyed during animation - stopping");
                    onComplete?.Invoke();
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);
                floatingRect.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            if (floatingItem != null)
            {
                jarVisuals[toIndex].AddVisualItem(floatingItem);
                floatingItem = null;
                selectedJarIndex = -1;
            }

            foreach (var jarVisual in jarVisuals)
            {
                jarVisual.SetSelected(false);
            }

            onComplete?.Invoke();
        }

        public void ShowInvalidMove()
        {
            Debug.Log("[View] Invalid move feedback");
            if (feedbackWrong != null)
            {
                StartCoroutine(ShowFeedbackCoroutine(feedbackWrong));
            }
        }

        public void ShowPuzzleComplete()
        {
            Debug.Log("[View] Puzzle complete!");
            if (feedbackCorrect != null)
            {
                StartCoroutine(ShowFeedbackCoroutine(feedbackCorrect));
            }
        }

        private IEnumerator ShowFeedbackCoroutine(Image feedbackImage)
        {
            if (feedbackImage == null) yield break;

            feedbackImage.gameObject.SetActive(true);
            var canvasGroup = feedbackImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = feedbackImage.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            feedbackImage.transform.localScale = Vector3.one * 0.8f;

            float elapsed = 0f;
            float fadeIn = feedbackDuration * 0.5f;

            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeIn;
                canvasGroup.alpha = t;
                feedbackImage.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, t);
                yield return null;
            }

            yield return new WaitForSeconds(feedbackDuration * 0.5f);

            elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeIn;
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            feedbackImage.gameObject.SetActive(false);
        }

        public IEnumerator FadeOutJars()
        {
            if (jarsCanvasGroup == null)
            {
                jarsCanvasGroup = jarsContainer.GetComponent<CanvasGroup>();
                if (jarsCanvasGroup == null)
                {
                    jarsCanvasGroup = jarsContainer.gameObject.AddComponent<CanvasGroup>();
                }
            }

            float elapsed = 0f;
            while (elapsed < puzzleFadeDuration)
            {
                elapsed += Time.deltaTime;
                jarsCanvasGroup.alpha = 1f - (elapsed / puzzleFadeDuration);
                yield return null;
            }
            jarsCanvasGroup.alpha = 0f;
        }

        public IEnumerator FadeInJars()
        {
            if (jarsCanvasGroup == null)
            {
                jarsCanvasGroup = jarsContainer.GetComponent<CanvasGroup>();
                if (jarsCanvasGroup == null)
                {
                    jarsCanvasGroup = jarsContainer.gameObject.AddComponent<CanvasGroup>();
                }
            }

            jarsCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < puzzleFadeDuration)
            {
                elapsed += Time.deltaTime;
                jarsCanvasGroup.alpha = elapsed / puzzleFadeDuration;
                yield return null;
            }
            jarsCanvasGroup.alpha = 1f;
        }

        #endregion

        #region RESULTS

        public void ShowResults(int finalScore, int moves)
        {
            Debug.Log($"[View] Session ended: Score {finalScore}, Moves {moves}");
        }

        #endregion

        #region LIFECYCLE

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;

            if (pauseButton)
                pauseButton.interactable = false;

            UpdateMoves(0);

            if (game.ResumeRequested && game.currentMiniGame is KiqqiJarSortingManager mgr)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(mgr));
                return;
            }

            ClearJars();
            ClearSelection();
            timerRunning = false;

            base.OnShow();
            Debug.Log("[JarSortingView] OnShow");
        }

        private IEnumerator HandleResumeFadeIn(KiqqiJarSortingManager mgr)
        {
            if (jarsCanvasGroup != null)
            {
                float t = 0f;
                jarsCanvasGroup.alpha = 0f;

                while (t < puzzleFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    jarsCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / puzzleFadeDuration);
                    yield return null;
                }

                jarsCanvasGroup.alpha = 1f;
            }

            mgr.ResumeFromPause(this);
            timerRunning = true;

            if (pauseButton)
                pauseButton.interactable = true;
        }

        public override void OnHide()
        {
            base.OnHide();
            ClearJars();
            Debug.Log("[JarSortingView] OnHide");
        }

        #endregion
    }

    public class JarVisual : MonoBehaviour
    {
        private Jar jar;
        private int jarIndex;
        private KiqqiJarSortingManager manager;
        private GameObject itemTemplate;

        private Image jarImage;
        private Button jarButton;
        private Transform itemsContainer;
        private Transform floatingItemAnchor;

        public void Initialize(Jar jar, int jarIndex, KiqqiJarSortingManager manager, GameObject itemTemplate)
        {
            this.jar = jar;
            this.jarIndex = jarIndex;
            this.manager = manager;
            this.itemTemplate = itemTemplate;

            jarImage = GetComponent<Image>();
            if (jarImage == null)
            {
                jarImage = GetComponentInChildren<Image>();
            }

            jarButton = GetComponentInChildren<Button>();
            
            if (jarButton != null)
            {
                jarButton.onClick.RemoveAllListeners();
                jarButton.onClick.AddListener(() => manager.OnJarClicked(jarIndex));
                Debug.Log($"[JarVisual {jarIndex}] Button wired to jar index {jarIndex}");
            }
            else
            {
                Debug.LogError($"[JarVisual {jarIndex}] No Button found in jarTpl or children!");
            }

            itemsContainer = transform.Find("ItemsContainer");
            
            if (itemsContainer == null)
            {
                Debug.LogError($"[JarVisual {jarIndex}] ItemsContainer child not found in jarTpl prefab! Please add it.");
                return;
            }

            floatingItemAnchor = transform.Find("FloatingItemAnchor");

            RenderJar();
        }

        private void RenderJar()
        {
            if (itemsContainer == null)
            {
                Debug.LogError($"[JarVisual {jarIndex}] Cannot render - ItemsContainer is null!");
                return;
            }

            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }

            Vector2 itemSize = manager.GetItemSize();
            float spacing = manager.GetItemSpacing();

            for (int i = 0; i < jar.items.Count; i++)
            {
                GameObject itemObj;
                
                if (itemTemplate != null)
                {
                    itemObj = Instantiate(itemTemplate, itemsContainer);
                    itemObj.SetActive(true);
                }
                else
                {
                    itemObj = new GameObject($"Item_{i}");
                    itemObj.transform.SetParent(itemsContainer, false);
                    itemObj.AddComponent<Image>();
                }

                Image itemImage = itemObj.GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.color = manager.GetItemColor(jar.items[i]);
                }

                RectTransform rect = itemObj.GetComponent<RectTransform>();
                if (rect == null)
                {
                    rect = itemObj.AddComponent<RectTransform>();
                }
                
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = itemSize;
                rect.anchoredPosition = new Vector2(0, (i * (itemSize.y + spacing)) + (itemSize.y / 2f));
            }
        }

        public void SetSelected(bool selected)
        {
            if (jarImage != null)
            {
                jarImage.color = selected ? Color.yellow : Color.white;
            }
        }

        public int GetTopItemType()
        {
            return jar.TopItem;
        }

        public GameObject ExtractTopItem()
        {
            if (itemsContainer == null || itemsContainer.childCount == 0)
                return null;

            int topIndex = itemsContainer.childCount - 1;
            Transform topItemTransform = itemsContainer.GetChild(topIndex);
            GameObject topItem = topItemTransform.gameObject;

            RectTransform rect = topItem.GetComponent<RectTransform>();
            if (rect != null && floatingItemAnchor != null)
            {
                rect.position = floatingItemAnchor.position;
            }
            else if (rect != null)
            {
                Vector3 currentPos = rect.position;
                currentPos.y += 100f;
                rect.position = currentPos;
            }

            return topItem;
        }

        public void ReturnTopItem(GameObject item)
        {
            if (item == null || itemsContainer == null)
                return;

            Vector2 itemSize = manager.GetItemSize();
            float spacing = manager.GetItemSpacing();
            int itemIndex = itemsContainer.childCount - 1;

            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(0, (itemIndex * (itemSize.y + spacing)) + (itemSize.y / 2f));
            }
        }

        public void AddVisualItem(GameObject item)
        {
            if (item == null || itemsContainer == null)
                return;

            item.transform.SetParent(itemsContainer, false);
            
            Vector2 itemSize = manager.GetItemSize();
            float spacing = manager.GetItemSpacing();
            int itemIndex = itemsContainer.childCount - 1;

            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = itemSize;
                rect.anchoredPosition = new Vector2(0, (itemIndex * (itemSize.y + spacing)) + (itemSize.y / 2f));
            }
        }

        public GameObject CreateFloatingItem(int itemType, KiqqiJarSortingManager manager)
        {
            GameObject floatingObj = Instantiate(itemTemplate, transform.parent);
            floatingObj.SetActive(true);

            Image itemImage = floatingObj.GetComponent<Image>();
            if (itemImage != null)
            {
                itemImage.color = manager.GetItemColor(itemType);
            }

            RectTransform rect = floatingObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = manager.GetItemSize();
                rect.position = GetFloatingItemPosition();
            }

            return floatingObj;
        }

        public Vector3 GetFloatingItemPosition()
        {
            if (floatingItemAnchor != null)
            {
                return floatingItemAnchor.position;
            }
            
            Vector3 topOfJar = transform.position;
            topOfJar.y += 100f;
            return topOfJar;
        }

        public void ReturnFloatingItem(GameObject floatingObj)
        {
        }
    }
}
