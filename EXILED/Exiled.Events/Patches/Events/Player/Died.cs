// -----------------------------------------------------------------------
// <copyright file="Died.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Player
{
    using System.Collections.Generic;
    using System.Reflection.Emit;

    using Exiled.API.Features;
    using Exiled.API.Features.Pools;
    using Exiled.API.Features.Roles;
    using Exiled.Events.Attributes;
    using Exiled.Events.EventArgs.Player;

    using HarmonyLib;

    using PlayerRoles;
    using PlayerRoles.Ragdolls;

    using PlayerStatsSystem;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches <see cref="PlayerStats.KillPlayer(DamageHandlerBase)" />.
    /// Adds the <see cref="Handlers.Player.Died" /> event.
    /// </summary>
    [EventPatch(typeof(Handlers.Player), nameof(Handlers.Player.Died))]
    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.KillPlayer))]
    internal static class Died
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            Label ret = generator.DefineLabel();

            LocalBuilder player = generator.DeclareLocal(typeof(Player));
            LocalBuilder oldRole = generator.DeclareLocal(typeof(RoleTypeId));
            LocalBuilder ragdoll = generator.DeclareLocal(typeof(BasicRagdoll));

            newInstructions.InsertRange(0, new CodeInstruction[]
            {
                // oldRole = Player.Get(this._hub).Role.Type;
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, Field(typeof(PlayerStats), nameof(PlayerStats._hub))),
                new(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(ReferenceHub) })),
                new(OpCodes.Callvirt, PropertyGetter(typeof(Player), nameof(Player.Role))),
                new(OpCodes.Callvirt, PropertyGetter(typeof(Role), nameof(Role.Type))),
                new(OpCodes.Stloc, oldRole.LocalIndex),
            });

            int index = newInstructions.FindIndex(x => x.opcode == OpCodes.Pop);

            newInstructions[index] = new CodeInstruction(OpCodes.Stloc, ragdoll.LocalIndex);

            newInstructions.InsertRange(
                newInstructions.Count - 1,
                new CodeInstruction[]
                {
                    // player
                    new(OpCodes.Ldloc_S, player.LocalIndex),

                    // oldRole
                    new(OpCodes.Ldloc_S, oldRole.LocalIndex),

                    // handler
                    new(OpCodes.Ldarg_1),

                    // ragdoll
                    new(OpCodes.Ldloc_S, ragdoll.LocalIndex),

                    // DiedEventArgs evDied = new(Player, RoleTypeId, DamageHandlerBase)
                    new(OpCodes.Newobj, GetDeclaredConstructors(typeof(DiedEventArgs))[0]),

                    // Handlers.Player.OnDied(evDied)
                    new(OpCodes.Call, Method(typeof(Handlers.Player), nameof(Handlers.Player.OnDied))),
                });

            newInstructions[newInstructions.Count - 1].WithLabels(ret);

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }
    }
}