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

	public static void BindAllTo(ConfigFile config)
	{
		LoadstoneFile = config;

		// Tweaks
		// 	Lethal
		PostLoadStartDelay = LoadstoneFile.Bind<float>(
				"Tweaks.Lethal.PostLoadStartDelay",
				"Post-load Start Delay",
				0f,
				new ConfigDescription(
					"How long to wait (in seconds) between all players loading in and starting the game. The vanilla value is 0.5",
					acceptableValues: new AcceptableValueRange<float>(0, 5)));
	
		PostGenerateSpawnDelay = LoadstoneFile.Bind<float>(
				"Tweaks.Lethal.PostGenerateSpawnDelay",
				"Post-generate Spawn Delay",
				0f,
				new ConfigDescription(
					"How long to wait (in seconds) between all players finishing level generation, and spawning in scrap and enemies. The vanilla value is 0.3",
					acceptableValues: new AcceptableValueRange<float>(0, 5)));

		//	Dungeon
		ShouldGenAsync = LoadstoneFile.Bind<bool>(
				"Tweaks.Dungeon.ShouldGenAsync",
				"Should Dungeon Generate Asynchronously",
				true,
				new ConfigDescription(
					"Whether or not the dungeon should generate asynchronously. The vanilla value is false"));

		DungeonAsyncMaxTime = LoadstoneFile.Bind<float>(
				"Tweaks.Dungeon.DungeonAsyncMaxTime",
				"Async Dungeon Generation Max Time per Frame",
				100f,
				new ConfigDescription(
					"How long to spend generating the dungeon each frame, in milliseconds. There is no vanilla value",
					acceptableValues: new AcceptableValueRange<float>(1, 1000)));
	}
}