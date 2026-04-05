using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlightMod.AI.Buffs;
using BlightMod.Core;
using BlightMod.Localization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BlightMod.AI.Bestiary;

internal enum KnowledgeDemonPreludeChoiceType
{
    FullBattle,
    LateBloomer,
    Wealth,
    Weapon,
    Equipment,
}

internal static class KnowledgeDemonPreludeState
{
    private static CombatState? _lastProcessedCombat;

    public static bool TryBegin(CombatState combatState)
    {
        if (ReferenceEquals(_lastProcessedCombat, combatState))
        {
            return false;
        }

        _lastProcessedCombat = combatState;
        return true;
    }
}

internal static class KnowledgeDemonLateBloomerTracker
{
    private static readonly HashSet<ulong> PendingPlayers = new();

    private static readonly HashSet<ulong> DownedPlayers = new();

    private static readonly Dictionary<ulong, Action<Creature>> DeathHandlers = new();

    private static CombatState? _activeCombat;

    public static void BeginCombat(CombatState combatState)
    {
        Reset();
        _activeCombat = combatState;
    }

    public static void Register(Player player)
    {
        if (_activeCombat == null || PendingPlayers.Contains(player.NetId))
        {
            return;
        }

        PendingPlayers.Add(player.NetId);

        Action<Creature> handler = _ => DownedPlayers.Add(player.NetId);
        DeathHandlers[player.NetId] = handler;
        player.Creature.Died += handler;
    }

    public static bool HasPendingForCombat(CombatState? combatState)
    {
        return combatState != null
            && ReferenceEquals(_activeCombat, combatState)
            && PendingPlayers.Count > 0;
    }

    public static async Task ApplyRewards(IRunState runState)
    {
        foreach (ulong playerId in PendingPlayers)
        {
            Player player = runState.GetPlayer(playerId);
            if (player == null)
            {
                continue;
            }

            if (DownedPlayers.Contains(playerId))
            {
                continue;
            }

            await CreatureCmd.GainMaxHp(player.Creature, 20m);
        }

        Reset();
    }

    public static void Reset()
    {
        if (_activeCombat?.RunState != null)
        {
            foreach (KeyValuePair<ulong, Action<Creature>> pair in DeathHandlers)
            {
                Player player = _activeCombat.RunState.GetPlayer(pair.Key);
                if (player != null)
                {
                    player.Creature.Died -= pair.Value;
                }
            }
        }

        DeathHandlers.Clear();
        PendingPlayers.Clear();
        DownedPlayers.Clear();
        _activeCombat = null;
    }
}

internal static class KnowledgeDemonPreludeDrawPileTracker
{
    private static readonly Dictionary<ulong, List<CardModel>> PendingCardsByPlayer = new();

    private static CombatState? _activeCombat;

    public static void BeginCombat(CombatState combatState)
    {
        Reset();
        _activeCombat = combatState;
    }

    public static void Queue(Player player, CardModel sourceCard)
    {
        if (_activeCombat == null || sourceCard == null)
        {
            return;
        }

        if (!PendingCardsByPlayer.TryGetValue(player.NetId, out List<CardModel>? pending))
        {
            pending = new List<CardModel>();
            PendingCardsByPlayer[player.NetId] = pending;
        }

        pending.Add(sourceCard);
    }

    public static bool HasPendingForCombat(CombatState? combatState)
    {
        return combatState != null
            && ReferenceEquals(_activeCombat, combatState)
            && PendingCardsByPlayer.Count > 0;
    }

    public static async Task ApplyPending(CombatState combatState)
    {
        foreach ((ulong playerId, List<CardModel> cards) in PendingCardsByPlayer)
        {
            Player player = combatState.RunState.GetPlayer(playerId);
            if (player == null)
            {
                continue;
            }

            foreach (CardModel source in cards)
            {
                if (!player.RunState.ContainsCard(source))
                {
                    continue;
                }

                CardModel drawCopy = combatState.CloneCard(source);
                await CardPileCmd.Add(drawCopy, PileType.Draw, CardPilePosition.Random);
            }
        }

        Reset();
    }

    public static void Reset()
    {
        PendingCardsByPlayer.Clear();
        _activeCombat = null;
    }
}

