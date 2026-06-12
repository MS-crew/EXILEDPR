// -----------------------------------------------------------------------
// <copyright file="ChangedRadioPresetEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.API.Features.Items;

    using Interfaces;

    using InventorySystem.Items.Radio;

    using static InventorySystem.Items.Radio.RadioMessages;

    /// <summary>
    /// Contains all information after a radio preset is changed.
    /// </summary>
    public class ChangedRadioPresetEventArgs : IItemEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangedRadioPresetEventArgs" /> class.
        /// </summary>
        /// <param name="player">
        /// <inheritdoc cref="Player" />
        /// </param>
        /// <param name="item">
        /// <inheritdoc cref="Item" />
        /// </param>
        /// <param name="value">
        /// <inheritdoc cref="Value" />
        /// </param>
        public ChangedRadioPresetEventArgs(Player player, RadioItem item, RadioRangeLevel value)
        {
            Player = player;
            Radio = Item.Get<Radio>(item);
            Value = (RadioRange)value;
        }

        /// <summary>
        /// Gets the player who is using the radio.
        /// </summary>
        public Player Player { get; }

        /// <inheritdoc/>
        public Item Item => Radio;

        /// <summary>
        /// Gets the <see cref="API.Features.Items.Radio" /> which was used.
        /// </summary>
        public Radio Radio { get; }

        /// <summary>
        /// Gets the current radio preset value.
        /// </summary>
        public RadioRange Value { get; }
    }
}