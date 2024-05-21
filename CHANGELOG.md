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

