using DunGen.Adapters;
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
	// Replaces NavMesh generation with a custom async version for the surface
	[HarmonyPatch(typeof(RoundManager), "SpawnOutsideHazards")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> SpawnOutsideHazardsPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug("Writing SpawnOutsideHazards Transpiler");

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

		Loadstone.LogDebug("Verifying SpawnOutsideHazards Transpiler");
		return newInstructions;
	}

	// Replaces NavMesh generation with a custom async version for DunGen
	[HarmonyPatch(typeof(UnityNavMeshAdapter), "BakeFullDungeon")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> BakeFullDungeonPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug("Writing UnityNavMeshAdapter Transpiler");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Callvirt, AccessTools.DeclaredMethod(typeof(NavMeshSurface), "BuildNavMesh")))
			.Repeat( matcher =>
					matcher.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_0))
					.SetOperandAndAdvance(
						AccessTools.DeclaredMethod(typeof(NavmeshPatches), "GenerateNavMeshAsync")))
			.InstructionEnumeration();

		Loadstone.LogDebug("Verifying UnityNavMeshAdapter Transpiler");
		return newInstructions;
	}

	static void GenerateNavMeshAsync(NavMeshSurface navMeshSurface, MonoBehaviour coroutineHijack) {
		List<NavMeshBuildSource> sources = (List<NavMeshBuildSource>)typeof(NavMeshSurface)
			.GetMethod("CollectSources", BindingFlags.NonPublic | BindingFlags.Instance)
			.Invoke(navMeshSurface, new object[] {});

		Bounds bounds = new Bounds(navMeshSurface.center, navMeshSurface.size);
		if (navMeshSurface.collectObjects != CollectObjects.Volume)
		{
			bounds = (Bounds)typeof(NavMeshSurface)
				.GetMethod("CalculateWorldBounds", BindingFlags.NonPublic | BindingFlags.Instance)
				.Invoke(navMeshSurface, new object[] {sources});
		}
						
		Loadstone.LogDebug($"Updating navmesh with {sources.Count} obstacles");

		var buildSettings = navMeshSurface.GetBuildSettings();
		//buildSettings.tileSize = 64;
		//buildSettings.maxJobWorkers = 4;
		
		if (navMeshSurface.navMeshData != null)
		{
			navMeshSurface.navMeshData.position = navMeshSurface.transform.position;
			navMeshSurface.navMeshData.rotation = navMeshSurface.transform.rotation;
		} else {
			navMeshSurface.navMeshData = new NavMeshData(buildSettings.agentTypeID)
			{
				position = navMeshSurface.transform.position,
				rotation = navMeshSurface.transform.rotation
			};
		}

		coroutineHijack.StartCoroutine(NavMeshUpdateCheck(
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

		Loadstone.LogDebug("Updated navmesh");
	}
}
