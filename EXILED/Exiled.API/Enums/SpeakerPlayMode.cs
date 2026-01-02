// -----------------------------------------------------------------------
// <copyright file="SpeakerPlayMode.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Enums
{
    /// <summary>
    /// Specifies the available modes for playing audio through a speaker.
    /// </summary>
    public enum SpeakerPlayMode
    {
        /// <summary>
        /// Play audio globally to all players.
        /// </summary>
        Global,

        /// <summary>
        /// Play audio to a specific list of players.
        /// </summary>
        PlayerList,

        /// <summary>
        /// Play audio to players matching a predicate.
        /// </summary>
        Predicate,
    }
}
