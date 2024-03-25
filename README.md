# Loadstone
Reduces the stuttering experienced while loading levels, and will reduce or entirely remove the amount of time that the voicechat is unable to function for.

## Warning
This mod is still in early development, and as such might be incompatible with other mods, and could potentially cause crashes. If any issue of this sort happens, please submit an issue report to the mod's [github page](https://github.com/AdalynBlack/Loadstone/issues).

## Compatibility
This mod aims to be fully compatible with vanilla, and to have compatibility with as many other mods as possible. The following mods should run without errors:

- BiggerLobby
- MoreCompany
- LethalExpansion
- LethalExpansionCore
- LethalLevelLoader

This is not a comprehensive list, but rather, those mods should work without issue, and if they don't I will see to it that they work as soon as possible. Other popular mods like Skinwalkers, Ship Loot, HelmetCams, Mimics, Reserved Slot, Lethal Things, and more should also work just fine, but less commonly used mods aren't guaranteed to be compatible. If there's a mod compatibility issue, check the [current issues](https://github.com/AdalynBlack/Loadstone/issues) to see if the issue has already been reported, and if not, feel free to report the issue. (It's very helpful if you can provide game logs and a mod list, as well as a profile code so that I can test and fix the issue as quickly as possible)

## Configuring
This mod provides a few config options that can be used to customize exactly how loading happens. The default config is optimized to allow voicechat to work for the entire time spent loading in, while spending as little time actually loading as possible. If loading fails in some capacity, the provided config options should hopefully allow for the issue to be resolved.

All config options can be modified in-game without restarting through LethalConfig if it is installed.

- Tweaks
  - Dungeon
    - Asynchronous Generation
      - Toggles Loadstone's forced async level generation. This is on by default, and if turned off *will* cause stuttering as the level generates. The vanilla game sets async generation to be off by default, and as such, most issues with level loading should be able to be fixed by disabling this option. The mod still improves performance in other places, but a lot of the performance uplift does come from this one option.
    - Async Gen Wait Time
      - This config option tells the game's map generator how long it's allowed to spend each frame generating the level. Increasing this will reduce your framerate while loading, but allow for faster load times, while decreasing it will increase your framerate while loading, at the cost of longer load times. I hope to eventually convert the level generator to be truly asynchronous so that map generation can happen at full speed without compromising framerates, but that will require a deep rework of existing systems.
  - Lethal
    - Post-load Start Delay
      - This value controls a tiemr that decides how long to wait between all clients successfully loading the moon, and starting the process of generating the map layout. I'm not fully sure what the intended purpose of this is, but the vanilla value is 0.5, while the mod sets it to 0 by default.
    - Post-generate Spawn Delay
      - This value controls a timer that decides how long to wait between all clients having generated the level, and deciding to actually spawn in scrap and enemies. I'm not fully sure what the intended purpose of this is, but the vanilla value is 0.3, while the mod sets it to 0 by default.
  - LCSoundTool
    - Should Loading Music Play
      - Enable this if you wish to have music play while levels load in
    - Loading Music Fade Time
      - Determines how long it will take for the music to fully fade out once the level finishes loading. The default value of 15 seconds is intended to make the music fade out the moment you land on the ground
    - Loading Music Volume
      - How loud the loading music should be
