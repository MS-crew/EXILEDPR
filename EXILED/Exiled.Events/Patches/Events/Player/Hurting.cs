// -----------------------------------------------------------------------
// <copyright file="Hurting.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Player
{
    using System.Collections.Generic;
    using System.Reflection.Emit;

    using Exiled.API.Features.Pools;
    using Exiled.Events.Attributes;
    using Exiled.Events.EventArgs.Player;

    using HarmonyLib;

    using PlayerStatsSystem;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches <see cref="PlayerStats.DealDamage(DamageHandlerBase)" />.
    /// Adds the <see cref="Handlers.Player.Hurt" /> event.
    /// </summary>
    [EventPatch(typeof(Handlers.Player), nameof(Handlers.Player.Hurt))]
    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.DealDamage))]
    internal static class Hurting
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            int offset = 2;
            int index = newInstructions.FindIndex(instruction => instruction.operand == (object)Method(typeof(DamageHandlerBase), nameof(DamageHandlerBase.ApplyDamage))) + offset;

            newInstructions.InsertRange(
                index,
                new[]
                {
                    // this._hub
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, Field(typeof(PlayerStats), nameof(PlayerStats._hub))),

                    // handler
                    new(OpCodes.Ldarg_1),

                    // handlerOutput
                    new(OpCodes.Ldloc_3),

                    // HurtEventArgs ev = new(ReferenceHub, handler, handleroutput)
                    new(OpCodes.Newobj, GetDeclaredConstructors(typeof(HurtEventArgs))[0]),
                    new(OpCodes.Dup),

                    // Handlers.Player.OnHurt(ev);
                    new(OpCodes.Call, Method(typeof(Handlers.Player), nameof(Handlers.Player.OnHurt))),

                    // handlerOutput = ev.HandlerOutput;
                    new(OpCodes.Callvirt, PropertyGetter(typeof(HurtEventArgs), nameof(HurtEventArgs.HandlerOutput))),
                    new(OpCodes.Stloc_1),
                });

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }
    }
}