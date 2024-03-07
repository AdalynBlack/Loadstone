using DunGen;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Loadstone.Patches;

public class DungeonGeneratorPatches {
	[HarmonyPatch(typeof(DungeonGenerator), "PostProcess", MethodType.Enumerator)]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> PostProcessPatch(IEnumerable<CodeInstruction> instructions)
	{
		Loadstone.TranspilerLog.LogDebug($"Attempting to inject async check into DungeonGenerator::PostProcess");
		Type[] findWaitParams = { typeof(Func<bool>) };
		Type[] findFuncParams = { typeof(System.Object), typeof(IntPtr) };

		var constructors = typeof(Func<System.Boolean>).GetConstructors();
		foreach (var constructor in constructors)
		{
			Loadstone.TranspilerLog.LogDebug(constructor);
		}

		var newInstructions = new CodeMatcher(instructions)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ldnull),
					new CodeMatch(OpCodes.Stfld))
			.SetOpcodeAndAdvance(OpCodes.Nop) // Remove the ldnull while maintaining labels
			.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldloc_2),
					new CodeInstruction(OpCodes.Ldftn, AccessTools.Method(typeof(DungeonGeneratorPatches), "PostProcessCheck")),
					new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Func<System.Boolean>), parameters: findFuncParams)),
					new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(UnityEngine.WaitUntil), parameters: findWaitParams)))
			.InstructionEnumeration();
		
		Loadstone.TranspilerLog.LogDebug($"Validating injected async check into DungeonGenerator::PostProcess");
		return newInstructions;
	}

	static bool PostProcessCheck()
	{
		return DungeonPatches.ConversionComplete;
	}
}
