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

