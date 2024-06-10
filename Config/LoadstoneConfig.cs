using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace Loadstone.Config;

public static class LoadstoneConfig
{
	public static ConfigFile LoadstoneFile;

	public static ConfigEntry<SeedDisplayType> SeedDisplayConfig;

	public static ConfigEntry<bool> ShouldGenAsync;
	public static ConfigEntry<float> DungeonAsyncMaxTime;

	public static ConfigEntry<bool> ShouldLoadingMusicPlay;
	public static ConfigEntry<float> LoadingMusicFadeTime;
	public static ConfigEntry<float> LoadingMusicVolume;

	public enum SeedDisplayType
	{
		Popup,
		Darken,
		JustLog
	}

	public static void BindAllTo(ConfigFile config)
	{
		LoadstoneFile = config;

		// Tweaks
		// 	Lethal
		SeedDisplayConfig = LoadstoneFile.Bind<SeedDisplayType>(
				"Tweaks.Lethal",
				"Seed Display Type",
				SeedDisplayType.Popup,
				"Decides how the random seed should appear when loading into a level. The vanilla value is \"Darken\"");

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
					acceptableValues: new AcceptableValueRange<float>(0, 1.5f)));
	}
}
