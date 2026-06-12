// -----------------------------------------------------------------------
// <copyright file="ToggledWeaponFlashlightEventArgs.cs" company="ExMod Team">
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
    /// Contains all information after a player toggles the weapon's flashlight.
    /// </summary>
    public class ToggledWeaponFlashlightEventArgs : IPlayerEvent, IFirearmEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToggledWeaponFlashlightEventArgs" /> class.
        /// </summary>
        /// <param name="player"><inheritdoc cref="Player" /></param>
        /// <param name="firearm"><inheritdoc cref="Firearm" /></param>
        /// <param name="newState"><inheritdoc cref="NewState" /></param>
        public ToggledWeaponFlashlightEventArgs(Player player, BaseFirearm firearm, bool newState)
        {
            Player = player;
            Firearm = Item.Get<Firearm>(firearm);
            NewState = newState;
        }

        /// <summary>
        /// Gets a value indicating whether the new weapon's flashlight state is enabled.
        /// </summary>
        public bool NewState { get; }

        /// <summary>
        /// Gets the <see cref="API.Features.Items.Firearm" /> being held.
        /// </summary>
        public Firearm Firearm { get; }

        /// <inheritdoc/>
        public Item Item => Firearm;

        /// <summary>
        /// Gets the player who toggled the weapon's flashlight.
        /// </summary>
        public Player Player { get; }
    }
}