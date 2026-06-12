// -----------------------------------------------------------------------
// <copyright file="ReceivedEffectEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Player
{
    using CustomPlayerEffects;

    using Exiled.API.Features;

    using Interfaces;

    /// <summary>
    /// Contains all information after a player received a <see cref="StatusEffectBase" />.
    /// </summary>
    public class ReceivedEffectEventArgs : IPlayerEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivedEffectEventArgs" /> class.
        /// </summary>
        /// <param name="player"><inheritdoc cref="Player"/></param>
        /// <param name="effect"><inheritdoc cref="Effect"/></param>
        /// <param name="intensity">The new intensity the effect.</param>
        /// <param name="duration"><inheritdoc cref="Duration"/></param>
        public ReceivedEffectEventArgs(Player player, StatusEffectBase effect, byte intensity, float duration)
        {
            Player = player;
            Effect = effect;
            Intensity = intensity;
            Duration = intensity is 0 ? 0 : duration;
        }

        /// <summary>
        /// Gets the <see cref="Player" /> receiving the effect.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets the <see cref="StatusEffectBase" /> being received.
        /// </summary>
        public StatusEffectBase Effect { get; }

        /// <summary>
        /// Gets the value indicating how long the effect will last. If its value is 0, then it doesn't always reflect the real effect duration.
        /// </summary>
        public float Duration { get; } = 0;

        /// <summary>
        /// Gets the value of the new intensity of this effect on the player.
        /// </summary>
        public byte Intensity { get; }
    }
}