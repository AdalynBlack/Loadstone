using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using System.Collections;
using System.Collections.Generic;

namespace Loadstone.Config;

internal static class LoadstoneDynamicConfig
{
	internal static void RegisterDynamicConfig()
	{
		AddConfigItems(new BaseConfigItem[] {
				new FloatSliderConfigItem(LoadstoneConfig.PostLoadStartDelay, 
						new FloatSliderOptions {
							RequiresRestart = false,
							Min = 0f,
							Max = 5f}),
				new FloatSliderConfigItem(LoadstoneConfig.PostGenerateSpawnDelay, 
						new FloatSliderOptions {
							RequiresRestart = false,
							Min = 0f,
							Max = 5f}),

				new BoolCheckBoxConfigItem(LoadstoneConfig.ShouldGenAsync,
						new BoolCheckBoxOptions {RequiresRestart = false}),
				new FloatSliderConfigItem(LoadstoneConfig.DungeonAsyncMaxTime, 
						new FloatSliderOptions {
							RequiresRestart = false,
							Min = 0f,
							Max = 1000f}),
				
				new BoolCheckBoxConfigItem(LoadstoneConfig.ShouldLoadingMusicPlay,
						new BoolCheckBoxOptions {RequiresRestart = false}),
				new FloatSliderConfigItem(LoadstoneConfig.LoadingMusicFadeTime, 
						new FloatSliderOptions {
							RequiresRestart = false,
							Min = 0f,
							Max = 30f})
				});

	}

	internal static void AddConfigItems(IEnumerable<BaseConfigItem> configItems)
	{
		foreach (var item in configItems)
		{
			LethalConfigManager.AddConfigItem(item);
		}
	}
}
