using BlightMod.Core;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BlightMod.AI;

internal static class BlightAIContext
{
    public static bool IsCurrentNodeMutant()
    {
        if (!BlightModeManager.IsAtLeastAscension(1))
        {
            return false;
        }

        RunState? state = RunManager.Instance?.DebugOnlyGetState();
        var point = state?.CurrentMapPoint;
        string seed = state?.Rng.StringSeed ?? string.Empty;

        return point != null
            && !string.IsNullOrEmpty(seed)
            && BlightModeManager.IsNodeMutant(point, seed);
    }

    public static bool ShouldOverrideMonsterAi(MonsterModel monster)
    {
        if (!BlightModeManager.IsBlightModeActive || monster == null)
        {
            return false;
        }

        var pointType = RunManager.Instance?.DebugOnlyGetState()?.CurrentMapPoint?.PointType;
        return pointType switch
        {
            MapPointType.Boss => BlightModeManager.BlightAscensionLevel >= 5,
            MapPointType.Elite => BlightModeManager.BlightAscensionLevel >= 2,
            MapPointType.Monster => IsCurrentNodeMutant(),
            _ => false
        };
    }
}