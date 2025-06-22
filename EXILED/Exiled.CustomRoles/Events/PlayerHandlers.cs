// -----------------------------------------------------------------------
// <copyright file="PlayerHandlers.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.CustomRoles.Events
{
    using System;
    using System.Collections.Generic;

    using Exiled.API.Enums;
    using Exiled.CustomRoles.API;
    using Exiled.CustomRoles.API.Features;
    using Exiled.Events.EventArgs.Player;

    /// <summary>
    /// Handles general events for players.
    /// </summary>
    public class PlayerHandlers
    {
        private static readonly object SpawnLock = new();
        private readonly HashSet<int> playersBeingProcessed = new HashSet<int>(64);
        private readonly CustomRoles plugin;
        private readonly HashSet<SpawnReason> validSpawnReasons = new()
        {
            SpawnReason.RoundStart,
            SpawnReason.Respawn,
            SpawnReason.LateJoin,
            SpawnReason.Revived,
            SpawnReason.Escaped,
            SpawnReason.ItemUsage,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerHandlers"/> class.
        /// </summary>
        /// <param name="plugin">The <see cref="CustomRoles"/> plugin instance.</param>
        public PlayerHandlers(CustomRoles plugin)
        {
            this.plugin = plugin;
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Server.WaitingForPlayers"/>
        internal void OnWaitingForPlayers()
        {
            foreach (CustomRole role in CustomRole.Registered)
            {
                role.SpawnedPlayers = 0;
            }
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Player.SpawningRagdoll"/>
        internal void OnSpawningRagdoll(SpawningRagdollEventArgs ev)
        {
            if (plugin.StopRagdollPlayers.Contains(ev.Player))
            {
                ev.IsAllowed = false;
                plugin.StopRagdollPlayers.Remove(ev.Player);
            }
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Player.Spawning"/>
        internal void OnSpawned(SpawnedEventArgs ev)
        {
            if (!validSpawnReasons.Contains(ev.Reason))
            {
                return;
            }

            if (ev.Player.HasAnyCustomRole())
            {
                return;
            }

            if (!playersBeingProcessed.Add(ev.Player.Id))
            {
                return;
            }

            try
            {
                lock (SpawnLock)
                {
                    List<CustomRole> eligibleRoles = new(8);
                    float totalChance = 0f;

                    foreach (CustomRole role in CustomRole.Registered)
                    {
                        if (!role.IgnoreSpawnSystem && role.SpawnChance > 0 && role.Role == ev.Player.Role.Type && !role.Check(ev.Player) && (role.SpawnProperties is null || role.SpawnedPlayers < role.SpawnProperties.Limit))
                        {
                            eligibleRoles.Add(role);
                            totalChance += role.SpawnChance;
                        }
                    }

                    if (eligibleRoles.Count == 0)
                    {
                        return;
                    }

                    float lotterySize = Math.Max(100f, totalChance);
                    float randomRoll = (float)Loader.Loader.Random.NextDouble() * lotterySize;

                    if (randomRoll >= totalChance)
                    {
                        return;
                    }

                    CustomRole? chosenRole = null;
                    foreach (CustomRole role in eligibleRoles)
                    {
                        if (randomRoll < role.SpawnChance)
                        {
                            chosenRole = role;
                            break;
                        }

                        randomRoll -= role.SpawnChance;
                    }

                    chosenRole?.AddRole(ev.Player);
                }
            }
            finally
            {
                playersBeingProcessed.Remove(ev.Player.Id);
            }
        } 
    }
}
