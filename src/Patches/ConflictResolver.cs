using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Loadstone.Patches;

public static class ConflictResolver
{
	private static Harmony HarmonyInstance;

	static ConflictResolver()
	{
		HarmonyInstance = new Harmony(PluginInfo.PLUGIN_GUID);
	}
	
	public static void CheckUnknownPatches()
	{
		IEnumerable<MethodBase> patchedMethods = PatchManager.GetPatchedMethods();

		foreach (MethodBase method in patchedMethods)
		{
			PatchInfo info = PatchManager.GetPatchInfo(method);

			info.transpilers.Where(patch => !IsPatchOurs(patch, method)).ToList().ForEach(patch => WarnPatch(patch, method, "transpiler"));
			HasOurs = false;

			info.ilmanipulators.Where(patch => !IsPatchOurs(patch, method)).ToList().ForEach(patch => WarnPatch(patch, method, "IL manipulator"));
			HasOurs = false;

			info.prefixes.Where(patch => !IsPatchOurs(patch, method)).ToList().ForEach(patch => WarnPatch(patch, method, "prefix"));
			HasOurs = false;

			info.postfixes.Where(patch => !IsPatchOurs(patch, method)).ToList().ForEach(patch => WarnPatch(patch, method, "postfix"));
			HasOurs = false;

			info.finalizers.Where(patch => !IsPatchOurs(patch, method)).ToList().ForEach(patch => WarnPatch(patch, method, "finalizer"));
			HasOurs = false;
		}
	}

	static bool HasOurs = false;

	public static bool IsPatchOurs(Patch patch, MethodBase method)
	{
		string name = patch.GetMethod(method).DeclaringType.Assembly.GetName().Name;
		HasOurs = HasOurs || name == "com.adibtw.loadstone";
		return name == "com.adibtw.loadstone";
	}
	
	public static void WarnPatch(Patch patch, MethodBase method, string patchType)
	{
		if (HasOurs)
			Loadstone.LogWarning($"The assembly \"{patch.GetMethod(method).DeclaringType.Assembly.GetName().Name}\" has patched the method \"{method.ToString()}\" with a {patchType} using \"{patch.GetMethod(method).ToString()}\", which we also modify. Unexpected behaviour may occur");
	}

	public static void TryPatch(Type type)
	{
		try {
			HarmonyInstance.CreateClassProcessor(type, true).Patch();
		} catch (Exception exception) {
			Loadstone.LogFatal($"Loadstone failed to patch {type}. The following exception was received:\n{exception.ToString()}");
		}
	}
}
