#if NIGHTLY
using DunGen;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Loadstone.Patches;

public class PoolingPatches
{
	private static CodeMatch InstantiateMatcher = new CodeMatch(i =>
						i.opcode == OpCodes.Call
						&& ((MethodInfo)i.operand).Name == "Instantiate"
						&& ((MethodInfo)i.operand).DeclaringType == typeof(UnityEngine.Object));

	[HarmonyPatch(typeof(DungeonGenerator), "Generate")]
	[HarmonyPostfix]
	static void GenerateHijack(DungeonGenerator __instance)
	{
		var genStats = __instance.GenerationStats;
		Loadstone.LogDebug($"DunGen Stats:\nPre-process Time: {genStats.PreProcessTime}\nMain Path Generation Time: {genStats.MainPathGenerationTime}\nBranch Path Generation Time: {genStats.BranchPathGenerationTime}\nPost-process Time: {genStats.PostProcessTime}\nTotal Time: {genStats.TotalTime}");
	}

	[HarmonyPatch(typeof(RoundManager), "UnloadSceneObjectsEarly")]
	[HarmonyPostfix]
	static void RoundManagerStartHijack()
	{
		ObjectPool.ReleaseAllObjects();
	}

	// Replace standard instantiations with an automated object pooled version
	[HarmonyPatch(typeof(DungeonProxy), "AddTile")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> AddTilePoolingPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug("Attempting to inject pooling patches into DungeonProxy::AddTile");

		// Replace any GameObject.Instantiate calls with ObjectPooling's Instantiate method
		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false, InstantiateMatcher)
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "InstantiateTransparently", parameters: new Type[] { typeof(GameObject), typeof(Transform) }))
			.InstructionEnumeration();
		
		Loadstone.LogDebug("Validating injected pooling patches into DungeonProxy::AddTile");
		return newInstructions;
	}
	
	// Replace standard destroy functions with an automated object pool release function
	[HarmonyPatch(typeof(DungeonProxy), "RemoveTile")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> RemoveTilePoolingPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug("Attempting to inject pooling patches into DungeonProxy::RemoveTile");

		// Replace Object.DestroyImmediate calls with ObjectPooling's release method
		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "DestroyImmediate", parameters: new Type[] { typeof(UnityEngine.Object) })))
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "ReleaseObject"))
			.InstructionEnumeration();
		
		Loadstone.LogDebug("Validating injected pooling patches into DungeonProxy::RemoveTile");
		return newInstructions;
	}

	// Replace standard destroy functions with an automated object pool release function
	[HarmonyPatch(typeof(UnityUtil), "Destroy")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> DestroyPoolingPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug("Attempting to inject pooling patches into UnityUtil::Destroy");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "Destroy", parameters: new Type[] { typeof(UnityEngine.Object) })))
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "ReleaseObject"))
			.MatchForward(false,
					new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "DestroyImmediate", parameters: new Type[] { typeof(UnityEngine.Object) })))
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "ReleaseObject"))
			.InstructionEnumeration();
		
		Loadstone.LogDebug("Validating injected pooling patches into UnityUtil::Destroy");
		return newInstructions;
	}

	// Replace standard instantiations with an automated object pooled version
	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> FromProxyPoolingPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug("Attempting to inject pooling patches into Dungeon::FromProxy");

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false, InstantiateMatcher)
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "InstantiateTransparently", parameters: new Type[] { typeof(GameObject), typeof(Transform) }))

			.MatchForward(false, InstantiateMatcher)
			.SetOperandAndAdvance(
				AccessTools.DeclaredMethod(typeof(ObjectPool), "InstantiateTransparently", parameters: new Type[] { typeof(GameObject) }))

			.Start()
			.MatchForward(false,
				new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "DestroyImmediate", parameters: new Type[] { typeof(UnityEngine.Object), typeof(bool) })))
			.Repeat(matcher =>
				matcher.SetOperandAndAdvance(AccessTools.DeclaredMethod(typeof(PoolingPatches), "DestroyImmediateTransparently")))

			.InstructionEnumeration();
		
		Loadstone.LogDebug("Validating injected pooling patches into Dungeon::FromProxy");
		return newInstructions;
	}

	static void DestroyImmediateTransparently(UnityEngine.Object obj, bool allowDestroyingAssets)
	{
		if (!obj.GetType().IsAssignableFrom(typeof(UnityEngine.GameObject)))
		{
			UnityEngine.Object.DestroyImmediate(obj, allowDestroyingAssets);
			return;
		}

		ObjectPool.ReleaseObject(obj as GameObject);
	}
}
#endif
