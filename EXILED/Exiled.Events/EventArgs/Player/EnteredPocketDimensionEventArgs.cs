// -----------------------------------------------------------------------
// <copyright file="EnteredPocketDimensionEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;

    using Interfaces;

    /// <summary>
    /// Contains all information after a player enters the pocket dimension.
    /// </summary>
    public class EnteredPocketDimensionEventArgs : IPlayerEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnteredPocketDimensionEventArgs" /> class.
        /// </summary>
        /// <param name="player"> <inheritdoc cref="Player"/> </param>
        public EnteredPocketDimensionEventArgs(Player player)
        {
            Player = player;
        }

        /// <summary>
        /// Gets the player who's entered the pocket dimension.
        /// </summary>
        public Player Player { get; }
    }
}