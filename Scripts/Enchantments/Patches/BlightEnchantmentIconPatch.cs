using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BlightMod.Enchantments.Patches
{
    [HarmonyPatch(typeof(EnchantmentModel), "get_Icon")]
    public static class BlightEnchantmentIconPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(EnchantmentModel __instance, ref CompressedTexture2D __result)
        {
            ModelId id = __instance.Id;
            if (id.Category != "ENCHANTMENT" || string.IsNullOrEmpty(id.Entry) || !id.Entry.StartsWith("BLIGHT_", StringComparison.Ordinal))
            {
                return true;
            }

            if (id.Entry.StartsWith("BLIGHT_SWORD_DANCE", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<DoubleDamagePower>(out CompressedTexture2D? doubleDamageIcon))
                {
                    __result = doubleDamageIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }

            if (id.Entry.Equals("BLIGHT_COMPOSITE_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<GenesisPower>(out CompressedTexture2D? genesisIcon))
                {
                    __result = genesisIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }

            if (id.Entry.Equals("BLIGHT_DOOM1_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_DOOM6_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_DOOM10_ENCHANTMENT", StringComparison.Ordinal)
               )
            {
                if (TryLoadPowerIcon<DoomPower>(out CompressedTexture2D? doomIcon))
                {
                    __result = doomIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if (id.Entry.Equals("BLIGHT_PAIN1_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_PAIN2_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_PAIN3_ENCHANTMENT", StringComparison.Ordinal)
               )
            {
                if (TryLoadPowerIcon<FeralPower>(out CompressedTexture2D? feralIcon))
                {
                    __result = feralIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if(id.Entry.Equals("BLIGHT_DOUBLE_PLAY_ENCHANTMENT", StringComparison.Ordinal))
            {
               
                __result = ModelDb.Enchantment<Spiral>().Icon;
                
                return false;
            }
            if (id.Entry.Equals("BLIGHT_DAZED1_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_DAZED2_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<KnockdownPower>(out CompressedTexture2D? knockdownIcon))
                {
                    __result = knockdownIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if(id.Entry.Equals("BLIGHT_BLOCK2_ENCHANTMENT", StringComparison.Ordinal) || id.Entry.Equals("BLIGHT_BLOCK4_ENCHANTMENT", StringComparison.Ordinal) || id.Entry.Equals("BLIGHT_BLOCK6_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Nimble>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_CORRUPTED_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Corrupted>().Icon;

                return false;
            }
            if (id.Entry.Equals("BLIGHT_DUPLICATION_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<DuplicationPower>(out CompressedTexture2D? duplicationIcon))
                {
                    __result = duplicationIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if (id.Entry.Equals("BLIGHT_TEMPORARY_STRENGTH2_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_TEMPORARY_STRENGTH4_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<StrengthPower>(out CompressedTexture2D? strengthIcon))
                {
                    __result = strengthIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if (id.Entry.Equals("BLIGHT_WOUND1_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<VulnerablePower>(out CompressedTexture2D? vulnerableIcon))
                {
                    __result = vulnerableIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if (id.Entry.Equals("BLIGHT_BROKEN_BLADE1_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<WeakPower>(out CompressedTexture2D? weakIcon))
                {
                    __result = weakIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if (id.Entry.Equals("BLIGHT_RAVAGE1_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<FrailPower>(out CompressedTexture2D? frailIcon))
                {
                    __result = frailIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if (id.Entry.Equals("BLIGHT_WEAK_SELF1_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<WeakPower>(out CompressedTexture2D? weakIcon))
                {
                    __result = weakIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if (id.Entry.Equals("BLIGHT_VULNERABLE_SELF1_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<VulnerablePower>(out CompressedTexture2D? vulnerableIcon))
                {
                    __result = vulnerableIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if (id.Entry.Equals("BLIGHT_FRAIL_SELF1_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<FrailPower>(out CompressedTexture2D? frailIcon))
                {
                    __result = frailIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if(id.Entry.Equals("BLIGHT_ADROIT2_ENCHANTMENT", StringComparison.Ordinal)|| id.Entry.Equals("BLIGHT_ADROIT4_ENCHANTMENT", StringComparison.Ordinal) || id.Entry.Equals("BLIGHT_ADROIT1_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Adroit>().Icon;

                return false;
            }
            if (id.Entry.Equals("BLIGHT_ENVENOM2_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_ENVENOM3_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_ENVENOM4_ENCHANTMENT", StringComparison.Ordinal))
            {
                if (TryLoadPowerIcon<EnvenomPower>(out CompressedTexture2D? envenomIcon))
                {
                    __result = envenomIcon!;
                }
                else
                {
                    __result = ModelDb.Enchantment<Sharp>().Icon;
                }

                return false;
            }
            if(id.Entry.Equals("BLIGHT_FAVORED_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<CustomEnchantments.BlightFavoredEnchantment>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_GOOPY_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Goopy>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_GLAM_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Glam>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_IMBUED_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Imbued>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_INSTINCT_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Instinct>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_MOMENTUM1_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_MOMENTUM2_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Momentum>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_PERFECT_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<PerfectFit>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_ROYALLY_APPROVED_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<RoyallyApproved>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_SLITHER_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Slither>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_SLUMBERING_ESSENCE_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<SlumberingEssence>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_STEADY_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Steady>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_TEZCATARAS_EMBER_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<TezcatarasEmber>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_SWIFT1_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_SWIFT2_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Swift>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_VIGOROUS3_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_VIGOROUS6_ENCHANTMENT", StringComparison.Ordinal)
                || id.Entry.Equals("BLIGHT_VIGOROUS9_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Vigorous>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_SOWN_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<Sown>().Icon;

                return false;
            }
            if(id.Entry.Equals("BLIGHT_SOULS_POWER_ENCHANTMENT", StringComparison.Ordinal))
            {
                __result = ModelDb.Enchantment<SoulsPower>().Icon;

                return false;
            }
            // If a custom icon exists at images/enchantments/<entry_lowercase>.png, let vanilla logic load it.
            if (ResourceLoader.Exists(__instance.IntendedIconPath))
            {
                return true;
            }

            __result = ModelDb.Enchantment<Sharp>().Icon;
            return false;
        }

        private static bool TryLoadPowerIcon<TPower>(out CompressedTexture2D? icon) where TPower : PowerModel
        {
            icon = null;
            PowerModel power = ModelDb.Power<TPower>();

            // Power atlas icons are often AtlasTexture; EnchantmentModel.Icon requires CompressedTexture2D.
            // Prefer big icon png path (usually imported as CompressedTexture2D), then fall back safely.
            if (TryLoadCompressed(power.ResolvedBigIconPath, out icon))
            {
                return true;
            }

            if (TryLoadCompressed(power.IconPath, out icon))
            {
                return true;
            }

            return false;
        }

        private static bool TryLoadCompressed(string path, out CompressedTexture2D? icon)
        {
            icon = null;
            Resource resource;
            try
            {
                resource = ResourceLoader.Load(path, string.Empty, ResourceLoader.CacheMode.Reuse);
            }
            catch
            {
                return false;
            }

            icon = resource as CompressedTexture2D;
            return icon != null;
        }
    }
}
