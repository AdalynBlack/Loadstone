using DunGen;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Loadstone.Patches;

public class DungeonPatches {
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

	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyReversePatch]
	static void FromProxyIteration(Dungeon __instance, Dictionary<TileProxy, Tile> dictionary, DungeonGenerator generator, TileProxy tile) {
		IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Loadstone.TranspilerLog.LogDebug("Attempting to reverse-patch Dungeon::FromProxy's first inner for loop");
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

			matcher.Advance(-1).CreateLabel(out Label endLabel);

			matcher
				.MatchBack(false,
					new CodeMatch(OpCodes.Leave))
				.SetOperandAndAdvance(endLabel);

			var codeList = matcher.InstructionsInRange(start, end);

			Loadstone.TranspilerLog.LogDebug("Validating reverse-patched Dungeon::FromProxy's first inner for loop");
			return codeList.AsEnumerable();
		}

		// make compiler happy
		_ = StartTranspiler(null, null);
	}

	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyReversePatch]
	static void FromProxyEnd(Dungeon __instance, DungeonProxy proxyDungeon, DungeonGenerator generator, Dictionary<TileProxy, Tile> dictionary) {
		IEnumerable<CodeInstruction> EndTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			Loadstone.TranspilerLog.LogDebug("Attempting to reverse-patch Dungeon::FromProxy's final code");
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

			Loadstone.TranspilerLog.LogDebug("Validating reverse-patched Dungeon::FromProxy's final code");
			return codeList.AsEnumerable();
		}

		// make compiler happy
		_ = EndTranspiler(null);
	}
}
