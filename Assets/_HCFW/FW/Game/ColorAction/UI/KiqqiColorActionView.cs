using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiColorActionView : KiqqiGameViewBase
    {
        #region INSPECTOR CONFIGURATION

        [Header("Core References")]
        [SerializeField] private KiqqiColorActionAssetDB assetDB;

        [Header("Target Fruit Display (Semaphore-style)")]
        [Tooltip("Container with 3 child slots for target fruit icons")]
        public Transform targetFruitContainer;
        [Tooltip("The 3 child Image GameObjects in targetFruitContainer (assign in order: slot 0, 1, 2)")]
        public Image[] targetFruitSlots = new Image[3];

        [Header("Catch Feedback")]
        [Tooltip("Correct catch feedback image")]
        public Image feedbackCorrect;
        [Tooltip("Wrong catch feedback image")]
        public Image feedbackWrong;
        [Tooltip("Fade in time (seconds)")]
        public float fadeInTime = 0.15f;
        [Tooltip("Hold at full opacity time (seconds)")]
        public float holdTime = 0.3f;
        [Tooltip("Fade out time (seconds)")]
        public float fadeOutTime = 0.15f;

        [Header("Background")]
        [SerializeField] private Image gameplayBackground;
        [SerializeField] private float backgroundFadeDuration = 0.3f;

        #endregion

        #region RUNTIME STATE

        private Queue<GameObject> catchEffectPool = new Queue<GameObject>();
        private const int POOL_INITIAL_SIZE = 5;

        #endregion

        #region VIEW INITIALIZATION

        public override void OnShow()
        {
            var game = KiqqiAppManager.Instance.Game;
            timerRunning = false;

            InitializeCatchEffectPool();

            if (pauseButton)
                pauseButton.interactable = false;

            if (game.ResumeRequested && game.currentMiniGame is KiqqiColorActionManager mgr)
            {
                game.ResumeRequested = false;
                StartCoroutine(HandleResumeFadeIn(mgr));
                return;
            }

            ClearTargetFruitDisplay();
            ResetFeedback();

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

        #region BACKGROUND FADE & RESUME

        private IEnumerator HandleResumeFadeIn(KiqqiColorActionManager mgr)
        {
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

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiColorActionManager mgr)
            {
                mgr.StartMiniGame();
            }

            timerRunning = true;
        }

        #endregion

        #region TARGET FRUIT DISPLAY

        public void ShowTargetFruits(List<FruitType> targetFruits)
        {
            if (targetFruitSlots == null || targetFruitSlots.Length != 3)
            {
                Debug.LogWarning("[KiqqiColorActionView] Target fruit slots array must have exactly 3 elements!");
                return;
            }

            if (assetDB == null)
            {
                Debug.LogError("[KiqqiColorActionView] AssetDB not assigned!");
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                if (targetFruitSlots[i] == null)
                {
                    Debug.LogWarning($"[KiqqiColorActionView] Target fruit slot {i} not assigned!");
                    continue;
                }

                if (i < targetFruits.Count)
                {
                    targetFruitSlots[i].sprite = assetDB.GetFruitSprite(targetFruits[i]);
                    targetFruitSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    targetFruitSlots[i].sprite = assetDB.targetSlotXSprite;
                    targetFruitSlots[i].gameObject.SetActive(true);
                }
            }

            Debug.Log($"[KiqqiColorActionView] Displayed {targetFruits.Count} target fruits in semaphore-style display.");
        }

        #endregion

        #region CATCH FEEDBACK

        private void InitializeCatchEffectPool()
        {
            while (catchEffectPool.Count > 0)
            {
                var obj = catchEffectPool.Dequeue();
                if (obj != null) Destroy(obj);
            }

            if (assetDB != null && assetDB.catchEffectTemplate != null)
            {
                for (int i = 0; i < POOL_INITIAL_SIZE; i++)
                {
                    GameObject effect = Instantiate(assetDB.catchEffectTemplate, transform);
                    effect.SetActive(false);
                    catchEffectPool.Enqueue(effect);
                }
            }
        }

        private GameObject GetCatchEffect()
        {
            if (catchEffectPool.Count > 0)
            {
                return catchEffectPool.Dequeue();
            }
            else if (assetDB != null && assetDB.catchEffectTemplate != null)
            {
                return Instantiate(assetDB.catchEffectTemplate, transform);
            }
            return null;
        }

        private void ReturnCatchEffect(GameObject effect)
        {
            if (effect != null)
            {
                effect.SetActive(false);
                catchEffectPool.Enqueue(effect);
            }
        }

        public void ShowCatchFeedback(Vector3 position, bool correct, int scoreChange)
        {
            StartCoroutine(AnimateCatchFeedback(position, correct, scoreChange));
        }

        private IEnumerator AnimateCatchFeedback(Vector3 position, bool correct, int scoreChange)
        {
            GameObject catchEffect = GetCatchEffect();
            if (catchEffect != null)
            {
                catchEffect.SetActive(true);
                catchEffect.transform.position = position;

                float effectDuration = 0.4f;
                float elapsed = 0f;

                var catchImage = catchEffect.GetComponent<Image>();
                if (catchImage)
                {
                    catchImage.color = new Color(1, 1, 1, 0);

                    while (elapsed < effectDuration * 0.5f)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = elapsed / (effectDuration * 0.5f);
                        catchImage.color = new Color(1, 1, 1, t);
                        yield return null;
                    }

                    catchImage.color = new Color(1, 1, 1, 1);
                    yield return new WaitForSecondsRealtime(effectDuration * 0.2f);

                    elapsed = 0f;
                    while (elapsed < effectDuration * 0.3f)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = elapsed / (effectDuration * 0.3f);
                        catchImage.color = new Color(1, 1, 1, 1 - t);
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSecondsRealtime(effectDuration);
                }

                ReturnCatchEffect(catchEffect);
            }

            var feedbackImg = correct ? feedbackCorrect : feedbackWrong;
            if (feedbackImg)
            {
                feedbackImg.gameObject.SetActive(true);
                feedbackImg.color = new Color(1, 1, 1, 0);

                float t = 0f;
                while (t < fadeInTime)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / fadeInTime);
                    float eased = Mathf.SmoothStep(0f, 1f, n);
                    feedbackImg.color = new Color(1, 1, 1, eased);
                    yield return null;
                }

                feedbackImg.color = new Color(1, 1, 1, 1);
                yield return new WaitForSecondsRealtime(holdTime);

                t = 0f;
                while (t < fadeOutTime)
                {
                    t += Time.unscaledDeltaTime;
                    float n = Mathf.Clamp01(t / fadeOutTime);
                    float eased = Mathf.SmoothStep(1f, 0f, n);
                    feedbackImg.color = new Color(1, 1, 1, eased);
                    yield return null;
                }

                feedbackImg.gameObject.SetActive(false);
            }

            ResetFeedback();
        }

        private void ClearTargetFruitDisplay()
        {
            if (targetFruitSlots == null) return;

            foreach (var slot in targetFruitSlots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }

        private void ResetFeedback()
        {
            if (feedbackCorrect) feedbackCorrect.gameObject.SetActive(false);
            if (feedbackWrong) feedbackWrong.gameObject.SetActive(false);
        }

        #endregion

        #region SCORE & TIME

        public void UpdateScoreLabel(int score)
        {
            if (scoreValueText)
                scoreValueText.text = score.ToString("00000");
        }

        protected override void OnTimeUp()
        {
            timerRunning = false;

            if (KiqqiAppManager.Instance.Game.currentMiniGame is KiqqiColorActionManager mgr)
            {
                Debug.Log("[KiqqiColorActionView] Time up - waiting for final fruit.");
                mgr.NotifyTimeExpired();
            }
        }

        #endregion
    }
}
