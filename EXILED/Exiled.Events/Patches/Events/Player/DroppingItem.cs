// -----------------------------------------------------------------------
// <copyright file="DroppingItem.cs" company="ExMod Team">
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

    using InventorySystem;

    using static HarmonyLib.AccessTools;

    using Item = API.Features.Items.Item;
    using Player = Handlers.Player;

    /// <summary>
    /// Patches <see cref="Inventory.UserCode_CmdDropItem__UInt16__Boolean" />.
    /// <br>Adds the <see cref="Player.DroppingNothing" /> event.</br>
    /// </summary>
    [EventPatch(typeof(Player), nameof(Player.DroppingNothing))]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.UserCode_CmdDropItem__UInt16__Boolean))]
    internal static class DroppingItem
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            Label returnLabel = generator.DefineLabel();
            Label notNullLabel = generator.DefineLabel();

            LocalBuilder item = generator.DeclareLocal(typeof(Item));

            newInstructions[0].labels.Add(notNullLabel);

            newInstructions.InsertRange(0, new CodeInstruction[]
            {
                // if (player.TryGetItem(itemSerial, out Item item))
                //    goto notNullLabel;
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, Field(typeof(Inventory), nameof(Inventory._hub))),
                new(OpCodes.Call, Method(typeof(API.Features.Player), nameof(API.Features.Player.Get), new[] { typeof(ReferenceHub) })),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldloca_S, item.LocalIndex),
                new(OpCodes.Callvirt, Method(typeof(API.Features.Player), nameof(API.Features.Player.TryGetItem), new[] { typeof(ushort), typeof(Item).MakeByRefType() })),
                new(OpCodes.Brtrue_S, notNullLabel),

                // Player.Get(this._hub)
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, Field(typeof(Inventory), nameof(Inventory._hub))),
                new(OpCodes.Call, Method(typeof(API.Features.Player), nameof(API.Features.Player.Get), new[] { typeof(ReferenceHub) })),

                // DroppingNothingEventArgs ev = new(Player)
                new(OpCodes.Newobj, GetDeclaredConstructors(typeof(DroppingNothingEventArgs))[0]),

                // Player.OnDroppingNothing(ev)
                new(OpCodes.Call, Method(typeof(Player), nameof(Player.OnDroppingNothing))),

                // return
                new(OpCodes.Br_S, returnLabel),
            });

            newInstructions[newInstructions.Count - 1].labels.Add(returnLabel);

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }
    }
}