// -----------------------------------------------------------------------
// <copyright file="RoundTargetCount.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Generic
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using Exiled.API.Features;
    using Exiled.API.Features.Pools;

    using HarmonyLib;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches <see cref="RoundSummary.UpdateTargetCount" />.
    /// Adds the <see cref="Round.IgnoredPlayers" /> Propperty.
    /// </summary>
    [HarmonyPatch]
    internal class RoundTargetCount
    {
        private static MethodInfo TargetMethod()
        {
            // For find '<UpdateTargetCount>b__51_0' method in '<>c' class.
            MethodInfo updateTargetCount = Method(typeof(RoundSummary), nameof(RoundSummary.UpdateTargetCount));

            List<CodeInstruction> newInstructions = PatchProcessor.GetOriginalInstructions(updateTargetCount);

            int index = newInstructions.FindIndex(x => x.opcode == OpCodes.Ldftn && x.operand is MethodInfo mi && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == typeof(ReferenceHub) && mi.ReturnType == typeof(bool));

            MethodInfo targetMethod = (MethodInfo)newInstructions[index].operand;

            return targetMethod;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            Label skip = generator.DefineLabel();

            newInstructions[0].labels.Add(skip);

            newInstructions.InsertRange(0, new CodeInstruction[]
            {
                // if (Round.IgnoredPlayers.Contains(hub))
                // return false;
                new(OpCodes.Call, PropertyGetter(typeof(Round), nameof(Round.IgnoredPlayers))),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Callvirt, Method(typeof(HashSet<ReferenceHub>), nameof(HashSet<ReferenceHub>.Contains))),
                new(OpCodes.Brfalse_S, skip),

                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ret),
            });

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }
    }
}
