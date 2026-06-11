// -----------------------------------------------------------------------
// <copyright file="UsingItemCompleted.cs" company="ExMod Team">
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

    using HarmonyLib;

    using InventorySystem.Items.Usables;

    using LabApi.Events.Arguments.PlayerEvents;

    using Mirror;

    using static HarmonyLib.AccessTools;

#pragma warning disable SA1600 // Elements should be documented

    /// <summary>
    /// Patches <see cref="UsableItemsController.Update" />
    /// Fix the <see cref="Handlers.Player.UsingItemCompleted" /> event client visual logic.
    /// </summary>
    [EventPatch(typeof(Handlers.Player), nameof(Handlers.Player.UsingItemCompleted))]
    [HarmonyPatch(typeof(UsableItemsController), nameof(UsableItemsController.Update))]
    internal static class UsingItemCompleted
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            const int offset = 2;
            int index = newInstructions.FindIndex(x => x.Calls(PropertyGetter(typeof(PlayerItemUsageEffectsApplyingEventArgs), nameof(PlayerItemUsageEffectsApplyingEventArgs.ContinueProcess)))) + offset;

            newInstructions.InsertRange(index, new[]
            {
                // currentUsable.Item.OnUsingCancelled();
                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldfld, Field(typeof(CurrentlyUsedItem), nameof(CurrentlyUsedItem.Item))),
                new(OpCodes.Callvirt, Method(typeof(UsableItem), nameof(UsableItem.OnUsingCancelled))),

                // keyValuePair.Key.inventory.connectionToClient.Send(new StatusMessage(StatusMessage.StatusType.Cancel, currentUsable.ItemSerial), 0);
                new(OpCodes.Ldloca_S,  1),
                new(OpCodes.Call, PropertyGetter(typeof(KeyValuePair<ReferenceHub, PlayerHandler>), nameof(KeyValuePair<ReferenceHub, PlayerHandler>.Key))),
                new(OpCodes.Ldfld, Field(typeof(ReferenceHub), nameof(ReferenceHub.inventory))),
                new(OpCodes.Callvirt, PropertyGetter(typeof(NetworkBehaviour), nameof(NetworkBehaviour.connectionToClient))),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldfld, Field(typeof(CurrentlyUsedItem), nameof(CurrentlyUsedItem.ItemSerial))),
                new(OpCodes.Newobj, GetDeclaredConstructors(typeof(StatusMessage))[0]),
                new(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Callvirt, FirstMethod(typeof(NetworkConnection), m =>
                {
                    return m.IsGenericMethod && m.Name == nameof(NetworkConnection.Send);
                }).MakeGenericMethod(typeof(StatusMessage))),
            });

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }
    }
}