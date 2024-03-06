#if DEBUG
using DunGen;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Loadstone.Patches.Debug;

public class ProfilingPatches {
	static Stack callStack = new Stack();
	static Stack timingStack = new Stack();

	static List<StackProfile> stackProfiles = new List<StackProfile>();

	public struct StackProfile
	{
		public StackProfile(string methodName, long timeElapsed, int depth)
		{
			this.methodName = methodName;
			this.timeElapsed = timeElapsed;
			this.depth = depth;
		}

		public string methodName;
		public long timeElapsed;
		public int depth;
	}

	[HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
	[HarmonyPatch(typeof(RoundManager), "SpawnSyncedProps")]
	[HarmonyPatch(typeof(RoundManager), "GeneratedFloorPostProcessing")]
	[HarmonyPatch(typeof(RoundManager), "RefreshEnemyVents")]
	[HarmonyPatch(typeof(RoundManager), "FinishGeneratingLevel")]
	[HarmonyPatch(typeof(DungeonGenerator), "Generate")]
	[HarmonyPatch(typeof(DungeonGenerator), "OuterGenerate")]
	[HarmonyPatch(typeof(DungeonGenerator), "InnerGenerate")]
	[HarmonyPatch(typeof(DungeonGenerator), "OuterGenerate", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonGenerator), "InnerGenerate", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonGenerator), "ChangeStatus")]
	[HarmonyPatch(typeof(DungeonGenerator), "Clear")]
	[HarmonyPatch(typeof(DungeonGenerator), "GatherTilesToInject")]
	[HarmonyPatch(typeof(DungeonGenerator), "PreProcess")]
	[HarmonyPatch(typeof(DungeonGenerator), "PostProcess")]
	[HarmonyPatch(typeof(DungeonGenerator), "PostProcess", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonGenerator), "GenerateBranchPaths")]
	[HarmonyPatch(typeof(DungeonGenerator), "GenerateBranchPaths", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath")]
	[HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonProxy), "ConnectOverlappingDoorways")]
	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyPrefix]
	static void PushCall(MethodBase __originalMethod)
	{
		callStack.Push($"{__originalMethod.ReflectedType}::{__originalMethod.Name}");
		timingStack.Push(DateTime.Now.Ticks);
	}

	[HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
	[HarmonyPatch(typeof(RoundManager), "SpawnSyncedProps")]
	[HarmonyPatch(typeof(RoundManager), "GeneratedFloorPostProcessing")]
	[HarmonyPatch(typeof(RoundManager), "RefreshEnemyVents")]
	[HarmonyPatch(typeof(RoundManager), "FinishGeneratingLevel")]
	[HarmonyPatch(typeof(DungeonGenerator), "Generate")]
	[HarmonyPatch(typeof(DungeonGenerator), "OuterGenerate")]
	[HarmonyPatch(typeof(DungeonGenerator), "InnerGenerate")]
	[HarmonyPatch(typeof(DungeonGenerator), "OuterGenerate", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonGenerator), "InnerGenerate", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonGenerator), "ChangeStatus")]
	[HarmonyPatch(typeof(DungeonGenerator), "Clear")]
	[HarmonyPatch(typeof(DungeonGenerator), "GatherTilesToInject")]
	[HarmonyPatch(typeof(DungeonGenerator), "PreProcess")]
	[HarmonyPatch(typeof(DungeonGenerator), "PostProcess")]
	[HarmonyPatch(typeof(DungeonGenerator), "PostProcess", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonGenerator), "GenerateBranchPaths")]
	[HarmonyPatch(typeof(DungeonGenerator), "GenerateBranchPaths", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath")]
	[HarmonyPatch(typeof(DungeonGenerator), "GenerateMainPath", MethodType.Enumerator)]
	[HarmonyPatch(typeof(DungeonProxy), "ConnectOverlappingDoorways")]
	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyPostfix]
	static void Pop()
	{
		string lastMethodName = (string)callStack.Pop();
		long lastStartTime = (long)timingStack.Pop();

		stackProfiles.Add(new StackProfile(lastMethodName, DateTime.Now.Ticks - lastStartTime, callStack.Count));
	}

	[HarmonyPatch(typeof(RoundManager), "FinishGeneratingNewLevelClientRpc")]
	[HarmonyPostfix]
	static void PrintStack()
	{
		stackProfiles.Reverse();
		foreach (var profile in stackProfiles)
		{
			Loadstone.HarmonyLog.LogDebug($"{new String(' ', profile.depth)}{profile.methodName}: {profile.timeElapsed/10000f}ms");
		}
		stackProfiles.Clear();
	}

	[HarmonyPatch(typeof(RoundManager), "Generator_OnGenerationStatusChanged")]
	[HarmonyPrefix]
	static void GenerationStatusChangedPatch(DungeonGenerator generator, GenerationStatus status)
	{
		if (status == GenerationStatus.Complete)
		{
			var stats = generator.GenerationStats;
			Loadstone.HarmonyLog.LogDebug("Generation Stats");
			Loadstone.HarmonyLog.LogDebug("----------------");
			Loadstone.HarmonyLog.LogDebug($"PreProcessTime: {stats.PreProcessTime}");
			Loadstone.HarmonyLog.LogDebug($"MainPathGenerationTime: {stats.MainPathGenerationTime}");
			Loadstone.HarmonyLog.LogDebug($"BranchPathGenerationTime: {stats.BranchPathGenerationTime}");
			Loadstone.HarmonyLog.LogDebug($"PostProcessTime: {stats.PostProcessTime}");
			Loadstone.HarmonyLog.LogDebug($"TotalTime: {stats.TotalTime}");
		}
	}
}
#endif

