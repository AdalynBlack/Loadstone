using DunGen;
using HarmonyLib;
using Loadstone.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Loadstone.Patches;

public class FromProxyPatches {
	public static bool ConversionComplete = false;

	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyPrefix]
	static bool FromProxyPre(Dungeon __instance, DungeonProxy proxyDungeon, DungeonGenerator generator)
	{
		// Reset the completion variable
		Loadstone.LogInfo("Setting ConversionComplete false");
		ConversionComplete = false;
		__instance.StartCoroutine(FromProxyEnumerator(generator, proxyDungeon, __instance));
		return false;
	}

	static IEnumerator FromProxyEnumerator(DungeonGenerator generator, DungeonProxy proxyDungeon, Dungeon __instance)
	{
		// All code, unless otherwise noted, is effectively a recreation of Dungen's FromProxy function, piecing it back together as an IEnumerator
		__instance.Clear();
		Dictionary<TileProxy, Tile> dictionary = new Dictionary<TileProxy, Tile>();

		var shouldSkip = typeof(DungeonGenerator).GetMethod("ShouldSkipFrame", BindingFlags.NonPublic | BindingFlags.Instance);

		foreach (TileProxy tile in proxyDungeon.AllTiles)
		{
			FromProxyIteration(__instance, dictionary, generator, tile);

			if((bool)shouldSkip.Invoke(generator, new object[] {false}))
				yield return null;
		}

		FromProxyEnd(__instance, proxyDungeon, generator, dictionary);

		Loadstone.LogInfo("Setting ConversionComplete true");
		// Mark the FromProxy process as complete to allow the program to progress
		ConversionComplete = true;
	}

	// Extracts the first for loop's contents from FromProxy
	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyReversePatch]
	static void FromProxyIteration(Dungeon __instance, Dictionary<TileProxy, Tile> dictionary, DungeonGenerator generator, TileProxy tile) {
		IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Loadstone.LogDebug("Attempting to reverse-patch Dungeon::FromProxy's first inner for loop");
			var matcher = new CodeMatcher(instructions, generator);

			// Find the beginning of the for loop
			var start = matcher
				.MatchForward(false,
						new CodeMatch(OpCodes.Br))
				.Advance(4)
				.Pos;

			// Load the dictionary and TileProxy reference from the function arguments, and store them to the existing local variables
			matcher.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Stloc_2),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Stloc_0));

			// Find the end of the loop, insert a return instruction, and note the position
			var end = matcher
				.MatchForward(false,
						new CodeMatch(OpCodes.Endfinally),
						new CodeMatch(OpCodes.Ldloca_S),
						new CodeMatch(OpCodes.Call))
				.Advance(1)
				.Insert(new CodeInstruction(OpCodes.Ret))
				.Pos;

			// Createa a label at the new return instruction
			matcher.CreateLabel(out Label endLabel);

			// Find a part of the code that continues the loop, and redirect it to the new return statement
			matcher
				.MatchBack(false,
					new CodeMatch(OpCodes.Brtrue),
					new CodeMatch(OpCodes.Leave))
				.Advance(1)
				.SetOperandAndAdvance(endLabel);

			// Extract the for loop instructions using the previous start and end points
			var codeList = matcher.InstructionsInRange(start, end).AsEnumerable();

#if NIGHTLY
			// Apply the Object Pooling Patches if applicable, since Reverse Patches happen before any other transpilers
			if (LoadstoneConfig.ObjectPooling.Value)
				codeList = PoolingPatches.FromProxyPoolingPatch(codeList);
