# Loadstone
Reduces the stuttering experienced while loading levels, and will reduce or entirely remove the amount of time that the voicechat is unable to function for.

## Warning
This mod is still in early development, and as such might be incompatible with other mods, and could potentially cause crashes. If any issue of this sort happens, please submit an issue to the mod's [github page](https://github.com/AdalynBlack/Loadstone/issues).

## Compatibility
This mod aims to be fully compatible with vanilla, and to have compatibility with as many other mods as possible. The following common mods have been tested for compatibility, and should function as expected:
- BiggerLobby
- LethalExpansionCore

Note: just because a mod isn't present in this list doesn't mean it won't work, but that it isn't a mod expected to cause breakages that has also been verified to work. There are currently no known incompatibilities, and fixes for any incompatibilities are made as quickly as possible.

## Configuring
This mod provides a few config options that can be used to customize exactly how loading happens. The default config is optimized to allow voicechat to work for the entire time spent loading in, while spending as little time actually loading as possible. If loading fails in some capacity, the provided config options should hopefully allow for the issue to be resolved.

All config options can be modified in game through LethalConfig if it is installed.

- Tweaks
  - Dungeon
    - ShouldGenAsync
      - Toggles Loadstone's forced async level generation. This is on by default, and if turned off *will* cause stuttering as the level generates. The vanilla game sets async generation to be off by default, and as such, most issues with level loading should be able to be fixed by disabling this option. The mod still improves performance in other places, but a lot of the performance uplift does come from this one option.
    - DungeonAsyncMaxTime
      - This config option tells the game's map generator how long it's allowed to spend each frame generating the level. Increasing this will reduce your framerate while loading, but allow for faster load times, while decreasing it will increase your framerate while loading, at the cost of longer load times. I hope to eventually convert the level generator to be truly asynchronous so that map generation can happen at full speed without compromising framerates, but that will require a deep rework of existing systems.
  - Lethal
    - PostLoadStartDelay
      - This value controls a tiemr that decides how long to wait between all clients successfully loading the moon, and starting the process of generating the map layout. I'm not fully sure what the intended purpose of this is, but the vanilla value is 0.5, while the mod sets it to 0 by default.
    - PostGenerateSpawnDelay
      - This value controls a timer that decides how long to wait between all clients having generated the level, and deciding to actually spawn in scrap and enemies. I'm not fully sure what the intended purpose of this is, but the vanilla value is 0.3, while the mod sets it to 0 by default.
