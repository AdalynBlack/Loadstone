using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using System.Collections.Generic;

namespace Loadstone.Config;

internal static class LoadstoneDynamicConfig
{
	internal static void RegisterDynamicConfig()
	{
		AddConfigItems(new BaseConfigItem[] {
				new FloatSliderConfigItem(LoadstoneConfig.DungeonAsyncMaxTime, 
						new FloatSliderOptions {
							RequiresRestart = false,
							Min = 0f,
							Max = 1000f}),
				
				new BoolCheckBoxConfigItem(LoadstoneConfig.ShouldLoadingMusicPlay,
						new BoolCheckBoxOptions {RequiresRestart = false}),
				new BoolCheckBoxConfigItem(LoadstoneConfig.ShouldLoadingMusicLoop,
						new BoolCheckBoxOptions {RequiresRestart = false}),
				new FloatSliderConfigItem(LoadstoneConfig.LoadingMusicFadeTime, 
						new FloatSliderOptions {
							RequiresRestart = false,
							Min = 0f,
							Max = 30f}),
				new FloatSliderConfigItem(LoadstoneConfig.LoadingMusicVolume, 
						new FloatSliderOptions {
							RequiresRestart = false,
							Min = 0f,
							Max = 1.5f})
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
