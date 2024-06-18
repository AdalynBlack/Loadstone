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
		if (tileA.Tags.Tags.Count != 0 && tileB.Tags.Tags.Count != 0)
		{
			__result = false;
			return false;
		}

		__result = TagMatchDictionary[__instance][tileA][tileB];
		return false;
	}

	internal static Dictionary<DungeonFlow, Dictionary<Tile, Dictionary<Tile, bool>>> TagMatchDictionary;

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

	[HarmonyPatch(typeof(RoundManager), "Awake")]
	[HarmonyPrefix]
	static void TileTagPrecalcPatch()
	{
		var flows = Resources.FindObjectsOfTypeAll<DungeonFlow>();
		TagMatchDictionary = new Dictionary<DungeonFlow, Dictionary<Tile, Dictionary<Tile, bool>>>();

		foreach (var flow in flows)
		{
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
		}
	}
}
