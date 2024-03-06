using HarmonyLib;
using System.Reflection;

namespace Loadstone.Patches;

public class GenericPatches
{
	//[HarmonyPatch(typeof(RoundManager), "<LoadNewLevelWait>b__115_0")]
	//[HarmonyPatch(typeof(RoundManager), "<LoadNewLevelWait>b__115_1")]
	/*[HarmonyPrefix]
	static bool ForceTrue(ref bool __result, MethodBase __originalMethod)
	{
		Loadstone.HarmonyLog.LogDebug($"Forced \"{__originalMethod.Name}\" to return without waiting");
		__result = true;
		return false;
	}*/
}