internal static class KnowledgeDemonPreludeChoiceRegistry
{
    private sealed class CardModelReferenceComparer : IEqualityComparer<CardModel>
    {
        public static readonly CardModelReferenceComparer Instance = new();

        public bool Equals(CardModel? x, CardModel? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(CardModel obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }

    private static readonly Dictionary<CardModel, KnowledgeDemonPreludeChoiceType> ChoiceByCard =
        new(CardModelReferenceComparer.Instance);

    public static void Register(CardModel card, KnowledgeDemonPreludeChoiceType choice)
    {
        ChoiceByCard[card] = choice;
    }

    public static bool TryGet(CardModel card, out KnowledgeDemonPreludeChoiceType choice)
    {
        return ChoiceByCard.TryGetValue(card, out choice);
    }

    public static void Unregister(CardModel card)
    {
        ChoiceByCard.Remove(card);
    }

    public static string GetTitle(KnowledgeDemonPreludeChoiceType choice)
    {
        return BlightLocalization.GetText(choice switch
        {
            KnowledgeDemonPreludeChoiceType.FullBattle => "BLIGHT_KD_PRELUDE.option.full_battle.title",
            KnowledgeDemonPreludeChoiceType.LateBloomer => "BLIGHT_KD_PRELUDE.option.late_bloomer.title",
            KnowledgeDemonPreludeChoiceType.Wealth => "BLIGHT_KD_PRELUDE.option.wealth.title",
            KnowledgeDemonPreludeChoiceType.Weapon => "BLIGHT_KD_PRELUDE.option.weapon.title",
            _ => "BLIGHT_KD_PRELUDE.option.equipment.title",
        });
    }

    public static string GetDescription(KnowledgeDemonPreludeChoiceType choice)
    {
        return BlightLocalization.GetText(choice switch
        {
            KnowledgeDemonPreludeChoiceType.FullBattle => "BLIGHT_KD_PRELUDE.option.full_battle.description",
            KnowledgeDemonPreludeChoiceType.LateBloomer => "BLIGHT_KD_PRELUDE.option.late_bloomer.description",
            KnowledgeDemonPreludeChoiceType.Wealth => "BLIGHT_KD_PRELUDE.option.wealth.description",
            KnowledgeDemonPreludeChoiceType.Weapon => "BLIGHT_KD_PRELUDE.option.weapon.description",
            _ => "BLIGHT_KD_PRELUDE.option.equipment.description",
        });
    }
}

[HarmonyPatch(typeof(CardModel), "get_Title")]
public static class KnowledgeDemonPreludeChoiceTitlePatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, ref string __result)
    {
        if (!KnowledgeDemonPreludeChoiceRegistry.TryGet(__instance, out KnowledgeDemonPreludeChoiceType choice))
        {
            return true;
        }

        __result = KnowledgeDemonPreludeChoiceRegistry.GetTitle(choice);
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), new[] { typeof(PileType), typeof(Creature) })]
public static class KnowledgeDemonPreludeChoiceDescriptionPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, PileType pileType, Creature? target, ref string __result)
    {
        if (!KnowledgeDemonPreludeChoiceRegistry.TryGet(__instance, out KnowledgeDemonPreludeChoiceType choice))
        {
            return true;
        }

        __result = KnowledgeDemonPreludeChoiceRegistry.GetDescription(choice);
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), "get_Rarity")]
public static class KnowledgeDemonPreludeChoiceRarityPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, ref CardRarity __result)
    {
        if (!KnowledgeDemonPreludeChoiceRegistry.TryGet(__instance, out _))
        {
            return true;
        }

        __result = CardRarity.Rare;
        return false;
    }
}

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.AfterAddedToRoom))]
public static class KnowledgeDemonPreludeChoicePatch
{
    [HarmonyPostfix]
    public static void Postfix(MonsterModel __instance, ref Task __result)
    {
        if (__instance is not KnowledgeDemon knowledgeDemon)
        {
            return;
        }

        __result = RunPreludeAsync(__result, knowledgeDemon);
    }

