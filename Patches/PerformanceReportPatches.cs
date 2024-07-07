using DunGen;
using DunGen.Graph;
using HarmonyLib;
using Loadstone.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using UnityEngine;

namespace Loadstone.Patches;

public class PerformanceReportPatches
{
	private static Stopwatch timer = new Stopwatch();
	private static double DungeonWaitStartedAt = 0;
	private static double DungeonWaitEndedAt = 0;
	private static double FinishGeneratingLevelCalledAt = 0;
	
	[HarmonyPatch(typeof(StartOfRound), "StartGame")]
	[HarmonyPrefix]
	static void StartGameCalled()
	{
		timer.Reset();
		timer.Start();
	}

	[HarmonyPatch(typeof(RoundManager), "RefreshEnemiesList")]
	[HarmonyPrefix]
	static void FinishGeneratingLevel()
	{
		FinishGeneratingLevelCalledAt = timer.Elapsed.TotalMilliseconds;
		timer.Stop();

		GenerationStats genStats = null;
		if (CurrentGenerator != null)
			genStats = CurrentGenerator.GenerationStats;

		Loadstone.LogInfo("Level Loading Stats:");
		if (genStats != null)
		{
			Loadstone.LogInfo("  DunGen");
			Loadstone.LogInfo($"    MainPathRoomCount: {genStats.MainPathRoomCount}");
			Loadstone.LogInfo($"    BranchPathRoomCount: {genStats.BranchPathRoomCount}");
			Loadstone.LogInfo($"    MaxBranchDepth: {genStats.MaxBranchDepth}");
			Loadstone.LogInfo($"    TotalRetries: {genStats.TotalRetries}");
			Loadstone.LogInfo($"    PrunedBranchTileCount: {genStats.PrunedBranchTileCount}");
			Loadstone.LogInfo($"    PreProcessTime: {genStats.PreProcessTime/1000} seconds");
			Loadstone.LogInfo($"    MainPathGenerationTime: {genStats.MainPathGenerationTime/1000} seconds");
			Loadstone.LogInfo($"    BranchPathGenerationTime: {genStats.BranchPathGenerationTime/1000} seconds");
			Loadstone.LogInfo($"    PostProcessTime: {genStats.PostProcessTime/1000} seconds");
			Loadstone.LogInfo($"    TotalTime: {genStats.TotalTime/1000} seconds");
		}
		else
			Loadstone.LogInfo("  No DunGen timing stats present");
		Loadstone.LogInfo($"  Started Waiting for Others' Dungeons to Finish after {DungeonWaitStartedAt/1000.0} seconds");
		Loadstone.LogInfo($"  Finished Waiting for Others' Dungeons to Finish after {DungeonWaitEndedAt/1000.0} seconds");
		Loadstone.LogInfo($"  Total generation time took {FinishGeneratingLevelCalledAt/1000.0} seconds");
	}

	static void DungeonWaitStarted()
	{
		DungeonWaitStartedAt = timer.Elapsed.TotalMilliseconds;
	}

	static void DungeonWaitEnded()
	{
		DungeonWaitEndedAt = timer.Elapsed.TotalMilliseconds;
	}

	static DungeonGenerator CurrentGenerator = null;

	[HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
	[HarmonyPostfix]
	static void GetDungeonReference(RoundManager __instance)
	{
		CurrentGenerator = __instance.dungeonGenerator.Generator;
	}

	// Inserts functions for tracking when the dungeon wait starts and ends
	[HarmonyPatch(typeof(RoundManager), "LoadNewLevelWait", MethodType.Enumerator)]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> DungeonWaitDetection(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug("Attempting to patch Dungeon Wait Detection into RoundManager::LoadNewLevelWait");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldftn))
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldftn))
			.InsertAndAdvance(
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PerformanceReportPatches), "DungeonWaitStarted")))
			.Advance(10)
			.InsertAndAdvance(
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PerformanceReportPatches), "DungeonWaitEnded")))
			.InstructionEnumeration();

		Loadstone.LogDebug("Validating Dungeon Wait Detection patch in RoundManager::LoadNewLevelWait");
		return newInstructions;
	}
}
