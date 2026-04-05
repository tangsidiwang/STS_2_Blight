using BlightMod.Modifiers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlightMod.Modifiers
{
    /// <summary>
    /// Marker modifier to identify Blight runs in serialized save data.
    /// It has no gameplay effect.
    /// </summary>
    public class BlightRunTagModifier : BlightBaseModifier
    {
        public override LocString Title => new LocString("modifiers", "BLIGHT_RUN_TAG.title");

        public override LocString Description => new LocString("modifiers", "BLIGHT_RUN_TAG.description");

        protected override string IconPath => ModelDb.Power<ArtifactPower>().ResolvedBigIconPath;

        [SavedProperty]
        public int BlightAscensionLevel { get; set; } = 1;

        [SavedProperty]
        public string TriggeredDoubleWaveNodeKeys { get; set; } = string.Empty;
    }
}