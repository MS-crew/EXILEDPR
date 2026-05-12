// -----------------------------------------------------------------------
// <copyright file="WearableArmorPatch.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Generic
{
#pragma warning disable SA1313
    using System.Collections.Generic;

    using HarmonyLib;
    using Mirror;
    using PlayerRoles.FirstPersonControl.Thirdperson;
    using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

    /// <summary>
    /// Patches <see cref="WearableArmor.ServerCurArmor"/>.
    /// </summary>
    [HarmonyPatch(typeof(WearableSync), nameof(WearableSync.OnHubAdded),  MethodType.Getter)]
    internal static class WearableArmorPatch
    {
        private static bool Prefix(ref ReferenceHub hub)
        {
            foreach (KeyValuePair<uint, WearableSyncMessage> item in WearableSync.Database)
            {
                hub.connectionToClient.Send(item.Value);
            }

            return false;
        }
    }
}