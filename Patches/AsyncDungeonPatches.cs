using DunGen;
using HarmonyLib;
using Loadstone.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Loadstone.Patches;

public class AsyncDungeonPatches
{
	// Replaces DunGen's async frame skipping to make it run for a certain amount of time each frame, rather than targeting a specific framerate
	// This is meant to improve load times on lower end PCs, at the cost of lower framerates while loading
	[HarmonyPatch(typeof(DungeonGenerator), "ShouldSkipFrame")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> ShouldSkipFrameTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug($"Attempting to inject modified frame skip transpiler into DungeonGenerator::ShouldSkipFrame");

		var newInstructions = new CodeMatcher(instructions)
			.Start()
			.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(DungeonGenerator), "yieldTimer")),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.DeclaredMethod(typeof(Stopwatch), "Start")))
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldfld),
					new CodeMatch(OpCodes.Callvirt, AccessTools.DeclaredMethod(typeof(Stopwatch), "Restart")))
			.Advance(2)
			.SetOperandAndAdvance(
					AccessTools.DeclaredMethod(typeof(Stopwatch), "Reset"))
			.InstructionEnumeration();

		Loadstone.LogDebug($"Validating injected frame skip transpiler into DungeonGenerator::ShouldSkipFrame");
		return newInstructions;
	}

	[HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
	[HarmonyPrefix]
	static void GenerateNewFloorPatch(RoundManager __instance)
	{
		__instance.dungeonGenerator.Generator.GenerateAsynchronously = LoadstoneConfig.AsyncDungeon.Value;
		__instance.dungeonGenerator.Generator.PauseBetweenRooms = 0f;
		__instance.dungeonGenerator.Generator.MaxAsyncFrameMilliseconds = LoadstoneConfig.DungeonAsyncMaxTime.Value;
	}
}
