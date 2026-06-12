// -----------------------------------------------------------------------
// <copyright file="EscapedEvent.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Player
{
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE0060

    using System.Collections.Generic;
    using System.Reflection.Emit;

    using EventArgs.Player;

    using Exiled.API.Features;
    using Exiled.API.Features.Pools;
    using Exiled.API.Features.Roles;
    using Exiled.Events.Attributes;

    using HarmonyLib;

    using LabApi.Events.Arguments.PlayerEvents;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches <see cref="Escape.ServerHandlePlayer(ReferenceHub)"/> for <see cref="Handlers.Player.Escaped"/>.
    /// </summary>
    [EventPatch(typeof(Handlers.Player), nameof(Handlers.Player.Escaped))]
    [HarmonyPatch(typeof(Escape), nameof(Escape.ServerHandlePlayer))]
    internal static class EscapedEvent
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            Label returnLabel = generator.DefineLabel();

            LocalBuilder role = generator.DeclareLocal(typeof(Role));
            LocalBuilder player = generator.DeclareLocal(typeof(Player));

            int offset = 4;
            int index = newInstructions.FindIndex(x => x.Is(OpCodes.Stfld, Field(typeof(Escape.EscapeMessage), nameof(Escape.EscapeMessage.EscapeTime)))) + offset;

            newInstructions.InsertRange(index, new CodeInstruction[]
            {
                // Player player = Player.Get(hub);
                // Role role = player.Role;
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(ReferenceHub) })),
                new(OpCodes.Dup),
                new(OpCodes.Stloc_S, player),
                new(OpCodes.Callvirt, PropertyGetter(typeof(Player), nameof(Player.Role))),
                new(OpCodes.Stloc_S, role),
            });

            newInstructions.InsertRange(newInstructions.Count - 1, new CodeInstruction[]
            {
                // player
                new(OpCodes.Ldloc_S, player),

                // escapeScenario
                new(OpCodes.Ldloc_3),

                // role
                new(OpCodes.Ldloc_S, role),

                // EscapedEventArgs ev2 = new(ev.Player, ev.EscapeScenario, role);
                new(OpCodes.Newobj, GetDeclaredConstructors(typeof(EscapedEventArgs))[0]),

                // Handlers.Player.OnEscaped(ev);
                new(OpCodes.Call, Method(typeof(Handlers.Player), nameof(Handlers.Player.OnEscaped))),
            });

            newInstructions[newInstructions.Count - 1].WithLabels(returnLabel);

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }
    }
}