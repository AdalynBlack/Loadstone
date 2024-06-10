using BepInEx.Configuration;
using HarmonyLib;
using Loadstone.Config;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Loadstone.Patches;

public class StartOfRoundPatches
{
	[HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> OnLoadCompletePatch(IEnumerable<CodeInstruction> instructions)
	{
		if (LoadstoneConfig.SeedDisplayConfig.Value == LoadstoneConfig.SeedDisplayType.Darken)
			return instructions;

		Loadstone.TranspilerLog.LogDebug($"Attempting to disable screen overlay on scene load in \"StartOfRound::SceneManager_OnLoadComplete1\"");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldc_I4_1), // Replace true with false to disable the overlay instead of enabling it
					new CodeMatch(OpCodes.Callvirt))
			.SetOpcodeAndAdvance(OpCodes.Ldc_I4_0)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldc_I4_1), // Replace true with false to disable the overlay instead of enabling it
					new CodeMatch(OpCodes.Callvirt))
			.SetOpcodeAndAdvance(OpCodes.Ldc_I4_0)
			.InstructionEnumeration();

		Loadstone.TranspilerLog.LogDebug($"Validating disabled screen overlay on scene load in \"StartOfRound::SceneManager_OnLoadComplete1\"");
		return newInstructions;
	}

	[HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoad")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> OnLoadPatch(IEnumerable<CodeInstruction> instructions)
	{
		if (LoadstoneConfig.SeedDisplayConfig.Value == LoadstoneConfig.SeedDisplayType.Darken)
			return instructions;

		Loadstone.TranspilerLog.LogDebug($"Attempting to disable screen overlay on scene load in \"StartOfRound::SceneManager_OnLoad\"");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldc_I4_1), // Replace true with false to disable the overlay instead of enabling it
					new CodeMatch(OpCodes.Callvirt))
			.SetOpcodeAndAdvance(OpCodes.Ldc_I4_0)
			.InstructionEnumeration();

		Loadstone.TranspilerLog.LogDebug($"Validating disabled screen overlay on scene load in \"StartOfRound::SceneManager_OnLoad\"");
		return newInstructions;
	}
}
