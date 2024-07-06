using DunGen;
using HarmonyLib;
using Loadstone.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Loadstone.Patches;

public class FromProxyPatches {
	public static bool ConversionComplete = false;

	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyPrefix]
	static bool FromProxyPre(Dungeon __instance, DungeonProxy proxyDungeon, DungeonGenerator generator)
	{
		ConversionComplete = false;
		__instance.StartCoroutine(FromProxyEnumerator(generator, proxyDungeon, __instance));
		return false;
	}

	static IEnumerator FromProxyEnumerator(DungeonGenerator generator, DungeonProxy proxyDungeon, Dungeon __instance)
	{
		__instance.Clear();
		Dictionary<TileProxy, Tile> dictionary = new Dictionary<TileProxy, Tile>();

		var shouldSkip = typeof(DungeonGenerator).GetMethod("ShouldSkipFrame", BindingFlags.NonPublic | BindingFlags.Instance);

		foreach (TileProxy tile in proxyDungeon.AllTiles)
		{
			FromProxyIteration(__instance, dictionary, generator, tile);

			if((bool)shouldSkip.Invoke(generator, new object[] {false}))
				yield return null;
		}

		FromProxyEnd(__instance, proxyDungeon, generator, dictionary);
		ConversionComplete = true;
	}

	// Extracts the first for loop's contents from FromProxy
	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyReversePatch]
	static void FromProxyIteration(Dungeon __instance, Dictionary<TileProxy, Tile> dictionary, DungeonGenerator generator, TileProxy tile) {
		IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Loadstone.LogDebug("Attempting to reverse-patch Dungeon::FromProxy's first inner for loop");
			var matcher = new CodeMatcher(instructions, generator);

			var start = matcher
				.MatchForward(false,
						new CodeMatch(OpCodes.Br))
				.Advance(4)
				.Pos;

			matcher.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Stloc_2),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Stloc_0));

			var end = matcher
				.MatchForward(false,
						new CodeMatch(OpCodes.Endfinally),
						new CodeMatch(OpCodes.Ldloca_S),
						new CodeMatch(OpCodes.Call))
				.Advance(1)
				.Insert(new CodeInstruction(OpCodes.Ret))
				.Pos;

			matcher.CreateLabel(out Label endLabel);

			matcher
				.MatchBack(false,
					new CodeMatch(OpCodes.Brtrue),
					new CodeMatch(OpCodes.Leave))
				.Advance(1)
				.SetOperandAndAdvance(endLabel);

			var codeList = matcher.InstructionsInRange(start, end).AsEnumerable();

#if NIGHTLY
			if (LoadstoneConfig.ObjectPooling.Value)
				codeList = PoolingPatches.FromProxyPoolingPatch(codeList);
#endif

			Loadstone.LogDebug("Validating reverse-patched Dungeon::FromProxy's first inner for loop");
			return codeList;
		}

		// make compiler happy
		_ = StartTranspiler(null, null);
	}

	// Extracts the code in FromProxy after the first for loop
	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyReversePatch]
	static void FromProxyEnd(Dungeon __instance, DungeonProxy proxyDungeon, DungeonGenerator generator, Dictionary<TileProxy, Tile> dictionary) {
		IEnumerable<CodeInstruction> EndTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			Loadstone.LogDebug("Attempting to reverse-patch Dungeon::FromProxy's final code");
			var matcher = new CodeMatcher(instructions);

			var start = matcher
				.MatchForward(false,
						new CodeMatch(OpCodes.Endfinally))
				.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_1))
				.Pos;
	
			matcher.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Stloc_0),
					new CodeInstruction(OpCodes.Ldtoken, typeof(UnityEngine.GameObject)),
					new CodeInstruction(OpCodes.Pop));

			var end = matcher
				.End()
				.Pos;

			var codeList = matcher.InstructionsInRange(start, end);

			Loadstone.LogDebug("Validating reverse-patched Dungeon::FromProxy's final code");
			return codeList.AsEnumerable();
		}

		// make compiler happy
		_ = EndTranspiler(null);
	}

	// Injects a check to wait for FromProxy to finish executing before running PostProcess
	[HarmonyPatch(typeof(DungeonGenerator), "PostProcess", MethodType.Enumerator)]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> PostProcessPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.LogDebug($"Attempting to inject async check into DungeonGenerator::PostProcess");
		Type[] findWaitParams = { typeof(Func<bool>) };
		Type[] findFuncParams = { typeof(System.Object), typeof(IntPtr) };

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldnull),
					new CodeMatch(OpCodes.Stfld))
			.SetOpcodeAndAdvance(OpCodes.Nop) // Remove the ldnull while maintaining labels
			.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldloc_2),
					new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(typeof(FromProxyPatches), "PostProcessCheck")),
					new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Func<System.Boolean>), parameters: findFuncParams)),
					new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(UnityEngine.WaitUntil), parameters: findWaitParams)))
			.InstructionEnumeration();
		
		Loadstone.LogDebug($"Validating injected async check into DungeonGenerator::PostProcess");
		return newInstructions;
	}

	static bool PostProcessCheck()
	{
		return FromProxyPatches.ConversionComplete;
	}
}