#endif

			Loadstone.LogDebug("Validating reverse-patched Dungeon::FromProxy's first inner for loop");
			return codeList;
		}

		// make compiler happy
		_ = StartTranspiler(null, null);
	}

	// Extracts the code in FromProxy after the first for loop
	[HarmonyPatch(typeof(Dungeon), "FromProxy")]
	[HarmonyReversePatch]
	static void FromProxyEnd(Dungeon __instance, DungeonProxy proxyDungeon, DungeonGenerator generator, Dictionary<TileProxy, Tile> dictionary) {
		IEnumerable<CodeInstruction> EndTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			Loadstone.LogDebug("Attempting to reverse-patch Dungeon::FromProxy's final code");
			var matcher = new CodeMatcher(instructions);

			// Find the instructions immediately following the primary for loop, and note the position
			var start = matcher
				.MatchForward(false,
						new CodeMatch(OpCodes.Endfinally))
				.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_1))
				.Pos;
	
			// Load the dictionary to the expected local variable
			matcher.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Stloc_0),

			// These instructions exist to get around a bug within MonoMod
			// The original function has a GameObject variable. This carries over to this function as well
			// However, if no code pulls from UnityEngine, MonoMod mistakenly removes the function's dependency on UnityEngine
			// This is how I decided to inject a UnityEngine dependency to fix the issue
					new CodeInstruction(OpCodes.Ldtoken, typeof(UnityEngine.GameObject)),
					new CodeInstruction(OpCodes.Pop));

			// Store the position of the end of the function
			var end = matcher
				.End()
				.Pos;

			// Extract the code within the start and end range previously stored
			var codeList = matcher.InstructionsInRange(start, end);

			Loadstone.LogDebug("Validating reverse-patched Dungeon::FromProxy's final code");
			return codeList.AsEnumerable();
		}

		// make compiler happy
		_ = EndTranspiler(null);
	}

	// Injects a check to wait for FromProxy to finish executing before running PostProcess
	[HarmonyPatch(typeof(DungeonGenerator), "PostProcess", MethodType.Enumerator)]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> PostProcessPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
	{
		Loadstone.LogDebug($"Attempting to inject async check into DungeonGenerator::PostProcess");
		Type[] findWaitParams = { typeof(Func<System.Boolean>) };
		Type[] findFuncParams = { typeof(System.Object), typeof(IntPtr) };

		var matcher = new CodeMatcher(instructions, ilGenerator)
			.MatchForward(false,
				new CodeMatch(OpCodes.Switch));

		// Extract the switch list
		List<Label> switchList = (matcher.InstructionAt(0).operand as Label[]).ToList();
		var jumpIndex = switchList.Count;

		// Extract the leave label
		matcher.Advance(3);
		var leaveObject = matcher.InstructionAt(0).operand;
	
		if (leaveObject == null) {
			Loadstone.LogFatal($"Leave instruction did not have an operand! Cannot inject a wait for FromProxy! The dungeon *will* be corrupt");
			throw new ArgumentNullException("leaveObject is null. Cannot inject functioning code");
		}

		var leaveLabel = (Label)leaveObject;

		var currentField = (FieldInfo)matcher.MatchForward(true,
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldnull),
				new CodeMatch(OpCodes.Stfld))
			.InstructionAt(0).operand;

		var stateField = (FieldInfo)matcher.InstructionAt(3).operand;

		matcher.Start()
			.MatchForward(false,
				new CodeMatch(OpCodes.Switch))
			.Advance(7)
			.InsertAndAdvance(
				// <>2__current = WaitUntil(FromProxyPatches.ConversionCheck());
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldloc_2),
				//new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(FromProxyPatches))),
				new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(typeof(FromProxyPatches), "ConversionCheck")),
				new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Func<System.Boolean>), parameters: findFuncParams)),
				new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(UnityEngine.WaitUntil), parameters: findWaitParams)),
				new CodeInstruction(OpCodes.Stfld, currentField),
				// <>1__state = jumpIndex;
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldc_I4_S, jumpIndex),
				new CodeInstruction(OpCodes.Stfld, stateField),
				// return true;
				new CodeInstruction(OpCodes.Ldc_I4_1),
				new CodeInstruction(OpCodes.Stloc_0),
				new CodeInstruction(OpCodes.Leave, leaveLabel)
			).CreateLabel(out var conversionCheckLabel);

		// Tell the IEnumerator where to jump to for this new yield return statement
		switchList.Add(conversionCheckLabel);

		// Write the jump table back
		matcher
			.Start()
			.MatchForward(false,
				new CodeMatch(OpCodes.Switch))
			.SetOperandAndAdvance(switchList.ToArray());
		
		var newInstructions = matcher.InstructionEnumeration();
		
		Loadstone.LogDebug($"Validating injected async check into DungeonGenerator::PostProcess");
		return newInstructions;
	}

	static bool ConversionCheck()
	{
		Loadstone.LogInfo($"ConversionComplete checked, returning {FromProxyPatches.ConversionComplete}");
		return FromProxyPatches.ConversionComplete;
	}
}
