using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace Loadstone.Patches;

public class SpawnSyncedObjectPatches
{
	[HarmonyPatch(typeof(SpawnSyncedObject), MethodType.Constructor)]
	static void AwakePatch(SpawnSyncedObject __instance)
	{
		Loadstone.HarmonyLog.LogDebug($"Synced Object Spawner `{__instance}` has awoken. Attempt to spawn prefab `{__instance.spawnPrefab}`");

		var newInstance = UnityEngine.Object.Instantiate(__instance.spawnPrefab, __instance.transform.position, __instance.transform.rotation, __instance.transform);
		
		if (newInstance == null)
			return;

		newInstance.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);

		RoundManager.Instance.spawnedSyncedObjects.Add(newInstance);
	}
}
