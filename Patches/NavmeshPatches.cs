using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Loadstone.Patches;

public class NavmeshPatches
{
	[HarmonyPatch(typeof(RoundManager), "SpawnOutsideHazards")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> SpawnOutsideHazardsPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Writing SpawnOutsideHazards Transpiler");
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
					typeof(NavmeshPatches), "GenerateNavMeshAsync",
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
						
		Loadstone.HarmonyLog.LogDebug($"Updating navmesh with {sources.Count} obstacles");

		var buildSettings = navMeshSurface.GetBuildSettings();
		buildSettings.tileSize = 64;
		buildSettings.maxJobWorkers = 4;

		roundManager.StartCoroutine(NavMeshUpdateCheck(
					NavMeshBuilder.UpdateNavMeshDataAsync(
						navMeshSurface.navMeshData,
						buildSettings,
						sources,
						bounds),
					navMeshSurface));
	}

	static IEnumerator NavMeshUpdateCheck(AsyncOperation asyncOperation, NavMeshSurface navMeshSurface)
	{
		while (!asyncOperation.isDone)
			yield return null;

		navMeshSurface.RemoveData();
		navMeshSurface.AddData();

		Loadstone.HarmonyLog.LogDebug("Updated navmesh");
	}

}
