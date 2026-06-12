// -----------------------------------------------------------------------
// <copyright file="ChangedGroupEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;

    using Interfaces;

    /// <summary>
    /// Contains all information after a player's user group changed.
    /// </summary>
    public class ChangedGroupEventArgs : IPlayerEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangedGroupEventArgs" /> class.
        /// </summary>
        /// <param name="player"> <inheritdoc cref="Player"/> </param>
        /// <param name="newGroup"> <inheritdoc cref="NewGroup"/> </param>
        public ChangedGroupEventArgs(Player player, UserGroup newGroup)
        {
            Player = player;
            NewGroup = newGroup;
        }

        /// <summary>
        /// Gets the player who's changing his group.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets or sets the player's new group.
        /// </summary>
        public UserGroup NewGroup { get; set; }
    }
}