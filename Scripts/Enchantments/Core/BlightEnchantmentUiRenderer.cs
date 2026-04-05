using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Enchantments;

namespace BlightMod.Enchantments
{
    public static class BlightEnchantmentUiRenderer
    {
        private const string ExtraTabPrefix = "BlightEnchantTab_";
        private const float VerticalSpacing = 38f;
        private static readonly StringName HueParam = new StringName("h");
        private static readonly StringName SaturationParam = new StringName("s");
        private static readonly StringName ValueParam = new StringName("v");

        public static void ClearInjectedTabs(NCard cardView)
        {
            if (cardView == null)
            {
                return;
            }

            Control baseTab = cardView.EnchantmentTab;
            if (baseTab == null)
            {
                return;
            }

            if (baseTab.GetParent() is not Control parent)
            {
                return;
            }

            CleanupExtraTabs(parent);
        }

        public static void RenderTabs(NCard cardView)
        {
            if (cardView?.Model == null)
            {
                return;
            }

            Control baseTab = cardView.EnchantmentTab;
            if (baseTab.GetParent() is not Control parent)
            {
                return;
            }

            CleanupExtraTabs(parent);

            Vector2 basePosition = baseTab.Position;

            if (cardView.Model.Enchantment == null)
            {
                ResetTab(baseTab);
                return;
            }

            if (cardView.Model.Enchantment is BlightCompositeEnchantment composite && composite.Entries.Count > 0)
            {
                var visibleEntries = composite.Entries
                    .Select((entry, index) => new { entry, index })
                    .Where(item => IsEntryVisuallyApplicable(cardView.Model, item.entry))
                    .ToArray();

                if (visibleEntries.Length == 0)
                {
                    ResetTab(baseTab);
                    return;
                }

                for (int i = 0; i < visibleEntries.Length; i++)
                {
                    BlightEnchantmentEntry entry = visibleEntries[i].entry;
                    Control tab = i == 0 ? baseTab : CreateTabClone(baseTab, parent, i);
                    ApplyTabVisual(tab, entry, composite.IsEntryDisabled(visibleEntries[i].index));
                    tab.Position = basePosition + Vector2.Down * (VerticalSpacing * i);
                }

                return;
            }

            if (cardView.Model.Enchantment is IBlightEnchantment metadata)
            {
                if (!IsModelVisuallyApplicable(cardView.Model, cardView.Model.Enchantment))
                {
                    ResetTab(baseTab);
                    return;
                }

                BlightEnchantmentEntry singleEntry = new BlightEnchantmentEntry
                {
                    EnchantmentId = cardView.Model.Enchantment.Id,
                    Amount = cardView.Model.Enchantment.Amount,
                    Rarity = metadata.Rarity,
                    IsNegative = metadata.Rarity == BlightEnchantmentRarity.Negative,
                };

                ApplyTabVisual(baseTab, singleEntry, cardView.Model.Enchantment.Status == EnchantmentStatus.Disabled);
                baseTab.Position = basePosition;
                return;
            }

            // Keep vanilla visuals for non-blight enchantments.
            baseTab.Visible = true;
            baseTab.SelfModulate = Colors.White;
        }

        private static Control CreateTabClone(Control baseTab, Control parent, int index)
        {
            Control clone = (Control)baseTab.Duplicate();
            clone.Name = $"{ExtraTabPrefix}{index}";
            clone.Visible = true;
            parent.AddChild(clone);
            return clone;
        }

        private static void ApplyTabVisual(Control tab, BlightEnchantmentEntry entry, bool isDisabled)
        {
            tab.Visible = true;
            tab.SelfModulate = GetRarityColor(entry.Rarity, entry.IsNegative);
            SetMousePassthrough(tab);

            TextureRect? icon = tab.GetNodeOrNull<TextureRect>("Icon");
            MegaLabel? label = tab.GetNodeOrNull<MegaLabel>("Label");

            var model = SaveUtil.EnchantmentOrDeprecated(entry.EnchantmentId).ToMutable();
            model.Amount = entry.Amount;

            if (icon != null)
            {
                icon.Texture = ResolveIcon(model, entry);
            }

            if (label != null)
            {
                label.SetTextAutoSize(model.DisplayAmount.ToString());
                label.Visible = model.ShowAmount;
            }

            ApplyTabStatusVisual(tab, isDisabled);
        }

        private static void ApplyTabStatusVisual(Control tab, bool isDisabled)
        {
            TextureRect? icon = tab.GetNodeOrNull<TextureRect>("Icon");
            MegaLabel? label = tab.GetNodeOrNull<MegaLabel>("Label");

            if (isDisabled)
            {
                tab.Modulate = new Color(1f, 1f, 1f, 0.9f);
                if (tab.Material is ShaderMaterial disabledMaterial)
                {
                    disabledMaterial.SetShaderParameter(HueParam, 0.25);
                    disabledMaterial.SetShaderParameter(SaturationParam, 0.1);
                    disabledMaterial.SetShaderParameter(ValueParam, 0.6);
                }

                if (icon != null)
                {
                    icon.UseParentMaterial = true;
                }

                if (label != null)
                {
                    label.SelfModulate = StsColors.gray;
                }

                return;
            }

            tab.Modulate = Colors.White;
            if (tab.Material is ShaderMaterial enabledMaterial)
            {
                enabledMaterial.SetShaderParameter(HueParam, 0.25);
                enabledMaterial.SetShaderParameter(SaturationParam, 0.4);
                enabledMaterial.SetShaderParameter(ValueParam, 0.6);
            }

            if (icon != null)
            {
                icon.UseParentMaterial = false;
            }

            if (label != null)
            {
                label.SelfModulate = Colors.White;
            }
        }

