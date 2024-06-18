using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Loadstone.Config;
using Loadstone.Patches;
using Loadstone.Patches.ExpansionCore;
using Loadstone.Patches.LCSoundTool;
using System;
using System.Collections;

namespace Loadstone;

//   BepInEx
// Loadstone
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.github.lethalmods.lethalexpansioncore", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("LCSoundTool", BepInDependency.DependencyFlags.SoftDependency)]
public class Loadstone : BaseUnityPlugin
{
	internal static ManualLogSource HarmonyLog;
	internal static ManualLogSource TranspilerLog;

	private void Awake()
	{
		Logger.LogDebug("Loading Loadstone...");

		// I prepended spaces because I hate having it right next to the colon in logs lol
		HarmonyLog = BepInEx.Logging.Logger.CreateLogSource($" {PluginInfo.PLUGIN_NAME}(Harmony)");
		TranspilerLog = BepInEx.Logging.Logger.CreateLogSource($" {PluginInfo.PLUGIN_NAME}(Transpiler)");

		Logger.LogDebug("Loading Configs...");
		LoadstoneConfig.BindAllTo(Config);

		Logger.LogDebug("Patching Methods...");

		if (LoadstoneConfig.StatusChangeFix.Value)
		{
			ConflictResolver.TryPatch(typeof(StatusChangedFixer));

			if (LoadstoneConfig.AsyncDungeon.Value)
			{
				ConflictResolver.TryPatch(typeof(AsyncDungeonPatches));

				if (LoadstoneConfig.DungeonRealization.Value)
					ConflictResolver.TryPatch(typeof(FromProxyPatches));
			}
		}

		if (LoadstoneConfig.AsyncNavmesh.Value)
			ConflictResolver.TryPatch(typeof(NavmeshPatches));
		ConflictResolver.TryPatch(typeof(ScreenDarkenPatches));
		ConflictResolver.TryPatch(typeof(ObjectFindPatches));

		if (LoadstoneConfig.ObjectPooling.Value)
			ConflictResolver.TryPatch(typeof(PoolingPatches));
		ConflictResolver.TryPatch(typeof(DungenOptimizationPatches));

		CheckModded();

		Logger.LogInfo("Plugin Loadstone is loaded!");
	}

	private void Start()
	{
		ConflictResolver.CheckUnknownPatches();
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
				case "LCSoundTool":
					PatchLCSoundTool();
					break;
				case "ainavt.lc.lethalconfig":
					LoadstoneDynamicConfig.RegisterDynamicConfig();
					break;
			}
		}
	}

	private void PatchExpansionCore() {
		Logger.LogDebug("Patching ExpansionCore");

		ConflictResolver.TryPatch(typeof(DungeonGenerator_PatchPatches));
	}

	private void PatchLCSoundTool() {
		Logger.LogDebug("Patching with LCSoundTool");

		ConflictResolver.TryPatch(typeof(RoundManagerMusicPatches));
	}
}

