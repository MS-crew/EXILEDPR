// -----------------------------------------------------------------------
// <copyright file="SpawnedEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.API.Features.Roles;

    using Interfaces;

    using PlayerRoles;

    using UnityEngine;

    /// <summary>
    /// Contains all information after spawning a player.
    /// </summary>
    public class SpawnedEventArgs : IPlayerEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpawnedEventArgs" /> class.
        /// </summary>
        /// <param name="player">The spawned player.</param>
        /// <param name="newRole">The spawned player's new <see cref="PlayerRoleBase">role</see>.</param>
        /// <param name="useSpawnPoint">Indicates whether the player was spawned at the role's default spawn point.</param>
        /// <param name="spawnLocation">The exact position where the player was spawned.</param>
        /// <param name="horizontalRotation">The horizontal rotation of the player upon spawning.</param>
        public SpawnedEventArgs(Player player, Role newRole, bool useSpawnPoint, Vector3 spawnLocation, float horizontalRotation)
        {
            Player = player;
            NewRole = newRole;
            UseSpawnPoint = useSpawnPoint;
            SpawnLocation = spawnLocation;
            HorizontalRotation = horizontalRotation;
            Reason = (SpawnReason)NewRole.SpawnReason;
            SpawnFlags = NewRole.SpawnFlags;
        }

        /// <summary>
        /// Gets the <see cref="API.Features.Player" /> who spawned.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets the player's new <see cref="Role">role</see>.
        /// </summary>
        public Role NewRole { get; }

        /// <summary>
        /// Gets a value indicating whether the player was spawned at the role's default spawn point.
        /// </summary>
        public bool UseSpawnPoint { get; }

        /// <summary>
        /// Gets the exact position where the player was spawned.
        /// </summary>
        public Vector3 SpawnLocation { get; }

        /// <summary>
        /// Gets the horizontal rotation of the player upon spawning.
        /// </summary>
        public float HorizontalRotation { get; }

        /// <summary>
        /// Gets the reason for their class change.
        /// </summary>
        public SpawnReason Reason { get; }

        /// <summary>
        /// Gets the spawn flags for their class change.
        /// </summary>
        public RoleSpawnFlags SpawnFlags { get; }
    }
}