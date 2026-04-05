using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Rewards.ForgeOptions
{
    public static class ForgeChoiceRegistry
    {
        private sealed class CardModelReferenceComparer : IEqualityComparer<CardModel>
        {
            public static readonly CardModelReferenceComparer Instance = new CardModelReferenceComparer();

            public bool Equals(CardModel? x, CardModel? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(CardModel obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        private static readonly Dictionary<CardModel, IForgeOption> OptionByCard =
            new Dictionary<CardModel, IForgeOption>(CardModelReferenceComparer.Instance);

        public static int Count => OptionByCard.Count;

        public static void Register(CardModel card, IForgeOption option)
        {
            OptionByCard[card] = option;
            Log.Info($"[Blight][ForgeDebug] Registry.Register card={card?.Id} hash={RuntimeHelpers.GetHashCode(card)} option={option?.Title} count={OptionByCard.Count}");
        }

        public static bool TryGet(CardModel card, out IForgeOption option)
        {
            if (OptionByCard.TryGetValue(card, out option!))
            {
                return true;
            }

            foreach (CardModel related in EnumerateRelatedCards(card))
            {
                if (!OptionByCard.TryGetValue(related, out option!))
                {
                    continue;
                }

                // Cache successful resolution to stabilize lookups when UI uses cloned card instances.
                OptionByCard[card] = option;
                Log.Info($"[Blight][ForgeDebug] Registry.RelatedHit card={card?.Id} hash={RuntimeHelpers.GetHashCode(card)} relatedHash={RuntimeHelpers.GetHashCode(related)} option={option?.Title} count={OptionByCard.Count}");
                return true;
            }

            return false;
        }

        private static IEnumerable<CardModel> EnumerateRelatedCards(CardModel card)
        {
            var visited = new HashSet<CardModel>(CardModelReferenceComparer.Instance) { card };
            var queue = new Queue<CardModel>();

            Enqueue(card.CloneOf);
            Enqueue(card.DeckVersion);
            Enqueue(card.CanonicalInstance);

            while (queue.Count > 0)
            {
                CardModel current = queue.Dequeue();
                yield return current;

                Enqueue(current.CloneOf);
                Enqueue(current.DeckVersion);
                Enqueue(current.CanonicalInstance);
            }

            void Enqueue(CardModel? candidate)
            {
                if (candidate == null)
                {
                    return;
                }

                if (!visited.Add(candidate))
                {
                    return;
                }

                queue.Enqueue(candidate);
            }
        }

        public static void Unregister(CardModel card)
        {
            bool removed = OptionByCard.Remove(card);
            Log.Info($"[Blight][ForgeDebug] Registry.Unregister card={card?.Id} hash={RuntimeHelpers.GetHashCode(card)} removed={removed} count={OptionByCard.Count}");
        }

        public static void Clear()
        {
            int before = OptionByCard.Count;
            OptionByCard.Clear();
            Log.Info($"[Blight][ForgeDebug] Registry.Clear before={before} after={OptionByCard.Count}");
        }
    }
}
