using HarmonyLib;
using Loadstone.Config;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Loadstone.Patches;

[HarmonyPatch(typeof(RoundManager))]
public class RoundManagerMusicPatches
{
	static internal AudioSource loadingAudioSource;
	static internal AudioClip loadingAudioClip = null;

	[HarmonyPatch("Awake")]
	[HarmonyPrefix]
	static void CreateAudioSource()
	{
		if (loadingAudioSource != null)
			Object.Destroy(loadingAudioSource);

		if (loadingAudioClip == null)
			loadingAudioClip = Resources.FindObjectsOfTypeAll(typeof(AudioClip))
				.Cast<AudioClip>()
				.FirstOrDefault<AudioClip>(a => a.name == "ElevatorJingle");
		if (loadingAudioClip == null)
			Loadstone.LogError("Unable to find ElevatorJingle");
		
		if (loadingAudioClip.loadState != AudioDataLoadState.Loaded)
		{
			loadingAudioClip.LoadAudioData();
		}

		loadingAudioSource = Object.Instantiate(StartOfRound.Instance.speakerAudioSource);
		loadingAudioSource.name = "LoadstoneLoading";
		loadingAudioSource.clip = Object.Instantiate(loadingAudioClip);
		loadingAudioSource.clip.name = loadingAudioSource.name;
		loadingAudioSource.transform.parent = StartOfRound.Instance.speakerAudioSource.transform;
	}

	[HarmonyPatch("GenerateNewLevelClientRpc")]
	[HarmonyPrefix]
	static void PlayWaitingMusicPatch()
	{
		if (!LoadstoneConfig.ShouldLoadingMusicPlay.Value)
			return;

		loadingAudioSource.loop = LoadstoneConfig.ShouldLoadingMusicLoop.Value;

		loadingAudioSource.volume = LoadstoneConfig.LoadingMusicVolume.Value;
		loadingAudioSource.Play();
	}

	[HarmonyPatch("ResetEnemySpawningVariables")]
	[HarmonyPostfix]
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
