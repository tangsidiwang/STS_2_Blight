using System.Diagnostics;
using System.Runtime.CompilerServices;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.AI.Bestiary;

internal static class BlightBestiaryHpTemplate
{
    private const string BlightHpNumbersSuffix = "BlightHpNumbers";
    private const string BlightNumbersSuffix = "BlightNumbers";
    private const string TemplateTypeName = nameof(BlightBestiaryHpTemplate);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetHpAdd(
        int ascensionLevel,
        bool mutant,
        int a0,
        int a1To2,
        int a3To4,
        int a5Plus,
        int mutantBonus)
    {
        var centralRule = TryGetRuleForCaller();
        if (centralRule.HasValue)
        {
            a0 = centralRule.Value.A0;
            a1To2 = centralRule.Value.A1To2;
            a3To4 = centralRule.Value.A3To4;
            a5Plus = centralRule.Value.A5Plus;
            mutantBonus = centralRule.Value.Mutant;
        }

        int hpAdd = ascensionLevel switch
        {
            <= 0 => a0,
            <= 2 => a1To2,
            <= 4 => a3To4,
            _ => a5Plus
        };

        if (mutant)
        {
            hpAdd += mutantBonus;
        }

        return hpAdd;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetHpAdd(string monsterId)
    {
        return GetHpAdd(monsterId, BlightModeManager.BlightAscensionLevel, IsCurrentNodeMutant());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetHpAdd(string monsterId, int ascensionLevel, bool mutant)
    {
        var rule = BlightBestiaryHpRegistry.GetRule(monsterId);
        return GetHpAdd(ascensionLevel, mutant, rule.A0, rule.A1To2, rule.A3To4, rule.A5Plus, rule.Mutant);
    }

    public static bool IsCurrentNodeMutant()
    {
        if (!BlightModeManager.IsAtLeastAscension(1))
        {
            return false;
        }

        var state = RunManager.Instance?.DebugOnlyGetState();
        var point = state?.CurrentMapPoint;
        string seed = state?.Rng.StringSeed ?? string.Empty;

        return point != null
            && !string.IsNullOrEmpty(seed)
            && BlightModeManager.IsNodeMutant(point, seed);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BlightBestiaryHpRule? TryGetRuleForCaller()
    {
        var frames = new StackTrace().GetFrames();
        if (frames == null)
        {
            return null;
        }

        foreach (var frame in frames)
        {
            var declaringType = frame.GetMethod()?.DeclaringType;
            if (declaringType == null)
            {
                continue;
            }

            if (declaringType.Name == TemplateTypeName)
            {
                continue;
            }

            string typeName = declaringType.Name;
            string? monsterId = null;

            if (typeName.EndsWith(BlightHpNumbersSuffix))
            {
                monsterId = typeName[..^BlightHpNumbersSuffix.Length];
            }
            else if (typeName.EndsWith(BlightNumbersSuffix))
            {
                monsterId = typeName[..^BlightNumbersSuffix.Length];
            }

            if (string.IsNullOrEmpty(monsterId))
            {
                continue;
            }

            return BlightBestiaryHpRegistry.GetRule(monsterId);
        }

        return null;
    }
}
