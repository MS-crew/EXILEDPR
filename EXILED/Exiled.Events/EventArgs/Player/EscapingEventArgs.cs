// -----------------------------------------------------------------------
// <copyright file="EscapingEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using Exiled.API.Enums;
    using Exiled.API.Features;

    using Interfaces;

    using PlayerRoles;

    using UnityEngine;

    /// <summary>
    /// Contains all information before a player escapes.
    /// </summary>
    public class EscapingEventArgs : IPlayerEvent, IDeniableEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EscapingEventArgs" /> class.
        /// </summary>
        /// <param name="player"> <inheritdoc cref="Player"/> </param>
        /// <param name="newRole"> <inheritdoc cref="NewRole"/> </param>
        /// <param name="escapeScenario"> <inheritdoc cref="EscapeScenario"/> </param>
        /// <param name="escapeBounds"> <inheritdoc cref="EscapeZone"/> </param>
        /// <param name="isAllowed"> <inheritdoc cref="IsAllowed"/> </param>
        public EscapingEventArgs(Player player, RoleTypeId newRole, EscapeScenario escapeScenario, Bounds escapeBounds, bool isAllowed)
        {
            Player = player;
            NewRole = newRole;
            EscapeScenario = escapeScenario;
            EscapeZone = escapeBounds;
            IsAllowed = escapeScenario is not EscapeScenario.None;
        }

        /// <summary>
        /// Gets the player who's escaping.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets or sets the role that will be assigned when the player escapes.
        /// </summary>
        public RoleTypeId NewRole { get; set; }

        /// <summary>
        /// Gets or sets the EscapeScenario that will represent for this player.
        /// </summary>
        public EscapeScenario EscapeScenario
        {
            get => (field is EscapeScenario.None && IsAllowed) ? EscapeScenario.CustomEscape : field;
            set;
        }

        /// <summary>
        ///  Gets the which escape bounds did player escaping from.
        /// </summary>
        public Bounds EscapeZone { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the player can escape.
        /// </summary>
        public bool IsAllowed { get; set; }
    }
}