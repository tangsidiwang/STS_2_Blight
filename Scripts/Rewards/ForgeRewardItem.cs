using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlightMod.Enchantments;
using BlightMod.Rewards.ForgeOptions;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;

namespace BlightMod.Rewards
{
    public sealed class ForgeRewardItem : Reward
    {
        private static string RewardIcon => ImageHelper.GetImagePath("ui/reward_screen/reward_icon_special_card.png");

        private bool _claimed;
        private List<IForgeOption>? _resolvedOptions;

        public BlightEnchantmentRarity BaseRewardLevel { get; }

        protected override RewardType RewardType => RewardType.Relic;

        public override int RewardsSetIndex => 4;

        protected override string IconPath => RewardIcon;

        public override Vector2 IconPosition => new Vector2(0f, -3f);

        public override LocString Description
        {
            get
            {
                return new LocString("gameplay_ui", "CHOOSE_CARD_HEADER");
            }
        }

        public override bool IsPopulated => true;

        public ForgeRewardItem(BlightEnchantmentRarity rarityLevel, MegaCrit.Sts2.Core.Entities.Players.Player player)
            : base(player)
        {
            BaseRewardLevel = rarityLevel;
        }

        public override Task Populate()
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> OnSelect()
        {
            if (_claimed)
            {
                Log.Info("[Blight][Forge] OnSelect called after already claimed.");
                return false;
            }

            Rng rng = _rngOverride ?? Player.RunState.Rng.Niche;
            List<IForgeOption> optionDefs = ResolveOptions(rng);
            Log.Info($"[Blight][Forge] OnSelect begin: baseTier={BaseRewardLevel}, optionCount={optionDefs.Count}");
            List<CardModel> options = BuildChoiceCards(optionDefs);
            CardModel? selected = null;
            try
            {
                selected = await ForgeLocalSelection.ChooseOneFromCards(options, canSkip: false);
                if (selected == null || !ForgeChoiceRegistry.TryGet(selected, out IForgeOption? pickedOption))
                {
                    Log.Info("[Blight][Forge] No choice selected or registry lookup failed.");
                    return false;
                }

                bool success;
                try
                {
                    Log.Info($"[Blight][Forge] Executing option: {pickedOption.Title} | {pickedOption.Description}");
                    success = await pickedOption.ExecuteAsync(Player, rng);
                }
                catch (Exception e)
                {
                    Log.Error($"[Blight] Forge reward option execution failed: {e}");
                    return false;
                }

                if (success)
                {
                    _claimed = true;
                    Log.Info($"[Blight][Forge] Forge reward choice resolved successfully: {pickedOption.Title} ({BaseRewardLevel}).");
                }
                else
                {
                    Log.Info($"[Blight][Forge] Forge reward choice execution returned false: {pickedOption.Title} ({BaseRewardLevel}).");
                }

                return success;
            }
            finally
            {
                Log.Info($"[Blight][Forge] Cleaning temporary choice cards: {options.Count}");
                // Choice cards are temporary models; always clean them from run state.
                foreach (CardModel card in options)
                {
                    ForgeChoiceRegistry.Unregister(card);
                    card.RemoveFromState();
                }

                // Safety net for stale option mappings if any temporary card was replaced by the engine.
                ForgeChoiceRegistry.Clear();
            }
        }

        private List<IForgeOption> ResolveOptions(Rng rng)
        {
            if (_resolvedOptions != null)
            {
                return _resolvedOptions;
            }

            _resolvedOptions = ForgeOptionPool.RollOptions(BaseRewardLevel, Player, rng, 3);
            for (int i = 0; i < _resolvedOptions.Count; i++)
            {
                IForgeOption option = _resolvedOptions[i];
                Log.Info($"[Blight][Forge] Option[{i}] title={option.Title}, desc={option.Description}, rarity={option.Rarity}");
            }

            return _resolvedOptions;
        }

        private List<CardModel> BuildChoiceCards(IReadOnlyList<IForgeOption> optionDefs)
        {
            var runState = Player.RunState;
            if (runState == null)
            {
                throw new InvalidOperationException("Forge reward requires an active RunState.");
            }

            List<CardModel> deckTemplates = Player.Deck.Cards.Where(c => !c.HasBeenRemovedFromState).ToList();
            if (deckTemplates.Count == 0)
            {
                throw new InvalidOperationException("Forge reward requires at least one deck card to template choice cards.");
            }

            List<CardModel> result = new List<CardModel>(optionDefs.Count);
            for (int i = 0; i < optionDefs.Count; i++)
            {
                CardModel template = deckTemplates[i % deckTemplates.Count];
                CardModel canonical = ModelDb.GetById<CardModel>(template.Id);
                CardModel choiceCard = runState.CreateCard(canonical, Player);
                choiceCard.ClearEnchantmentInternal();
                ForgeChoiceRegistry.Register(choiceCard, optionDefs[i]);
                result.Add(choiceCard);
            }

            return result;
        }

        public override void MarkContentAsSeen()
        {
        }
    }
}
