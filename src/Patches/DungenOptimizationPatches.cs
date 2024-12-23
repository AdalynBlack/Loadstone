using DunGen;
using DunGen.Graph;
using HarmonyLib;
using Loadstone.Config;
using System;
using System.Collections.Generic;
using System.Linq;
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
		} catch (KeyNotFoundException) {
			Loadstone.LogWarning($"The tile pair of \"{tileA.name}\" and \"{tileB.name}\" was not found in the tag cache! This pair is now being cached, which will cause a small performance penalty");

			if (!TagMatchDictionary.ContainsKey(__instance))
			{
				TagMatchDictionary[__instance] = new Dictionary<Tile, Dictionary<Tile, bool>>();
				DungeonTagMatchTemp = TagMatchDictionary[__instance];
			}
			if (!DungeonTagMatchTemp.ContainsKey(tileA))
				DungeonTagMatchTemp[tileA] = new Dictionary<Tile, bool>();
			if (!DungeonTagMatchTemp.ContainsKey(tileB))
				DungeonTagMatchTemp[tileB] = new Dictionary<Tile, bool>();

			__result = HasMatchingTagPairOriginal(__instance, tileA, tileB);
			DungeonTagMatchTemp[tileA][tileB] = __result;
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

	public static List<Func<DungeonGenerator, bool>> cacheValidators =
		new List<Func<DungeonGenerator, bool>>{ generator => {
				var flow = generator.DungeonFlow;

				if (!TagMatchDictionary.ContainsKey(flow))
					return false;

				if (!TagMatchDictionary[flow].Values.Any(tile => tile == null))
					return true;

				Loadstone.LogWarning($"At least one tile in {flow.name} has been deleted since the flow was last cached! The cache will be fully recalculated as a result");
				return false;
			}};

	public static List<Func<DungeonGenerator, HashSet<Tile>>> tileCollectors =
		new List<Func<DungeonGenerator, HashSet<Tile>>>{ generator => {
				var flow = generator.DungeonFlow;

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

				return tiles;
			}};

	[HarmonyPriority(Priority.VeryLow)]
	[HarmonyPatch(typeof(DungeonGenerator), "Generate")]
	[HarmonyPrefix]
	static void TileTagPrecalcPatch(DungeonGenerator __instance)
	{
		var flow = __instance.DungeonFlow;
		
		if (!flow) {
			Loadstone.LogWarning("The dungeon generator's flow is null or deleted!");
			return;
		}

		var cacheValidity = cacheValidators.All(v => v(__instance));

		if (cacheValidity)
			return;

		HashSet<Tile> tiles = new HashSet<Tile>();

		tileCollectors.ForEach(collector => {
			tiles.UnionWith(collector(__instance));
		});

		TagMatchDictionary.Add(flow, TileConnectionTagOptimization(tiles, flow));
		DungeonTagMatchTemp = TagMatchDictionary[flow];
	}
}