    private static async Task RunPreludeAsync(Task original, KnowledgeDemon knowledgeDemon)
    {
        await original;

        if (!BlightModeManager.IsBlightModeActive || !BlightAIContext.ShouldOverrideMonsterAi(knowledgeDemon))
        {
            return;
        }

        Creature creature = knowledgeDemon.Creature;
        CombatState combatState = creature.CombatState;
        if (creature == null || combatState == null)
        {
            return;
        }

        if (!KnowledgeDemonPreludeState.TryBegin(combatState))
        {
            return;
        }

        KnowledgeDemonLateBloomerTracker.BeginCombat(combatState);
        KnowledgeDemonPreludeDrawPileTracker.BeginCombat(combatState);

        List<(Player player, Task<KnowledgeDemonPreludeChoiceType> choiceTask)> pendingChoices = new();
        foreach (Player player in combatState.Players)
        {
            if (player.Creature == null)
            {
                continue;
            }

            pendingChoices.Add((player, SelectPreludeChoice(player)));
        }

        await Task.WhenAll(pendingChoices.Select(static p => p.choiceTask));

        foreach ((Player player, Task<KnowledgeDemonPreludeChoiceType> choiceTask) in pendingChoices)
        {
            await ExecutePreludeChoice(choiceTask.Result, player, creature);
        }
    }

    private static async Task<KnowledgeDemonPreludeChoiceType> SelectPreludeChoice(Player player)
    {
        List<CardModel> choiceCards = BuildPreludeChoiceCards(player);
        CardSelectorPrefs prefs = new CardSelectorPrefs(new LocString("relics", "BLIGHT_KD_PRELUDE.prompt"), 1);

        try
        {
            CardModel? selected = (await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), choiceCards, player, prefs)).FirstOrDefault();
            if (selected != null && KnowledgeDemonPreludeChoiceRegistry.TryGet(selected, out KnowledgeDemonPreludeChoiceType choice))
            {
                return choice;
            }

