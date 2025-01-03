using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace Loadstone.Config;

public static class LoadstoneConfig
{
	public static int CurrentVersion = 1;

	public static ConfigFile LoadstoneFile;

	public static ConfigEntry<bool> AsyncDungeon;
	public static ConfigEntry<float> DungeonAsyncMaxTime;
	public static ConfigEntry<string> AsyncDungeonBlacklist;

	public static ConfigEntry<bool> AsyncNavmesh;

	public static ConfigEntry<bool> DungeonRealization;

	public static ConfigEntry<SeedDisplayType> SeedDisplayConfig;

	public static ConfigEntry<bool> StatusChangeFix;

#if NIGHTLY
	public static ConfigEntry<bool> ObjectPooling;
#endif

	public static ConfigEntry<bool> DunGenOptimizations;

	public static ConfigEntry<bool> LocalPerformanceReports;

	public static ConfigEntry<bool> ShouldLoadingMusicPlay;
	public static ConfigEntry<bool> ShouldLoadingMusicLoop;
	public static ConfigEntry<float> LoadingMusicFadeTime;
	public static ConfigEntry<float> LoadingMusicVolume;

	public static ConfigEntry<int> LastConfigVersion;


	public enum SeedDisplayType
	{
		Popup,
		Darken,
		JustLog
	}

	public static void BindAllTo(ConfigFile config)
	{
		LoadstoneFile = config;

		// Async Dungeon
		AsyncDungeon = LoadstoneFile.Bind<bool>(
				"AsyncDungeon",
				"Enabled",
				true,
				"Whether or not the dungeon should generate asynchronously. The vanilla value is false. This option requires StatusChangeFix to be enabled");

		DungeonAsyncMaxTime = LoadstoneFile.Bind<float>(
				"AsyncDungeon",
				"Dungeon Target Frametime",
				20f,
				new ConfigDescription(
					"How long to spend generating the dungeon each frame, in milliseconds. There is no vanilla value",
					acceptableValues: new AcceptableValueRange<float>(1, 1000)));

		AsyncDungeonBlacklist = LoadstoneFile.Bind<string>(
				"AsyncDungeon",
				"Async Dungeon Blacklist",
				"",
				"A comma-separated list of interiors which should not be processed asynchrounously");

		// Async Navmesh
		AsyncNavmesh = LoadstoneFile.Bind<bool>(
				"AsyncNavmesh",
				"Enabled",
				true,
				"Whether or not the navmesh should be generated asynchrounously. The vanilla value is false");

		// Dungeon Realization
		DungeonRealization = LoadstoneFile.Bind<bool>(
				"DungeonRealization",
				"Spread Over Multiple Frames",
				true,
				"Whether or not to spread dungeon realization over multiple frames. The vanilla value is false");

		// Screen Darkening
		SeedDisplayConfig = LoadstoneFile.Bind<SeedDisplayType>(
				"ScreenDarkening",
				"Seed Display Type",
				SeedDisplayType.Popup,
				"Decides how the random seed should appear when loading into a level. The vanilla value is \"Darken\"");
		
		// Status Change Fix
		StatusChangeFix = LoadstoneFile.Bind<bool>(
				"StatusChangeFix",
				"Enabled",
				true,
				"Enables a fix for the game's status change callback, which is non-functional in vanilla. The vanilla value is false");
		
		// DunGen Optimizations
		DunGenOptimizations = LoadstoneFile.Bind<bool>(
				"DunGenOptimizations",
				"Enabled",
				true,
				"Enables a number of optmizations for DunGen's dungeon generator");

#if NIGHTLY
		// Object Pooling
		ObjectPooling = LoadstoneFile.Bind<bool>(
				"ObjectPooling",
				"Enabled",
				false,
				"!!! EXPERIMENTAL FEATURE !!!\nEnables object pooling for dungeon spawning and certain parts of initial level generation. This can greatly improve load times, but may increase ram usage in modpacks with many custom interiors. This feature is currently very experimental. The vanilla value is false");
#endif

		// LocalPerformanceReports
		LocalPerformanceReports = LoadstoneFile.Bind<bool>(
				"LocalPerformanceReports",
				"Enabled",
				false,
				"Enables local performance reports, which will appear in the logs every time the ship lands");


		// LoadingMusic
		ShouldLoadingMusicPlay = LoadstoneFile.Bind<bool>(
				"LoadingMusic",
				"Should Loading Music Play",
				false,
				new ConfigDescription(
					"Should we play loading music as the level loads in?"));

		ShouldLoadingMusicLoop = LoadstoneFile.Bind<bool>(
				"LoadingMusic",
				"Should Loading Music Loop",
				true,
				new ConfigDescription(
					"Should the loading music loop?"));

		LoadingMusicFadeTime = LoadstoneFile.Bind<float>(
				"LoadingMusic",
				"Loading Music Fade Time",
				15f,
				new ConfigDescription(
					"How long should it take for the loading music to fade out",
					acceptableValues: new AcceptableValueRange<float>(0, 30)));

		LoadingMusicVolume = LoadstoneFile.Bind<float>(
				"LoadingMusic",
				"Loading Music Volume",
				0.75f,
				new ConfigDescription(
					"The volume of the loading music",
					acceptableValues: new AcceptableValueRange<float>(0, 1.5f)));

		LastConfigVersion = LoadstoneFile.Bind<int>(
				"Debug",
				"Last Config Version",
				0,
				new ConfigDescription(
					"The version when this config was last saved. Used for config migrations"));

		MigrateConfigs();
	}

	/* Migration code borrowed from LC_OpenBodyCams */
	private static void MigrateConfigs() {
		if (null == LoadstoneFile.OrphanedEntries)
		{
			Loadstone.LogError("Failed to migrate config, orphaned entries property was not found.");
			return;
		}

		foreach (var orphan in LoadstoneFile.OrphanedEntries.Keys)
		{
			Loadstone.LogDebug($"{orphan}");
		}

		if (LastConfigVersion.Value > CurrentVersion)
		{
			Loadstone.LogWarning("Previous config version is newer than current. Did you downgrade your mod?");
			return;
		}

		if (LastConfigVersion.Value == CurrentVersion)
			return;

		MigrateVersion(0, new ConfigDefinition("LCSoundTool", "Should Loading Music Play"), ShouldLoadingMusicPlay);
		MigrateVersion(0, new ConfigDefinition("LCSoundTool", "Loading Music Fade Time"), LoadingMusicFadeTime);
		MigrateVersion(0, new ConfigDefinition("LCSoundTool", "Loading Music Volume"), LoadingMusicVolume);
		LastConfigVersion.Value = 1;
	}

	private static void MigrateVersion<T>(int fromVersion, ConfigDefinition oldConfigDefinition, ConfigEntry<T> newConfig) {
		var orphans = LoadstoneFile.OrphanedEntries;
		if (LastConfigVersion.Value <= fromVersion && orphans.TryGetValue(oldConfigDefinition, out var oldConfig))
		{
			Loadstone.LogInfo($"Migrating '{oldConfigDefinition}' to '{newConfig.Definition}' from v{fromVersion}");
			newConfig.Value = TomlTypeConverter.ConvertToValue<T>(oldConfig);
			orphans.Remove(oldConfigDefinition);
		}
	}
}
