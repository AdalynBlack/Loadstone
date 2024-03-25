using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace Loadstone.Config;

public static class LoadstoneConfig
{
	public static ConfigFile LoadstoneFile;

	public static ConfigEntry<float> PostLoadStartDelay;
	public static ConfigEntry<float> PostGenerateSpawnDelay;

	public static ConfigEntry<bool> ShouldGenAsync;
	public static ConfigEntry<float> DungeonAsyncMaxTime;

	public static ConfigEntry<bool> ShouldLoadingMusicPlay;
	public static ConfigEntry<float> LoadingMusicFadeTime;
	public static ConfigEntry<float> LoadingMusicVolume;

	public static void BindAllTo(ConfigFile config)
	{
		LoadstoneFile = config;

		// Tweaks
		// 	Lethal
		PostLoadStartDelay = LoadstoneFile.Bind<float>(
				"Tweaks.Lethal",
				"Post-load Start Delay",
				0f,
				new ConfigDescription(
					"How long to wait (in seconds) between all players loading in and starting the game. The vanilla value is 0.5",
					acceptableValues: new AcceptableValueRange<float>(0, 5)));
	
		PostGenerateSpawnDelay = LoadstoneFile.Bind<float>(
				"Tweaks.Lethal",
				"Post-generate Spawn Delay",
				0f,
				new ConfigDescription(
					"How long to wait (in seconds) between all players finishing level generation, and spawning in scrap and enemies. The vanilla value is 0.3",
					acceptableValues: new AcceptableValueRange<float>(0, 5)));

		//	Dungeon
		ShouldGenAsync = LoadstoneFile.Bind<bool>(
				"Tweaks.Dungeon",
				"Asynchronous Generation",
				true,
				new ConfigDescription(
					"Whether or not the dungeon should generate asynchronously. The vanilla value is false"));

		DungeonAsyncMaxTime = LoadstoneFile.Bind<float>(
				"Tweaks.Dungeon",
				"Async Gen Wait Time",
				30f,
				new ConfigDescription(
					"How long to spend generating the dungeon each frame, in milliseconds. There is no vanilla value",
					acceptableValues: new AcceptableValueRange<float>(1, 1000)));

		//	LCSoundTool
		ShouldLoadingMusicPlay = LoadstoneFile.Bind<bool>(
				"Tweaks.LCSoundTool",
				"Should Loading Music Play",
				false,
				new ConfigDescription(
					"Should we play loading music as the level loads in? Requires LCSoundTool to be installed"));

		LoadingMusicFadeTime = LoadstoneFile.Bind<float>(
				"Tweaks.LCSoundTool",
				"Loading Music Fade Time",
				15f,
				new ConfigDescription(
					"How long should it take for the loading music to fade out",
					acceptableValues: new AcceptableValueRange<float>(0, 30)));

		LoadingMusicVolume = LoadstoneFile.Bind<float>(
				"Tweaks.LCSoundTool",
				"Loading Music Volume",
				0.75f,
				new ConfigDescription(
					"The volume of the loading music",
					acceptableValues: new AcceptableValueRange<float>(0, 1.5)));


		TryRemoveOldEntries();
	}

	private static void TryRemoveOldEntries()
	{
		var OldPostLoadStartDelay = LoadstoneFile.Bind<float>(
				"Tweaks.Lethal.PostLoadStartDelay",
				"Post-load Start Delay",
				PostLoadStartDelay.Value,
				new ConfigDescription(""));
		
		var OldPostGenerateSpawnDelay = LoadstoneFile.Bind<float>(
				"Tweaks.Lethal.PostGenerateSpawnDelay",
				"Post-generate Spawn Delay",
				PostGenerateSpawnDelay.Value,
				new ConfigDescription(""));

		var OldShouldGenAsync = LoadstoneFile.Bind<bool>(
				"Tweaks.Dungeon.ShouldGenAsync",
				"Should Dungeon Generate Asynchronously",
				ShouldGenAsync.Value,
				new ConfigDescription(""));

		var OldDungeonAsyncMaxTime = LoadstoneFile.Bind<float>(
				"Tweaks.Dungeon.DungeonAsyncMaxTime",
				"Async Dungeon Generation Max Time per Frame",
				DungeonAsyncMaxTime.Value,
				new ConfigDescription(""));

		PostLoadStartDelay.Value = OldPostLoadStartDelay.Value;
		PostGenerateSpawnDelay.Value = OldPostGenerateSpawnDelay.Value;
		ShouldGenAsync.Value = OldShouldGenAsync.Value;
		DungeonAsyncMaxTime.Value = OldDungeonAsyncMaxTime.Value == 100f ? 30f : OldDungeonAsyncMaxTime.Value;

		LoadstoneFile.Remove(OldPostLoadStartDelay.Definition);
		LoadstoneFile.Remove(OldPostGenerateSpawnDelay.Definition);
		LoadstoneFile.Remove(OldShouldGenAsync.Definition);
		LoadstoneFile.Remove(OldDungeonAsyncMaxTime.Definition);

		LoadstoneFile.Save();
	}
}
