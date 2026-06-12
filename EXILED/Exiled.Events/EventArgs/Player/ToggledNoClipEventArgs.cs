// -----------------------------------------------------------------------
// <copyright file="ToggledNoClipEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;

    using Interfaces;

    /// <summary>
    /// Contains all information after a player toggled noclip.
    /// </summary>
    public class ToggledNoClipEventArgs : IPlayerEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToggledNoClipEventArgs" /> class.
        /// </summary>
        /// <param name="player">
        /// <inheritdoc cref="Player" />
        /// </param>
        /// <param name="isEnabled">
        /// <inheritdoc cref="IsEnabled" />
        /// </param>
        public ToggledNoClipEventArgs(Player player, bool isEnabled)
        {
            Player = player;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Gets the player who toggled noclip.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets a value indicating whether the noclip mode is now enabled.
        /// </summary>
        public bool IsEnabled { get; }
    }
}