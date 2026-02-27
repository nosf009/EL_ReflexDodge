using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public class WaterLilliesDifficultyLayout
    {
        [Tooltip("Parent transform containing child positions for lilly items")]
        public Transform itemsParent;
    }

    public class KiqqiWaterLilliesView : KiqqiGameViewBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty-Specific Layouts")]
        [Tooltip("Beginner: 6 items")]
        public WaterLilliesDifficultyLayout beginnerLayout;
        [Tooltip("Easy: 8 items")]
        public WaterLilliesDifficultyLayout easyLayout;
        [Tooltip("Medium: 9 items")]
        public WaterLilliesDifficultyLayout mediumLayout;
        [Tooltip("Advanced: 12 items")]
        public WaterLilliesDifficultyLayout advancedLayout;
        [Tooltip("Hard: 12 items")]
        public WaterLilliesDifficultyLayout hardLayout;

        [Header("Templates")]
        public GameObject itemTemplate;

        [Header("Grow Animation")]
        [Tooltip("Duration of item grow-in animation")]
        public float growDuration = 0.4f;
        [Tooltip("Delay between each item growing")]
        public float growStagger = 0.05f;

        [Header("Highlight Settings")]
        [Tooltip("Scale multiplier when item is highlighted")]
        public float highlightScale = 1.3f;
        [Tooltip("Duration of highlight scale animation")]
        public float highlightScaleDuration = 0.2f;
        [Tooltip("Highlight effect color")]
        public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

        [Header("Background Fade")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 1f;

        [Header("Feedback")]
        public Image feedbackRight;
        public Image feedbackWrong;
        public float feedbackFadeDuration = 0.5f;

        [Header("Particles")]
        public GameObject correctParticleRoot;

        #endregion

        #region RUNTIME STATE

        private readonly List<Button> itemButtons = new();
        private readonly List<Image> itemImages = new();
        private readonly List<Image> itemBackgrounds = new();
        private readonly List<Color> itemOriginalColors = new();
        private readonly List<Vector3> itemOriginalScales = new();

        private WaterLilliesDifficultyLayout currentLayout;
        private Coroutine[] highlightCoroutines;

        #endregion

        #region VIEW INITIALIZATION & SHOW LOGIC

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiWaterLilliesManager mgr)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(mgr));
                return;
            }

            ClearItems();

            if (gameplayBackground)
            {
                var c = gameplayBackground.color;
                c.a = 0f;
                gameplayBackground.color = c;
                gameplayBackground.gameObject.SetActive(false);
            }

            base.OnShow();
        }

        #endregion

        #region LAYOUT SELECTION

        private WaterLilliesDifficultyLayout GetLayoutForDifficulty(KiqqiLevelManager.KiqqiDifficulty difficulty)
        {
            return difficulty switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => beginnerLayout,
                KiqqiLevelManager.KiqqiDifficulty.Easy => easyLayout,
                KiqqiLevelManager.KiqqiDifficulty.Medium => mediumLayout,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => advancedLayout,
                KiqqiLevelManager.KiqqiDifficulty.Hard => hardLayout,
                _ => beginnerLayout
            };
        }

        #endregion

        #region BACKGROUND FADE & RESUME HANDLING

        private IEnumerator HandleResumeFadeIn(KiqqiWaterLilliesManager mgr)
        {
            if (currentLayout != null)
            {
                if (currentLayout.itemsParent)
                    currentLayout.itemsParent.gameObject.SetActive(true);
            }

            if (gameplayBackground)
            {
                gameplayBackground.gameObject.SetActive(true);
                float t = 0f;
                Color bgCol = gameplayBackground.color;
                bgCol.a = 0f;
                gameplayBackground.color = bgCol;

                while (t < backgroundFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / backgroundFadeDuration);
                    float eased = 1f - Mathf.Pow(1f - n, 4f);

                    bgCol.a = Mathf.Lerp(0f, 1f, eased);
                    gameplayBackground.color = bgCol;

                    yield return null;
                }

                bgCol.a = 1f;
                gameplayBackground.color = bgCol;
            }

            mgr.ResumeFromPause(this);
            timerRunning = true;
            if (pauseButton)
                pauseButton.interactable = true;
        }

        protected override void OnCountdownFinished()
        {
            StartCoroutine(HandleBackgroundFadeThenStart());
        }

        private IEnumerator HandleBackgroundFadeThenStart()
        {
            ClearItems();

            if (currentLayout != null)
            {
                if (currentLayout.itemsParent)
                    currentLayout.itemsParent.gameObject.SetActive(true);
            }

            if (gameplayBackground)
            {
                gameplayBackground.gameObject.SetActive(true);
                float t = 0f;
                Color col = gameplayBackground.color;
                while (t < backgroundFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / backgroundFadeDuration);
                    float eased = 1f - Mathf.Pow(1f - n, 5f);
                    col.a = Mathf.Lerp(0f, 1f, eased);
                    gameplayBackground.color = col;
                    yield return null;
                }

                col.a = 1f;
                gameplayBackground.color = col;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            if (pauseButton)
            {
                pauseButton.interactable = true;
            }

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiWaterLilliesManager mgr)
            {
                mgr.StartMiniGame();
            }

            timerRunning = true;
        }

        #endregion

        #region TIME UP HANDLING

        protected override void OnTimeUp()
        {
            if (KiqqiAppManager.Instance.Game.currentMiniGame is KiqqiWaterLilliesManager mgr)
            {
                mgr.NotifyTimeExpired();
            }
        }

        #endregion

        #region ROUND ITEMS DISPLAY

        public void ShowRoundItems(List<LillyItem> items)
        {
            var difficulty = KiqqiAppManager.Instance.Levels.GetCurrentDifficulty();
            currentLayout = GetLayoutForDifficulty(difficulty);

            if (currentLayout == null)
            {
                Debug.LogError($"[WaterLilliesView] No layout configured for difficulty: {difficulty}");
                return;
            }

            ClearItems();
            BuildItems(items);
            StartCoroutine(GrowInItems());
        }

        private void BuildItems(List<LillyItem> items)
        {
            ClearItems();

            if (currentLayout == null || currentLayout.itemsParent == null || !itemTemplate)
            {
                Debug.LogWarning("[WaterLilliesView] Cannot build items - missing layout or template");
                return;
            }

            List<Transform> spawnPoints = new();
            foreach (Transform child in currentLayout.itemsParent)
            {
                spawnPoints.Add(child);
            }

            if (spawnPoints.Count < items.Count)
            {
                Debug.LogWarning($"[WaterLilliesView] Not enough spawn points ({spawnPoints.Count}) for items ({items.Count})");
            }

            highlightCoroutines = new Coroutine[items.Count];

            for (int i = 0; i < items.Count && i < spawnPoints.Count; i++)
            {
                GameObject itemObj = Instantiate(itemTemplate, spawnPoints[i]);
                itemObj.transform.localPosition = Vector3.zero;
                itemObj.transform.localScale = Vector3.zero;
                itemObj.SetActive(true);

                Button btn = itemObj.GetComponent<Button>();
                if (btn)
                {
                    int index = i;
                    btn.onClick.AddListener(() => OnItemClicked(index));
                    itemButtons.Add(btn);
                }

                Image img = itemObj.GetComponent<Image>();
                if (img)
                {
                    itemImages.Add(img);
                    if (items[i].sprite != null)
                    {
                        img.sprite = items[i].sprite;
                    }
                    img.color = items[i].color;
                    itemOriginalColors.Add(items[i].color);
                }
                else
                {
                    itemOriginalColors.Add(Color.white);
                }

                Image bgImg = itemObj.transform.Find("Background")?.GetComponent<Image>();
                if (bgImg)
                {
                    itemBackgrounds.Add(bgImg);
                }
                else
                {
                    itemBackgrounds.Add(null);
                }

                itemOriginalScales.Add(itemObj.transform.localScale);
            }
        }

        private IEnumerator GrowInItems()
        {
            for (int i = 0; i < itemButtons.Count; i++)
            {
                if (itemButtons[i] != null)
                {
                    StartCoroutine(GrowInSingleItem(i));
                    yield return new WaitForSeconds(growStagger);
                }
            }

            yield return new WaitForSeconds(growDuration);

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiWaterLilliesManager mgr)
            {
                mgr.OnGrowAnimationComplete();
            }
        }

        private IEnumerator GrowInSingleItem(int index)
        {
            if (index < 0 || index >= itemButtons.Count)
                yield break;

            Transform itemTransform = itemButtons[index].transform;
            Vector3 targetScale = Vector3.one;

            float elapsed = 0f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / growDuration;
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                itemTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, eased);

                yield return null;
            }

            itemTransform.localScale = targetScale;
            itemOriginalScales[index] = targetScale;
        }

        private void OnItemClicked(int index)
        {
            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiWaterLilliesManager mgr)
            {
                mgr.HandleItemClicked(index);
            }
        }

        private void ClearItems()
        {
            foreach (var btn in itemButtons)
            {
                if (btn != null && btn.gameObject != null)
                    Destroy(btn.gameObject);
            }

            itemButtons.Clear();
            itemImages.Clear();
            itemBackgrounds.Clear();
            itemOriginalColors.Clear();
            itemOriginalScales.Clear();

            if (highlightCoroutines != null)
            {
                for (int i = 0; i < highlightCoroutines.Length; i++)
                {
                    if (highlightCoroutines[i] != null)
                        StopCoroutine(highlightCoroutines[i]);
                }
            }
            highlightCoroutines = null;
        }

        #endregion

        #region HIGHLIGHT SYSTEM

        public void HighlightItem(int index, bool highlight)
        {
            if (index < 0 || index >= itemButtons.Count)
                return;

            if (highlightCoroutines != null && index < highlightCoroutines.Length)
            {
                if (highlightCoroutines[index] != null)
                {
                    StopCoroutine(highlightCoroutines[index]);
                    highlightCoroutines[index] = null;
                }
            }

            if (highlight)
            {
                if (highlightCoroutines != null)
                {
                    highlightCoroutines[index] = StartCoroutine(HighlightItemRoutine(index));
                }
            }
            else
            {
                if (highlightCoroutines != null)
                {
                    highlightCoroutines[index] = StartCoroutine(UnhighlightItemRoutine(index));
                }
            }
        }

        private IEnumerator HighlightItemRoutine(int index)
        {
            if (index >= itemButtons.Count) yield break;

            Transform itemTransform = itemButtons[index].transform;
            Image itemImage = index < itemImages.Count ? itemImages[index] : null;
            Image bgImage = index < itemBackgrounds.Count ? itemBackgrounds[index] : null;

            Vector3 originalScale = index < itemOriginalScales.Count ? itemOriginalScales[index] : Vector3.one;
            Vector3 targetScale = originalScale * highlightScale;

            Color originalColor = index < itemOriginalColors.Count ? itemOriginalColors[index] : Color.white;

            float elapsed = 0f;
            while (elapsed < highlightScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / highlightScaleDuration;
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                itemTransform.localScale = Vector3.Lerp(originalScale, targetScale, eased);

                if (itemImage)
                {
                    itemImage.color = Color.Lerp(originalColor, highlightColor, eased);
                }

                if (bgImage)
                {
                    Color bgColor = bgImage.color;
                    bgColor.a = Mathf.Lerp(0f, 0.5f, eased);
                    bgImage.color = bgColor;
                }

                yield return null;
            }

            itemTransform.localScale = targetScale;
            if (itemImage)
            {
                itemImage.color = highlightColor;
            }
            if (bgImage)
            {
                Color bgColor = bgImage.color;
                bgColor.a = 0.5f;
                bgImage.color = bgColor;
            }
        }

        private IEnumerator UnhighlightItemRoutine(int index)
        {
            if (index >= itemButtons.Count) yield break;

            Transform itemTransform = itemButtons[index].transform;
            Image itemImage = index < itemImages.Count ? itemImages[index] : null;
            Image bgImage = index < itemBackgrounds.Count ? itemBackgrounds[index] : null;

            Vector3 currentScale = itemTransform.localScale;
            Vector3 originalScale = index < itemOriginalScales.Count ? itemOriginalScales[index] : Vector3.one;

            Color currentColor = itemImage ? itemImage.color : Color.white;
            Color originalColor = index < itemOriginalColors.Count ? itemOriginalColors[index] : Color.white;

            float elapsed = 0f;
            while (elapsed < highlightScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / highlightScaleDuration;
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                itemTransform.localScale = Vector3.Lerp(currentScale, originalScale, eased);

                if (itemImage)
                {
                    itemImage.color = Color.Lerp(currentColor, originalColor, eased);
                }

                if (bgImage)
                {
                    Color bgColor = bgImage.color;
                    bgColor.a = Mathf.Lerp(bgColor.a, 0f, eased);
                    bgImage.color = bgColor;
                }

                yield return null;
            }

            itemTransform.localScale = originalScale;
            if (itemImage)
            {
                itemImage.color = originalColor;
            }
            if (bgImage)
            {
                Color bgColor = bgImage.color;
                bgColor.a = 0f;
                bgImage.color = bgColor;
            }
        }

        public void OnHighlightPhaseComplete()
        {
        }

        #endregion

        #region PLAYER FEEDBACK

        public void ShowPlayerClickFeedback(int index)
        {
            if (index < 0 || index >= itemButtons.Count)
                return;

            StartCoroutine(PlayerClickFeedbackRoutine(index));
        }

        private IEnumerator PlayerClickFeedbackRoutine(int index)
        {
            Transform itemTransform = itemButtons[index].transform;
            Vector3 originalScale = itemTransform.localScale;
            Vector3 punchScale = originalScale * 1.15f;

            float duration = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                itemTransform.localScale = Vector3.Lerp(originalScale, punchScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                itemTransform.localScale = Vector3.Lerp(punchScale, originalScale, t);
                yield return null;
            }

            itemTransform.localScale = originalScale;
        }

        public void ShowRoundResult(bool correct, int scoreChange)
        {
            if (correct)
            {
                if (feedbackRight)
                {
                    StartCoroutine(ShowFeedbackImage(feedbackRight));
                }

                if (correctParticleRoot)
                {
                    correctParticleRoot.SetActive(false);
                    correctParticleRoot.SetActive(true);
                }
            }
            else
            {
                if (feedbackWrong)
                {
                    StartCoroutine(ShowFeedbackImage(feedbackWrong));
                }
            }
        }

        private IEnumerator ShowFeedbackImage(Image feedbackImage)
        {
            if (feedbackImage == null) yield break;

            feedbackImage.gameObject.SetActive(true);

            CanvasGroup cg = feedbackImage.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = feedbackImage.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < feedbackFadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = 1f - (elapsed / feedbackFadeDuration);
                yield return null;
            }

            cg.alpha = 0f;
            feedbackImage.gameObject.SetActive(false);
        }

        #endregion

        #region PUBLIC API

        public void UpdateScoreLabel(int score)
        {
            if (scoreValueText)
            {
                scoreValueText.text = score.ToString();
            }
        }

        #endregion
    }
}
