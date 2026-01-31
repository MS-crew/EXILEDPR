// -----------------------------------------------------------------------
// <copyright file="ReceivingVoiceMessageEventArgs.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------using System;

namespace Exiled.Events.EventArgs.Player
{
    using System.Collections.Generic;

    using Exiled.API.Features;
    using Exiled.Events.EventArgs.Interfaces;

    using PlayerRoles.Voice;

    using VoiceChat.Networking;

    /// <summary>
    /// Contains all information before player receiving a voice message.
    /// </summary>
    public class ReceivingVoiceMessageEventArgs : IPlayerEvent, IDeniableEvent, IPoolableEvent
    {
        private static readonly Stack<ReceivingVoiceMessageEventArgs> Pool = new(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivingVoiceMessageEventArgs" /> class.
        /// </summary>
        /// <param name="receiver">The player receiving the voice message.</param>
        /// <param name="sender">The player sending the voice message.</param>
        /// <param name="voiceModule">The sender's voice module.</param>
        /// <param name="voiceMessage">The voice message being sent.</param>
        public ReceivingVoiceMessageEventArgs(Player receiver, Player sender, VoiceModuleBase voiceModule, VoiceMessage voiceMessage)
        {
            Sender = sender;
            Player = receiver;
            VoiceMessage = voiceMessage;
            VoiceModule = voiceModule;
        }

        private ReceivingVoiceMessageEventArgs()
        {
        }

        /// <summary>
        /// Gets the player receiving the voice message.
        /// </summary>
        public Player Player { get; private set; }

        /// <summary>
        /// Gets the player sended the voice message.
        /// </summary>
        public Player Sender { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="Sender"/>'s <see cref="VoiceMessage" />.
        /// </summary>
        public VoiceMessage VoiceMessage { get; set; }

        /// <summary>
        /// Gets the <see cref="Sender"/>'s <see cref="VoiceModuleBase" />.
        /// </summary>
        public VoiceModuleBase VoiceModule { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the player can receive the voice message.
        /// </summary>
        public bool IsAllowed { get; set; } = true;

        /// <summary>
        /// Rents an instance from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <param name="receiver">The player receiving the message.</param>
        /// <param name="sender">The player sending the message.</param>
        /// <param name="module">The sender's voice module.</param>
        /// <param name="msg">The voice message packet.</param>
        /// <returns>A reusable <see cref="ReceivingVoiceMessageEventArgs"/> instance.</returns>
        public static ReceivingVoiceMessageEventArgs Rent(Player receiver, Player sender, VoiceModuleBase module, VoiceMessage msg)
        {
            if (Pool.Count > 0)
            {
                ReceivingVoiceMessageEventArgs instance = Pool.Pop();
                instance.Init(receiver, sender, module, msg);
                return instance;
            }

            return new ReceivingVoiceMessageEventArgs(receiver, sender, module, msg);
        }

        /// <summary>
        /// Returns the instance to the pool and clears references to prevent memory leaks.
        /// </summary>
        public void Return()
        {
            Player = null;
            Sender = null;
            VoiceModule = null;
            VoiceMessage = default;
            IsAllowed = true;

            Pool.Push(this);
        }

        /// <summary>
        /// Resets the object state for reuse.
        /// </summary>
        private void Init(Player receiver, Player sender, VoiceModuleBase module, VoiceMessage msg)
        {
            Player = receiver;
            Sender = sender;
            VoiceModule = module;
            VoiceMessage = msg;
            IsAllowed = true;
        }
    }
}
