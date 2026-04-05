using BlightMod.Rewards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace BlightMod.Rewards.Patches
{
    [HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen.RewardCollectedFrom))]
    public static class ForgeRewardCollectedDebugPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Control button)
        {
            if (button is NRewardButton rewardButton && rewardButton.Reward is ForgeRewardItem)
            {
                Log.Info("[Blight][Forge] NRewardsScreen.RewardCollectedFrom fired for forge reward.");
            }
        }
    }

    [HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen.RewardSkippedFrom))]
    public static class ForgeRewardSkippedDebugPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Control button)
        {
            if (button is NRewardButton rewardButton && rewardButton.Reward is ForgeRewardItem)
            {
                Log.Info("[Blight][Forge] NRewardsScreen.RewardSkippedFrom fired for forge reward.");
            }
        }
    }
}
