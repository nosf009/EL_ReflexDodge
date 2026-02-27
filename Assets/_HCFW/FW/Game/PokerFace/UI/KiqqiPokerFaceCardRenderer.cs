using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    public class KiqqiPokerFaceCardRenderer : MonoBehaviour
    {
        [Header("Layouts for 2 / 3 / 4 symbols")]
        [SerializeField] private GameObject layout2;
        [SerializeField] private GameObject layout3;
        [SerializeField] private GameObject layout4;

        // caches for quick lookup
        private Image[] symbols2;
        private Image[] symbols3;
        private Image[] symbols4;

        private void Awake()
        {
            if (layout2) symbols2 = layout2.GetComponentsInChildren<Image>(true);
            if (layout3) symbols3 = layout3.GetComponentsInChildren<Image>(true);
            if (layout4) symbols4 = layout4.GetComponentsInChildren<Image>(true);
        }

        public void ApplySprites(Sprite[] spriteSet)
        {
            if (spriteSet == null || spriteSet.Length == 0)
            {
                DisableAllLayouts();
                return;
            }

            int count = spriteSet.Length;

            // select proper layout
            DisableAllLayouts();
            Image[] targetSymbols = null;

            switch (count)
            {
                case 2:
                    if (layout2) layout2.SetActive(true);
                    targetSymbols = symbols2;
                    break;
                case 3:
                    if (layout3) layout3.SetActive(true);
                    targetSymbols = symbols3;
                    break;
                case 4:
                    if (layout4) layout4.SetActive(true);
                    targetSymbols = symbols4;
                    break;
                default:
                    Debug.LogWarning($"[KiqqiPokerFaceCardRenderer] Unsupported symbol count: {count}");
                    return;
            }

            // assign sprites
            for (int i = 0; i < targetSymbols.Length; i++)
            {
                bool active = (i < count && spriteSet[i] != null);
                targetSymbols[i].gameObject.SetActive(active);
                if (active)
                    targetSymbols[i].sprite = spriteSet[i];
            }
        }

        private void DisableAllLayouts()
        {
            if (layout2) layout2.SetActive(false);
            if (layout3) layout3.SetActive(false);
            if (layout4) layout4.SetActive(false);
        }
    }
}
