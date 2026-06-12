// -----------------------------------------------------------------------
// <copyright file="ChangedRoleEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Enums;
    using Exiled.API.Features;

    using Interfaces;

    using PlayerRoles;

    /// <summary>
    /// Contains all information after a player's <see cref="RoleTypeId" /> changed.
    /// </summary>
    public class ChangedRoleEventArgs : IPlayerEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangedRoleEventArgs" /> class.
        /// </summary>
        /// <param name="player">
        /// <inheritdoc cref="Player" />
        /// </param>
        /// <param name="newRole">
        /// <inheritdoc cref="NewRole" />
        /// </param>
        /// <param name="oldRole">
        /// <inheritdoc cref="OldRole" />
        /// </param>
        /// <param name="reason">
        /// <inheritdoc cref="Reason" />
        /// </param>
        /// <param name="spawnFlags">
        /// <inheritdoc cref="SpawnFlags" />
        /// </param>
        public ChangedRoleEventArgs(Player player, PlayerRoleBase newRole, RoleTypeId oldRole, RoleChangeReason reason, RoleSpawnFlags spawnFlags)
        {
            Player = player;
            NewRole = newRole;
            OldRole = oldRole;
            Reason = (SpawnReason)reason;
            SpawnFlags = spawnFlags;
        }

        /// <summary>
        /// Gets the player whose <see cref="RoleTypeId" /> is changed.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets the new role object of the player.
        /// </summary>
        public PlayerRoleBase NewRole { get; }

        /// <summary>
        /// Gets the player's old role.
        /// </summary>
        public RoleTypeId OldRole { get; }

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