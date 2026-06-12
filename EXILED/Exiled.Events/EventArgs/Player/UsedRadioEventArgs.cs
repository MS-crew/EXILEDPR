// -----------------------------------------------------------------------
// <copyright file="UsedRadioEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;
    using Exiled.API.Features.Items;

    using Interfaces;

    using InventorySystem.Items.Radio;

    /// <summary>
    /// Contains all information after a radio battery charge is changed.
    /// </summary>
    public class UsedRadioEventArgs : IPlayerEvent, IItemEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsedRadioEventArgs" /> class.
        /// </summary>
        /// <param name="player"><inheritdoc cref="Player"/></param>
        /// <param name="radio"><inheritdoc cref="Radio"/></param>
        /// <param name="drain"><inheritdoc cref="Drain"/></param>
        public UsedRadioEventArgs(Player player, RadioItem radio, float drain)
        {
            Player = player;
            Radio = Item.Get<Radio>(radio);
            Drain = drain;
        }

        /// <summary>
        /// Gets the player who used the radio.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets the <see cref="API.Features.Items.Radio" /> which was used.
        /// </summary>
        public Radio Radio { get; }

        /// <inheritdoc/>
        public Item Item => Radio;

        /// <summary>
        /// Gets the amount of battery that was drained.
        /// </summary>
        public float Drain { get; }
    }
}