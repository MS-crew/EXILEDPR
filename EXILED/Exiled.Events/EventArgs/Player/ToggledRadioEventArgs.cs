// -----------------------------------------------------------------------
// <copyright file="ToggledRadioEventArgs.cs" company="ExMod Team">
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
    /// Contains all information after a player toggles a radio.
    /// </summary>
    public class ToggledRadioEventArgs : IPlayerEvent, IItemEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToggledRadioEventArgs" /> class.
        /// </summary>
        /// <param name="player"> <inheritdoc cref="Player"/> </param>
        /// <param name="radio"> <inheritdoc cref="Radio"/> </param>
        /// <param name="newState"> <inheritdoc cref="NewState"/> </param>
        public ToggledRadioEventArgs(Player player, RadioItem radio, bool newState)
        {
            Player = player;
            Radio = Item.Get<Radio>(radio);
            NewState = newState;
        }

        /// <summary>
        /// Gets the <see cref="API.Features.Items.Radio" /> which was toggled.
        /// </summary>
        public Radio Radio { get; }

        /// <inheritdoc/>
        public Item Item => Radio;

        /// <summary>
        /// Gets a value indicating whether the radio is now turned on.
        /// </summary>
        public bool NewState { get; }

        /// <summary>
        /// Gets the player who toggled the radio.
        /// </summary>
        public Player Player { get; }
    }
}