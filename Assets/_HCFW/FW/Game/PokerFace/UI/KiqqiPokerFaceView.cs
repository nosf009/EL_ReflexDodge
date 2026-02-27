using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.FantasyRPG;

namespace Kiqqi.Framework
{
    /// <summary>
    /// View layer for "Other Ones" (Poker Face) gameplay.
    /// Displays a grid of buttons/symbols and handles all view-side logic and effects.
    /// </summary>
    public class KiqqiPokerFaceView : KiqqiGameViewBase
    {
        #region INSPECTOR CONFIGURATION
        [Header("UI References")]
        public Transform elementRoot;
        public GameObject elementTemplate;
        public Text resultLabel;

        [Header("Background Fade")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 1f;

        [Header("Particles")]
        public GameObject correctParticleRoot;

        [Header("Feedback Sprites")]
        public Image feedbackRight;
        public Image feedbackWrong;
        public float feedbackFadeDuration = 0.35f;
        public float feedbackYOffset = 60f;

        [Header("Template Parents (by difficulty)")]
        [SerializeField] private Transform beginnerTemplate;
        [SerializeField] private Transform easyTemplate;
        [SerializeField] private Transform mediumTemplate;
        [SerializeField] private Transform advancedTemplate;
        [SerializeField] private Transform hardTemplate;
        #endregion

        #region RUNTIME STATE
        private readonly List<Button> buttons = new();
        protected IReadOnlyList<Button> Cards => buttons;
        private readonly List<Image> images = new();
        //private bool overrideFadeOnly = false;
        #endregion

        #region VIEW INITIALIZATION & SHOW LOGIC (DO NOT MODIFY)

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            if (pauseButton)
                pauseButton.interactable = false;

            //if (game.ResumeRequested)
            //    overrideFadeOnly = true;
            //else
            //    overrideFadeOnly = false;

            //if (overrideFadeOnly)
            //    transitionType = TransitionType.Fade;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiPokerFaceManager mgr)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(mgr));
                return;
            }

            ClearGrid();
            if (resultLabel) resultLabel.text = "";

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

        #region BACKGROUND FADE & RESUME HANDLING

        private IEnumerator HandleResumeFadeIn(KiqqiPokerFaceManager mgr)
        {
            if (elementRoot)
                elementRoot.gameObject.SetActive(true);

            var cardRenderers = elementRoot ? elementRoot.GetComponentsInChildren<KiqqiPokerFaceCardRenderer>(true) : null;
            var cardImages = new List<Image>();
            if (cardRenderers != null)
            {
                foreach (var r in cardRenderers)
                {
                    if (r == null) continue;
                    var imgs = r.GetComponentsInChildren<Image>(true);
                    foreach (var img in imgs)
                    {
                        var c = img.color;
                        c.a = 0f;
                        img.color = c;
                        cardImages.Add(img);
                    }
                }
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

                    foreach (var img in cardImages)
                    {
                        if (!img) continue;
                        var col = img.color;
                        col.a = Mathf.Lerp(0f, 1f, eased);
                        img.color = col;
                    }

                    yield return null;
                }

                bgCol.a = 1f;
                gameplayBackground.color = bgCol;

                foreach (var img in cardImages)
                {
                    if (!img) continue;
                    var col = img.color;
                    col.a = 1f;
                    img.color = col;
                }
            }
            mgr.ResumeFromPause(this);
            timerRunning = true;
            if (pauseButton)
                pauseButton.interactable = true;

            //overrideFadeOnly = false;
            //transitionType = TransitionType.Slide;
        }

        protected override void OnCountdownFinished()
        {
            StartCoroutine(HandleBackgroundFadeThenStart());
        }

        private IEnumerator HandleBackgroundFadeThenStart()
        {
            ClearGrid();

            if (elementRoot)
                elementRoot.gameObject.SetActive(true);

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

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiPokerFaceManager mgr)
            {
                mgr.StartMiniGame();
            }

