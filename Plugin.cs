using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Loadstone.Config;
using Loadstone.Patches;
using Loadstone.Patches.ExpansionCore;
using System;
using System.Collections;

#if DEBUG
using Loadstone.Patches.Debug;
#endif

namespace Loadstone;

//   BepInEx
// Loadstone
[BepInPlugin("com.adibtw.loadstone", "Loadstone", "0.0.2")]
[BepInDependency("com.github.lethalmods.lethalexpansioncore", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
public class Loadstone : BaseUnityPlugin
{
	internal static ManualLogSource HarmonyLog;
	internal static ManualLogSource TranspilerLog;

	private void Awake()
	{
		Logger.LogDebug("Loading Loadstone...");

		// I prepended spaces because I hate having it right next to the colon in logs lol
		HarmonyLog = BepInEx.Logging.Logger.CreateLogSource(" Loadstone(Harmony)");
		TranspilerLog = BepInEx.Logging.Logger.CreateLogSource(" Loadstone(Transpiler)");

		Logger.LogDebug("Loading Configs...");
		LoadstoneConfig.BindAllTo(Config);

		Logger.LogDebug("Patching Methods...");
		TryPatch(typeof(StartOfRoundPatches));
		TryPatch(typeof(RoundManagerPatches));
		TryPatch(typeof(DungeonPatches));
		TryPatch(typeof(DungeonGeneratorPatches));

		CheckModded();

#if DEBUG
		Logger.LogDebug("Patching in profiler...");
		TryPatch(typeof(ProfilingPatches));
#endif

		Logger.LogInfo("Plugin Loadstone is loaded!");
	}

	private void TryPatch(Type type)
	{
		try {
			Harmony.CreateAndPatchAll(type);
		} catch (Exception exception) {
			Logger.LogFatal($"Loadstone failed to patch {type}. The following exception was received:\n{exception.ToString()}");
		}
	}

	private void CheckModded()
	{
		OptionalModPatcher();
		Logger.LogInfo("Loadstone modded compat is loaded!");
	}

	private void OptionalModPatcher() {
		var pluginNames = Chainloader.PluginInfos.Keys;
		
		foreach (var name in pluginNames) {
			switch(name) {
				case "com.github.lethalmods.lethalexpansioncore":
					PatchExpansionCore();
					break;
				case "ainavt.lc.lethalconfig":
					LoadstoneDynamicConfig.RegisterDynamicConfig();
					break;
			}
		}
	}

	private void PatchExpansionCore() {
		Logger.LogDebug("Patching ExpansionCore");

		TryPatch(typeof(DungeonGenerator_PatchPatches));
	}
}

