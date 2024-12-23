using HarmonyLib;
using LethalExpansionCore.Patches;

namespace Loadstone.Patches.ExpansionCore;

public class DungeonGenerator_PatchPatches
{
	// Disable the generate postfix step used by LethalExpansionCore
	[HarmonyPatch(typeof(DungeonGenerator_Patch), "Generate_Postfix")]
	[HarmonyPrefix]
	static bool Generate_PostfixPrefix()
	{
		return false;
	}
}
