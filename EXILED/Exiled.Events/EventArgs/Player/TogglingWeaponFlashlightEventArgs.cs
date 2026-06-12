// -----------------------------------------------------------------------
// <copyright file="TogglingWeaponFlashlightEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Features;
    using Exiled.API.Features.Items;

    using Interfaces;

    using BaseFirearm = InventorySystem.Items.Firearms.Firearm;

    /// <summary>
    /// Contains all information before a player toggles the weapon's flashlight.
    /// </summary>
    public class TogglingWeaponFlashlightEventArgs : IPlayerEvent, IFirearmEvent, IDeniableEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TogglingWeaponFlashlightEventArgs" /> class.
        /// </summary>
        /// <param name="player">
        /// <inheritdoc cref="Player" />
        /// </param>
        /// <param name="firearm">
        /// <inheritdoc cref="Firearm" />
        /// </param>
        /// <param name="newState">
        /// <inheritdoc cref="NewState" />
        /// </param>
        /// <param name="isAllowed">
        /// <inheritdoc cref="IsAllowed" />
        /// </param>
        public TogglingWeaponFlashlightEventArgs(Player player, BaseFirearm firearm, bool newState, bool isAllowed)
        {
            Player = player;
            Firearm = Item.Get<Firearm>(firearm);
            NewState = newState;
            IsAllowed = isAllowed;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the new weapon's flashlight state will be enabled.
        /// </summary>
        public bool NewState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the weapon's flashlight can be toggled.
        /// </summary>
        public bool IsAllowed { get; set; } = true;

        /// <summary>
        /// Gets the <see cref="API.Features.Items.Firearm" /> being held.
        /// </summary>
        public Firearm Firearm { get; }

        /// <inheritdoc/>
        public Item Item => Firearm;

        /// <summary>
        /// Gets the player who's toggling the weapon's flashlight.
        /// </summary>
        public Player Player { get; }
    }
}