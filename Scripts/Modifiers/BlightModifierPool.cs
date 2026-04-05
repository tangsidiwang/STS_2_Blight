using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace BlightMod.Modifiers
{
    public static class BlightModifierPool
    {
        // A3 开局 buff 池：仅包含当前需求的两个全局效果。
        private static readonly List<Func<ModifierModel>> A3_Modifiers = new List<Func<ModifierModel>> {
            static () => ModelDb.Modifier<A3.BlightA3WeaknessModifier>().ToMutable(),
            static () => ModelDb.Modifier<A3.BlightA3EnemyStrengthModifier>().ToMutable(),
        };

        // A4 开局 buff 池：先留空，后续可直接在此增加新 Modifier。
        private static readonly List<Func<ModifierModel>> A4_Modifiers = new List<Func<ModifierModel>> {
            static () => ModelDb.Modifier<A4.BlightA4EnemyMaxHpModifier>().ToMutable(),
        };

        // A5 开局 buff 池：先留空，后续可直接在此增加新 Modifier。
        private static readonly List<Func<ModifierModel>> A5_Modifiers = new List<Func<ModifierModel>>{
            static () => ModelDb.Modifier<A3.BlightA3EnemyStrengthModifier>().ToMutable(),
            static () => ModelDb.Modifier<A3.BlightA3WeaknessModifier>().ToMutable(),
            static () => ModelDb.Modifier<A4.BlightA4EnemyMaxHpModifier>().ToMutable(),
        };

        /// <summary>
        /// 根据游戏种子决定这一局抽出什么被动给玩家
        /// </summary>
        public static ModifierModel GetRandomA3Modifier(string seed)
        {
            return GetRandomFromPool(seed, "A3", A3_Modifiers) ?? ModelDb.Modifier<A3.BlightA3WeaknessModifier>().ToMutable();
        }

        public static ModifierModel? GetRandomA4Modifier(string seed)
        {
            return GetRandomFromPool(seed, "A4", A4_Modifiers);
        }

        public static ModifierModel? GetRandomA5Modifier(string seed)
        {
            return GetRandomFromPool(seed, "A5", A5_Modifiers);
        }

        private static ModifierModel? GetRandomFromPool(string seed, string bucket, List<Func<ModifierModel>> pool)
        {
            if (pool.Count == 0)
            {
                return null;
            }

            if (string.IsNullOrEmpty(seed))
            {
                return pool[0]();
            }

            int hash = StringHelper.GetDeterministicHashCode($"{seed}_Blight_{bucket}_Modifier");
            var rng = new Random(hash);
            Func<ModifierModel> factory = pool[rng.Next(pool.Count)];
            return factory();
        }
    }
}
