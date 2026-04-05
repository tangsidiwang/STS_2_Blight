using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BlightMod.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BlightMod.AI.Bestiary;

public sealed class SlimedBerserkerBlightAI : IBlightMonsterAI
{
    private const int VomitIchorStatusCount = 10;
    private const int VomitIchorDrawPileCount = 5;
    private const int VomitIchorDiscardPileCount = 5;

    public string TargetMonsterId => "SlimedBerserker";

    public void ApplyBlightStartBuffs(MonsterModel monster, int blightAscensionLevel)
    {
    }

    public MonsterMoveStateMachine GenerateBlightStateMachine(MonsterModel monster, int blightAscensionLevel)
    {
        if (monster is not SlimedBerserker slimedBerserker)
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        if (!BlightAIContext.ShouldOverrideMonsterAi(monster) || !BlightAIContext.IsCurrentNodeMutant())
        {
            return (MonsterMoveStateMachine)AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").GetValue(monster)!;
        }

        int pummelingDamage = (int)AccessTools.Property(typeof(SlimedBerserker), "PummelingDamage")!.GetValue(slimedBerserker)!;
        int smotherDamage = (int)AccessTools.Property(typeof(SlimedBerserker), "SmotherDamage")!.GetValue(slimedBerserker)!;

        var vomitIchor = new MoveState("VOMIT_ICHOR_MOVE", targets => VomitIchorMove(slimedBerserker, targets), new StatusIntent(VomitIchorStatusCount));
        var furiousPummeling = new MoveState("FURIOUS_PUMMELING_MOVE", targets => InvokeOriginalMove(slimedBerserker, "FuriousPummelingMove", targets), new MultiAttackIntent(pummelingDamage, 4));
        var leechingHug = new MoveState("LEECHING_HUG_MOVE", targets => InvokeOriginalMove(slimedBerserker, "LeechingHugMove", targets), new DebuffIntent(), new BuffIntent());
        var smother = new MoveState("SMOTHER_MOVE", targets => InvokeOriginalMove(slimedBerserker, "SmotherMove", targets), new SingleAttackIntent(smotherDamage));

        vomitIchor.FollowUpState = furiousPummeling;
        furiousPummeling.FollowUpState = leechingHug;
        leechingHug.FollowUpState = smother;
        smother.FollowUpState = vomitIchor;

        return new MonsterMoveStateMachine(new List<MonsterState> { vomitIchor, smother, leechingHug, furiousPummeling }, vomitIchor);
    }

    private static async Task VomitIchorMove(SlimedBerserker slimedBerserker, IReadOnlyList<Creature> targets)
    {
        string slimeSfx = (string)AccessTools.Property(typeof(SlimedBerserker), "SlimeSfx")!.GetValue(slimedBerserker)!;
        SfxCmd.Play(slimeSfx);
        await CreatureCmd.TriggerAnim(slimedBerserker.Creature, "Vomit", 0.7f);
        await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Draw, VomitIchorDrawPileCount, addedByPlayer: false);
        await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, VomitIchorDiscardPileCount, addedByPlayer: false);
    }

    private static Task InvokeOriginalMove(SlimedBerserker slimedBerserker, string methodName, IReadOnlyList<Creature> targets)
    {
        return (Task)AccessTools.Method(typeof(SlimedBerserker), methodName)!.Invoke(slimedBerserker, new object[] { targets })!;
    }
}

internal static class SlimedBerserkerBlightHpNumbers
{
    public const int A0HpAdd = 0;
    public const int A1To2HpAdd = 0;
    public const int A3To4HpAdd = 0;
    public const int A5PlusHpAdd = 0;
    public const int MutantHpAdd = 0;

    public static int GetHpAdd()
    {
        bool mutant = BlightBestiaryHpTemplate.IsCurrentNodeMutant();
        return BlightBestiaryHpTemplate.GetHpAdd(BlightModeManager.BlightAscensionLevel, mutant, A0HpAdd, A1To2HpAdd, A3To4HpAdd, A5PlusHpAdd, MutantHpAdd);
    }
}

[HarmonyPatch(typeof(SlimedBerserker), "get_MinInitialHp")]
public static class SlimedBerserkerMinHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SlimedBerserkerBlightHpNumbers.GetHpAdd();
    }
}

[HarmonyPatch(typeof(SlimedBerserker), "get_MaxInitialHp")]
public static class SlimedBerserkerMaxHpPatch
{
    public static void Postfix(ref int __result)
    {
        if (!BlightModeManager.IsBlightModeActive)
        {
            return;
        }

        __result += SlimedBerserkerBlightHpNumbers.GetHpAdd();
    }
}