// -----------------------------------------------------------------------
// <copyright file="HandCuffedEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;

    using Interfaces;

    /// <summary>
    /// Contains all information after hand cuffed a player.
    /// </summary>
    public class HandCuffedEventArgs : IPlayerEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HandCuffedEventArgs" /> class.
        /// </summary>
        /// <param name="cuffer">
        /// <inheritdoc cref="Player" />
        /// </param>
        /// <param name="target">
        /// <inheritdoc cref="Target" />
        /// </param>
        public HandCuffedEventArgs(Player cuffer, Player target)
        {
            Player = cuffer;
            Target = target;
        }

        /// <summary>
        /// Gets the player who is cuffed.
        /// </summary>
        public Player Target { get; }

        /// <summary>
        /// Gets the cuffer player.
        /// </summary>
        public Player Player { get; }
    }
}