using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Loadstone.Patches;

public class StatusChangedFixer
{
	[HarmonyPatch(typeof(RoundManager), "Generator_OnGenerationStatusChanged")]
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> StatusChangedPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
	{
		Loadstone.TranspilerLog.LogDebug("Attempting to fix Generator_OnGenerationStatusChanged");

		var newInstructions = new CodeMatcher(instructions, ilGenerator)
			.MatchForward(false,
					new CodeMatch(OpCodes.Ret))
			.CreateLabel(out var returnLabel)
			.Start()
			.MatchForward(false,
					new CodeMatch(OpCodes.Bne_Un))
			.SetOperandAndAdvance(returnLabel)
			.InstructionEnumeration();

		return newInstructions;
	}
}
