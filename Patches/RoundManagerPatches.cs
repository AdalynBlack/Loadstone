using BepInEx.Configuration;
using HarmonyLib;
using Loadstone.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

using MonoMod.Utils;
using Mono.Cecil;

namespace Loadstone.Patches;

[HarmonyPatch(typeof(RoundManager))]
public class RoundManagerPatches
{
	[HarmonyPatch("Generator_OnGenerationStatusChanged")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> StatusChangedPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
	{
		Loadstone.TranspilerLog.LogDebug("Attempting to fix Generator_OnGenerationStatusChanged");

		var newInstructions = new CodeMatcher(instructions, ilGenerator)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ret))
			.CreateLabel(out var returnLabel)
			.Start()
			.MatchForward(false,
					new CodeMatch(OpCodes.Bne_Un))
			.SetOperandAndAdvance(returnLabel)
			.InstructionEnumeration();

		return newInstructions;
	}
	
	[HarmonyPatch("LoadNewLevelWait")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> LoadNewLevelWaitInitPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug("Attempting to inject custom start position in coroutine \"RoundManager::LoadNewLevelWait\"");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldc_I4_0))
			.SetOpcodeAndAdvance(OpCodes.Ldc_I4_2)
			.InstructionEnumeration();

		Loadstone.TranspilerLog.LogDebug("Validating injected custom start position in coroutine \"RoundManager::LoadNewLevelWait\"");
		return newInstructions;
	}

	[HarmonyPatch("GenerateNewLevelClientRpc")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> GenerateNewLevelPatch(IEnumerable<CodeInstruction> instructions)
	{
		if (LoadstoneConfig.SeedDisplayConfig.Value == LoadstoneConfig.SeedDisplayType.Darken)
			return instructions;

		Loadstone.TranspilerLog.LogDebug($"Attempting to disable screen overlay on scene load in \"RoundManager::GenerateNewLevelClientRpc\"");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldc_I4_1), // Replace true with false to disable the overlay instead of enabling it
					new CodeMatch(OpCodes.Callvirt))
			.SetOpcodeAndAdvance(OpCodes.Ldc_I4_0)
			.InstructionEnumeration();

		Loadstone.TranspilerLog.LogDebug($"Validating disabled screen overlay on scene load in \"RoundManager::GenerateNewLevelClientRpc\"");
		return newInstructions;
	}

	[HarmonyPatch("GenerateNewLevelClientRpc")]
	[HarmonyPostfix]
	static void GenerateNewLevelClientRpcPrefixPath(int randomSeed)
	{
		Loadstone.HarmonyLog.LogInfo($"Random seed: {randomSeed}");

		if (LoadstoneConfig.SeedDisplayConfig.Value != LoadstoneConfig.SeedDisplayType.Popup)
			return;

		HUDManager.Instance.DisplayTip("Random Seed", $"{randomSeed}");
	}

	[HarmonyPatch("GenerateNewFloor")]
	[HarmonyPrefix]
	static void GenerateNewFloorPatch(RoundManager __instance)
	{
		__instance.dungeonGenerator.Generator.GenerateAsynchronously = LoadstoneConfig.ShouldGenAsync.Value;
		__instance.dungeonGenerator.Generator.PauseBetweenRooms = 0f;
		__instance.dungeonGenerator.Generator.MaxAsyncFrameMilliseconds = LoadstoneConfig.DungeonAsyncMaxTime.Value;
	}

	static IEnumerator NavMeshUpdateCheck(AsyncOperation asyncOperation, NavMeshSurface navMeshSurface)
	{
		while (!asyncOperation.isDone)
			yield return null;

		navMeshSurface.RemoveData();
		navMeshSurface.AddData();
	}

	[HarmonyPatch("SpawnOutsideHazards")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> SpawnOutsideHazardsPatch(IEnumerable<CodeInstruction> instructions)
	{
		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
							typeof(GameObject), "GetComponent",
							generics: new Type[] {typeof(NavMeshSurface)})))

			.Advance(1)
			.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_0))

			.Set(
					OpCodes.Call,
					AccessTools.Method(
						typeof(RoundManagerPatches), "GenerateNavMeshAsync",
						parameters: new Type[] {typeof(NavMeshSurface), typeof(RoundManager)}))
			.InstructionEnumeration();

		return newInstructions;
	}

	static void GenerateNavMeshAsync(NavMeshSurface navMeshSurface, RoundManager roundManager) {
		List<NavMeshBuildSource> sources = (List<NavMeshBuildSource>)typeof(NavMeshSurface)
			.GetMethod("CollectSources", BindingFlags.NonPublic | BindingFlags.Instance)
			.Invoke(navMeshSurface, new object[] {});
		Bounds bounds = (Bounds)typeof(NavMeshSurface)
			.GetMethod("CalculateWorldBounds", BindingFlags.NonPublic | BindingFlags.Instance)
			.Invoke(navMeshSurface, new object[] {sources});
						
		Loadstone.HarmonyLog.LogDebug($"Updated navmesh with {sources.Count} obstacles");

		roundManager.StartCoroutine(NavMeshUpdateCheck(
					NavMeshBuilder.UpdateNavMeshDataAsync(
						navMeshSurface.navMeshData,
						navMeshSurface.GetBuildSettings(),
						sources,
						bounds),
					navMeshSurface));
	}
}
