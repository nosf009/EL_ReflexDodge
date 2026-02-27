using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    [System.Serializable]
    public class CandyFactoryDifficultyLayout
    {
        [Tooltip("Parent transform containing child positions for item shelf")]
        public Transform itemShelfParent;

        [Tooltip("Parent transform containing child positions for bags")]
        public Transform bagParent;
    }

    public class KiqqiCandyFactoryView : KiqqiGameViewBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Difficulty-Specific Layouts")]
        [Tooltip("Beginner: 4 items, 2 bags")]
        public CandyFactoryDifficultyLayout beginnerLayout;
        [Tooltip("Easy: 6 items, 2 bags")]
        public CandyFactoryDifficultyLayout easyLayout;
        [Tooltip("Medium: 6 items, 3 bags")]
        public CandyFactoryDifficultyLayout mediumLayout;
        [Tooltip("Advanced: 8 items, 3 bags")]
        public CandyFactoryDifficultyLayout advancedLayout;
        [Tooltip("Hard: 8 items, 4 bags")]
        public CandyFactoryDifficultyLayout hardLayout;

        [Header("Templates")]
        public GameObject itemTemplate;
        public GameObject bagTemplate;

        [Header("Bag Content Preview Settings")]
        [Tooltip("Size of candy sprites shown inside bags during preview")]
        public Vector2 bagContentSize = new Vector2(180, 180);

        [Header("Background Fade")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 1f;

        [Header("Feedback")]
        public Image feedbackRight;
        public Image feedbackWrong;
        public float feedbackFadeDuration = 0.35f;
        public float feedbackYOffset = 60f;

        [Header("Particles")]
        public GameObject correctParticleRoot;

        #endregion

        #region RUNTIME STATE

        private readonly List<Button> itemButtons = new();
        private readonly List<Button> bagButtons = new();
        private readonly List<Image> itemImages = new();
        private readonly List<Image> bagContentImages = new();

        private int selectedItemIndex = -1;
        private Coroutine previewCoroutine;
        private CandyFactoryDifficultyLayout currentLayout;

        #endregion

        #region VIEW INITIALIZATION & SHOW LOGIC

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiCandyFactoryManager mgr)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(mgr));
                return;
            }

            ClearItems();
            ClearBags();

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

        private CandyFactoryDifficultyLayout GetLayoutForDifficulty(KiqqiLevelManager.KiqqiDifficulty difficulty)
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

        private IEnumerator HandleResumeFadeIn(KiqqiCandyFactoryManager mgr)
        {
            if (currentLayout != null)
            {
                if (currentLayout.itemShelfParent)
                    currentLayout.itemShelfParent.gameObject.SetActive(true);
                if (currentLayout.bagParent)
                    currentLayout.bagParent.gameObject.SetActive(true);
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
            ClearBags();

            if (currentLayout != null)
            {
                if (currentLayout.itemShelfParent)
                    currentLayout.itemShelfParent.gameObject.SetActive(true);
                if (currentLayout.bagParent)
                    currentLayout.bagParent.gameObject.SetActive(true);
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
            if (gm?.currentMiniGame is KiqqiCandyFactoryManager mgr)
            {
                mgr.StartMiniGame();
            }

            timerRunning = true;
        }

        #endregion

        #region TIME UP HANDLING

        protected override void OnTimeUp()
        {
            if (KiqqiAppManager.Instance.Game.currentMiniGame is KiqqiCandyFactoryManager mgr)
            {
                mgr.NotifyTimeExpired();
            }
        }

        #endregion

        #region PREVIEW PHASE

        public void ShowPreviewPhase(List<BagConfig> bags, List<CandyItem> items, float duration)
        {
            var difficulty = KiqqiAppManager.Instance.Levels.GetCurrentDifficulty();
            currentLayout = GetLayoutForDifficulty(difficulty);

            if (currentLayout == null)
            {
                Debug.LogError($"[CandyFactoryView] No layout configured for difficulty: {difficulty}");
                return;
            }

            if (previewCoroutine != null)
                StopCoroutine(previewCoroutine);

            previewCoroutine = StartCoroutine(PreviewRoutine(bags, items, duration));
        }

        private IEnumerator PreviewRoutine(List<BagConfig> bags, List<CandyItem> items, float duration)
        {
            ClearItems();
            ClearBags();

            BuildBags(bags, true);
            BuildItems(items);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            HideBagContents();

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiCandyFactoryManager mgr)
            {
                mgr.OnPreviewComplete();
            }
        }

        #endregion

        #region BUILD UI ELEMENTS

        private void BuildItems(List<CandyItem> items)
        {
            ClearItems();

            if (currentLayout == null || currentLayout.itemShelfParent == null || !itemTemplate)
            {
                Debug.LogWarning("[CandyFactoryView] Cannot build items - missing layout or template");
                return;
            }

            List<Transform> spawnPoints = new();
            foreach (Transform child in currentLayout.itemShelfParent)
            {
                spawnPoints.Add(child);
            }

            if (spawnPoints.Count < items.Count)
            {
                Debug.LogWarning($"[CandyFactoryView] Not enough spawn points ({spawnPoints.Count}) for items ({items.Count})");
            }

            for (int i = 0; i < items.Count && i < spawnPoints.Count; i++)
            {
                GameObject itemObj = Instantiate(itemTemplate, spawnPoints[i]);
                itemObj.transform.localPosition = Vector3.zero;
                itemObj.transform.localScale = Vector3.one;
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
                }
            }
        }

        private void BuildBags(List<BagConfig> bags, bool showContents)
        {
            ClearBags();

            if (currentLayout == null || currentLayout.bagParent == null || !bagTemplate)
            {
                Debug.LogWarning("[CandyFactoryView] Cannot build bags - missing layout or template");
                return;
            }

            List<Transform> spawnPoints = new();
            foreach (Transform child in currentLayout.bagParent)
            {
                spawnPoints.Add(child);
            }

            if (spawnPoints.Count < bags.Count)
            {
                Debug.LogWarning($"[CandyFactoryView] Not enough spawn points ({spawnPoints.Count}) for bags ({bags.Count})");
            }

            var levelMgr = KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiCandyFactoryManager;
            var levelLogic = levelMgr?.GetComponent<KiqqiCandyFactoryLevelManager>();

            if (levelLogic == null)
            {
                levelLogic = FindFirstObjectByType<KiqqiCandyFactoryLevelManager>();
                if (levelLogic == null)
                {
                    Debug.LogError("[CandyFactoryView] KiqqiCandyFactoryLevelManager not found in scene!");
                }
            }

            for (int i = 0; i < bags.Count && i < spawnPoints.Count; i++)
            {
                GameObject bagObj = Instantiate(bagTemplate, spawnPoints[i]);
                bagObj.transform.localPosition = Vector3.zero;
                bagObj.transform.localScale = Vector3.one;
                bagObj.SetActive(true);

                Button btn = bagObj.GetComponent<Button>();
                if (btn)
                {
                    int index = i;
                    btn.onClick.AddListener(() => OnBagClicked(index));
                    bagButtons.Add(btn);
                }

                Image bagImg = bagObj.GetComponent<Image>();
                if (bagImg)
                {
                    bagImg.color = bags[i].bagColor;
                }

                Transform contentRoot = bagObj.transform.Find("ContentRoot");
                if (contentRoot)
                {
                    foreach (int itemID in bags[i].requiredItemTypes)
                    {
                        GameObject contentObj = new GameObject($"Content_{itemID}");
                        contentObj.transform.SetParent(contentRoot, false);

                        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
                        contentRect.sizeDelta = bagContentSize;
                        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
                        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
                        contentRect.pivot = new Vector2(0.5f, 0.5f);
                        contentRect.anchoredPosition = Vector2.zero;

                        Image contentImg = contentObj.AddComponent<Image>();
                        bagContentImages.Add(contentImg);

                        if (levelLogic != null)
                        {
                            var itemDef = levelLogic.GetItemDefinition(itemID);
                            if (itemDef?.sprite != null)
                            {
                                contentImg.sprite = itemDef.sprite;
                            }
                            else
                            {
                                Debug.LogWarning($"[CandyFactoryView] No sprite found for item ID: {itemID}");
                            }
                        }

                        contentObj.SetActive(showContents);
                    }
                }
            }
        }

        private void HideBagContents()
        {
            foreach (Image img in bagContentImages)
            {
                if (img)
                    img.gameObject.SetActive(false);
            }
        }

        #endregion

        #region USER INPUT HANDLING

        private void OnItemClicked(int index)
        {
            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiCandyFactoryManager mgr)
            {
                mgr.HandleItemSelected(index);
            }
        }

        private void OnBagClicked(int index)
        {
            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiCandyFactoryManager mgr)
            {
                mgr.HandleBagSelected(index);
            }
        }

        public void HighlightSelectedItem(int index)
        {
            selectedItemIndex = index;

            for (int i = 0; i < itemButtons.Count; i++)
            {
                if (itemButtons[i])
                {
                    var colors = itemButtons[i].colors;
                    colors.normalColor = (i == index) ? Color.yellow : Color.white;
                    itemButtons[i].colors = colors;
                }
            }
        }

        public void RemoveItemFromShelf(int index)
        {
            if (index >= 0 && index < itemButtons.Count && itemButtons[index])
            {
                itemButtons[index].gameObject.SetActive(false);
            }
        }

        #endregion

        #region FEEDBACK & PARTICLES

        public void ShowFeedback(bool correct, int bagIndex)
        {
            StartCoroutine(FeedbackRoutine(correct, bagIndex));
        }

        private IEnumerator FeedbackRoutine(bool correct, int bagIndex)
        {
            Image feedback = correct ? feedbackRight : feedbackWrong;

            if (feedback && bagIndex >= 0 && bagIndex < bagButtons.Count && bagButtons[bagIndex])
            {
                RectTransform bagRect = bagButtons[bagIndex].GetComponent<RectTransform>();
                RectTransform feedbackRect = feedback.GetComponent<RectTransform>();

                if (feedbackRect && bagRect)
                {
                    feedbackRect.position = bagRect.position + new Vector3(0f, feedbackYOffset, 0f);
                    feedback.gameObject.SetActive(true);

                    Color col = feedback.color;
                    col.a = 1f;
                    feedback.color = col;

                    float t = 0f;
                    while (t < feedbackFadeDuration)
                    {
                        t += Time.deltaTime;
                        col.a = Mathf.Lerp(1f, 0f, t / feedbackFadeDuration);
                        feedback.color = col;
                        yield return null;
                    }

                    feedback.gameObject.SetActive(false);
                }
            }

            if (correct)
            {
                KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect");
                if (correctParticleRoot && bagIndex >= 0 && bagIndex < bagButtons.Count && bagButtons[bagIndex])
                {
                    RectTransform bagRect = bagButtons[bagIndex].GetComponent<RectTransform>();
                    if (bagRect)
                    {
                        PlayCorrectParticlesAt(bagRect);
                    }
                }
            }
            else
            {
                KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
            }
        }

        private void PlayCorrectParticlesAt(RectTransform target)
        {
            if (!correctParticleRoot || !target) return;

            correctParticleRoot.transform.position = target.position;
            correctParticleRoot.SetActive(false);
            correctParticleRoot.SetActive(true);

            var ps = correctParticleRoot.GetComponent<ParticleSystem>();
            if (ps)
                ps.Play();
        }

        #endregion

        #region SCORE UPDATE

        public void UpdateScoreLabel(int score)
        {
            if (scoreValueText)
                scoreValueText.text = score.ToString("00000");
        }

        #endregion

        #region CLEANUP

        private void ClearItems()
        {
            foreach (var btn in itemButtons)
                if (btn) Destroy(btn.gameObject);
            itemButtons.Clear();
            itemImages.Clear();
            selectedItemIndex = -1;
        }

        private void ClearBags()
        {
            foreach (var btn in bagButtons)
                if (btn) Destroy(btn.gameObject);
            bagButtons.Clear();
            bagContentImages.Clear();
        }

        #endregion
    }
}
