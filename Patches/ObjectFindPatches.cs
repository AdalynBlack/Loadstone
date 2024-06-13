using DunGen;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace Loadstone.Patches;

public class ObjectFindPatches
{
	[HarmonyDebug]
	[HarmonyPatch(typeof(InteractTrigger), "Start")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> InteractTriggerPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Attempting to replace InteractTrigger's StartOfRound finding function");
		var newInstructions = StartOfRoundFixer(instructions);
		Loadstone.TranspilerLog.LogDebug($"Verifying InteractTrigger's replaced StartOfRound finding function");
		return newInstructions;
	}

	[HarmonyPatch(typeof(OutOfBoundsTrigger), "Start")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> OutOfBoundsTriggerPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Attempting to replace OutOfBoundsTrigger's StartOfRound finding function");
		var newInstructions = StartOfRoundFixer(instructions);
		Loadstone.TranspilerLog.LogDebug($"Verifying OutOfBoundsTrigger's replaced StartOfRound finding function");
		return newInstructions;
	}

	[HarmonyPatch(typeof(FoliageDetailDistance), "Start")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> FoliageDetailDistancePatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Attempting to replace FoliageDetailDistance's StartOfRound finding function");
		var newInstructions = StartOfRoundFixer(instructions);
		Loadstone.TranspilerLog.LogDebug($"Verifying FoliageDetailDistance's replaced StartOfRound finding function");
		return newInstructions;
	}

	[HarmonyPatch(typeof(ItemDropship), "Start")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> ItemDropshipPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Attempting to replace ItemDropship's StartOfRound finding function");
		var newInstructions = StartOfRoundFixer(instructions);
		Loadstone.TranspilerLog.LogDebug($"Verifying ItemDropship's replaced StartOfRound finding function");
		return newInstructions;
	}

	[HarmonyPatch(typeof(animatedSun), "Start")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> animatedSunPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Attempting to replace animatedSun's TImeOfDay finding function");
		var newInstructions = InstanceFixer(instructions, typeof(TimeOfDay));
		Loadstone.TranspilerLog.LogDebug($"Verifying animatedSun's replaced TImeOfDay finding function");
		return newInstructions;
	}

	static IEnumerable<CodeInstruction> StartOfRoundFixer(IEnumerable<CodeInstruction> instructions)
		=> InstanceFixer(instructions, typeof(StartOfRound));

	static IEnumerable<CodeInstruction> InstanceFixer(IEnumerable<CodeInstruction> instructions, Type instanceType)
	{
		return new CodeMatcher(instructions)
			.MatchForward(false,
				new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "FindObjectOfType", parameters: new Type[] {}, generics: new Type[] {instanceType})))
			.Repeat(matcher =>
				matcher.SetOperandAndAdvance(
						AccessTools.DeclaredMethod(instanceType, "get_Instance")))
			.InstructionEnumeration();
	}
}