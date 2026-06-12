// -----------------------------------------------------------------------
// <copyright file="UsingRadioEventArgs.cs" company="ExMod Team">
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
    /// Contains all information before radio battery charge is changed.
    /// </summary>
    public class UsingRadioEventArgs : IPlayerEvent, IDeniableEvent, IItemEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsingRadioEventArgs" /> class.
        /// </summary>
        /// <param name="player"> <inheritdoc cref="Player"/> </param>
        /// <param name="radio"> <inheritdoc cref="Radio"/> </param>
        /// <param name="drain"> <inheritdoc cref="Drain"/> </param>
        /// <param name="isAllowed"> <inheritdoc cref="IsAllowed"/> </param>
        public UsingRadioEventArgs(Player player, RadioItem radio, float drain, bool isAllowed = true)
        {
            Player = player;
            Radio = Item.Get<Radio>(radio);
            Drain = drain;
            IsAllowed = isAllowed;
        }

        /// <summary>
        /// Gets the player who's using the radio.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets the <see cref="API.Features.Items.Radio" /> which is being used.
        /// </summary>
        public Radio Radio { get; }

        /// <inheritdoc/>
        public Item Item => Radio;

        /// <summary>
        /// Gets or sets the radio battery drain per use.
        /// </summary>
        public float Drain { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the radio can be used.
        /// </summary>
        public bool IsAllowed { get; set; }
    }
}