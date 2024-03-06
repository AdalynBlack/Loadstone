using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Loadstone.Config;
using Loadstone.Patches;
using Loadstone.Patches.ExpansionCore;
using System.Collections;

namespace Loadstone;

//   BepInEx
// Loadstone
[BepInPlugin("com.adibtw.loadstone", "Loadstone", "0.0.0")]
[BepInDependency("com.github.lethalmods.lethalexpansioncore", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
public class Loadstone : BaseUnityPlugin
{
	internal static ManualLogSource HarmonyLog;
	internal static ManualLogSource TranspilerLog;

	private void Awake()
	{
		Logger.LogDebug("Loading Loadstone...");

		Logger.LogDebug("Loading Configs...");
		LoadstoneConfig.BindAllTo(Config);

		// I prepended spaces because I hate having it right next to the colon in logs lol
		HarmonyLog = BepInEx.Logging.Logger.CreateLogSource(" Loadstone(Harmony)");
		TranspilerLog = BepInEx.Logging.Logger.CreateLogSource(" Loadstone(Transpiler)");

		Logger.LogDebug("Patching Methods...");
		Harmony.CreateAndPatchAll(typeof(StartOfRoundPatches));
		Harmony.CreateAndPatchAll(typeof(RoundManagerPatches));
		Harmony.CreateAndPatchAll(typeof(SpawnSyncedObjectPatches));
		Harmony.CreateAndPatchAll(typeof(DungeonPatches));
		Harmony.CreateAndPatchAll(typeof(DungeonGeneratorPatches));
		Harmony.CreateAndPatchAll(typeof(GenericPatches));

		CheckModded();

		Logger.LogInfo("Plugin Loadstone is loaded!");
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

		Harmony.CreateAndPatchAll(typeof(DungeonGenerator_PatchPatches));
	}
}

