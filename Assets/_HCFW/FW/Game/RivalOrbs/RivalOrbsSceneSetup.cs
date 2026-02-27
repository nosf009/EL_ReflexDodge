using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class RivalOrbsSceneSetup : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private GameObject viewGameObject;
        [SerializeField] private GameObject managerContainer;

        [ContextMenu("Setup Rival Orbs Scene")]
        public void SetupScene()
        {
            Debug.Log("[RivalOrbsSetup] Starting scene setup...");

            if (viewGameObject == null)
            {
                Debug.LogError("[RivalOrbsSetup] View GameObject not assigned!");
                return;
            }

            KiqqiFuelCarView oldView = viewGameObject.GetComponent<KiqqiFuelCarView>();
            if (oldView != null)
            {
                DestroyImmediate(oldView);
                Debug.Log("[RivalOrbsSetup] Removed old FuelCar view component");
            }

            KiqqiRivalOrbsView newView = viewGameObject.GetComponent<KiqqiRivalOrbsView>();
            if (newView == null)
            {
                newView = viewGameObject.AddComponent<KiqqiRivalOrbsView>();
                Debug.Log("[RivalOrbsSetup] Added RivalOrbs view component");
            }

            Transform timePanel = viewGameObject.transform.Find("pfTimePanel");
            Transform scorePanel = viewGameObject.transform.Find("pfScorePanel");
            Transform countdown = viewGameObject.transform.Find("pfLevelStartCounter");
            Transform pauseBtn = viewGameObject.transform.Find("pfPauseBtn");

            if (timePanel != null)
            {
                Text timeValueText = timePanel.Find("pfTimeValueText")?.GetComponent<Text>();
                if (timeValueText != null)
                {
                    SetPrivateField(newView, "timeValueText", timeValueText);
                    Debug.Log("[RivalOrbsSetup] Assigned timeValueText");
                }
            }

            if (scorePanel != null)
            {
                Text scoreValueText = scorePanel.Find("gvScoreValueText")?.GetComponent<Text>();
                if (scoreValueText != null)
                {
                    SetPrivateField(newView, "scoreValueText", scoreValueText);
                    Debug.Log("[RivalOrbsSetup] Assigned scoreValueText");
                }
            }

            if (countdown != null)
            {
                Text countdownText = countdown.Find("pfLevelStartCounterText")?.GetComponent<Text>();
                if (countdownText != null)
                {
                    SetPrivateField(newView, "countdownLabel", countdownText);
                    Debug.Log("[RivalOrbsSetup] Assigned countdownLabel");
                }
            }

            if (pauseBtn != null)
            {
                Button pauseButton = pauseBtn.GetComponent<Button>();
                if (pauseButton != null)
                {
                    SetPrivateField(newView, "pauseButton", pauseButton);
                    Debug.Log("[RivalOrbsSetup] Assigned pauseButton");
                }
            }

            CreateGameplayElements(viewGameObject.transform);

            if (managerContainer != null)
            {
                KiqqiRivalOrbsLevelManager levelMgr = managerContainer.GetComponent<KiqqiRivalOrbsLevelManager>();
                if (levelMgr == null)
                {
                    levelMgr = managerContainer.AddComponent<KiqqiRivalOrbsLevelManager>();
                    Debug.Log("[RivalOrbsSetup] Added RivalOrbs level manager");
                }

                KiqqiRivalOrbsManager gameMgr = managerContainer.GetComponent<KiqqiRivalOrbsManager>();
                if (gameMgr == null)
                {
                    gameMgr = managerContainer.AddComponent<KiqqiRivalOrbsManager>();
                    Debug.Log("[RivalOrbsSetup] Added RivalOrbs game manager");
                }

                SetPrivateField(gameMgr, "levelLogic", levelMgr);
                Debug.Log("[RivalOrbsSetup] Linked managers");
            }

            Debug.Log("[RivalOrbsSetup] Scene setup complete!");
        }

        private void CreateGameplayElements(Transform parent)
        {
            GameObject gameplayRoot = new GameObject("roGameplayRoot");
            RectTransform rootRT = gameplayRoot.AddComponent<RectTransform>();
            gameplayRoot.transform.SetParent(parent, false);

            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            rootRT.sizeDelta = new Vector2(600f, 1000f);
            rootRT.anchoredPosition = Vector2.zero;

            Image bgImage = gameplayRoot.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            GameObject topArea = new GameObject("roTopArea");
            RectTransform topRT = topArea.AddComponent<RectTransform>();
            topArea.transform.SetParent(rootRT, false);
            topRT.anchorMin = new Vector2(0f, 0.5f);
            topRT.anchorMax = new Vector2(1f, 1f);
            topRT.offsetMin = Vector2.zero;
            topRT.offsetMax = Vector2.zero;
            Image topImg = topArea.AddComponent<Image>();
            topImg.color = new Color(0.3f, 0.5f, 0.7f, 1f);

            GameObject bottomArea = new GameObject("roBottomArea");
            RectTransform bottomRT = bottomArea.AddComponent<RectTransform>();
            bottomArea.transform.SetParent(rootRT, false);
            bottomRT.anchorMin = new Vector2(0f, 0f);
            bottomRT.anchorMax = new Vector2(1f, 0.5f);
            bottomRT.offsetMin = Vector2.zero;
            bottomRT.offsetMax = Vector2.zero;
            Image bottomImg = bottomArea.AddComponent<Image>();
            bottomImg.color = new Color(0.7f, 0.5f, 0.3f, 1f);

            GameObject barrier = new GameObject("roBarrier");
            RectTransform barrierRT = barrier.AddComponent<RectTransform>();
            barrier.transform.SetParent(rootRT, false);
            barrierRT.anchorMin = new Vector2(0.5f, 0f);
            barrierRT.anchorMax = new Vector2(0.5f, 1f);
            barrierRT.sizeDelta = new Vector2(20f, 0f);
            barrierRT.anchoredPosition = Vector2.zero;
            Image barrierImg = barrier.AddComponent<Image>();
            barrierImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            GameObject gap = new GameObject("roBarrierGap");
            RectTransform gapRT = gap.AddComponent<RectTransform>();
            gap.transform.SetParent(barrierRT, false);
            gapRT.anchorMin = new Vector2(0.5f, 0.5f);
            gapRT.anchorMax = new Vector2(0.5f, 0.5f);
            gapRT.sizeDelta = new Vector2(20f, 200f);
            gapRT.anchoredPosition = Vector2.zero;
            Image gapImg = gap.AddComponent<Image>();
            gapImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            GameObject orbContainer = new GameObject("roOrbContainer");
            RectTransform orbContainerRT = orbContainer.AddComponent<RectTransform>();
            orbContainer.transform.SetParent(rootRT, false);
            orbContainerRT.anchorMin = Vector2.zero;
            orbContainerRT.anchorMax = Vector2.one;
            orbContainerRT.offsetMin = Vector2.zero;
            orbContainerRT.offsetMax = Vector2.zero;

            GameObject topOrbPrefab = CreateOrbPrefab("roTopOrbPrefab", new Color(0.2f, 0.7f, 0.9f, 1f));
            topOrbPrefab.transform.SetParent(parent, false);
            topOrbPrefab.SetActive(false);

            GameObject bottomOrbPrefab = CreateOrbPrefab("roBottomOrbPrefab", new Color(0.9f, 0.6f, 0.2f, 1f));
            bottomOrbPrefab.transform.SetParent(parent, false);
            bottomOrbPrefab.SetActive(false);

            Debug.Log("[RivalOrbsSetup] Created gameplay elements");
        }

        private GameObject CreateOrbPrefab(string name, Color color)
        {
            GameObject orb = new GameObject(name);
            RectTransform rt = orb.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60f, 60f);

            Image img = orb.AddComponent<Image>();
            img.color = color;

            return orb;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
