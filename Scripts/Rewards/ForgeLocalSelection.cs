using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace BlightMod.Rewards
{
    internal static class ForgeLocalSelection
    {
        public static async Task<CardModel?> ChooseOneFromCards(IReadOnlyList<CardModel> cards, bool canSkip = false)
        {
            if (cards == null || cards.Count == 0)
            {
                return null;
            }

            if (CardSelectCmd.Selector != null)
            {
                return (await CardSelectCmd.Selector.GetSelectedCards(cards, 0, 1)).FirstOrDefault();
            }

            NChooseACardSelectionScreen? screen = NChooseACardSelectionScreen.ShowScreen(cards, canSkip);
            if (screen == null)
            {
                return null;
            }

            return (await screen.CardsSelected()).FirstOrDefault();
        }

        public static async Task<CardModel?> SelectDeckForUpgrade(Player player, CardSelectorPrefs prefs)
        {
            List<CardModel> cards = PileType.Deck.GetPile(player).Cards.Where(static c => c.IsUpgradable).ToList();
            return (await SelectDeckCards(player, cards, prefs, DeckSelectionScreenType.Upgrade)).FirstOrDefault();
        }

        public static async Task<CardModel?> SelectDeckForRemoval(Player player, CardSelectorPrefs prefs, Func<CardModel, bool>? filter = null)
        {
            List<CardModel> deck = PileType.Deck.GetPile(player).Cards.ToList();
            List<CardModel> cards = deck
                .Where(c => c.IsRemovable && (filter == null || filter(c)))
                .OrderBy(c => c.Type != CardType.Curse ? deck.IndexOf(c) : -999999999)
                .ToList();

            return (await SelectDeckCards(player, cards, prefs, DeckSelectionScreenType.Generic)).FirstOrDefault();
        }

        public static async Task<CardModel?> SelectDeckGeneric(Player player, CardSelectorPrefs prefs, Func<CardModel, bool>? filter = null, Func<CardModel, int>? sortingOrder = null)
        {
            List<CardModel> source = PileType.Deck.GetPile(player).Cards.ToList();
            List<CardModel> cards = (filter == null ? source : source.Where(filter)).ToList();
            if (sortingOrder != null)
            {
                cards = cards.OrderBy(sortingOrder).ToList();
            }

            return (await SelectDeckCards(player, cards, prefs, DeckSelectionScreenType.Generic)).FirstOrDefault();
        }

        private static async Task<IEnumerable<CardModel>> SelectDeckCards(Player player, IReadOnlyList<CardModel> cards, CardSelectorPrefs prefs, DeckSelectionScreenType screenType)
        {
            if (player.Creature.IsDead)
            {
                return Array.Empty<CardModel>();
            }

            if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
            {
                return cards;
            }

            if (CardSelectCmd.Selector != null)
            {
                return await CardSelectCmd.Selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect);
            }

            return screenType switch
            {
                DeckSelectionScreenType.Upgrade => await NDeckUpgradeSelectScreen.ShowScreen(cards, prefs, player.RunState).CardsSelected(),
                _ => await ShowDeckCardScreen(cards, prefs),
            };
        }

        private static async Task<IEnumerable<CardModel>> ShowDeckCardScreen(IReadOnlyList<CardModel> cards, CardSelectorPrefs prefs)
        {
            NDeckCardSelectScreen screen = NDeckCardSelectScreen.Create(cards, prefs);
            NOverlayStack.Instance.Push(screen);
            return await screen.CardsSelected();
        }

        private enum DeckSelectionScreenType
        {
            Generic,
            Upgrade,
        }
    }
}