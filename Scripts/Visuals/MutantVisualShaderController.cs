using BlightMod.AI;
using BlightMod.Core;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BlightMod.Visuals;

public static class MutantVisualShaderController
{
	private const string HueParameter = "h";
	private static Shader? _cachedShader;

	public static MutantVisualProfile ActiveProfile { get; private set; } = MutantVisualProfile.Default;

	public static void SetProfile(MutantVisualProfile profile)
	{
		ActiveProfile = profile ?? MutantVisualProfile.Default;
	}

	public static void TryApply(NCreature creature)
	{
		MonsterModel? monster = creature.Entity?.Monster;
		if (!ShouldUseMutantShader(creature, monster))
		{
			return;
		}

		try
		{
			ApplyToVisuals(creature.Visuals);
		}
		catch (System.Exception exception)
		{
			string monsterId = monster?.Id.Entry ?? creature.Entity?.Monster?.GetType().Name ?? "UNKNOWN_MONSTER";
			Log.Warn($"[Blight][MutantShader] Failed to apply shader to {monsterId}: {exception}");
		}
	}

	private static void ApplyToVisuals(NCreatureVisuals visuals)
	{
		if (visuals.SpineBody != null)
		{
			ApplyToSpine(visuals.SpineBody);
			return;
		}

		ApplyRecursively(visuals.Body);
	}

	private static void ApplyToSpine(MegaSprite spineBody)
	{
		Material? currentMaterial = spineBody.GetNormalMaterial();
		ShaderMaterial shaderMaterial = EnsureShaderMaterial(currentMaterial);
		ApplyProfile(shaderMaterial);
		spineBody.SetNormalMaterial(shaderMaterial);
	}

	private static void ApplyRecursively(Node node)
	{
		if (node is CanvasItem canvasItem)
		{
			ShaderMaterial shaderMaterial = EnsureShaderMaterial(canvasItem.Material);
			ApplyProfile(shaderMaterial);
			canvasItem.Material = shaderMaterial;
		}

		foreach (Node child in node.GetChildren())
		{
			ApplyRecursively(child);
		}
	}

	private static ShaderMaterial EnsureShaderMaterial(Material? currentMaterial)
	{
		if (currentMaterial is ShaderMaterial shaderMaterial && IsMutantShader(shaderMaterial))
		{
			return shaderMaterial;
		}

		return new ShaderMaterial
		{
			Shader = LoadShader()
		};
	}

	private static bool IsMutantShader(ShaderMaterial shaderMaterial)
	{
		Shader? shader = shaderMaterial.Shader;
		if (shader == null)
		{
			return false;
		}

		Shader targetShader = LoadShader();
		return ReferenceEquals(shader, targetShader) || shader.ResourcePath == targetShader.ResourcePath;
	}

	private static Shader LoadShader()
	{
		_cachedShader ??= new Shader
		{
			Code = MutantVisualShaderSource.Code
		};

		return _cachedShader;
	}

	private static bool ShouldUseMutantShader(NCreature creature, MonsterModel? monster)
	{
		if (creature?.Entity?.Monster == null || monster == null)
		{
			return false;
		}

		if (!BlightModeManager.IsBlightModeActive)
		{
			return false;
		}

		if (!BlightAIContext.IsCurrentNodeMutant())
		{
			return false;
		}

		return BlightMonsterDirector.HasCustomOverride(monster);
	}

	private static void ApplyProfile(ShaderMaterial shaderMaterial)
	{
		MutantVisualProfile profile = ActiveProfile;
		shaderMaterial.SetShaderParameter("charcoal_color", profile.CharcoalColor);
		shaderMaterial.SetShaderParameter("blood_shadow_color", profile.BloodShadowColor);
		shaderMaterial.SetShaderParameter("blood_mid_color", profile.BloodMidColor);
		shaderMaterial.SetShaderParameter("blood_highlight_color", profile.BloodHighlightColor);
		shaderMaterial.SetShaderParameter("blend_strength", profile.BlendStrength);
		shaderMaterial.SetShaderParameter("tile_scale", profile.TileScale);
		shaderMaterial.SetShaderParameter("stain_scale", profile.StainScale);
		shaderMaterial.SetShaderParameter("streak_strength", profile.StreakStrength);
		shaderMaterial.SetShaderParameter("mist_strength", profile.MistStrength);
		shaderMaterial.SetShaderParameter("crust_strength", profile.CrustStrength);
		shaderMaterial.SetShaderParameter("edge_hardness", profile.EdgeHardness);
		shaderMaterial.SetShaderParameter("contrast", profile.Contrast);
		shaderMaterial.SetShaderParameter("normal_strength", profile.NormalStrength);
		shaderMaterial.SetShaderParameter("roughness_strength", profile.RoughnessStrength);
		shaderMaterial.SetShaderParameter("fake_light_direction", profile.FakeLightDirection);
		shaderMaterial.SetShaderParameter(HueParameter, 0.0f);
	}
}