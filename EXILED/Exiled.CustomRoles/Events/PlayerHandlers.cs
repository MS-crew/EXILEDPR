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

    using Exiled.API.Features;
    using Exiled.CustomRoles.API.Features;
    using Exiled.Events.EventArgs.Player;

    /// <summary>
    /// Handles general events for players.
    /// </summary>
    public class PlayerHandlers
    {
        private readonly CustomRoles plugin;

        private readonly HashSet<int> playersBeingProcessed = new HashSet<int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerHandlers"/> class.
        /// </summary>
        /// <param name="plugin">The <see cref="CustomRoles"/> plugin instance.</param>
        public PlayerHandlers(CustomRoles plugin)
        {
            this.plugin = plugin;
        }

        /// <inheritdoc cref="Exiled.Events.Handlers.Server.WaitingForPlayers"/>
        internal void OnWaitingForPlayers() => playersBeingProcessed.Clear();

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
            if (!playersBeingProcessed.Add(ev.Player.Id))
            {
                return;
            }

            try
            {
                if (CustomRole.TryGet(ev.Player, out _))
                {
                    return;
                }

                float totalChance = 0f;
                List<CustomRole> eligibleRoles = new List<CustomRole>();

                foreach (CustomRole role in CustomRole.Registered)
                {
                    if (!role.IgnoreSpawnSystem && role.SpawnChance > 0 && role.Role == ev.Player.Role.Type && !role.Check(ev.Player))
                    {
                        eligibleRoles.Add(role);
                        totalChance += role.SpawnChance;
                    }
                }

                if (eligibleRoles.Count == 0)
                {
                    return;
                }

                float chanceSize = Math.Max(100f, totalChance);
                float randomChance = (float)Loader.Loader.Random.NextDouble() * chanceSize;

                if (randomChance >= totalChance)
                {
                    return;
                }

                CustomRole? chosenRole = null;
                foreach (CustomRole role in eligibleRoles)
                {
                    if (randomChance < role.SpawnChance)
                    {
                        chosenRole = role;
                        break;
                    }

                    randomChance -= role.SpawnChance;
                }

                chosenRole?.AddRole(ev.Player);
            }
            catch (Exception e)
            {
                Log.Error($"Custom role ataması sırasında kritik bir hata oluştu: {e}");
            }
            finally
            {
                playersBeingProcessed.Remove(ev.Player.Id);
            }
        }
    }
}
