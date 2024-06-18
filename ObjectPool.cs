using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Loadstone;

public static class ObjectPool
{
	// Dictionary from prefab to a list of currently instantiated objects of that type
	private static Dictionary<GameObject, Stack<GameObject>> availableObjects = new Dictionary<GameObject, Stack<GameObject>>(256);
	private static Dictionary<GameObject, GameObject> inUseObjects = new Dictionary<GameObject, GameObject>(1024);

	public static GameObject InstantiateTransparently(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
	{
		GameObject returnObject;

		if (!availableObjects.TryAdd(original, new Stack<GameObject>(4))) {
			if (availableObjects[original].TryPop(out returnObject) && returnObject != null)
			{
				inUseObjects[returnObject] = original;

				returnObject.SetActive(true);
				returnObject.hideFlags = HideFlags.None;

				returnObject.transform.parent = parent;
				returnObject.transform.localPosition = position;
				returnObject.transform.localRotation = rotation;
				return returnObject;
			}
			if (returnObject == null)
				Loadstone.HarmonyLog.LogDebug($"A pooled {original.name} was null!");
		}

		returnObject = Object.Instantiate(original, position, rotation, parent);
		inUseObjects[returnObject] = original;

		return returnObject;
	}

	public static GameObject InstantiateTransparently(GameObject original, Transform parent)
		=> InstantiateTransparently(original, Vector3.zero, Quaternion.identity, parent);

	public static GameObject InstantiateTransparently(GameObject original)
		=> InstantiateTransparently(original, Vector3.zero, Quaternion.identity, null);

	public static void InstantiateInvisibly(GameObject original)
	{
		var newObj = Object.Instantiate(original);

		Object.DontDestroyOnLoad(newObj);
		newObj.SetActive(false);
		newObj.hideFlags = HideFlags.DontSave;

		availableObjects[original].Push(newObj);
	}

	public static void ReleaseObject(GameObject toRelease)
	{
		if (toRelease == null)
			return;

		if (!inUseObjects.Remove(toRelease, out var original))
		{
			if (toRelease.TryGetComponent<NetworkObject>(out var networkObject))
				networkObject.Despawn();
			else
				Object.DestroyImmediate(toRelease, false);
			return;
		}

		toRelease.transform.parent = null;
		Object.DontDestroyOnLoad(toRelease);
		toRelease.SetActive(false);
		toRelease.hideFlags = HideFlags.DontSave;

		availableObjects[original].Push(toRelease);
	}

	public static void ReleaseAllObjects()
	{
		foreach (var toRelease in inUseObjects.Keys)
		{
			if (toRelease == null)
			{
				Loadstone.HarmonyLog.LogDebug($"A pooled object for {inUseObjects[toRelease].name} being mass released was null!");
				continue;
			}

			toRelease.transform.parent = null;
			Object.DontDestroyOnLoad(toRelease);
			toRelease.SetActive(false);
			toRelease.hideFlags = HideFlags.DontSave;

			availableObjects[inUseObjects[toRelease]].Push(toRelease);
		}

		inUseObjects.Clear();
	}
}
