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
	[HarmonyPatch(typeof(DungeonGenerator), "ShouldSkipFrame")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> ShouldSkipFrameTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Attempting to inject modified frame skip transpiler into DungeonGenerator::ShouldSkipFrame");

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

		Loadstone.TranspilerLog.LogDebug($"Validating injected frame skip transpiler into DungeonGenerator::ShouldSkipFrame");
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

	[HarmonyPatch(typeof(DungeonGenerator), "PostProcess", MethodType.Enumerator)]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> PostProcessPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Attempting to inject async check into DungeonGenerator::PostProcess");
		Type[] findWaitParams = { typeof(Func<bool>) };
		Type[] findFuncParams = { typeof(System.Object), typeof(IntPtr) };

		var constructors = typeof(Func<System.Boolean>).GetConstructors();
		foreach (var constructor in constructors)
		{
			Loadstone.TranspilerLog.LogDebug(constructor);
		}

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldnull),
					new CodeMatch(OpCodes.Stfld))
			.SetOpcodeAndAdvance(OpCodes.Nop) // Remove the ldnull while maintaining labels
			.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldloc_2),
					new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(typeof(AsyncDungeonPatches), "PostProcessCheck")),
					new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Func<System.Boolean>), parameters: findFuncParams)),
					new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(UnityEngine.WaitUntil), parameters: findWaitParams)))
			.InstructionEnumeration();
		
		Loadstone.TranspilerLog.LogDebug($"Validating injected async check into DungeonGenerator::PostProcess");
		return newInstructions;
	}

	static bool PostProcessCheck()
	{
		return FromProxyPatches.ConversionComplete;
	}
}
