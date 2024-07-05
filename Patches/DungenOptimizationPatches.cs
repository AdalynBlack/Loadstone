using DunGen;
using DunGen.Graph;
using HarmonyLib;
using Loadstone.Config;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace Loadstone.Patches;

public class DungenOptimizationPatches
{
	[HarmonyPatch(typeof(DungeonFlow), "HasMatchingTagPair")]
	[HarmonyPrefix]
	static bool HasMatchingTagPairEarlyOut(DungeonFlow __instance, Tile tileA, Tile tileB, ref bool __result)
	{
		if (tileA.Tags.Tags.Count == 0 || tileB.Tags.Tags.Count == 0)
		{
			__result = false;
			return false;
		}

		try {
			__result = DungeonTagMatchTemp[tileA][tileB];
		} catch (KeyNotFoundException e) {
			Loadstone.HarmonyLog.LogError($"A tile was not found in the tile tag cache, and is now being cached: {e}");

			if (!TagMatchDictionary.ContainsKey(__instance))
			{
				TagMatchDictionary[__instance] = new Dictionary<Tile, Dictionary<Tile, bool>>();
				DungeonTagMatchTemp = TagMatchDictionary[__instance];
			}
			if (!DungeonTagMatchTemp.ContainsKey(tileA))
				DungeonTagMatchTemp[tileA] = new Dictionary<Tile, bool>();
			if (!DungeonTagMatchTemp.ContainsKey(tileB))
				DungeonTagMatchTemp[tileB] = new Dictionary<Tile, bool>();

			DungeonTagMatchTemp[tileA][tileB] = HasMatchingTagPairOriginal(__instance, tileA, tileB);
			DungeonTagMatchTemp[tileB][tileA] = HasMatchingTagPairOriginal(__instance, tileB, tileA);
		}

		return false;
	}

	internal static Dictionary<Tile, Dictionary<Tile, bool>> DungeonTagMatchTemp = null;
	internal static Dictionary<DungeonFlow, Dictionary<Tile, Dictionary<Tile, bool>>> TagMatchDictionary = new Dictionary<DungeonFlow, Dictionary<Tile, Dictionary<Tile, bool>>>();

	// Extracts the original code for HasMatchingTagPair so we don't use the overridden code
	[HarmonyPatch(typeof(DungeonFlow), "HasMatchingTagPair")]
	[HarmonyReversePatch]
	static bool HasMatchingTagPairOriginal(DungeonFlow flow, Tile tileA, Tile tileB) => throw new NotImplementedException("Reverse Patch Stub");

	static void GenerateTileHashSet(ref HashSet<Tile> tiles, List<TileSet> tileSets)
	{
		foreach (var tileSet in tileSets)
		{
			foreach (var tileChance in tileSet.TileWeights.Weights)
			{
				var tile = tileChance.Value.GetComponent<Tile>();

				if (tile == null)
					continue;

				tiles.Add(tile);
			}
		}
	}

	static Dictionary<Tile, Dictionary<Tile, bool>> TileConnectionTagOptimization(HashSet<Tile> tiles, DungeonFlow flow)
	{
		var flowTagMatchDict = new Dictionary<Tile, Dictionary<Tile, bool>>();

		foreach (var tileA in tiles)
		{
			var tileADict = new Dictionary<Tile, bool>();
			foreach (var tileB in tiles)
			{
				tileADict.Add(tileB, HasMatchingTagPairOriginal(flow, tileA, tileB));
			}
			flowTagMatchDict.Add(tileA, tileADict);
		}

		return flowTagMatchDict;
	}

	[HarmonyPatch(typeof(DungeonGenerator), "Generate")]
	[HarmonyPrefix]
	static void TileTagPrecalcPatch(DungeonGenerator __instance)
	{
		var flow = __instance.DungeonFlow;

		if (TagMatchDictionary.ContainsKey(flow))
			return;

		HashSet<Tile> tiles = new HashSet<Tile>();

		foreach (var node in flow.Nodes)
		{
			GenerateTileHashSet(ref tiles, node.TileSets);
		}

		foreach (var line in flow.Lines)
		{
			foreach (var archetype in line.DungeonArchetypes)
			{
				GenerateTileHashSet(ref tiles, archetype.TileSets);
				GenerateTileHashSet(ref tiles, archetype.BranchCapTileSets);
			}
		}

		TagMatchDictionary.Add(flow, TileConnectionTagOptimization(tiles, flow));
		DungeonTagMatchTemp = TagMatchDictionary[flow];
	}
}
