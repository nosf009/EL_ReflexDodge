using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public enum PokerFaceSuit
    {
        Spades,
        Hearts,
        Diamonds,
        Clubs
        // you can rename/add as needed (you said 3 sets, so e.g. Spades, Clubs, Stars)
    }

    [System.Serializable]
    public class PokerFaceSuitGroup
    {
        public PokerFaceSuit suitType;
        [Tooltip("All sprite variants belonging to this suit (4 symbols expected).")]
        public List<Sprite> variants = new();
    }

    public class KiqqiPokerFaceAssets : MonoBehaviour
    {
        public static KiqqiPokerFaceAssets Instance { get; private set; }

        [Header("Grouped Suit Variants")]
        public List<PokerFaceSuitGroup> suitGroups = new();

        private Dictionary<PokerFaceSuit, List<Sprite>> suitDict = new();

        private Dictionary<Sprite, PokerFaceSuit> spriteToSuit = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildDictionary();
        }

        private void BuildDictionary()
        {
            suitDict.Clear();
            spriteToSuit.Clear();

            foreach (var g in suitGroups)
            {
                if (g == null || g.variants == null || g.variants.Count == 0)
                    continue;

                suitDict[g.suitType] = g.variants;

                // Build reverse lookup for fast GetSuitFromSprite()
                foreach (var sprite in g.variants)
                {
                    if (sprite == null) continue;
                    spriteToSuit[sprite] = g.suitType;
                }
            }

            Debug.Log($"[KiqqiPokerFaceAssets] Registered {suitDict.Count} suit groups, {spriteToSuit.Count} sprites total.");
        }


        public Sprite GetRandomSprite(PokerFaceSuit suit)
        {
            if (!suitDict.TryGetValue(suit, out var list) || list.Count == 0)
                return null;
            return list[Random.Range(0, list.Count)];
        }

        public PokerFaceSuit GetRandomSuit()
        {
            if (suitDict.Count == 0) return PokerFaceSuit.Spades;
            var keys = new List<PokerFaceSuit>(suitDict.Keys);
            return keys[Random.Range(0, keys.Count)];
        }

        public Sprite[] PickRandomSymbolsFromSuit(PokerFaceSuit suit, int count)
        {
            var list = suitDict.ContainsKey(suit) ? suitDict[suit] : null;
            if (list == null || list.Count == 0) return new Sprite[count];
            Sprite[] result = new Sprite[count];
            for (int i = 0; i < count; i++)
                result[i] = list[Random.Range(0, list.Count)];
            return result;
        }

        public PokerFaceSuit GetSuitFromSprite(Sprite sprite)
        {
            if (sprite == null)
                return PokerFaceSuit.Spades; // safe fallback
            if (spriteToSuit.TryGetValue(sprite, out var suit))
                return suit;

            // If somehow missing (e.g. dynamically added sprite), rebuild lookup once
            BuildDictionary();
            if (spriteToSuit.TryGetValue(sprite, out suit))
                return suit;

            return PokerFaceSuit.Spades; // final fallback
        }

    }
}
