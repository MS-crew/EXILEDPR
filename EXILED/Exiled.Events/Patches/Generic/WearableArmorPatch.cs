// -----------------------------------------------------------------------
// <copyright file="WearableArmorPatch.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Generic
{
#pragma warning disable SA1313
    using Exiled.API.Extensions;
    using Exiled.API.Features;
    using HarmonyLib;
    using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

    /// <summary>
    /// Patches <see cref="WearableArmor.ServerCurArmor"/> to implement <see cref="Player.DisplayedArmor"/>.
    /// </summary>
    [HarmonyPatch(typeof(WearableArmor), nameof(WearableArmor.ServerCurArmor),  MethodType.Getter)]
    internal static class WearableArmorPatch
    {
        private static bool Prefix(WearableArmor __instance, ref ItemType __result)
        {
            if (!Player.TryGet(__instance.gameObject, out Player player))
                return true;

            if (player.DisplayedArmor == ItemType.None || !player.DisplayedArmor.IsArmor())
                return true;

            __result = player.DisplayedArmor;
            return false;
        }
    }
}