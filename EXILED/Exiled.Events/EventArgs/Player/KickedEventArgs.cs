// -----------------------------------------------------------------------
// <copyright file="KickedEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;

    using Interfaces;

    /// <summary>
    /// Contains all information after kicking a player from the server.
    /// </summary>
    public class KickedEventArgs : IPlayerEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KickedEventArgs" /> class.
        /// </summary>
        /// <param name="target">Player who got kicked.</param>
        /// <param name="issuer">Player who kicked.</param>
        /// <param name="reason">The kick reason.</param>
        public KickedEventArgs(Player target, Player issuer, string reason)
        {
            Player = target;
            Issuer = issuer;
            Reason = reason;
        }

        /// <summary>
        /// Gets the kick reason.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Gets the player who kicked.
        /// </summary>
        public Player Issuer { get; }

        /// <summary>
        /// Gets the kicked player.
        /// </summary>
        public Player Player { get; }
    }
}