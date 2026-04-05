using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace BlightMod.Localization
{
    internal static class BlightLocalization
    {
        private const string MissingLocalizationFallback = "error本地化错误";

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["BLIGHT_BUTTON.title"] = "Blight Mode",
            ["BLIGHT_BUTTON.description"] = "[color=red]Challenge escalating difficulty[/color]",
            ["BLIGHT_MP_BUTTON.title"] = "Blight Co-op",
            ["BLIGHT_MP_BUTTON.description"] = "[color=red]Host a blight multiplayer run[/color]",
            ["BLIGHT_ASCENSION.0.title"] = "Blight 0",
            ["BLIGHT_ASCENSION.0.description"] = "Baseline uses A10 difficulty.",
            ["BLIGHT_ASCENSION.1.title"] = "Blight 1",
            ["BLIGHT_ASCENSION.1.description"] = "Mutant enemies can appear in normal and elite fights.",
            ["BLIGHT_ASCENSION.2.title"] = "Blight 2",
            ["BLIGHT_ASCENSION.2.description"] = "Elites become deadlier and gain extra random upgrades.",
            ["BLIGHT_ASCENSION.3.title"] = "Blight 3",
            ["BLIGHT_ASCENSION.3.description"] = "Start each run with a random negative effect.",
            ["BLIGHT_ASCENSION.4.title"] = "Blight 4",
            ["BLIGHT_ASCENSION.4.description"] = "Double-wave enemy fights can appear.",
            ["BLIGHT_ASCENSION.5.title"] = "Blight 5",
            ["BLIGHT_ASCENSION.5.description"] = "Bosses gain additional blight abilities.",
            ["BLIGHT_FORGE.title"] = "Blight Infusion",
            ["BLIGHT_FORGE.target.playable"] = "playable card",
            ["BLIGHT_FORGE.target.attack"] = "attack card",
            ["BLIGHT_FORGE.target.skill"] = "skill card",
            ["BLIGHT_FORGE.target.attack_or_skill"] = "attack or skill card",
            ["BLIGHT_FORGE.rarity.common"] = "Common",
            ["BLIGHT_FORGE.rarity.uncommon"] = "Uncommon",
            ["BLIGHT_FORGE.rarity.rare"] = "Rare",
            ["BLIGHT_FORGE.rarity.ultra_rare"] = "Ultra Rare",
            ["BLIGHT_FORGE.rarity.negative"] = "Negative",
            ["BLIGHT_FORGE.reward_label"] = "Choose Forge Reward ({Rarity})",
            ["BLIGHT_FORGE.description"] = "Enchant a {Target} with {EnchantName}.",
            ["BLIGHT_FORGE.utility.title.relic"] = "Hidden Treasure",
            ["BLIGHT_FORGE.utility.title.armor"] = "Armor",
            ["BLIGHT_FORGE.utility.title.smith"] = "Fine Tuning",
            ["BLIGHT_FORGE.utility.title.remove"] = "Sever Attachment",
            ["BLIGHT_FORGE.utility.title.max_hp_percent"] = "Tempering",
            ["BLIGHT_FORGE.utility.title.fallback"] = "Forge Ember",
            ["BLIGHT_FORGE.utility.desc.relic"] = "Obtain {Count} random relic(s).",
            ["BLIGHT_FORGE.utility.desc.armor"] = "Start each combat with {Amount} Plating.",
            ["BLIGHT_FORGE.utility.desc.smith"] = "Choose a card to upgrade.",
            ["BLIGHT_FORGE.utility.desc.remove"] = "Remove a card from your deck.",
            ["BLIGHT_FORGE.utility.desc.max_hp_percent"] = "Increase max HP by {Percent}%.",
            ["BLIGHT_FORGE.utility.desc.fallback"] = "Gain a random benefit.",
            ["BLIGHT_FORGE.fallback.sharp2"] = "Sharp 2",
            ["BLIGHT_FORGE.fallback.sharp4"] = "Sharp 4",
            ["BLIGHT_FORGE.fallback.damage"] = "Damage Boost",
            ["BLIGHT_FORGE.fallback.block"] = "Block Boost",
            ["BLIGHT_FORGE.fallback.double"] = "Double Cast",
            ["BLIGHT_FORGE.fallback.fragile_edge"] = "Brittle Edge",
            ["BLIGHT_RUN_TAG.title"] = "Blight Mode",
            ["BLIGHT_RUN_TAG.description"] = "This is a Blight run. The following modifiers stay active for the entire run.",
            ["BLIGHT_A3_WEAKNESS.title"] = "Weakening",
            ["BLIGHT_A3_WEAKNESS.description"] = "At the start of each combat, all players gain 1 Weak.",
            ["BLIGHT_A3_ENEMY_STRENGTH.title"] = "Empower",
            ["BLIGHT_A3_ENEMY_STRENGTH.description"] = "At the start of each combat, all enemies gain 1 Strength.",
            ["BLIGHT_A4_ENEMY_MAX_HP.title"] = "Gigantism",
            ["BLIGHT_A4_ENEMY_MAX_HP.description"] = "At the start of each combat, all enemies gain +5% max HP.",
            ["BLIGHT_COMPOSITE_ENCHANTMENT.title"] = "Composite Enchantment",
            ["BLIGHT_COMPOSITE_ENCHANTMENT.description"] = "This card has multiple enchantment effects.",
            ["BLIGHT_SHARP2_ENCHANTMENT.title"] = "Sharp 1",
            ["BLIGHT_SHARP2_ENCHANTMENT.description"] = "This card deals 1 additional damage.",
            ["BLIGHT_SHARP4_ENCHANTMENT.title"] = "Sharp 2",
            ["BLIGHT_SHARP4_ENCHANTMENT.description"] = "This card deals 2 additional damage.",
            ["BLIGHT_SHARP6_ENCHANTMENT.title"] = "Sharp 4",
            ["BLIGHT_SHARP6_ENCHANTMENT.description"] = "This card deals 4 additional damage.",
            ["BLIGHT_DAMAGE_ENCHANTMENT.title"] = "Sharp",
            ["BLIGHT_DAMAGE_ENCHANTMENT.description"] = "This card deals {Amount} additional damage.",
            ["BLIGHT_ADROIT1_ENCHANTMENT.title"] = "Adroit 1",
            ["BLIGHT_ADROIT1_ENCHANTMENT.description"] = "Gain 1 Block.",
            ["BLIGHT_ADROIT2_ENCHANTMENT.title"] = "Adroit 2",
            ["BLIGHT_ADROIT2_ENCHANTMENT.description"] = "Gain 2 Block.",
            ["BLIGHT_ADROIT4_ENCHANTMENT.title"] = "Adroit 4",
            ["BLIGHT_ADROIT4_ENCHANTMENT.description"] = "Gain 4 Block.",
            ["BLIGHT_ENVENOM2_ENCHANTMENT.title"] = "Envenom 2",
            ["BLIGHT_ENVENOM2_ENCHANTMENT.description"] = "After playing, apply 2 Poison to the target.",
            ["BLIGHT_ENVENOM3_ENCHANTMENT.title"] = "Envenom 3",
            ["BLIGHT_ENVENOM3_ENCHANTMENT.description"] = "After playing, apply 3 Poison to the target.",
            ["BLIGHT_ENVENOM4_ENCHANTMENT.title"] = "Envenom 4",
            ["BLIGHT_ENVENOM4_ENCHANTMENT.description"] = "After playing, apply 4 Poison to the target.",
            ["BLIGHT_BLOCK2_ENCHANTMENT.title"] = "Guard 1",
            ["BLIGHT_BLOCK2_ENCHANTMENT.description"] = "This card gains 1 additional Block.",
            ["BLIGHT_BLOCK4_ENCHANTMENT.title"] = "Guard 2",
            ["BLIGHT_BLOCK4_ENCHANTMENT.description"] = "This card gains 2 additional Block.",
            ["BLIGHT_BLOCK6_ENCHANTMENT.title"] = "Guard 4",
            ["BLIGHT_BLOCK6_ENCHANTMENT.description"] = "This card gains 4 additional Block.",
            ["BLIGHT_DOUBLE_PLAY_ENCHANTMENT.title"] = "Vortex",
            ["BLIGHT_DOUBLE_PLAY_ENCHANTMENT.description"] = "This card gains Replay 1.",
            ["BLIGHT_FRAGILE_EDGE_ENCHANTMENT.title"] = "Brittle Edge",
            ["BLIGHT_FRAGILE_EDGE_ENCHANTMENT.description"] = "This card deals {Amount} less damage.",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.title"] = "Corrupted",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.description"] = "Deals 50% more damage, but lose 2 HP.",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.extraCardText"] = "Lose 2 HP.",
            ["BLIGHT_DUPLICATION_ENCHANTMENT.title"] = "Duplication",
            ["BLIGHT_DUPLICATION_ENCHANTMENT.description"] = "After playing, the next card you play this turn is played an additional time.",
            ["BLIGHT_FAVORED_ENCHANTMENT.title"] = "Favored",
            ["BLIGHT_FAVORED_ENCHANTMENT.description"] = "This card deals double damage.",
            ["BLIGHT_GOOPY_ENCHANTMENT.title"] = "Goopy",
            ["BLIGHT_GOOPY_ENCHANTMENT.description"] = "This card gains Exhaust. After each play, its Block permanently increases by 1.",
            ["BLIGHT_GLAM_ENCHANTMENT.title"] = "Glamour",
            ["BLIGHT_GLAM_ENCHANTMENT.description"] = "This card can be replayed once each combat.",
            ["BLIGHT_IMBUED_ENCHANTMENT.title"] = "Imbued",
            ["BLIGHT_IMBUED_ENCHANTMENT.description"] = "Automatically play this card at the start of each combat.",
            ["BLIGHT_INSTINCT_ENCHANTMENT.title"] = "Instinct",
            ["BLIGHT_INSTINCT_ENCHANTMENT.description"] = "This card costs 1 less.",
            ["BLIGHT_MOMENTUM1_ENCHANTMENT.title"] = "Momentum 1",
            ["BLIGHT_MOMENTUM1_ENCHANTMENT.description"] = "Each time this card is played, it gains 1 attack damage this combat.",
            ["BLIGHT_MOMENTUM2_ENCHANTMENT.title"] = "Momentum 2",
            ["BLIGHT_MOMENTUM2_ENCHANTMENT.description"] = "Each time this card is played, it gains 2 attack damage this combat.",
            ["BLIGHT_PERFECT_ENCHANTMENT.title"] = "Perfect Fit",
            ["BLIGHT_PERFECT_ENCHANTMENT.description"] = "When this card would be shuffled into the draw pile, put it on top instead.",
            ["BLIGHT_ROYALLY_APPROVED_ENCHANTMENT.title"] = "Royally Approved",
            ["BLIGHT_ROYALLY_APPROVED_ENCHANTMENT.description"] = "This card gains Innate and Retain.",
            ["BLIGHT_SLITHER_ENCHANTMENT.title"] = "Slither",
            ["BLIGHT_SLITHER_ENCHANTMENT.description"] = "When drawn, this card's cost randomizes between 0 and 3.",
            ["BLIGHT_STEADY_ENCHANTMENT.title"] = "Steady",
            ["BLIGHT_STEADY_ENCHANTMENT.description"] = "This card gains Retain.",
            ["BLIGHT_SWIFT1_ENCHANTMENT.title"] = "Swift 1",
            ["BLIGHT_SWIFT1_ENCHANTMENT.description"] = "The first time you play this card, draw 1 card.",
            ["BLIGHT_SWIFT2_ENCHANTMENT.title"] = "Swift 2",
            ["BLIGHT_SWIFT2_ENCHANTMENT.description"] = "The first time you play this card, draw 2 cards.",
            ["BLIGHT_TEZCATARAS_EMBER_ENCHANTMENT.title"] = "Tezcataras's Ember",
            ["BLIGHT_TEZCATARAS_EMBER_ENCHANTMENT.description"] = "Costs 0 and gains Eternal.",
            ["BLIGHT_TEMPORARY_STRENGTH2_ENCHANTMENT.title"] = "Temporary Strength 2",
            ["BLIGHT_TEMPORARY_STRENGTH2_ENCHANTMENT.description"] = "After playing, gain 2 Strength this turn.",
            ["BLIGHT_TEMPORARY_STRENGTH4_ENCHANTMENT.title"] = "Temporary Strength 4",
            ["BLIGHT_TEMPORARY_STRENGTH4_ENCHANTMENT.description"] = "After playing, gain 4 Strength this turn.",
            ["BLIGHT_WOUND1_ENCHANTMENT.title"] = "Wound 1",
            ["BLIGHT_WOUND1_ENCHANTMENT.description"] = "After playing, apply 1 Vulnerable to the target.",
            ["BLIGHT_BROKEN_BLADE1_ENCHANTMENT.title"] = "Broken Blade 1",
            ["BLIGHT_BROKEN_BLADE1_ENCHANTMENT.description"] = "After playing, apply 1 Weak to the target.",
            ["BLIGHT_RAVAGE1_ENCHANTMENT.title"] = "Ravage 1",
            ["BLIGHT_RAVAGE1_ENCHANTMENT.description"] = "After playing, apply 1 Frail to the target.",
            ["BLIGHT_VIGOROUS3_ENCHANTMENT.title"] = "Vigorous 3",
            ["BLIGHT_VIGOROUS3_ENCHANTMENT.description"] = "The first time you play this card, deal 3 additional damage.",
            ["BLIGHT_VIGOROUS6_ENCHANTMENT.title"] = "Vigorous 6",
            ["BLIGHT_VIGOROUS6_ENCHANTMENT.description"] = "The first time you play this card, deal 6 additional damage.",
            ["BLIGHT_VIGOROUS9_ENCHANTMENT.title"] = "Vigorous 9",
            ["BLIGHT_VIGOROUS9_ENCHANTMENT.description"] = "The first time you play this card, deal 9 additional damage.",
            ["BLIGHT_SLUMBERING_ESSENCE_ENCHANTMENT.title"] = "Slumbering Essence",
            ["BLIGHT_SLUMBERING_ESSENCE_ENCHANTMENT.description"] = "At end of turn, if this card is in your hand, reduce its cost by 1 until played.",
            ["BLIGHT_SOWN_ENCHANTMENT.title"] = "Sown",
            ["BLIGHT_SOWN_ENCHANTMENT.description"] = "The first time you play this card each combat, gain 1 Energy.",
            ["BLIGHT_SOULS_POWER_ENCHANTMENT.title"] = "Soul Power",
            ["BLIGHT_SOULS_POWER_ENCHANTMENT.description"] = "This card loses Exhaust.",
            ["BLIGHT_DOOM1_ENCHANTMENT.title"] = "Doom 3",
            ["BLIGHT_DOOM1_ENCHANTMENT.description"] = "After playing, apply 3 Doom to yourself.",
            ["BLIGHT_DOOM6_ENCHANTMENT.title"] = "Doom 6",
            ["BLIGHT_DOOM6_ENCHANTMENT.description"] = "After playing, apply 6 Doom to yourself.",
            ["BLIGHT_DOOM10_ENCHANTMENT.title"] = "Doom 10",
            ["BLIGHT_DOOM10_ENCHANTMENT.description"] = "After playing, apply 10 Doom to yourself.",
            ["BLIGHT_DAZED1_ENCHANTMENT.title"] = "Dazed 1",
            ["BLIGHT_DAZED1_ENCHANTMENT.description"] = "After playing, shuffle 1 Dazed into your discard pile.",
            ["BLIGHT_DAZED2_ENCHANTMENT.title"] = "Dazed 2",
            ["BLIGHT_DAZED2_ENCHANTMENT.description"] = "After playing, shuffle 2 Dazed into your discard pile.",
            ["BLIGHT_WEAK_SELF1_ENCHANTMENT.title"] = "Weak 1",
            ["BLIGHT_WEAK_SELF1_ENCHANTMENT.description"] = "After playing, apply 1 Weak to yourself.",
            ["BLIGHT_VULNERABLE_SELF1_ENCHANTMENT.title"] = "Vulnerable 1",
            ["BLIGHT_VULNERABLE_SELF1_ENCHANTMENT.description"] = "After playing, apply 1 Vulnerable to yourself.",
            ["BLIGHT_FRAIL_SELF1_ENCHANTMENT.title"] = "Frail 1",
            ["BLIGHT_FRAIL_SELF1_ENCHANTMENT.description"] = "After playing, apply 1 Frail to yourself.",
            ["BLIGHT_PAIN1_ENCHANTMENT.title"] = "Pain 1",
            ["BLIGHT_PAIN1_ENCHANTMENT.description"] = "After playing, lose 1 HP.",
            ["BLIGHT_PAIN2_ENCHANTMENT.title"] = "Pain 2",
            ["BLIGHT_PAIN2_ENCHANTMENT.description"] = "After playing, lose 2 HP.",
            ["BLIGHT_PAIN3_ENCHANTMENT.title"] = "Pain 3",
            ["BLIGHT_PAIN3_ENCHANTMENT.description"] = "After playing, lose 3 HP.",
            ["BLIGHT_SWORD_DANCE1_ENCHANTMENT.title"] = "Sword Dance 1",
            ["BLIGHT_SWORD_DANCE1_ENCHANTMENT.description"] = "After playing, deal 1 damage to all enemies.",
            ["BLIGHT_SWORD_DANCE2_ENCHANTMENT.title"] = "Sword Dance 2",
            ["BLIGHT_SWORD_DANCE2_ENCHANTMENT.description"] = "After playing, deal 2 damage to all enemies.",
            ["BLIGHT_SWORD_DANCE3_ENCHANTMENT.title"] = "Sword Dance 4",
            ["BLIGHT_SWORD_DANCE3_ENCHANTMENT.description"] = "After playing, deal 4 damage to all enemies.",
            ["BLIGHT_EFFIGY_WARD.title"] = "Effigy Ward",
            ["BLIGHT_EFFIGY_WARD.description"] = "At the end of its side's turn, gain {Amount} Block.",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.title"] = "Death Grip",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.description"] = "At the end of your turn, lose {Amount} HP. Removed when Ceremonial Beast dies.",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.smartDescription"] = "At the end of your turn, lose [blue]{Amount}[/blue] HP. Removed when Ceremonial Beast dies.",
            ["HIVE_POWER.title"] = "Hive",
            ["HIVE_POWER.description"] = "Every 3 times this takes attack damage, add 1 Wound to the player's discard pile.",
            ["METEOR_POWER.title"] = "Meteor",
            ["METEOR_POWER.description"] = "Every 3 Skill cards played by players, add 1 Dazed to that player's discard pile.",

            ["COIN_SCATTER_POWER.title"] = "Coin Scatter",
            ["COIN_SCATTER_POWER.description"] = "Whenever this takes damage,  loses Gold.",
            ["COIN_SCATTER_POWER.smartDescription"] = "Whenever this takes damage,  loses [blue]{Amount}[/blue] Gold.",
            ["BLIGHT_KD_PRELUDE.prompt"] = "Choose your opening boon",
            ["BLIGHT_KD_PRELUDE.option.full_battle.title"] = "All-In Battle",
            ["BLIGHT_KD_PRELUDE.option.full_battle.description"] = "Gain [blue]3[/blue] [gold]Strength[/gold]. Knowledge Demon gains [blue]3[/blue] [gold]Strength[/gold] and [blue]5[/blue] [gold]Plating[/gold].",
            ["BLIGHT_KD_PRELUDE.option.late_bloomer.title"] = "Late Bloomer",
            ["BLIGHT_KD_PRELUDE.option.late_bloomer.description"] = "Lose [blue]20[/blue] [gold]HP[/gold] now. If you win and never get downed, gain [blue]20[/blue] [gold]Max HP[/gold].",
            ["BLIGHT_KD_PRELUDE.option.wealth.title"] = "Wealth",
            ["BLIGHT_KD_PRELUDE.option.wealth.description"] = "Gain [blue]300[/blue] [gold]Gold[/gold] and [blue]25[/blue] [gold]Coin Scatter[/gold].",
            ["BLIGHT_KD_PRELUDE.option.weapon.title"] = "Weapon",
            ["BLIGHT_KD_PRELUDE.option.weapon.description"] = "Twice, choose [blue]1[/blue] of [blue]3[/blue] random [gold]rare cards[/gold] to add to deck. Then add [blue]1[/blue] random [gold]curse[/gold].",
            ["BLIGHT_KD_PRELUDE.option.equipment.title"] = "Equipment",
            ["BLIGHT_KD_PRELUDE.option.equipment.description"] = "Obtain [blue]1[/blue] [gold]Wax relic[/gold] and gain [blue]-1[/blue] [gold]Strength[/gold].",
            ["SAND_SHIELD_POWER.title"] = "Sand Shield",
            ["SAND_SHIELD_POWER.description"] = "Immune to all damage and debuffs. Lose 1 stack at the end of turn.",
            ["THE_INSATIABLE_SHRIEK_POWER.title"] = "Insatiable Shriek",
            ["THE_INSATIABLE_SHRIEK_POWER.description"] = "Loses stacks equal to unblocked damage taken. At 0, becomes stunned and shifts to phase two.",


            ["BLIGHT_ARMOR_RELIC.title"] = "Armor",
            ["BLIGHT_ARMOR_RELIC.description"] = "At the start of each combat, gain {PlatingPower} Plating.",
            ["BLIGHT_ARMOR_RELIC.flavor"] = "Patchwork steel becomes ritual shell under the blight.",
            ["BLIGHT_CAMPFIRE_HEAL.description"] = "Restore {HealPercent}% of your max HP ({Heal}).{ExtraText}",
            ["BLIGHT_SAND_SPEAR.title"] = "Sand Spear",
            ["BLIGHT_SAND_SPEAR.description"] = "Deal {Damage:diff()} damage to an enemy with [gold]Sand Shield[/gold]. Remove 1 [gold]Sand Shield[/gold]. If it is depleted, [gold]Stun[/gold] that enemy.",
        };

        private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>
        {
            ["BLIGHT_BUTTON.title"] = "荒疫模式",
            ["BLIGHT_BUTTON.description"] = "[color=red]挑战逐渐升级的更高难度[/color]",
            ["BLIGHT_MP_BUTTON.title"] = "荒疫联机",
            ["BLIGHT_MP_BUTTON.description"] = "[color=red]创建一局荒疫联机游戏[/color]",
            ["BLIGHT_ASCENSION.0.title"] = "荒疫 0",
            ["BLIGHT_ASCENSION.0.description"] = "A10难度",
            ["BLIGHT_ASCENSION.1.title"] = "荒疫 1",
            ["BLIGHT_ASCENSION.1.description"] = "概率出现变异个体，包括小怪和精英",
            ["BLIGHT_ASCENSION.2.title"] = "荒疫 2",
            ["BLIGHT_ASCENSION.2.description"] = "精英更加致命，在精英战时精英会携带额外的随机强化词条",
            ["BLIGHT_ASCENSION.3.title"] = "荒疫 3",
            ["BLIGHT_ASCENSION.3.description"] = "开局会获得随机负面效果",
            ["BLIGHT_ASCENSION.4.title"] = "荒疫 4",
            ["BLIGHT_ASCENSION.4.description"] = "概率出现俩波敌人",
            ["BLIGHT_ASCENSION.5.title"] = "荒疫 5",
            ["BLIGHT_ASCENSION.5.description"] = "首领 (Boss) 会额外获得特殊荒疫能力",
            ["BLIGHT_FORGE.title"] = "荒疫注入",
            ["BLIGHT_FORGE.target.playable"] = "可打出的卡牌",
            ["BLIGHT_FORGE.target.attack"] = "攻击牌",
            ["BLIGHT_FORGE.target.skill"] = "技能牌",
            ["BLIGHT_FORGE.target.attack_or_skill"] = "攻击牌或技能牌",
            ["BLIGHT_FORGE.rarity.common"] = "普通",
            ["BLIGHT_FORGE.rarity.uncommon"] = "罕见",
            ["BLIGHT_FORGE.rarity.rare"] = "稀有",
            ["BLIGHT_FORGE.rarity.ultra_rare"] = "超稀有",
            ["BLIGHT_FORGE.rarity.negative"] = "负面",
            ["BLIGHT_FORGE.reward_label"] = "选择锻造奖励（{Rarity}）",
            ["BLIGHT_FORGE.description"] = "为一张{Target}附魔{EnchantName}。",
            ["BLIGHT_FORGE.utility.title.relic"] = "隐秘宝藏",
            ["BLIGHT_FORGE.utility.title.armor"] = "铠甲",
            ["BLIGHT_FORGE.utility.title.smith"] = "巧手打磨",
            ["BLIGHT_FORGE.utility.title.remove"] = "断除执念",
            ["BLIGHT_FORGE.utility.title.max_hp_percent"] = "锻体",
            ["BLIGHT_FORGE.utility.title.fallback"] = "锻造余烬",
            ["BLIGHT_FORGE.utility.desc.relic"] = "获得{Count}件随机遗物。",
            ["BLIGHT_FORGE.utility.desc.armor"] = "每场战斗开始时获得{Amount}层覆甲。",
            ["BLIGHT_FORGE.utility.desc.smith"] = "选择一张牌进行升级。",
            ["BLIGHT_FORGE.utility.desc.remove"] = "从牌组中移除一张牌。",
            ["BLIGHT_FORGE.utility.desc.max_hp_percent"] = "最大生命值提高{Percent}%。",
            ["BLIGHT_FORGE.utility.desc.fallback"] = "获得一个随机收益。",
            ["BLIGHT_FORGE.fallback.sharp2"] = "锋利2",
            ["BLIGHT_FORGE.fallback.sharp4"] = "锋利4",
            ["BLIGHT_FORGE.fallback.damage"] = "伤害强化",
            ["BLIGHT_FORGE.fallback.block"] = "格挡强化",
            ["BLIGHT_FORGE.fallback.double"] = "双重施放",
            ["BLIGHT_FORGE.fallback.fragile_edge"] = "脆刃",
            ["BLIGHT_RUN_TAG.title"] = "荒疫模式",
            ["BLIGHT_RUN_TAG.description"] = "当前为荒疫模式，以下词缀会在整局内持续生效。",
            ["BLIGHT_A3_WEAKNESS.title"] = "弱化",
            ["BLIGHT_A3_WEAKNESS.description"] = "每场战斗开始时，所有玩家获得1层虚弱。",
            ["BLIGHT_A3_ENEMY_STRENGTH.title"] = "强化",
            ["BLIGHT_A3_ENEMY_STRENGTH.description"] = "每场战斗开始时，所有敌人获得1点力量。",
            ["BLIGHT_A4_ENEMY_MAX_HP.title"] = "巨化",
            ["BLIGHT_A4_ENEMY_MAX_HP.description"] = "每场战斗开始时，所有敌人获得+5%最大生命值。",
            ["BLIGHT_COMPOSITE_ENCHANTMENT.title"] = "复合附魔",
            ["BLIGHT_COMPOSITE_ENCHANTMENT.description"] = "该牌同时拥有多个附魔效果。",
            ["BLIGHT_SHARP2_ENCHANTMENT.title"] = "锋利1",
            ["BLIGHT_SHARP2_ENCHANTMENT.description"] = "这张牌额外造成1点伤害。",
            ["BLIGHT_SHARP4_ENCHANTMENT.title"] = "锋利2",
            ["BLIGHT_SHARP4_ENCHANTMENT.description"] = "这张牌额外造成2点伤害。",
            ["BLIGHT_SHARP6_ENCHANTMENT.title"] = "锋利4",
            ["BLIGHT_SHARP6_ENCHANTMENT.description"] = "这张牌额外造成4点伤害。",
            ["BLIGHT_DAMAGE_ENCHANTMENT.title"] = "锋利",
            ["BLIGHT_DAMAGE_ENCHANTMENT.description"] = "这张牌额外造成{Amount}点伤害。",
            ["BLIGHT_ADROIT1_ENCHANTMENT.title"] = "伶俐1",
            ["BLIGHT_ADROIT1_ENCHANTMENT.description"] = "获得1点格挡。",
            ["BLIGHT_ADROIT2_ENCHANTMENT.title"] = "伶俐2",
            ["BLIGHT_ADROIT2_ENCHANTMENT.description"] = "获得2点格挡。",
            ["BLIGHT_ADROIT4_ENCHANTMENT.title"] = "伶俐4",
            ["BLIGHT_ADROIT4_ENCHANTMENT.description"] = "获得4点格挡。",
            ["BLIGHT_ENVENOM2_ENCHANTMENT.title"] = "毒液2",
            ["BLIGHT_ENVENOM2_ENCHANTMENT.description"] = "打出后对目标施加2层中毒。",
            ["BLIGHT_ENVENOM3_ENCHANTMENT.title"] = "毒液3",
            ["BLIGHT_ENVENOM3_ENCHANTMENT.description"] = "打出后对目标施加3层中毒。",
            ["BLIGHT_ENVENOM4_ENCHANTMENT.title"] = "毒液4",
            ["BLIGHT_ENVENOM4_ENCHANTMENT.description"] = "打出后对目标施加4层中毒。",
            ["BLIGHT_BLOCK2_ENCHANTMENT.title"] = "灵巧1",
            ["BLIGHT_BLOCK2_ENCHANTMENT.description"] = "这张牌额外获得1点格挡。",
            ["BLIGHT_BLOCK4_ENCHANTMENT.title"] = "灵巧2",
            ["BLIGHT_BLOCK4_ENCHANTMENT.description"] = "这张牌额外获得2点格挡。",
            ["BLIGHT_BLOCK6_ENCHANTMENT.title"] = "灵巧4",
            ["BLIGHT_BLOCK6_ENCHANTMENT.description"] = "这张牌额外获得4点格挡。",
            ["BLIGHT_DOUBLE_PLAY_ENCHANTMENT.title"] = "涡旋",
            ["BLIGHT_DOUBLE_PLAY_ENCHANTMENT.description"] = "这张牌获得重放1。",
            ["BLIGHT_FRAGILE_EDGE_ENCHANTMENT.title"] = "脆刃",
            ["BLIGHT_FRAGILE_EDGE_ENCHANTMENT.description"] = "这张牌伤害减少{Amount}点。",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.title"] = "腐化",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.description"] = "造成的伤害增加50%，但会失去2生命。",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.extraCardText"] = "失去2生命。",
            ["BLIGHT_DUPLICATION_ENCHANTMENT.title"] = "复制",
            ["BLIGHT_DUPLICATION_ENCHANTMENT.description"] = "打出后，本回合内你下一张打出的牌会额外打出一次。",
            ["BLIGHT_FAVORED_ENCHANTMENT.title"] = "宠爱",
            ["BLIGHT_FAVORED_ENCHANTMENT.description"] = "造成的伤害翻倍。",
            ["BLIGHT_GOOPY_ENCHANTMENT.title"] = "黏糊",
            ["BLIGHT_GOOPY_ENCHANTMENT.description"] = "这张牌获得消耗。每次打出后，这张牌的格挡永久增加1点。",
            ["BLIGHT_GLAM_ENCHANTMENT.title"] = "华彩",
            ["BLIGHT_GLAM_ENCHANTMENT.description"] = "这张牌每场战斗能够重放一次。",
            ["BLIGHT_IMBUED_ENCHANTMENT.title"] = "注能",
            ["BLIGHT_IMBUED_ENCHANTMENT.description"] = "每场战斗开始时，自动打出这张牌。",
            ["BLIGHT_INSTINCT_ENCHANTMENT.title"] = "本能",
            ["BLIGHT_INSTINCT_ENCHANTMENT.description"] = "这张牌的费用减少1。",
            ["BLIGHT_MOMENTUM1_ENCHANTMENT.title"] = "动量1",
            ["BLIGHT_MOMENTUM1_ENCHANTMENT.description"] = "本场战斗中，每次打出这张牌后，其攻击伤害增加1点。",
            ["BLIGHT_MOMENTUM2_ENCHANTMENT.title"] = "动量2",
            ["BLIGHT_MOMENTUM2_ENCHANTMENT.description"] = "本场战斗中，每次打出这张牌后，其攻击伤害增加2点。",
            ["BLIGHT_PERFECT_ENCHANTMENT.title"] = "完美契合",
            ["BLIGHT_PERFECT_ENCHANTMENT.description"] = "每当这张牌要被洗入抽牌堆时，将其放在抽牌堆顶。",
            ["BLIGHT_ROYALLY_APPROVED_ENCHANTMENT.title"] = "皇家认证",
            ["BLIGHT_ROYALLY_APPROVED_ENCHANTMENT.description"] = "这张牌拥有固有和保留。",
            ["BLIGHT_SLITHER_ENCHANTMENT.title"] = "蛇行",
            ["BLIGHT_SLITHER_ENCHANTMENT.description"] = "当你抽到这张牌时，使其费用在0到3之间随机变化。",
            ["BLIGHT_STEADY_ENCHANTMENT.title"] = "稳定",
            ["BLIGHT_STEADY_ENCHANTMENT.description"] = "这张牌获得保留。",
            ["BLIGHT_SWIFT1_ENCHANTMENT.title"] = "迅速1",
            ["BLIGHT_SWIFT1_ENCHANTMENT.description"] = "你第一次打出这张牌时，抽1张牌。",
            ["BLIGHT_SWIFT2_ENCHANTMENT.title"] = "迅速2",
            ["BLIGHT_SWIFT2_ENCHANTMENT.description"] = "你第一次打出这张牌时，抽2张牌。",
            ["BLIGHT_TEZCATARAS_EMBER_ENCHANTMENT.title"] = "特兹卡塔拉的余烬",
            ["BLIGHT_TEZCATARAS_EMBER_ENCHANTMENT.description"] = "费用为0且获得永恒。",
            ["BLIGHT_TEMPORARY_STRENGTH2_ENCHANTMENT.title"] = "临时力量2",
            ["BLIGHT_TEMPORARY_STRENGTH2_ENCHANTMENT.description"] = "打出后，本回合获得2点力量。",
            ["BLIGHT_TEMPORARY_STRENGTH4_ENCHANTMENT.title"] = "临时力量4",
            ["BLIGHT_TEMPORARY_STRENGTH4_ENCHANTMENT.description"] = "打出后，本回合获得4点力量。",
            ["BLIGHT_WOUND1_ENCHANTMENT.title"] = "重伤1",
            ["BLIGHT_WOUND1_ENCHANTMENT.description"] = "打出后对目标施加1层易伤。",
            ["BLIGHT_BROKEN_BLADE1_ENCHANTMENT.title"] = "断刃1",
            ["BLIGHT_BROKEN_BLADE1_ENCHANTMENT.description"] = "打出后对目标施加1层虚弱。",
            ["BLIGHT_RAVAGE1_ENCHANTMENT.title"] = "摧残1",
            ["BLIGHT_RAVAGE1_ENCHANTMENT.description"] = "打出后对目标施加1层脆弱。",
            ["BLIGHT_VIGOROUS3_ENCHANTMENT.title"] = "活力3",
            ["BLIGHT_VIGOROUS3_ENCHANTMENT.description"] = "第一次打出这张牌时，造成3点额外伤害。",
            ["BLIGHT_VIGOROUS6_ENCHANTMENT.title"] = "活力6",
            ["BLIGHT_VIGOROUS6_ENCHANTMENT.description"] = "第一次打出这张牌时，造成6点额外伤害。",
            ["BLIGHT_VIGOROUS9_ENCHANTMENT.title"] = "活力9",
            ["BLIGHT_VIGOROUS9_ENCHANTMENT.description"] = "第一次打出这张牌时，造成9点额外伤害。",
            ["BLIGHT_SLUMBERING_ESSENCE_ENCHANTMENT.title"] = "沉睡精华",
            ["BLIGHT_SLUMBERING_ESSENCE_ENCHANTMENT.description"] = "回合结束时，如果这张牌在你的手牌中，则将其费用降低1点，直到其被打出。",
            ["BLIGHT_SOWN_ENCHANTMENT.title"] = "播种",
            ["BLIGHT_SOWN_ENCHANTMENT.description"] = "你在每场战斗中第一次打出这张牌时，获得1点能量。",
            ["BLIGHT_SOULS_POWER_ENCHANTMENT.title"] = "灵魂力量",
            ["BLIGHT_SOULS_POWER_ENCHANTMENT.description"] = "这张牌失去消耗。",
            ["BLIGHT_DOOM1_ENCHANTMENT.title"] = "灾厄3",
            ["BLIGHT_DOOM1_ENCHANTMENT.description"] = "打出后给自己施加3层灾厄。",
            ["BLIGHT_DOOM6_ENCHANTMENT.title"] = "灾厄6",
            ["BLIGHT_DOOM6_ENCHANTMENT.description"] = "打出后给自己施加6层灾厄。",
            ["BLIGHT_DOOM10_ENCHANTMENT.title"] = "灾厄10",
            ["BLIGHT_DOOM10_ENCHANTMENT.description"] = "打出后给自己施加10层灾厄。",
            ["BLIGHT_DAZED1_ENCHANTMENT.title"] = "晕眩1",
            ["BLIGHT_DAZED1_ENCHANTMENT.description"] = "打出后向你的弃牌堆加入1张晕眩。",
            ["BLIGHT_DAZED2_ENCHANTMENT.title"] = "晕眩2",
            ["BLIGHT_DAZED2_ENCHANTMENT.description"] = "打出后向你的弃牌堆加入2张晕眩。",
            ["BLIGHT_WEAK_SELF1_ENCHANTMENT.title"] = "虚弱1",
            ["BLIGHT_WEAK_SELF1_ENCHANTMENT.description"] = "打出后给自己施加1层虚弱。",
            ["BLIGHT_VULNERABLE_SELF1_ENCHANTMENT.title"] = "易伤1",
            ["BLIGHT_VULNERABLE_SELF1_ENCHANTMENT.description"] = "打出后给自己施加1层易伤。",
            ["BLIGHT_FRAIL_SELF1_ENCHANTMENT.title"] = "脆弱1",
            ["BLIGHT_FRAIL_SELF1_ENCHANTMENT.description"] = "打出后给自己施加1层脆弱。",
            ["BLIGHT_PAIN1_ENCHANTMENT.title"] = "痛苦1",
            ["BLIGHT_PAIN1_ENCHANTMENT.description"] = "打出后失去1点生命。",
            ["BLIGHT_PAIN2_ENCHANTMENT.title"] = "痛苦2",
            ["BLIGHT_PAIN2_ENCHANTMENT.description"] = "打出后失去2点生命。",
            ["BLIGHT_PAIN3_ENCHANTMENT.title"] = "痛苦3",
            ["BLIGHT_PAIN3_ENCHANTMENT.description"] = "打出后失去3点生命。",
            ["BLIGHT_SWORD_DANCE1_ENCHANTMENT.title"] = "剑舞1",
            ["BLIGHT_SWORD_DANCE1_ENCHANTMENT.description"] = "打出后对所有敌人造成1点伤害。",
            ["BLIGHT_SWORD_DANCE2_ENCHANTMENT.title"] = "剑舞2",
            ["BLIGHT_SWORD_DANCE2_ENCHANTMENT.description"] = "打出后对所有敌人造成2点伤害。",
            ["BLIGHT_SWORD_DANCE3_ENCHANTMENT.title"] = "剑舞4",
            ["BLIGHT_SWORD_DANCE3_ENCHANTMENT.description"] = "打出后对所有敌人造成4点伤害。",
            ["BLIGHT_EFFIGY_WARD.title"] = "塑像守护",
            ["BLIGHT_EFFIGY_WARD.description"] = "在其阵营回合结束时，获得 {Amount} 点格挡。",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.title"] = "死亡绞索",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.description"] = "在你的回合结束时，失去 {Amount} 点生命。仪典巨兽死亡时移除。",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.smartDescription"] = "在你的回合结束时，失去[blue]{Amount}[/blue]点生命。仪典巨兽死亡时移除。",
            ["HIVE_POWER.title"] = "蜂巢",
            ["HIVE_POWER.description"] = "每受到 3 次攻击伤害，向玩家的弃牌堆加入 1 张伤口。",
            ["METEOR_POWER.title"] = "陨星",
            ["METEOR_POWER.description"] = "玩家每打出 3 张技能牌，向该玩家的弃牌堆加入 1 张晕眩。",

            ["COIN_SCATTER_POWER.title"] = "撒币",
            ["COIN_SCATTER_POWER.description"] = "每当其受到一次伤害，失去金币。",
            ["COIN_SCATTER_POWER.smartDescription"] = "每当其受到一次伤害，失去 [blue]{Amount}[/blue] 金币。",
            ["BLIGHT_KD_PRELUDE.prompt"] = "选择你的开局选项",
            ["BLIGHT_KD_PRELUDE.option.full_battle.title"] = "全力战斗",
            ["BLIGHT_KD_PRELUDE.option.full_battle.description"] = "你获得[blue]3[/blue]层[gold]力量[/gold]。知识恶魔获得[blue]3[/blue]层[gold]力量[/gold]和[blue]5[/blue]层[gold]覆甲[/gold]。",
            ["BLIGHT_KD_PRELUDE.option.late_bloomer.title"] = "大器晚成",
            ["BLIGHT_KD_PRELUDE.option.late_bloomer.description"] = "立即失去[blue]20[/blue]点[gold]生命[/gold]。若战斗胜利且全程未倒地，获得[blue]20[/blue]点[gold]生命上限[/gold]。",
            ["BLIGHT_KD_PRELUDE.option.wealth.title"] = "财富",
            ["BLIGHT_KD_PRELUDE.option.wealth.description"] = "获得[blue]300[/blue][gold]金币[/gold]并获得[blue]25[/blue]层[gold]撒币[/gold]。",
            ["BLIGHT_KD_PRELUDE.option.weapon.title"] = "武器",
            ["BLIGHT_KD_PRELUDE.option.weapon.description"] = "进行两次[blue]3选1[/blue]：每次从随机[gold]金卡[/gold]中选[blue]1[/blue]张加入牌库。随后加入[blue]1[/blue]张随机[gold]诅咒[/gold]。",
            ["BLIGHT_KD_PRELUDE.option.equipment.title"] = "装备",
            ["BLIGHT_KD_PRELUDE.option.equipment.description"] = "获得[blue]1[/blue]件[gold]蜡质遗物[/gold]，并获得[blue]-1[/blue][gold]力量[/gold]。",
            ["SAND_SHIELD_POWER.title"] = "砂之盾",
            ["SAND_SHIELD_POWER.description"] = "免疫所有伤害与负面效果。回合结束时失去1层。",
            ["THE_INSATIABLE_SHRIEK_POWER.title"] = "饥噬尖啸",
            ["THE_INSATIABLE_SHRIEK_POWER.description"] = "每次受到未被格挡的伤害时，失去等量层数。降至0时被击晕并转入二阶段。",


            ["BLIGHT_ARMOR_RELIC.title"] = "铠甲",
            ["BLIGHT_ARMOR_RELIC.description"] = "每场战斗开始时，获得 {PlatingPower} 层覆甲。",
            ["BLIGHT_ARMOR_RELIC.flavor"] = "在荒疫侵染下，缝补的钢甲也成了仪式的一部分。",
            ["BLIGHT_CAMPFIRE_HEAL.description"] = "回复最大生命值的{HealPercent}%（{Heal}）。{ExtraText}",
            ["BLIGHT_SAND_SPEAR.title"] = "砂之矛",
            ["BLIGHT_SAND_SPEAR.description"] = "对拥有[gold]砂之盾[/gold]的敌人造成{Damage:diff()}点伤害。移除1层[gold]砂之盾[/gold]。若其被清空，则使该敌人[gold]眩晕[/gold]。",
        };

        private static readonly Dictionary<string, string> Russian = new Dictionary<string, string>
        {
            
            ["BLIGHT_BUTTON.title"] = "Вылазка с Порчей",
            ["BLIGHT_BUTTON.description"] = "[red]Испытание с нарастающей сложностью[/red]",
            ["BLIGHT_MP_BUTTON.title"] = "Вылазка с Порчей",
            ["BLIGHT_MP_BUTTON.description"] = "[red]Создайте совместную вылазку в режиме Порчи[/red]",
            ["BLIGHT_ASCENSION.0.title"] = "Порча 0",
            ["BLIGHT_ASCENSION.0.description"] = "Базовая сложность наследует 10-ое Возвышение.",
            ["BLIGHT_ASCENSION.1.title"] = "Порча 1",
            ["BLIGHT_ASCENSION.1.description"] = "Мутировавшие враги могут появляться в обычных боях и боях с Элитой.",
            ["BLIGHT_ASCENSION.2.title"] = "Порча 2",
            ["BLIGHT_ASCENSION.2.description"] = "Элитные враги становятся смертоноснее и получают случайные усиления.",
            ["BLIGHT_ASCENSION.3.title"] = "Порча 3",
            ["BLIGHT_ASCENSION.3.description"] = "Вылазка начинается со случайным негативным эффектом.",
            ["BLIGHT_ASCENSION.4.title"] = "Порча 4",
            ["BLIGHT_ASCENSION.4.description"] = "Вам могут встретиться бои с двумя волнами врагов.",
            ["BLIGHT_ASCENSION.5.title"] = "Порча 5",
            ["BLIGHT_ASCENSION.5.description"] = "Боссы получают дополнительные способности Порчи.",
            ["BLIGHT_FORGE.title"] = "Инъекция порчи",
            ["BLIGHT_FORGE.target.playable"] = "разыгрываемую карту",
            ["BLIGHT_FORGE.target.attack"] = "выбранную Атаку",
            ["BLIGHT_FORGE.target.skill"] = "выбранный Навык",
            ["BLIGHT_FORGE.target.attack_or_skill"] = "на выбранную Атаку или Навык",
            ["BLIGHT_FORGE.rarity.common"] = "обычную",
            ["BLIGHT_FORGE.rarity.uncommon"] = "необычную",
            ["BLIGHT_FORGE.rarity.rare"] = "редкую",
            ["BLIGHT_FORGE.rarity.ultra_rare"] = "легендарную",
            ["BLIGHT_FORGE.rarity.negative"] = "негативную",
            ["BLIGHT_FORGE.reward_label"] = "Выбрать награду ковки ({Rarity})",
            ["BLIGHT_FORGE.description"] = "Наложить чары [purple]{EnchantName}[/purple] на {Target}.",
            ["BLIGHT_FORGE.utility.title.relic"] = "Скрытое сокровище",
            ["BLIGHT_FORGE.utility.title.armor"] = "Броня",
            ["BLIGHT_FORGE.utility.title.smith"] = "Точная настройка",
            ["BLIGHT_FORGE.utility.title.remove"] = "Разорвать связь",
            ["BLIGHT_FORGE.utility.title.max_hp_percent"] = "Закалка",
            ["BLIGHT_FORGE.utility.title.fallback"] = "Уголь кузни",
            ["BLIGHT_FORGE.utility.desc.relic"] = "Получите {Count} случайных реликвий.",
            ["BLIGHT_FORGE.utility.desc.armor"] = "В начале каждого боя получайте {Amount} панциря.",
            ["BLIGHT_FORGE.utility.desc.smith"] = "Выберите карту для улучшения.",
            ["BLIGHT_FORGE.utility.desc.remove"] = "Удалите карту из вашей колоды.",
            ["BLIGHT_FORGE.utility.desc.max_hp_percent"] = "Увеличьте максимальное ОЗ на {Percent}%.",
            ["BLIGHT_FORGE.utility.desc.fallback"] = "Получите случайный бонус.",
            ["BLIGHT_FORGE.fallback.sharp2"] = "Острота 2",
            ["BLIGHT_FORGE.fallback.sharp4"] = "Острота 4",
            ["BLIGHT_FORGE.fallback.damage"] = "Усиление урона",
            ["BLIGHT_FORGE.fallback.block"] = "Усиление защиты",
            ["BLIGHT_FORGE.fallback.double"] = "Двойной розыгрыш",
            ["BLIGHT_FORGE.fallback.fragile_edge"] = "Хрупкое лезвие",
            ["BLIGHT_RUN_TAG.title"] = "Режим Порчи",
            ["BLIGHT_RUN_TAG.description"] = "Это вылазка в режиме Порчи. Следующие модификаторы активны на все время вылазки.",
            ["BLIGHT_A3_WEAKNESS.title"] = "Слабина",
            ["BLIGHT_A3_WEAKNESS.description"] = "Все игроки начинают каждый бой с [blue]1[/blue] [gold]слабости[/gold] до конца следующего хода.",
            ["BLIGHT_A3_ENEMY_STRENGTH.title"] = "Усиление",
            ["BLIGHT_A3_ENEMY_STRENGTH.description"] = "В начале каждого боя все враги получают [blue]1[/blue] силы.",
            ["BLIGHT_A4_ENEMY_MAX_HP.title"] = "Гигантизм",
            ["BLIGHT_A4_ENEMY_MAX_HP.description"] = "В начале каждого боя все враги получают [blue]+5%[/blue] к максимальным ОЗ",
            ["BLIGHT_COMPOSITE_ENCHANTMENT.title"] = "Композитные чары",
            ["BLIGHT_COMPOSITE_ENCHANTMENT.description"] = "Эта карта имеет несколько эффектов зачарования.",
            ["BLIGHT_SHARP2_ENCHANTMENT.title"] = "Острота 1",
            ["BLIGHT_SHARP2_ENCHANTMENT.description"] = "Эта карта наносит [blue]1[/blue] доп. урона.",
            ["BLIGHT_SHARP4_ENCHANTMENT.title"] = "Острота 2",
            ["BLIGHT_SHARP4_ENCHANTMENT.description"] = "Эта карта наносит [blue]2[/blue] доп. урона.",
            ["BLIGHT_SHARP6_ENCHANTMENT.title"] = "Острота 4",
            ["BLIGHT_SHARP6_ENCHANTMENT.description"] = "Эта карта наносит [blue]4[/blue] доп. урона.",
            ["BLIGHT_DAMAGE_ENCHANTMENT.title"] = "Острота",
            ["BLIGHT_DAMAGE_ENCHANTMENT.description"] = "Эта карта наносит [blue]{Amount}[/blue] доп. урона.",
            ["BLIGHT_ADROIT1_ENCHANTMENT.title"] = "Смекалка 1",
            ["BLIGHT_ADROIT1_ENCHANTMENT.description"] = "Дает [blue]1[/blue] [gold]защиты[/gold].",
            ["BLIGHT_ADROIT2_ENCHANTMENT.title"] = "Смекалка 2",
            ["BLIGHT_ADROIT2_ENCHANTMENT.description"] = "Дает [blue]2[/blue] [gold]защиты[/gold].",
            ["BLIGHT_ADROIT4_ENCHANTMENT.title"] = "Смекалка 4",
            ["BLIGHT_ADROIT4_ENCHANTMENT.description"] = "Дает [blue]4[/blue] [gold]защиты[/gold].",
            ["BLIGHT_ENVENOM2_ENCHANTMENT.title"] = "Отравление 2",
            ["BLIGHT_ENVENOM2_ENCHANTMENT.description"] = "После разыгрывания, накладывает [blue]2[/blue] [gold]яда[/gold] на цель.",
            ["BLIGHT_ENVENOM3_ENCHANTMENT.title"] = "Отравление 3",
            ["BLIGHT_ENVENOM3_ENCHANTMENT.description"] = "После разыгрывания, накладывает [blue]3[/blue] [gold]яда[/gold] на цель.",
            ["BLIGHT_ENVENOM4_ENCHANTMENT.title"] = "Отравление 4",
            ["BLIGHT_ENVENOM4_ENCHANTMENT.description"] = "После разыгрывания, накладывает [blue]4[/blue] [gold]яда[/gold] на цель.",
            ["BLIGHT_BLOCK2_ENCHANTMENT.title"] = "Гибкость 2",
            ["BLIGHT_BLOCK2_ENCHANTMENT.description"] = "Эта карта дает [blue]2[/blue] доп. [gold]защиты[/gold].",
            ["BLIGHT_BLOCK4_ENCHANTMENT.title"] = "Гибкость 4",
            ["BLIGHT_BLOCK4_ENCHANTMENT.description"] = "Эта карта дает [blue]4[/blue] доп. [gold]защиты[/gold].",
            ["BLIGHT_BLOCK6_ENCHANTMENT.title"] = "Гибкость 6",
            ["BLIGHT_BLOCK6_ENCHANTMENT.description"] = "Эта карта дает [blue]6[/blue] доп. [gold]защиты[/gold].",
            ["BLIGHT_DOUBLE_PLAY_ENCHANTMENT.title"] = "Спираль",
            ["BLIGHT_DOUBLE_PLAY_ENCHANTMENT.description"] = "Эта карта получает [blue]1[/blue] [gold]повтор[/gold].",
            ["BLIGHT_FRAGILE_EDGE_ENCHANTMENT.title"] = "Хрупкое лезвие",
            ["BLIGHT_FRAGILE_EDGE_ENCHANTMENT.description"] = "Эта карта наносит на [blue]{Amount}[/blue] урона меньше.",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.title"] = "Чудовищная",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.description"] = "Эта карта наносит на [blue]50%[/blue] больше урона, но отнимает [blue]2[/blue] ОЗ.",
            ["BLIGHT_DUPLICATION_ENCHANTMENT.title"] = "Дублирование",
            ["BLIGHT_DUPLICATION_ENCHANTMENT.description"] = "Следующая разыгранная в этом ходу карта разыгрывается еще раз.",
            ["BLIGHT_FAVORED_ENCHANTMENT.title"] = "Фаворит",
            ["BLIGHT_FAVORED_ENCHANTMENT.description"] = "Удваивает урон от атак этой картой.",
            ["BLIGHT_GOOPY_ENCHANTMENT.title"] = "Вязкость",
            ["BLIGHT_GOOPY_ENCHANTMENT.description"] = "Эта карта [gold]сжигается[/gold]. При разыгрывании даваемая [gold]защита[/gold] навсегда повышается на [blue]1[/blue].",
            ["BLIGHT_GLAM_ENCHANTMENT.title"] = "Шик",
            ["BLIGHT_GLAM_ENCHANTMENT.description"] = "Один раз за бой, эта карта разыгрывается еще раз.",
            ["BLIGHT_IMBUED_ENCHANTMENT.title"] = "Знаковость",
            ["BLIGHT_IMBUED_ENCHANTMENT.description"] = "Эта карта автоматически разыгрывается в начале каждого боя.",
            ["BLIGHT_INSTINCT_ENCHANTMENT.title"] = "Инстинкт",
            ["BLIGHT_INSTINCT_ENCHANTMENT.description"] = "Стоимость этой карты уменьшается на [blue]1[/blue].",
            ["BLIGHT_MOMENTUM1_ENCHANTMENT.title"] = "Размах 1",
            ["BLIGHT_MOMENTUM1_ENCHANTMENT.description"] = "После разыгрывания, повышает свой урон на [blue]1[/blue] до конца боя.",
            ["BLIGHT_MOMENTUM2_ENCHANTMENT.title"] = "Размах 2",
            ["BLIGHT_MOMENTUM2_ENCHANTMENT.description"] = "После разыгрывания, повышает свой урон на [blue]2[/blue] до конца боя.",
            ["BLIGHT_PERFECT_ENCHANTMENT.title"] = "Свое место",
            ["BLIGHT_PERFECT_ENCHANTMENT.description"] = "При перемешивании стопки добора эта карта кладется на верх.",
            ["BLIGHT_ROYALLY_APPROVED_ENCHANTMENT.title"] = "Королевская печать",
            ["BLIGHT_ROYALLY_APPROVED_ENCHANTMENT.description"] = "Эта карта [gold]начальная[/gold] и [gold]оставляется[/gold].",
            ["BLIGHT_SLITHER_ENCHANTMENT.title"] = "Зигзаг",
            ["BLIGHT_SLITHER_ENCHANTMENT.description"] = "Когда вы берете эту карту, ее стоимость становится случайной (от [blue]0[/blue] до [blue]3[/blue]).",
            ["BLIGHT_STEADY_ENCHANTMENT.title"] = "Надежность",
            ["BLIGHT_STEADY_ENCHANTMENT.description"] = "Эта карта [gold]оставляется[/gold].",
            ["BLIGHT_SWIFT1_ENCHANTMENT.title"] = "Скорость 1",
            ["BLIGHT_SWIFT1_ENCHANTMENT.description"] = "При разыгрывании вы берете [blue]1[/blue] карту, один раз за бой.",
            ["BLIGHT_SWIFT2_ENCHANTMENT.title"] = "Скорость 2",
            ["BLIGHT_SWIFT2_ENCHANTMENT.description"] = "При разыгрывании вы берете [blue]2[/blue] карты, один раз за бой.",
            ["BLIGHT_TEZCATARAS_EMBER_ENCHANTMENT.title"] = "Уголек Тецкатары",
            ["BLIGHT_TEZCATARAS_EMBER_ENCHANTMENT.description"] = "Карта становится [blue]бесплатной[/blue] и [gold]вечной[/gold].",
            ["BLIGHT_TEMPORARY_STRENGTH2_ENCHANTMENT.title"] = "Временная сила 2",
            ["BLIGHT_TEMPORARY_STRENGTH2_ENCHANTMENT.description"] = "После разыгрывания, дает [blue]2[/blue] силы до конца хода.",
            ["BLIGHT_TEMPORARY_STRENGTH4_ENCHANTMENT.title"] = "Временная сила 4",
            ["BLIGHT_TEMPORARY_STRENGTH4_ENCHANTMENT.description"] = "После разыгрывания, дает [blue]4[/blue] силы до конца хода.",
            ["BLIGHT_WOUND1_ENCHANTMENT.title"] = "Ранение 1",
            ["BLIGHT_WOUND1_ENCHANTMENT.description"] = "После разыгрывания, накладывает [blue]1[/blue] [gold]уязвимости[/gold] на цель.",
            ["BLIGHT_BROKEN_BLADE1_ENCHANTMENT.title"] = "Разоружение 1",
            ["BLIGHT_BROKEN_BLADE1_ENCHANTMENT.description"] = "После разыгрывания, накладывает [blue]1[/blue] [gold]слабости[/gold] на цель.",
            ["BLIGHT_RAVAGE1_ENCHANTMENT.title"] = "Опустошение 1",
            ["BLIGHT_RAVAGE1_ENCHANTMENT.description"] = "После разыгрывания, накладывает [blue]1[/blue] [gold]хрупкости[/gold] на цель.",
            ["BLIGHT_VIGOROUS3_ENCHANTMENT.title"] = "Бодрость 3",
            ["BLIGHT_VIGOROUS3_ENCHANTMENT.description"] = "При первом разыгрывании в каждом бою эта карта наносит на [blue]3[/blue] больше урона.",
            ["BLIGHT_VIGOROUS6_ENCHANTMENT.title"] = "Бодрость 6",
            ["BLIGHT_VIGOROUS6_ENCHANTMENT.description"] = "При первом разыгрывании в каждом бою эта карта наносит на [blue]6[/blue] больше урона.",
            ["BLIGHT_VIGOROUS9_ENCHANTMENT.title"] = "Бодрость 9",
            ["BLIGHT_VIGOROUS9_ENCHANTMENT.description"] = "При первом разыгрывании в каждом бою эта карта наносит на [blue]9[/blue] больше урона.",
            ["BLIGHT_SLUMBERING_ESSENCE_ENCHANTMENT.title"] = "Дремлющая эссенция",
            ["BLIGHT_SLUMBERING_ESSENCE_ENCHANTMENT.description"] = "Если эта карта находится в руке в конце хода, ее стоимость снижается на [blue]1[/blue], пока вы ее не разыграете.",
            ["BLIGHT_SOWN_ENCHANTMENT.title"] = "Посев",
            ["BLIGHT_SOWN_ENCHANTMENT.description"] = "Эта карта дает {Amount:energyIcons()} при первом разыгрывании в каждом бою.",
            ["BLIGHT_SOULS_POWER_ENCHANTMENT.title"] = "Искра души",
            ["BLIGHT_SOULS_POWER_ENCHANTMENT.description"] = "Эта карта больше не [gold]сжигается[/gold].",
            ["BLIGHT_DOOM1_ENCHANTMENT.title"] = "Погибель 3",
            ["BLIGHT_DOOM1_ENCHANTMENT.description"] = "После разыгрывания, вы получаете [blue]3[/blue] [gold]злого рока[/gold].",
            ["BLIGHT_DOOM6_ENCHANTMENT.title"] = "Погибель 6",
            ["BLIGHT_DOOM6_ENCHANTMENT.description"] = "После разыгрывания, вы получаете [blue]6[/blue] [gold]злого рока[/gold].",
            ["BLIGHT_DOOM10_ENCHANTMENT.title"] = "Погибель 10",
            ["BLIGHT_DOOM10_ENCHANTMENT.description"] = "После разыгрывания, вы получаете [blue]10[/blue] [gold]злого рока[/gold].",
            ["BLIGHT_DAZED1_ENCHANTMENT.title"] = "Ошеломление 1",
            ["BLIGHT_DAZED1_ENCHANTMENT.description"] = "После разыгрывания, замешивает [blue]1[/blue] [gold]«Головокружение»[/gold] в вашу стопку сброса.",
            ["BLIGHT_DAZED2_ENCHANTMENT.title"] = "Ошеломление 2",
            ["BLIGHT_DAZED2_ENCHANTMENT.description"] = "После разыгрывания, замешивает [blue]2[/blue] [gold]«Головокружения»[/gold] в вашу стопку сброса.",
            ["BLIGHT_WEAK_SELF1_ENCHANTMENT.title"] = "Бессилие 1",
            ["BLIGHT_WEAK_SELF1_ENCHANTMENT.description"] = "После разыгрывания, вы получаете [blue]1[/blue] [gold]слабости[/gold] до конца следующего хода.",
            ["BLIGHT_VULNERABLE_SELF1_ENCHANTMENT.title"] = "Неосторожность 1",
            ["BLIGHT_VULNERABLE_SELF1_ENCHANTMENT.description"] = "После разыгрывания, вы получаете [blue]1[/blue] [gold]уязвимости[/gold]  до конца следующего хода.",
            ["BLIGHT_FRAIL_SELF1_ENCHANTMENT.title"] = "Неряшливость 1",
            ["BLIGHT_FRAIL_SELF1_ENCHANTMENT.description"] = "После разыгрывания, вы получаете [blue]1[/blue] [gold]хрупкости[/gold]  до конца следующего хода.",
            ["BLIGHT_PAIN1_ENCHANTMENT.title"] = "Боль 1",
            ["BLIGHT_PAIN1_ENCHANTMENT.description"] = "После разыгрывания, вы теряете [blue]1[/blue] ОЗ.",
            ["BLIGHT_PAIN2_ENCHANTMENT.title"] = "Боль 2",
            ["BLIGHT_PAIN2_ENCHANTMENT.description"] = "После разыгрывания, вы теряете [blue]2[/blue] ОЗ.",
            ["BLIGHT_PAIN3_ENCHANTMENT.title"] = "Боль 3",
            ["BLIGHT_PAIN3_ENCHANTMENT.description"] = "После разыгрывания, вы теряете [blue]3[/blue] ОЗ.",
            ["BLIGHT_SWORD_DANCE1_ENCHANTMENT.title"] = "Танец клинков 1",
            ["BLIGHT_SWORD_DANCE1_ENCHANTMENT.description"] = "После разыгрывания, наносит [blue]1[/blue] урона ВСЕМ врагам.",
            ["BLIGHT_SWORD_DANCE2_ENCHANTMENT.title"] = "Танец клинков 2",
            ["BLIGHT_SWORD_DANCE2_ENCHANTMENT.description"] = "После разыгрывания, наносит [blue]2[/blue] урона ВСЕМ врагам.",
            ["BLIGHT_SWORD_DANCE3_ENCHANTMENT.title"] = "Танец клинков 4",
            ["BLIGHT_SWORD_DANCE3_ENCHANTMENT.description"] = "После разыгрывания, наносит [blue]4[/blue] урона ВСЕМ врагам.",
            ["BLIGHT_EFFIGY_WARD.title"] = "Купол истукана",
            ["BLIGHT_EFFIGY_WARD.description"] = "В конце своего хода получает [blue]{Amount}[/blue] [gold]защиты[/gold].",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.title"] = "Хватка смерти",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.description"] = "В конце вашего хода вы теряете {Amount} ОЗ. Эффект снимается после смерти Церемониального Зверя.",
            ["CEREMONIAL_BEAST_CONSTRICT_POWER.smartDescription"] = "В конце вашего хода вы теряете [blue]{Amount}[/blue] ОЗ. Эффект снимается после смерти Церемониального Зверя.",
            ["HIVE_POWER.title"] = "Рой пчел",
            ["HIVE_POWER.description"] = "Каждые [blue]3[/blue] раза когда получает урон от Атак, добавляет в стопку сброса игроков [blue]1[/blue] [gold]«Рану»[/gold].",
            ["METEOR_POWER.title"] = "Метеор",
            ["METEOR_POWER.description"] = "Каждые [blue]3[/blue] разыгранных игроками Навыка, добавляет в стопку сброса игрока [blue]1[/blue] [gold]«Головокружение»[/gold].",

            ["BLIGHT_ARMOR_RELIC.title"] = "Кольчуга",
            ["BLIGHT_ARMOR_RELIC.description"] = "Дает {PlatingPower} [gold]панциря[/gold] в начале боя.",
            ["BLIGHT_ARMOR_RELIC.flavor"] = "Под воздействием Порчи, ношение кольчуги становится обыденностью.",
            ["BLIGHT_CAMPFIRE_HEAL.description"] = "Восстановите {HealPercent}% вашего максимального ОЗ ({Heal}).{ExtraText}",
            ["BLIGHT_CORRUPTED_ENCHANTMENT.extraCardText"] = "Потеряйте 2 ОЗ.",
            



            ["COIN_SCATTER_POWER.title"] = "Рассыпь монет",
            ["COIN_SCATTER_POWER.description"] = "Каждый раз, когда получает урон, атакующий игрок теряет золото.",
            ["COIN_SCATTER_POWER.smartDescription"] = "Каждый раз, когда получает урон, атакующий игрок теряет [blue]{Amount}[/blue] золота.",
            ["BLIGHT_KD_PRELUDE.prompt"] = "Выберите стартовый эффект",
            ["BLIGHT_KD_PRELUDE.option.full_battle.title"] = "Полная мощь",
            ["BLIGHT_KD_PRELUDE.option.full_battle.description"] = "Вы получаете [blue]3[/blue] [gold]силы[/gold]. Демон Знаний получает [blue]3[/blue] [gold]силы[/gold] и [blue]5[/blue] [gold]панциря[/gold].",
            ["BLIGHT_KD_PRELUDE.option.late_bloomer.title"] = "Поздний расцвет",
            ["BLIGHT_KD_PRELUDE.option.late_bloomer.description"] = "Потеряйте [blue]20[/blue] [gold]ОЗ[/gold] сейчас. Если победите и ни разу не падали, получите [blue]20[/blue] [gold]макс. ОЗ[/gold].",
            ["BLIGHT_KD_PRELUDE.option.wealth.title"] = "Богатство",
            ["BLIGHT_KD_PRELUDE.option.wealth.description"] = "Получите [blue]300[/blue] [gold]золота[/gold] и [blue]25[/blue] [gold]Рассыпи монет[/gold].",
            ["BLIGHT_KD_PRELUDE.option.weapon.title"] = "Оружие",
            ["BLIGHT_KD_PRELUDE.option.weapon.description"] = "Дважды выберите [blue]1[/blue] из [blue]3[/blue] случайных [gold]редких карт[/gold] в колоду. Затем получите [blue]1[/blue] случайное [gold]проклятие[/gold].",
            ["BLIGHT_KD_PRELUDE.option.equipment.title"] = "Снаряжение",
            ["BLIGHT_KD_PRELUDE.option.equipment.description"] = "Получите [blue]1[/blue] [gold]восковую реликвию[/gold] и [blue]-1[/blue] [gold]силу[/gold].",
            ["SAND_SHIELD_POWER.title"] = "Песчаный щит",
            ["SAND_SHIELD_POWER.description"] = "Невосприимчив ко всему урону и дебаффам. Теряет 1 заряд в конце хода.",
            ["THE_INSATIABLE_SHRIEK_POWER.title"] = "Ненасытный визг",
            ["THE_INSATIABLE_SHRIEK_POWER.description"] = "Теряет заряды, равные непоглощённому урону. При 0 оглушается и переходит во вторую фазу.",
            
            ["BLIGHT_SAND_SPEAR.title"] = "Песчаное копье",
            ["BLIGHT_SAND_SPEAR.description"] = "Нанесите {Damage:diff()} урона врагу с [gold]Песчаным щитом[/gold]. Снимите 1 заряд [gold]Песчаного щита[/gold]. Если щит исчерпан, [gold]оглушите[/gold] врага.",
        };

        public static IReadOnlyDictionary<string, string> GetRelics(string language)
        {
            return BuildMerged(GetLanguagePack(language), PrefixKeys(English, "relics"), PrefixKeys(Chinese, "relics"), PrefixKeys(Russian, "relics"), language, table: "relics");
        }

        public static IReadOnlyDictionary<string, string> GetEnchantments(string language)
        {
            return BuildMerged(GetLanguagePack(language), PrefixKeys(English, "enchantments"), PrefixKeys(Chinese, "enchantments"), PrefixKeys(Russian, "enchantments"), language, table: "enchantments");
        }

        public static IReadOnlyDictionary<string, string> GetModifiers(string language)
        {
            return BuildMerged(GetLanguagePack(language), PrefixKeys(English, "modifiers"), PrefixKeys(Chinese, "modifiers"), PrefixKeys(Russian, "modifiers"), language, table: "modifiers");
        }

        public static IReadOnlyDictionary<string, string> GetPowers(string language)
        {
            return BuildMerged(GetLanguagePack(language), PrefixKeys(English, "powers"), PrefixKeys(Chinese, "powers"), PrefixKeys(Russian, "powers"), language, table: "powers");
        }

        public static IReadOnlyDictionary<string, string> GetCards(string language)
        {
            return BuildMerged(
                GetLanguagePack(language),
                ExtractByPrefix(English, "BLIGHT_SAND_SPEAR."),
                ExtractByPrefix(Chinese, "BLIGHT_SAND_SPEAR."),
                ExtractByPrefix(Russian, "BLIGHT_SAND_SPEAR."),
                language,
                table: "cards");
        }

        public static string GetText(string key)
        {
            string language = LocManager.Instance?.Language ?? "eng";
            Dictionary<string, string> pack = GetLanguagePack(language);
            if (pack.TryGetValue(key, out string? text))
            {
                return text;
            }

            if (English.TryGetValue(key, out text))
            {
                return text;
            }

            return MissingLocalizationFallback;
        }

        public static string Format(string key, params (string Name, string Value)[] replacements)
        {
            string text = GetText(key);
            foreach ((string name, string value) in replacements)
            {
                text = text.Replace("{" + name + "}", value ?? string.Empty, StringComparison.Ordinal);
            }

            return text;
        }

        private static Dictionary<string, string> GetLanguagePack(string language)
        {
            if (IsChineseLanguage(language))
            {
                return Chinese;
            }

            if (IsRussianLanguage(language))
            {
                return Russian;
            }

            return English;
        }

        private static bool IsChineseLanguage(string? language)
        {
            return string.Equals(language, "zhs", StringComparison.OrdinalIgnoreCase)
                || string.Equals(language, "zht", StringComparison.OrdinalIgnoreCase)
                || string.Equals(language, "chi", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRussianLanguage(string? language)
        {
            return string.Equals(language, "ru", StringComparison.OrdinalIgnoreCase)
                || string.Equals(language, "rus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(language, "russian", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> PrefixKeys(Dictionary<string, string> source, string tableName)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> pair in source)
            {
                if (pair.Key.StartsWith("BLIGHT_BUTTON.", StringComparison.Ordinal)
                    || pair.Key.StartsWith("BLIGHT_MP_BUTTON.", StringComparison.Ordinal)
                    || pair.Key.StartsWith("BLIGHT_ASCENSION.", StringComparison.Ordinal)
                    || pair.Key.StartsWith("BLIGHT_FORGE.", StringComparison.Ordinal))
                {
                    continue;
                }

                result[pair.Key] = pair.Value;
            }

            return result;
        }

        private static Dictionary<string, string> ExtractByPrefix(Dictionary<string, string> source, string keyPrefix)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> pair in source)
            {
                if (!pair.Key.StartsWith(keyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                result[pair.Key] = pair.Value;
            }

            return result;
        }

        private static IReadOnlyDictionary<string, string> BuildMerged(
            Dictionary<string, string> activePack,
            Dictionary<string, string> englishTable,
            Dictionary<string, string> chineseTable,
            Dictionary<string, string> russianTable,
            string language,
            string table)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(englishTable);
            if (IsChineseLanguage(language))
            {
                foreach (KeyValuePair<string, string> pair in chineseTable)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            if (IsRussianLanguage(language))
            {
                foreach (KeyValuePair<string, string> pair in russianTable)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            foreach (KeyValuePair<string, string> pair in activePack)
            {
                if (pair.Key.StartsWith("BLIGHT_BUTTON.", StringComparison.Ordinal)
                    || pair.Key.StartsWith("BLIGHT_MP_BUTTON.", StringComparison.Ordinal)
                    || pair.Key.StartsWith("BLIGHT_ASCENSION.", StringComparison.Ordinal)
                    || pair.Key.StartsWith("BLIGHT_FORGE.", StringComparison.Ordinal))
                {
                    continue;
                }

                result[pair.Key] = pair.Value;
            }

            return result;
        }
    }
}