        private static void SetMousePassthrough(Control control)
        {
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
            foreach (Node child in control.GetChildren())
            {
                if (child is Control childControl)
                {
                    SetMousePassthrough(childControl);
                }
            }
        }

        private static void CleanupExtraTabs(Control parent)
        {
            foreach (Node child in parent.GetChildren())
            {
                if (child.Name.ToString().StartsWith(ExtraTabPrefix, StringComparison.Ordinal))
                {
                    if (child.GetParent() == parent)
                    {
                        parent.RemoveChild(child);
                    }

                    child.QueueFree();
                }
            }
        }

        private static void ResetTab(Control tab)
        {
            tab.Visible = false;
            tab.SelfModulate = Colors.White;

            MegaLabel? label = tab.GetNodeOrNull<MegaLabel>("Label");
            if (label != null)
            {
                label.Visible = false;
            }
        }

        private static Color GetRarityColor(BlightEnchantmentRarity rarity, bool isNegative)
        {
            if (isNegative || rarity == BlightEnchantmentRarity.Negative)
            {
                return StsColors.red;
            }

            return rarity switch
            {
                BlightEnchantmentRarity.Common => Colors.White,
                BlightEnchantmentRarity.Uncommon => StsColors.blue,
                BlightEnchantmentRarity.Rare => StsColors.gold,
                BlightEnchantmentRarity.UltraRare => StsColors.gold,
                _ => Colors.White,
            };
        }

        private static Texture2D ResolveIcon(MegaCrit.Sts2.Core.Models.EnchantmentModel model, BlightEnchantmentEntry entry)
        {
            if (entry.EnchantmentId.Equals(ModelDb.Enchantment<CustomEnchantments.BlightSwordDance1Enchantment>().Id)
                || entry.EnchantmentId.Equals(ModelDb.Enchantment<CustomEnchantments.BlightSwordDance2Enchantment>().Id)
                || entry.EnchantmentId.Equals(ModelDb.Enchantment<CustomEnchantments.BlightSwordDance3Enchantment>().Id))
            {
                return ModelDb.Power<DoubleDamagePower>().Icon;
            }

            // Demo: Sharp2/Sharp4 reuse vanilla Sharp icon so they don't show missing placeholders.
            if (entry.EnchantmentId.Equals(ModelDb.Enchantment<CustomEnchantments.BlightSharp2Enchantment>().Id)
                || entry.EnchantmentId.Equals(ModelDb.Enchantment<CustomEnchantments.BlightSharp4Enchantment>().Id))
            {
                return ModelDb.Enchantment<Sharp>().Icon;
            }

            return model.Icon;
        }

        private static bool IsEntryVisuallyApplicable(CardModel card, BlightEnchantmentEntry entry)
        {
            if (entry?.EnchantmentId == null)
            {
                return false;
            }

            EnchantmentModel model;
            try
            {
                model = SaveUtil.EnchantmentOrDeprecated(entry.EnchantmentId).ToMutable();
            }
            catch
            {
                return false;
            }

            model.Amount = entry.Amount;
            return IsModelVisuallyApplicable(card, model);
        }

        private static bool IsModelVisuallyApplicable(CardModel card, EnchantmentModel model)
        {
            if (card == null || model == null)
            {
                return false;
            }

            if (model is CustomEnchantments.BlightGoopyEnchantment)
            {
                return (card.Type == CardType.Skill || card.Type == CardType.Attack)
                    && card.GainsBlock
                    && (card.Pile == null || card.Pile.Type != PileType.Deck || !card.Keywords.Contains(CardKeyword.Unplayable));
            }

            if (model is CustomEnchantments.BlightInstinctEnchantment)
            {
                return card.Type == CardType.Attack;
            }

            if (model is CustomEnchantments.BlightImbuedEnchantment)
            {
                return model.CanEnchantCardType(card.Type)
                    && (card.Pile == null || card.Pile.Type != PileType.Deck || !card.Keywords.Contains(CardKeyword.Unplayable));
            }

            if (model is CustomEnchantments.BlightMomentum1Enchantment || model is CustomEnchantments.BlightMomentum2Enchantment)
            {
                return model.CanEnchantCardType(card.Type)
                    && (card.Pile == null || card.Pile.Type != PileType.Deck || !card.Keywords.Contains(CardKeyword.Unplayable));
            }

            if (model is IBlightEnchantment)
            {
                return model.CanEnchantCardType(card.Type);
            }

            return model.CanEnchantCardType(card.Type);
        }
    }
}
