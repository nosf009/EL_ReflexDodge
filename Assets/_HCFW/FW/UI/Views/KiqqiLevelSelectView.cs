using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Displays all available levels using a button grid.
    /// Each button corresponds to one level.
    /// </summary>
    public class KiqqiLevelSelectView : KiqqiUIView
    {
        [Header("UI References")]
        public Text titleLabel;
        public Transform gridRoot;
        public GameObject tplButton;
        public Button backButton;

        private readonly List<GameObject> spawnedButtons = new();

        private void OnEnable()
        {
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() =>
                {
                    KiqqiAppManager.Instance.UI.ShowView<KiqqiMainMenuView>();
                });
            }
        }

        private void OnDisable()
        {
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
            }
        }

        public override void OnShow()
        {
            base.OnShow();
            PopulateLevelButtons();
        }

        private void PopulateLevelButtons()
        {
            if (!tplButton || !gridRoot)
            {
                Debug.LogWarning("[KiqqiLevelSelectView] Missing grid or template references.");
                return;
            }

            foreach (var go in spawnedButtons)
                Destroy(go);
            spawnedButtons.Clear();

            tplButton.SetActive(false);

            var lm = KiqqiAppManager.Instance.Levels;
            int total = lm.totalLevels;
            int current = lm.currentLevel;
            int unlocked = Mathf.Max(1, lm.GetUnlockedLevel());

            for (int i = 1; i <= total; i++)
            {
                var btnObj = Instantiate(tplButton, gridRoot);
                btnObj.SetActive(true);

                var helper = btnObj.GetComponent<KiqqiLevelSelectButton>();
                if (helper)
                {
                    bool unlockedState = i <= unlocked;
                    bool isCurrent = i == current;

                    helper.Setup(i, unlockedState, isCurrent, (levelIndex) =>
                    {
                        lm.SetCurrentLevel(levelIndex);
                        Debug.Log($"[KiqqiLevelSelectView] Level selected: {levelIndex}");

                        var gm = KiqqiAppManager.Instance.Game;

                        // Directly start the explicitly wired main mini-game
                        gm.StartMainGame();
                    });
                }

                spawnedButtons.Add(btnObj);
            }
        }
    }
}
