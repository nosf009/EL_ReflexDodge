using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// UI helper for level select grid buttons.
    /// Handles label, interactivity, and selection highlight.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class KiqqiLevelSelectButton : MonoBehaviour
    {
        [Header("UI References")]
        public Text labelText;
        public Image backgroundImage;

        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        public Color selectedColor = new Color(1f, 1f, 0.7f, 1f);

        private Button button;
        private int levelIndex;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        /// <summary>
        /// Configure this button instance.
        /// </summary>
        public void Setup(int index, bool unlocked, bool isCurrent, System.Action<int> onClick)
        {
            levelIndex = index;

            if (labelText)
                labelText.text = index.ToString();

            if (button)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                if (unlocked)
                    button.onClick.AddListener(() => onClick?.Invoke(levelIndex));
            }

            // Set visuals
            if (backgroundImage)
            {
                if (isCurrent) backgroundImage.color = selectedColor;
                else backgroundImage.color = unlocked ? normalColor : lockedColor;
            }
        }
    }
}
