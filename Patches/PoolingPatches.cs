using DunGen;
using HarmonyLib;
using Loadstone.Config;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace Loadstone.Patches;

public class PoolingPatches
{
	private static CodeMatch InstantiateMatcher = new CodeMatch(i =>
						i.opcode == OpCodes.Call
						&& ((MethodInfo)i.operand).Name == "Instantiate"
						&& ((MethodInfo)i.operand).DeclaringType == typeof(UnityEngine.Object));

	[HarmonyPatch(typeof(RoundManager), "Start")]
	[HarmonyPostfix]
	static void RoundManagerStartHijack()
	{
		NetworkManager.Singleton.SceneManager.OnUnload += (_a, _b, _c) => ObjectPool.ReleaseAllObjects();
	}

	[HarmonyPatch(typeof(DungeonProxy), "AddTile")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> AddTilePoolingPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug("Attempting to inject pooling patches into DungeonProxy::AddTile");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					InstantiateMatcher)
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "InstantiateTransparently", parameters: new Type[] { typeof(GameObject), typeof(Transform) }))
			.InstructionEnumeration();
		
		Loadstone.TranspilerLog.LogDebug("Validating injected pooling patches into DungeonProxy::AddTile");
		return newInstructions;
	}
	
	[HarmonyPatch(typeof(DungeonProxy), "RemoveTile")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> RemoveTilePoolingPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug("Attempting to inject pooling patches into DungeonProxy::RemoveTile");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "DestroyImmediate", parameters: new Type[] { typeof(UnityEngine.Object) })))
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "ReleaseObject"))
			.InstructionEnumeration();
		
		Loadstone.TranspilerLog.LogDebug("Validating injected pooling patches into DungeonProxy::RemoveTile");
		return newInstructions;
	}

	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> FromProxyPoolingPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug("Attempting to inject pooling patches into Dungeon::FromProxy");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					InstantiateMatcher)
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "InstantiateTransparently", parameters: new Type[] { typeof(GameObject), typeof(Transform) }))

			.MatchForward(false,
					InstantiateMatcher)
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "InstantiateTransparently", parameters: new Type[] { typeof(GameObject) }))

			.InstructionEnumeration();
		
		Loadstone.TranspilerLog.LogDebug("Validating injected pooling patches into Dungeon::FromProxy");
		return newInstructions;
	}

	[HarmonyPatch(typeof(RoundManager), "SpawnSyncedProps")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> SpawnSyncedPropsPoolingPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug("Attempting to inject pooling patches into RoundManager::SpawnSyncedProps");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					InstantiateMatcher)
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "InstantiateTransparently", parameters: new Type[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Transform) }))
			.InstructionEnumeration();
		
		Loadstone.TranspilerLog.LogDebug("Validating injected pooling patches into RoundManager::SpawnSyncedProps");
		return newInstructions;
	}
}
