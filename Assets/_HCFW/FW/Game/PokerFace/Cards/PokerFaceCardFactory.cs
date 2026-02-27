using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    public static class PokerFaceCardFactory
    {
        public struct PokerFaceSetResult
        {
            public Sprite[][] cards;
            public int uniqueIndex;
        }

        public static PokerFaceSetResult GenerateCardSets(KiqqiLevelManager.KiqqiDifficulty diff, int cardCount)
        {
            int symbolsPerCard = GetSymbolsPerCard(diff);
            var assets = KiqqiPokerFaceAssets.Instance;
            if (assets == null)
            {
                Debug.LogError("[PokerFaceCardFactory] KiqqiPokerFaceAssets missing!");
                return new PokerFaceSetResult { cards = new Sprite[cardCount][], uniqueIndex = 0 };
            }

            // -----------------------------
            // 1. Define pattern table
            // -----------------------------
            var patternTable = new Dictionary<int, int[][]>
            {
                { 4, new[] { new[] { 3 } } },                              // Beginner
                { 5, new[] { new[] { 2, 2 } } },                           // Easy
                { 6, new[] { new[] { 3, 2 }, new[] { 2, 3 } } },           // Medium
                { 7, new[] { new[] { 3, 3 }, new[] { 2, 2, 2 } } },        // Advanced
                { 8, new[] { new[] { 3, 2, 2 }, new[] { 2, 3, 2 }, new[] { 2, 2, 3 } } } // Hard
            };

            if (!patternTable.TryGetValue(cardCount, out var availablePatterns))
            {
                Debug.LogWarning($"[PokerFaceCardFactory] No pattern defined for cardCount={cardCount}, fallback to 3 same + 1 unique");
                availablePatterns = new[] { new[] { 3 } };
            }

            // Pick one random pattern for variation
            var selectedPattern = availablePatterns[Random.Range(0, availablePatterns.Length)];

            List<Sprite[]> cards = new();
            List<PokerFaceSuit> usedSuits = new();

            // -----------------------------
            // 2. Generate combo groups
            // -----------------------------
            foreach (int groupSize in selectedPattern)
            {
                var suit = assets.GetRandomSuit();
                usedSuits.Add(suit);

                var baseCard = PickRandomSymbolsWithRepeats(symbolsPerCard, assets, suit);
                for (int i = 0; i < groupSize; i++)
                    cards.Add((Sprite[])baseCard.Clone());
            }

            // -----------------------------
            // 3. Create the unique card
            // -----------------------------
            var baseForUnique = cards[Random.Range(0, cards.Count)];
            var uniqueSuit = GetDifferentSuit(assets, usedSuits[Random.Range(0, usedSuits.Count)]);
            var uniqueCard = MakeDistinctVariant(baseForUnique, assets, uniqueSuit);

            cards.Add(uniqueCard);
            int uniqueIndex = cards.Count - 1;

            // -----------------------------
            // 3.5 Balance color visibility
            // -----------------------------
            BalanceColorVisibility(cards, ref uniqueCard, ref uniqueIndex, assets);


            // -----------------------------
            // 4. Shuffle all cards
            // -----------------------------
            for (int i = 0; i < cards.Count; i++)
            {
                int j = Random.Range(i, cards.Count);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }

            // Find where the unique card ended up after shuffle
            for (int i = 0; i < cards.Count; i++)
            {
                bool allSame = true;
                for (int s = 0; s < symbolsPerCard; s++)
                {
                    if (cards[i][s] != uniqueCard[s])
                    {
                        allSame = false;
                        break;
                    }
                }
                if (allSame)
                {
                    uniqueIndex = i;
                    break;
                }
            }

#if UNITY_EDITOR
            // -----------------------------
            // 5. Debug print
            // -----------------------------
            var dbg = new System.Text.StringBuilder();
            for (int i = 0; i < cards.Count; i++)
                dbg.Append($"[{i}:{string.Join(",", System.Array.ConvertAll(cards[i], s => s.name))}] ");
            //Debug.Log($"[PokerFace Debug] Pattern=({string.Join("+", selectedPattern)}) Unique={uniqueIndex} Cards={cards.Count} -> {dbg}");
#endif

            return new PokerFaceSetResult
            {
                cards = cards.ToArray(),
                uniqueIndex = uniqueIndex
            };
        }

        private static void BalanceColorVisibility(
            List<Sprite[]> cards,
            ref Sprite[] uniqueCard,
            ref int uniqueIndex,
            KiqqiPokerFaceAssets assets)
        {
            // Count overall color families (assuming Hearts/Diamonds = red, Spades/Clubs = black)
            int redCount = 0;
            int blackCount = 0;

            foreach (var card in cards)
            {
                foreach (var sprite in card)
                {
                    var suit = assets.GetSuitFromSprite(sprite);
                    if (suit == PokerFaceSuit.Hearts || suit == PokerFaceSuit.Diamonds)
                        redCount++;
                    else
                        blackCount++;
                }
            }

            bool uniqueIsRed = false;
            foreach (var s in uniqueCard)
            {
                var suit = assets.GetSuitFromSprite(s);
                if (suit == PokerFaceSuit.Hearts || suit == PokerFaceSuit.Diamonds)
                {
                    uniqueIsRed = true;
                    break;
                }
            }

            // If unique card is the *only* color family in minority (e.g., 1 red among all black)
            if ((uniqueIsRed && redCount < blackCount / 4) ||
                (!uniqueIsRed && blackCount < redCount / 4))
            {
                // Flip the unique to opposite color family but preserve its symbol logic
                var targetSuit = uniqueIsRed
                    ? PokerFaceSuit.Spades   // switch to black
                    : PokerFaceSuit.Hearts;  // switch to red

                Sprite[] adjusted = new Sprite[uniqueCard.Length];
                for (int i = 0; i < uniqueCard.Length; i++)
                    adjusted[i] = assets.GetRandomSprite(targetSuit);

                uniqueCard = adjusted;
                cards[uniqueIndex] = adjusted;
            }
        }



        // ------------------------------------------------------------------
        private static Sprite[] MakeDistinctVariant(
            Sprite[] baseCard, KiqqiPokerFaceAssets assets, PokerFaceSuit uniqueSuit)
        {
            Sprite[] variant = new Sprite[baseCard.Length];
            for (int i = 0; i < baseCard.Length; i++)
            {
                // pick random sprite from unique suit
                var s = assets.GetRandomSprite(uniqueSuit);
                variant[i] = s;
            }

            // safety: ensure not identical to base
            int sameCount = 0;
            for (int i = 0; i < baseCard.Length; i++)
                if (variant[i] == baseCard[i]) sameCount++;

            if (sameCount == baseCard.Length)
            {
                // force one difference
                int idx = Random.Range(0, baseCard.Length);
                Sprite newSprite;
                int guard = 0;
                do
                {
                    newSprite = assets.GetRandomSprite(uniqueSuit);
                    guard++;
                }
                while (newSprite == baseCard[idx] && guard < 30);
                variant[idx] = newSprite;
            }

            return variant;
        }

        private static PokerFaceSuit GetDifferentSuit(KiqqiPokerFaceAssets assets, PokerFaceSuit baseSuit)
        {
            var suits = new List<PokerFaceSuit>(System.Enum.GetValues(typeof(PokerFaceSuit)) as PokerFaceSuit[]);
            suits.Remove(baseSuit);
            if (suits.Count == 0) return baseSuit;
            return suits[Random.Range(0, suits.Count)];
        }

        private static Sprite[] PickRandomSymbolsWithRepeats(
            int count, KiqqiPokerFaceAssets assets, PokerFaceSuit baseSuit)
        {
            List<Sprite> result = new();

            // Chance that a card mixes more than one suit (increases with difficulty indirectly)
            // You can tweak this constant if you ever pass difficulty here.
            float mixChance = 0.6f; // 40% of cards will have mixed red/black symbols

            bool doMix = Random.value < mixChance;

            for (int i = 0; i < count; i++)
            {
                PokerFaceSuit chosenSuit = baseSuit;

                if (doMix)
                {
                    // small per-symbol random chance to switch suit
                    if (Random.value < 0.5f)
                        chosenSuit = GetDifferentSuit(assets, baseSuit);
                }

                result.Add(assets.GetRandomSprite(chosenSuit));
            }

            return result.ToArray();
        }


        private static int GetSymbolsPerCard(KiqqiLevelManager.KiqqiDifficulty diff)
        {
            return diff switch
            {
                KiqqiLevelManager.KiqqiDifficulty.Beginner => 2,
                KiqqiLevelManager.KiqqiDifficulty.Easy => 2,
                KiqqiLevelManager.KiqqiDifficulty.Medium => 3,
                KiqqiLevelManager.KiqqiDifficulty.Advanced => 3,
                KiqqiLevelManager.KiqqiDifficulty.Hard => 4,
                _ => 2
            };
        }
    }
}
