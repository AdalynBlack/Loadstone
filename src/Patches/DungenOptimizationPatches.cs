using DunGen;
using DunGen.Tags;
using DunGen.Graph;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

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
			__result = DungeonTagMatchTemp[tileA.Tags][tileB.Tags];
		} catch (KeyNotFoundException) {
			Loadstone.LogWarning($"Tag pair for \"{tileA.name}\" and \"{tileB.name}\" was not found in the tag cache! This pair is now being cached, which will cause a small performance penalty");

			if (!TagMatchDictionary.ContainsKey(__instance))
			{
				TagMatchDictionary[__instance] = new Dictionary<TagContainer, Dictionary<TagContainer, bool>>();
				DungeonTagMatchTemp = TagMatchDictionary[__instance];
			}
			if (!DungeonTagMatchTemp.ContainsKey(tileA.Tags))
				DungeonTagMatchTemp[tileA.Tags] = new Dictionary<TagContainer, bool>();
			if (!DungeonTagMatchTemp.ContainsKey(tileB.Tags))
				DungeonTagMatchTemp[tileB.Tags] = new Dictionary<TagContainer, bool>();

			__result = HasMatchingTagPairOriginal(__instance, tileA, tileB);
			DungeonTagMatchTemp[tileA.Tags][tileB.Tags] = __result;
			DungeonTagMatchTemp[tileB.Tags][tileA.Tags] = HasMatchingTagPairOriginal(__instance, tileB, tileA);
		}

		return false;
	}

	internal static Dictionary<TagContainer, Dictionary<TagContainer, bool>> DungeonTagMatchTemp = null;
	internal static Dictionary<DungeonFlow, Dictionary<TagContainer, Dictionary<TagContainer, bool>>> TagMatchDictionary = new Dictionary<DungeonFlow, Dictionary<TagContainer, Dictionary<TagContainer, bool>>>();

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

	static Dictionary<TagContainer, Dictionary<TagContainer, bool>> TileConnectionTagOptimization(HashSet<Tile> tiles, DungeonFlow flow)
	{
		var flowTagMatchDict = new Dictionary<TagContainer, Dictionary<TagContainer, bool>>();

		foreach (var tileA in tiles)
		{
			var tagsADict = new Dictionary<TagContainer, bool>();
			foreach (var tileB in tiles)
			{
				tagsADict.Add(tileB.Tags, HasMatchingTagPairOriginal(flow, tileA, tileB));
			}
			flowTagMatchDict.Add(tileA.Tags, tagsADict);
		}

		return flowTagMatchDict;
	}

	public static List<Func<DungeonGenerator, bool>> cacheValidators =
		new List<Func<DungeonGenerator, bool>>{ generator => {
				var flow = generator.DungeonFlow;

				if (!TagMatchDictionary.ContainsKey(flow))
					return false;

				if (!TagMatchDictionary[flow].Values.Any(tag => tag == null))
					return true;

				Loadstone.LogWarning($"At least one tag container in {flow.name} has been deleted since the flow was last cached! The cache will be fully recalculated as a result");
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
	static void TagPrecalcPatch(DungeonGenerator __instance)
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