            timerRunning = true;
        }

        #endregion

        #region ELEMENT GRID & DEALING

        public void BuildElements(int count)
        {
            foreach (var b in buttons)
                if (b) Destroy(b.gameObject);
            buttons.Clear();
            images.Clear();

            if (!elementRoot || !elementTemplate)
                return;

            var diff = KiqqiAppManager.Instance.Levels.GetCurrentDifficulty();
            var result = PokerFaceCardFactory.GenerateCardSets(diff, count);
            var sets = result.cards;

            if (KiqqiAppManager.Instance.Game.currentMiniGame is KiqqiPokerFaceManager mgr)
            {
                mgr.SetUniqueIndex(result.uniqueIndex);
            }

            Transform targetGroup = null;

            switch (diff)
            {
                case KiqqiLevelManager.KiqqiDifficulty.Beginner: targetGroup = beginnerTemplate; break;
                case KiqqiLevelManager.KiqqiDifficulty.Easy: targetGroup = easyTemplate; break;
                case KiqqiLevelManager.KiqqiDifficulty.Medium: targetGroup = mediumTemplate; break;
                case KiqqiLevelManager.KiqqiDifficulty.Advanced: targetGroup = advancedTemplate; break;
                case KiqqiLevelManager.KiqqiDifficulty.Hard: targetGroup = hardTemplate; break;
            }

            if (targetGroup == null)
                Debug.LogWarning($"[KiqqiPokerFaceView] No TargetTemplate found for {count} cards — using centered fallback.");

            List<Transform> targetPoints = new();
            if (targetGroup)
            {
                foreach (Transform child in targetGroup)
                    targetPoints.Add(child);
            }

            var rects = new List<RectTransform>();

            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(elementTemplate, elementRoot);
                go.SetActive(true);

                var layout = go.GetComponent<LayoutElement>();
                if (layout) layout.ignoreLayout = true;

                var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
                int localIndex = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    AnimateTapReaction(localIndex);
                    (KiqqiAppManager.Instance.Game.currentMiniGame as KiqqiPokerFaceManager)
                        ?.HandleElementTapped(localIndex);
                });
                buttons.Add(btn);

                var rt = go.GetComponent<RectTransform>();
                rt.localScale = Vector3.zero;
                rects.Add(rt);

                var renderer = go.GetComponent<KiqqiPokerFaceCardRenderer>();
                if (renderer && sets != null && i < sets.Length)
                    renderer.ApplySprites(sets[i]);
            }

            List<Vector2> targetPositions = new();
            List<Quaternion> targetRotations = new();

            for (int i = 0; i < rects.Count; i++)
            {
                if (targetPoints.Count > 0 && i < targetPoints.Count)
                {
                    var pt = targetPoints[i];
                    targetPositions.Add(pt.GetComponent<RectTransform>().anchoredPosition);
                    targetRotations.Add(pt.localRotation);
                }
                else
                {
                    targetPositions.Add(Vector2.zero);
                    targetRotations.Add(Quaternion.identity);
                }
            }

            StartCoroutine(AnimateDealToTargets(rects, targetPositions, targetRotations));
        }

        private IEnumerator AnimateDealToTargets(List<RectTransform> rects, List<Vector2> targets, List<Quaternion> rotations)
        {
            if (rects.Count == 0) yield break;
            if (pauseButton) { pauseButton.interactable = false; }

            foreach (var b in buttons)
                b.interactable = false;

            float baseDelay = 0.2f;
            float overlap = 0.15f;
            var activeCoroutines = new List<Coroutine>();

            for (int i = 0; i < rects.Count; i++)
            {
                Vector2 target = targets[i];
                Quaternion rot = rotations[i];
                float delay = i * baseDelay;

                Coroutine anim = StartCoroutine(AnimateDealCard(rects[i], target, delay, rot));
                activeCoroutines.Add(anim);

                if (i < rects.Count - 1)
                    yield return new WaitForSeconds(baseDelay * overlap);
            }

            foreach (var co in activeCoroutines)
                yield return co;

            yield return new WaitForEndOfFrame();

            foreach (var b in buttons)
                b.interactable = true;

            if (pauseButton)
                pauseButton.interactable = true;
        }

        private IEnumerator AnimateDealCard(RectTransform rt, Vector2 targetPos, float delay, Quaternion targetRot)
        {
            yield return new WaitForSeconds(delay);
            if (rt == null || rt.gameObject == null) yield break;

            float duration = 0.55f;
            float arcHeight = 120f;
            float startYOffset = -Screen.height * 0.55f;

            UnityEngine.UI.Shadow shadow = null;
            Color shadowColor = Color.black;

            if (rt != null)
            {
                shadow = rt.GetComponent<UnityEngine.UI.Shadow>();
                if (shadow)
                {
                    shadowColor = shadow.effectColor;
                    shadow.effectDistance = new Vector2(25f, -25f);
                    shadow.effectColor = new Color(shadowColor.r, shadowColor.g, shadowColor.b, 0.7f);
                }
            }

            Vector2 startPos = new Vector2(targetPos.x + Random.Range(-80f, 80f), targetPos.y + startYOffset);
            Vector2 controlPos = targetPos + new Vector2(Random.Range(-40f, 40f), arcHeight);

            rt.anchoredPosition = startPos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * 0.92f;

            float t = 0f;
            while (t < duration)
            {
                if (rt == null || rt.gameObject == null) yield break;

                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - n, 2.5f);

                Vector2 a = Vector2.Lerp(startPos, controlPos, eased);
                Vector2 b = Vector2.Lerp(controlPos, targetPos, eased);
                Vector2 pos = Vector2.Lerp(a, b, eased);
                rt.anchoredPosition = pos;

                rt.localRotation = Quaternion.Slerp(Quaternion.identity, targetRot, eased);
                rt.localScale = Vector3.Lerp(Vector3.one * 0.92f, Vector3.one, eased);

                if (shadow)
                {
                    shadow.effectDistance = Vector2.Lerp(new Vector2(25f, -25f), new Vector2(5f, -5f), eased);
                    shadow.effectColor = new Color(
                        shadowColor.r,
                        shadowColor.g,
                        shadowColor.b,
                        Mathf.Lerp(0.7f, 0.5f, eased)
                    );
                }

                yield return null;
            }

            if (rt == null || rt.gameObject == null) yield break;

            rt.anchoredPosition = targetPos;
            rt.localRotation = targetRot;
            rt.localScale = Vector3.one;

            if (shadow)
            {
                shadow.effectDistance = new Vector2(5f, -5f);
                shadow.effectColor = new Color(shadowColor.r, shadowColor.g, shadowColor.b, 0.5f);
            }
        }

        #endregion

        #region TAP & FEEDBACK ANIMATIONS
        public void AnimateTapReaction(int index)
        {
            if (index < 0 || index >= buttons.Count) return;
            var rt = buttons[index].GetComponent<RectTransform>();
            if (!rt) return;
            StartCoroutine(TapLift(rt));
        }

        private IEnumerator TapLift(RectTransform rt)
        {
            float lift = 35f;
            float scaleUp = 1.08f;
            float durUp = 0.08f, durDown = 0.12f;

            Vector3 basePos = rt.localPosition;
            Vector3 baseScale = rt.localScale;

            float t = 0f;
            while (t < durUp)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / durUp);
                float ease = 1f - Mathf.Pow(1f - n, 2.5f);
                rt.localPosition = basePos + Vector3.up * lift * ease;
                rt.localScale = Vector3.Lerp(baseScale, baseScale * scaleUp, ease);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.05f);

            t = 0f;
            while (t < durDown)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / durDown);
                float ease = n * n * (3f - 2f * n);
                rt.localPosition = Vector3.Lerp(basePos + Vector3.up * lift, basePos, ease);
                rt.localScale = Vector3.Lerp(baseScale * scaleUp, baseScale, ease);
                yield return null;
            }

            rt.localPosition = basePos;
            rt.localScale = baseScale;
        }
        #endregion

        #region PARTICLES & FEEDBACK
        public void PlayCorrectParticlesAtCard(RectTransform cardRT)
        {
            if (!correctParticleRoot || !cardRT)
                return;

            var rootRect = correctParticleRoot.transform as RectTransform;
            rootRect.SetParent(elementRoot, worldPositionStays: false);
            rootRect.position = cardRT.position;
            rootRect.localScale = Vector3.one;
            rootRect.SetAsLastSibling();

            correctParticleRoot.SetActive(true);

            var allPS = correctParticleRoot.GetComponentsInChildren<UIParticleSystem>(true);
            foreach (var ps in allPS)
                ps.StartParticleEmission();

            StartCoroutine(DisableParticleRootAfter(1f));
        }

        private IEnumerator DisableParticleRootAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            if (!correctParticleRoot) yield break;

            var allPS = correctParticleRoot.GetComponentsInChildren<UIParticleSystem>(true);
            foreach (var ps in allPS)
                ps.StopParticleEmission();

            correctParticleRoot.SetActive(false);
        }
        #endregion

        #region FEEDBACK DISPLAY & RESULTS
        public void ShowCorrectFeedback(int index)
        {
            if (resultLabel) resultLabel.text = "OK";
            KiqqiAppManager.Instance.Audio.PlaySfx("answercorrect2");
        }

        public void ShowWrongFeedback(int index)
        {
            if (resultLabel) resultLabel.text = "NOK";
            KiqqiAppManager.Instance.Audio.PlaySfx("answerwrong");
        }

        public virtual void ShowFeedback(bool correct, int tappedIndex)
        {
            float delayBeforeFeedback = 0.2f;
            StartCoroutine(ShowFeedbackDelayed(correct, delayBeforeFeedback, tappedIndex));
        }

        private IEnumerator ShowFeedbackDelayed(bool correct, float delay, int tappedIndex)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return AnimateFeedback(correct, tappedIndex);
        }

        public RectTransform GetCardRect(int index)
        {
            if (index < 0 || index >= buttons.Count) return null;
            return buttons[index].GetComponent<RectTransform>();
        }

        protected IEnumerator AnimateFeedback(bool correct, int tappedIndex)
        {
            var img = correct ? feedbackRight : feedbackWrong;
            if (!img) yield break;

            if (elementRoot)
                elementRoot.gameObject.SetActive(false);

            img.gameObject.SetActive(true);
            var rt = img.rectTransform;

            Vector2 startPos = new(0, 100f);
            Vector2 endPos = Vector2.zero;
            Vector2 downPos = new(0, -60f);
            float fadeDur = 0.35f;
            float hold = 0.3f;

            img.color = new Color(1, 1, 1, 0);
            rt.anchoredPosition = startPos;
            rt.localScale = Vector3.one * 0.9f;

            KiqqiAppManager.Instance.Audio.PlaySfx(correct ? "answercorrect2" : "answerwrong");

            yield return AnimateUI(rt, img, startPos, endPos, 0f, 1f, fadeDur, true);
            yield return new WaitForSecondsRealtime(hold);
            yield return AnimateUI(rt, img, endPos, downPos, 1f, 0f, fadeDur, false);

            img.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.15f);

            if (elementRoot)
                elementRoot.gameObject.SetActive(true);

            var gm = KiqqiAppManager.Instance.Game;
            if (gm.currentMiniGame is KiqqiPokerFaceManager mgr)
            {
                if (mgr is KiqqiPokerFaceTutorialManager tut && tut.skipNextDeal)
                    yield break;

                mgr.TriggerNextDealAfterFeedback();
            }
        }

        public void ResetFeedback()
        {
            if (feedbackRight) feedbackRight.gameObject.SetActive(false);
            if (feedbackWrong) feedbackWrong.gameObject.SetActive(false);
            if (resultLabel) resultLabel.text = "";
        }

        private IEnumerator AnimateUI(RectTransform rt, Image img, Vector2 from, Vector2 to,
                                      float alphaFrom, float alphaTo, float dur, bool scaleUp)
        {
            float t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / dur);
                float eased = 1f - Mathf.Pow(1f - n, 3f);
                rt.anchoredPosition = Vector2.Lerp(from, to, eased);
                img.color = new Color(1, 1, 1, Mathf.Lerp(alphaFrom, alphaTo, eased));
                rt.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, eased);
                yield return null;
            }
        }
        #endregion

        #region TIME & UTILITY
        public void UpdateScoreLabel(int score)
        {
            if (scoreValueText)
                scoreValueText.text = score.ToString("00000");
        }

        private void ClearGrid()
        {
            if (!elementRoot) return;

            foreach (Transform child in elementRoot)
            {
                if (child.gameObject.activeInHierarchy)
                    Destroy(child.gameObject);
            }

            buttons.Clear();
            images.Clear();
        }

        protected override void OnTimeUp()
        {
            timerRunning = false;

            // NEW: let the player finish the current pick
            if (KiqqiAppManager.Instance.Game.currentMiniGame is KiqqiPokerFaceManager mini)
            {
                Debug.Log("[KiqqiPokerFaceView] Time up — waiting for final player pick.");
                mini.NotifyTimeExpired();
            }
        }

        #endregion
    }
}
