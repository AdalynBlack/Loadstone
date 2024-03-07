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

namespace Loadstone.Patches;

public class RoundManagerPatches
{
	static IEnumerable<CodeInstruction> PatchUnorderedSearch(IEnumerable<CodeInstruction> instructions, Type type)
	{
		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(
							typeof(UnityEngine.Object), "FindObjectsOfType",
							generics: new Type[] { type })))
			.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldc_I4_0))
			.SetOperandAndAdvance(AccessTools.Method(typeof(UnityEngine.Object), "FindObjectsByType",
					parameters: new Type[] { typeof(FindObjectsSortMode) },
					generics: new Type[] { type }))
			.InstructionEnumeration();
		
		return newInstructions;
	}

	[HarmonyPatch(typeof(RoundManager), "SpawnSyncedProps")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> SpawnSyncedPropsFindPatch(IEnumerable<CodeInstruction> instructions)
	{
		return PatchUnorderedSearch(instructions, typeof(SpawnSyncedObject));
	}

	[HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> GenerateNewFloorFindPatch(IEnumerable<CodeInstruction> instructions)
	{
		return PatchUnorderedSearch(instructions, typeof(EntranceTeleport));
	}

	[HarmonyPatch(typeof(RoundManager), "LoadNewLevelWait", MethodType.Enumerator)]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> LoadNewLevelWaitPatch(IEnumerable<CodeInstruction> instructions)
	{
		float levelLoadWaitTime = LoadstoneConfig.PostGenerateSpawnDelay.Value;
		Loadstone.TranspilerLog.LogDebug($"Attempting to inject custom time value of \"{levelLoadWaitTime}\" into \"RoundManager::LoadNewLevelWait\"");

		var newInstructions = new CodeMatcher(instructions)
			// State starts at 2, which falls through to 3
			.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_3)) // Skip yield return
			.Advance(-1)
			.RemoveInstructions(5)

			// State 3 falls through to 4
			.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_4)) // Skip yield return
			.Advance(-1)
			.RemoveInstructions(5)

			// State 4 falls through to 5
			.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_5)) // Keep dungeon check in place
			.Advance(-1)

			// State 5 has a custom time inserted
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldc_R4), // Find the first "load Float32 instruction" that is followed by creating a "WaitForSeconds" object and replace the value loaded with shipDoorWaitTime
					new CodeMatch(OpCodes.Newobj))

			.SetAndAdvance(
					OpCodes.Ldsfld,
					typeof(LoadstoneConfig).GetField("PostGenerateSpawnDelay"))
			.InsertAndAdvance(new CodeInstruction(
						OpCodes.Callvirt, AccessTools.Method(typeof(ConfigEntry<float>), "get_Value")))

			// State 7 falls through to 8
			.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_7)) // Skip yield return
			.Advance(-1)
			.RemoveInstructions(5)

			.InstructionEnumeration();

		Loadstone.TranspilerLog.LogDebug($"Validating injected custom time value of \"{levelLoadWaitTime}\" into \"RoundManager::LoadNewLevelWait\"");
		return newInstructions;
	}

	[HarmonyPatch(typeof(RoundManager), "Generator_OnGenerationStatusChanged")]
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
	
	[HarmonyPatch(typeof(RoundManager), "LoadNewLevelWait")]
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

	[HarmonyPatch(typeof(RoundManager), "GenerateNewLevelClientRpc")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> GenerateNewLevelPatch(IEnumerable<CodeInstruction> instructions)
	{
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

	[HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
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

	[HarmonyPatch(typeof(RoundManager), "SpawnOutsideHazards")]
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
