using Godot;

namespace BlightMod.Visuals;

public sealed class MutantVisualProfile
{
	public Color CharcoalColor { get; init; } = new(0.02f, 0.02f, 0.02f, 1f);
	public Color BloodShadowColor { get; init; } = new(0.16f, 0.01f, 0.01f, 1f);
	public Color BloodMidColor { get; init; } = new(0.33f, 0.03f, 0.03f, 1f);
	public Color BloodHighlightColor { get; init; } = new(0.52f, 0.07f, 0.06f, 1f);
	public float BlendStrength { get; init; } = 0.92f;
	public float TileScale { get; init; } = 6.0f;
	public float StainScale { get; init; } = 2.8f;
	public float StreakStrength { get; init; } = 0.7f;
	public float MistStrength { get; init; } = 0.4f;
	public float CrustStrength { get; init; } = 0.85f;
	public float EdgeHardness { get; init; } = 0.58f;
	public float Contrast { get; init; } = 1.55f;
	public float NormalStrength { get; init; } = 0.55f;
	public float RoughnessStrength { get; init; } = 0.82f;
	public Vector2 FakeLightDirection { get; init; } = new(-0.45f, -0.85f);

	public static MutantVisualProfile Default { get; } = new();
}