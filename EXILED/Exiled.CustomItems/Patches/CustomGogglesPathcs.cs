// -----------------------------------------------------------------------
// <copyright file="CustomGogglesPathcs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------
#pragma warning disable SA1649
#pragma warning disable SA1313
#pragma warning disable SA1402
namespace Exiled.CustomItems.Patches
{
    using Exiled.API.Features.Items;
    using Exiled.CustomItems.API.Features;

    using HarmonyLib;

    using InventorySystem.Items;
    using InventorySystem.Items.Pickups;
    using InventorySystem.Items.Usables.Scp1344;

    /// <summary>
    /// Patches the <see cref="Scp1344Item.CanStartUsing"/> property to prevent players from equipping multiple SCP-1344 instances simultaneously.
    /// </summary>
    [HarmonyPatch(typeof(Scp1344Item), nameof(Scp1344Item.CanStartUsing), MethodType.Getter)]
    public class CanStartUsingPatch
    {
        private static void Postfix(Scp1344Item __instance, ref bool __result)
        {
            if (!__result)
                return;

            foreach (ItemBase item in __instance.OwnerInventory.UserInventory.Items.Values)
            {
                if (item.ItemTypeId != ItemType.SCP1344)
                    continue;

                if (item is not Scp1344Item scp1344)
                    continue;

                if (!scp1344.IsWorn)
                    continue;

                __result = false;
                break;
            }
        }
    }

    /// <summary>
    /// Patches the <see cref="Scp1344Item.ActivateFinalEffects"/> method to prevent negative effects (blindness/severed eyes) when removing <see cref="CustomGoggles"/> if safe removal is enabled.
    /// </summary>
    [HarmonyPatch(typeof(Scp1344Item), nameof(Scp1344Item.ActivateFinalEffects))]
    public class ActivateFinalEffectsPatch
    {
        private static bool Prefix(Scp1344Item __instance)
        {
            if (!CustomItem.TryGet(Item.Get(__instance), out CustomItem? customItem))
                return true;

            if (customItem is not CustomGoggles customGoggles)
                return true;

            if (!customGoggles.CanBeRemoveSafely)
                return true;

            return false;
        }
    }

    /// <summary>
    /// Patches the <see cref="Scp1344Item.ServerDropItem"/> method to prevent the item from being dropped when the deactivation animation finishes, keeping it in the inventory instead.
    /// </summary>
    [HarmonyPatch(typeof(Scp1344Item), nameof(Scp1344Item.ServerDropItem))]
    public class ServerDropItemPatch
    {
        private static bool Prefix(Scp1344Item __instance, bool spawn, ref ItemPickupBase __result)
        {
            if (!spawn)
                return true;

            if (__instance.Status == Scp1344Status.Active)
                return true;

            if (!__instance.IsWorn)
                return true;

            if (!CustomItem.TryGet(Item.Get(__instance), out CustomItem? customItem))
                return true;

            if (customItem is not CustomGoggles customGoggles)
                return true;

            if (!customGoggles.CanBeRemoveSafely)
                return true;

            __instance.Status = Scp1344Status.Idle;
            __instance._useTime = 0f;
            __instance._savedIntensity = 0;
            __instance._cancelationTime = 0f;
            __instance.OwnerInventory.ServerSelectItem(0);
            return false;
        }
    }
}
#pragma warning restore SA1402
#pragma warning disable SA1313
#pragma warning restore SA1649
