// -----------------------------------------------------------------------
// <copyright file="UsingItemCompletedEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;
    using Exiled.API.Features.Items;

    using Interfaces;

    /// <summary>
    /// Contains all information before a player uses an item.
    /// </summary>
    public class UsingItemCompletedEventArgs : IPlayerEvent, IDeniableEvent, IUsableEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsingItemCompletedEventArgs" /> class.
        /// </summary>
        /// <param name="player">The player who's going to use the item.</param>
        /// <param name="item">The items to be using.</param>
        /// <param name="continueProcess">Continue the using item process when the event is canceled.</param>
        public UsingItemCompletedEventArgs(Player player, Usable item, bool continueProcess)
        {
            Player = player;
            Usable = item;
            ContinueProcess = continueProcess;
        }

        /// <summary>
        /// Gets the item that the player using.
        /// </summary>
        public Usable Usable { get; }

        /// <inheritdoc/>
        public Item Item => Usable;

        /// <summary>
        /// Gets the player who using the item.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to continue the using item process when the event is canceled.
        /// </summary>
        public bool ContinueProcess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the player can use the item.
        /// </summary>
        public bool IsAllowed { get; set; } = true;
    }
}