            return KnowledgeDemonPreludeChoiceType.FullBattle;
        }
        finally
        {
            foreach (CardModel card in choiceCards)
            {
                KnowledgeDemonPreludeChoiceRegistry.Unregister(card);
            }
        }
    }

    private static List<CardModel> BuildPreludeChoiceCards(Player player)
    {
        CardModel template = ResolveChoiceTemplate(player);
        CardModel canonicalTemplate = template.CanonicalInstance;

        var choices = new[]
        {
            KnowledgeDemonPreludeChoiceType.FullBattle,
            KnowledgeDemonPreludeChoiceType.LateBloomer,
            KnowledgeDemonPreludeChoiceType.Wealth,
            KnowledgeDemonPreludeChoiceType.Weapon,
            KnowledgeDemonPreludeChoiceType.Equipment,
        };

        List<CardModel> cards = new(choices.Length);
        foreach (KnowledgeDemonPreludeChoiceType choice in choices)
        {
            CardModel choiceCard = player.RunState.CreateCard(canonicalTemplate, player);
            choiceCard.ClearEnchantmentInternal();
            KnowledgeDemonPreludeChoiceRegistry.Register(choiceCard, choice);
            cards.Add(choiceCard);
        }

        return cards;
    }

    private static CardModel ResolveChoiceTemplate(Player player)
    {
        CardModel? fromDeck = player.Deck?.Cards?.FirstOrDefault();
        if (fromDeck != null)
        {
            return fromDeck;
        }

        CardModel? fromPool = player.Character?.CardPool?.AllCards?.FirstOrDefault();
        if (fromPool != null)
        {
            return fromPool;
        }

        return ModelDb.Card<StrikeIronclad>();
    }

    private static async Task ExecutePreludeChoice(KnowledgeDemonPreludeChoiceType choice, Player player, Creature knowledgeDemonCreature)
    {
        switch (choice)
        {
            case KnowledgeDemonPreludeChoiceType.FullBattle:
                await PowerCmd.Apply<StrengthPower>(player.Creature, 3m, player.Creature, null);
                await PowerCmd.Apply<StrengthPower>(knowledgeDemonCreature, 3m, knowledgeDemonCreature, null);
                await PowerCmd.Apply<PlatingPower>(knowledgeDemonCreature, 5m, knowledgeDemonCreature, null);
                return;

            case KnowledgeDemonPreludeChoiceType.LateBloomer:
                await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), player.Creature, 20m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
                KnowledgeDemonLateBloomerTracker.Register(player);
                return;

            case KnowledgeDemonPreludeChoiceType.Wealth:
                await PlayerCmd.GainGold(300m, player);
                await PowerCmd.Apply<CoinScatterPower>(player.Creature, 25m, player.Creature, null);
                return;

            case KnowledgeDemonPreludeChoiceType.Weapon:
                await GrantWeaponPackage(player);
                return;

            case KnowledgeDemonPreludeChoiceType.Equipment:
                RelicModel waxRelic = RelicFactory.PullNextRelicFromFront(player).ToMutable();
                waxRelic.IsWax = true;
                await RelicCmd.Obtain(waxRelic, player);
                await PowerCmd.Apply<StrengthPower>(player.Creature, -1m, player.Creature, null);
                return;
        }
    }

    private static async Task GrantWeaponPackage(Player player)
    {
        for (int i = 0; i < 2; i++)
        {
            IReadOnlyList<CardModel> options = BuildRareOptions(player, 3);
            if (options.Count == 0)
            {
                break;
            }

            CardModel? selected = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), options, player, canSkip: false);
            if (selected != null)
            {
                await CardPileCmd.Add(selected, PileType.Deck);
                KnowledgeDemonPreludeDrawPileTracker.Queue(player, selected);
            }
        }

        List<CardModel> cursePool = ModelDb.CardPool<CurseCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .ToList();

        if (cursePool.Count <= 0)
        {
            return;
        }

        CardModel randomCurse = player.PlayerRng.Rewards.NextItem(cursePool);
        CardModel deckCurse = player.RunState.CreateCard(randomCurse, player);
        await CardPileCmd.Add(deckCurse, PileType.Deck);
        KnowledgeDemonPreludeDrawPileTracker.Queue(player, deckCurse);
    }

    private static IReadOnlyList<CardModel> BuildRareOptions(Player player, int optionCount)
    {
        IEnumerable<CardModel> classRares = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(static c => c.Rarity == CardRarity.Rare && c.CanBeGeneratedInCombat);

        IEnumerable<CardModel> colorlessRares = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(static c => c.Rarity == CardRarity.Rare && c.CanBeGeneratedInCombat);

        List<CardModel> candidates = classRares
            .Concat(colorlessRares)
            .GroupBy(c => c.Id)
            .Select(static g => g.First())
            .ToList();

        if (candidates.Count <= 0)
        {
            return Array.Empty<CardModel>();
        }

        int pickCount = Math.Min(optionCount, candidates.Count);
        List<CardModel> result = new(pickCount);
        for (int i = 0; i < pickCount; i++)
        {
            CardModel picked = player.PlayerRng.Rewards.NextItem(candidates);
            candidates.Remove(picked);
            result.Add(player.RunState.CreateCard(picked, player));
        }

        return result;
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatVictory))]
public static class KnowledgeDemonLateBloomerAfterVictoryPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Task __result, IRunState runState, CombatState? combatState, CombatRoom room)
    {
        __result = HandleAfterVictoryAsync(__result, runState, combatState);
    }

    private static async Task HandleAfterVictoryAsync(Task original, IRunState runState, CombatState? combatState)
    {
        await original;

        if (!KnowledgeDemonLateBloomerTracker.HasPendingForCombat(combatState))
        {
            return;
        }

        await KnowledgeDemonLateBloomerTracker.ApplyRewards(runState);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
public static class KnowledgeDemonPreludeBeforeCombatStartPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Task __result, IRunState runState, CombatState? combatState)
    {
        __result = HandleBeforeCombatStartAsync(__result, combatState);
    }

    private static async Task HandleBeforeCombatStartAsync(Task original, CombatState? combatState)
    {
        await original;

        if (!KnowledgeDemonPreludeDrawPileTracker.HasPendingForCombat(combatState))
        {
            return;
        }

        await KnowledgeDemonPreludeDrawPileTracker.ApplyPending(combatState!);
    }
}
