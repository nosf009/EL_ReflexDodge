using UnityEngine;

namespace Kiqqi.Framework
{
    public class KiqqiColorActionAssetDB : MonoBehaviour
    {
        [Header("Fruit Configuration")]
        [Tooltip("Number of fruit types to use (1-10). Set to match your sprite count.")]
        [Range(1, 10)]
        public int activeFruitCount = 5;

        [Header("Fruit Sprites")]
        [Tooltip("Assign sprites in order. Only first 'activeFruitCount' sprites will be used.")]
        public Sprite[] fruitSprites = new Sprite[10];

        [Header("UI Elements")]
        [Tooltip("Scene-based template GameObject for spawning fruit (Image + Button)")]
        public GameObject fruitTemplate;

        [Tooltip("Scene-based template GameObject for target fruit icon display")]
        public GameObject targetIconTemplate;

        [Tooltip("Sprite for 'X' mark on unused target slots")]
        public Sprite targetSlotXSprite;

        [Header("Visual Effects")]
        [Tooltip("Scene-based template for catch effect (net animation)")]
        public GameObject catchEffectTemplate;

        public Sprite GetFruitSprite(FruitType type)
        {
            int index = (int)type;
            
            if (index >= activeFruitCount)
            {
                Debug.LogWarning($"[ColorActionAssetDB] FruitType {type} is outside active range (0-{activeFruitCount - 1}). Wrapping to valid range.");
                index = index % activeFruitCount;
            }

            if (index >= 0 && index < fruitSprites.Length && fruitSprites[index] != null)
                return fruitSprites[index];

            Debug.LogWarning($"[ColorActionAssetDB] Sprite not found or null for {type} at index {index}");
            return null;
        }

        public int GetActiveFruitCount()
        {
            return Mathf.Clamp(activeFruitCount, 1, 10);
        }
    }
}
