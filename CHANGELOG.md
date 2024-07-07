## v0.1.11
- Fixed a Null Reference Exception with the new performance stats module

## v0.1.10
- Fixed an Incompatibility with HarmonyXTranspilerFix

## v0.1.9
- Modified how logging is done internally to improve consistency
- Added a slight optimization to tag caching
- Added performance report logging
  - This will be output into the bepinex log file. If you're having performance issues, please send the performance report data, or the log as a whole to help me learn where the issues are coming from

## v0.1.8
- Fixed Nightly using the wrong audio file path

## v0.1.7
- Fixed the issues from v0.1.5. The dungeon should now generate identically between modded and unmodded clients
- Updated main to v0.1.7

## v0.1.6
- Downgraded main branch to 0.0.9 due to unexpected issues

## v0.1.5
- Ported many features from nightly to main!
  - Code reorganization
    - Introduces modules internally, which reflects newly added toggles to disable modules if needed
  - Improved Navmesh Async Generation
    - The dungeon itself will now generate its navmesh asynchronously
  - Removed inefficient object searches from LC's code
  - Added the DunGen optimization module, which contains new optimizations to improve DunGen's performance
- Nightly only features
  - Object pooling still remains nightly only due to a number of bugs
- Improved DunGen Optimization Module compatibility
  - Should fix issues with SDM and other modded interiors
- Waiting music will now load as soon as you join a ship, rather than during level generation, so as to be less intrusive

## v0.1.4
- The Dungeon Realization module now works with Object Pooling
  - Dungeon Realization now applies the Object Pooling patches to itself if it detects Object Pooling is enabled

## v0.1.3
- Added the DunGen Optimizations module
  - Improves particularly slow parts of DunGen's level generation algorithm. This module will be getting more updates and improvements in the future
- Updated Object Pooling
  - Object Pooling is still experimental due to compatibility issues with other mods
  - Object Pooling has received improvements to which objects it is able to pool
    - Most tiles can now be pooled properly without issues
    - Previously, tiles would be deleted due to DunGen regenerating the level from scratch when an error occurred
      - Now, DunGen will properly release tiles when regenerating the level, substantially increasing performance
  - Object Pooling has been updated to work better with NetworkObjects

## v0.1.2
- Added experimental object pooling
  - Currently expected to be quite buggy, and so is off by default
  - Has potential to greatly improve load times, but may also increase ram usage slightly, especially with many modded interiors
- Changed how async dungeon loading works to hopefully improve load times by default for slower PCs

## v0.1.1
- Fixed nightly branch using the wrong audio clip name
- Improved navmesh async generation
  - The dungeon will now be generated asynchronously, as opposed to just the surface
- Updated the logger names for nightly
- Patched out some inefficient and unnecessary object searches

## v0.1.0
- Heavy internal code reorganization
	- This will make adding and modifying features much easier in the future
- Module toggles
	- Every feature is now a self contained module that can be turned on and off with its own config option
	- Module toggles currently will not work properly without a game restart
- First nightly build of Loadstone released
	- Nightly builds can be found on thunderstore at https://thunderstore.io/c/lethal-company/p/AdiBTW/LoadstoneNightly/

## v0.0.9
- Improvements to async navmesh generation
	- The game should now properly distribute navmesh generation over multiple frames
- Removed Async Gen Wait Time and Post-Load Start Delay
	- These two options didn't significantly improve performance, and caused issues in certain edge cases, resulting in partially loaded moons without functional entrances or lighting

## v0.0.8
- Removed unsorted search patches
	- These patches didn't cover *nearly* every case in the game, and the overall performance impact is small enough to be disregarded for this mod, in favor of better compatibility with other mods and future updates
- Added seed pop-up config

## v0.0.7
- Added the random seed pop-up as an on-screen overlay

## v0.0.6
- Converted the audio file from wav to ogg to save on file space. Fixes #4

## v0.0.5
- Implemented a conflict detection system to warn of potential mod conflicts

## v0.0.4
- Implemented optional loading music via LCSoundTool
	- To enable the loading music, you'll need to install LCSoundTool and enable it in the config
	- If you wish to override the loaing music, you can use CustomSounds and override "LoadstoneLoading" with whatever you wish to replace it with

## v0.0.3
- Changed the default value of Async Gen Wait Time to be 30ms instead of 100ms
	- Testing has shown that this has negligible impact on loading times, but should increase smoothness during loading dramatically
- Fixed config naming
	- The old config should be ported automatically, and will automatically apply the above wait-time change if the previous default of 100ms is detected
- Changed the package description
- More README modifications


## v0.0.2
- Fixed some mistakes in the README
- Added more verbose debugging during patching
- Fixed #1, where a partial failure of one patch was occurring when using the automatic HarmonyX backend

## v0.0.1
- Fixed README formatting

