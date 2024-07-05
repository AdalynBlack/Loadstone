using HarmonyLib;
using Loadstone.Config;
using LCSoundTool;
using System.Collections;
using UnityEngine;

namespace Loadstone.Patches.LCSoundTool;

[HarmonyPatch(typeof(RoundManager))]
public class RoundManagerMusicPatches
{
	static internal AudioSource loadingAudioSource;
	static internal AudioClip loadingAudioClip = SoundTool.GetAudioClip($"AdiBTW-{PluginInfo.PLUGIN_NAME}", "LoadstoneLoading.ogg");

	[HarmonyPatch("Awake")]
	[HarmonyPrefix]
	static void CreateAudioSource()
	{
		if (loadingAudioSource != null)
			Object.Destroy(loadingAudioSource);

		if (loadingAudioClip == null)
			return;
		
		if (loadingAudioClip.loadState != AudioDataLoadState.Loaded)
		{
			loadingAudioClip.LoadAudioData();
		}

		loadingAudioSource = Object.Instantiate(StartOfRound.Instance.speakerAudioSource);
		loadingAudioSource.name = "LoadstoneLoading";
		loadingAudioSource.clip = loadingAudioClip;
		loadingAudioSource.transform.parent = StartOfRound.Instance.speakerAudioSource.transform;
	}

	[HarmonyPatch("LoadNewLevel")]
	[HarmonyPrefix]
	static void PlayWaitingMusicPatch()
	{
		if (!LoadstoneConfig.ShouldLoadingMusicPlay.Value)
			return;

		loadingAudioSource.volume = LoadstoneConfig.LoadingMusicVolume.Value;
		loadingAudioSource.Play();
	}

	[HarmonyPatch("ResetEnemySpawningVariables")]
	[HarmonyPrefix]
	static void StopWaitingMusicPatch()
	{
		RoundManager.Instance.StartCoroutine(FadeOutMusic(loadingAudioSource));
	}

	static IEnumerator FadeOutMusic(AudioSource source)
	{
		float originalVolume = source.volume;
		float timeElapsed = 0;
		while (source.volume > 0.01) {
			source.volume = Mathf.Lerp(originalVolume, 0, timeElapsed);
			timeElapsed += Time.deltaTime / LoadstoneConfig.LoadingMusicFadeTime.Value;
			yield return null;
		}
		source.Stop();
		source.volume = originalVolume;

		Loadstone.LogDebug("Music fully faded and stopped");
	}
}
