// -----------------------------------------------------------------------
// <copyright file="FlippedCoinEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;
    using Exiled.API.Features.Items;

    using Interfaces;

    using InventorySystem.Items.Coin;

    /// <summary>
    /// Contains all information after a player flips a coin.
    /// </summary>
    public class FlippedCoinEventArgs : IPlayerEvent, IItemEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlippedCoinEventArgs" /> class.
        /// </summary>
        /// <param name="player"> <inheritdoc cref="Player"/> </param>
        /// <param name="coin"> <inheritdoc cref="Item"/> </param>
        /// <param name="isTails"> <inheritdoc cref="IsTails"/> </param>
        public FlippedCoinEventArgs(Player player, Coin coin, bool isTails)
        {
            Player = player;
            Item = Item.Get(coin);
            IsTails = isTails;
        }

        /// <summary>
        /// Gets the player who flipped the coin.
        /// </summary>
        public Player Player { get; }

        /// <inheritdoc/>
        public Item Item { get; }

        /// <summary>
        /// Gets a value indicating whether the coin landed on tails.
        /// </summary>
        public bool IsTails { get; }
    }
}