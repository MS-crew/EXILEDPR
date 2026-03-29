// -----------------------------------------------------------------------
// <copyright file="PlaybackSettings.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio
{
    using System;
    using System.Collections.Generic;

    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.API.Features.Toys;
    using Exiled.API.Interfaces.Audio;

    /// <summary>
    /// Represents all configurable audio and network settings for play from pool method.
    /// </summary>
    public class PlaybackSettings
    {
        /// <summary>
        /// Gets or sets the volume level.
        /// </summary>
        public float Volume { get; set; } = Speaker.DefaultVolume;

        /// <summary>
        /// Gets or sets the playback pitch.
        /// </summary>
        public float Pitch { get; set; } = 1f;

        /// <summary>
        /// Gets or sets a value indicating whether the audio source is spatialized (3D sound).
        /// </summary>
        public bool IsSpatial { get; set; } = Speaker.DefaultSpatial;

        /// <summary>
        /// Gets or sets the minimum distance at which the audio reaches full volume.
        /// </summary>
        public float MinDistance { get; set; } = Speaker.DefaultMinDistance;

        /// <summary>
        /// Gets or sets the maximum distance at which the audio can be heard.
        /// </summary>
        public float MaxDistance { get; set; } = Speaker.DefaultMaxDistance;

        /// <summary>
        /// Gets or sets a value indicating whether the file should be streamed from disk.
        /// </summary>
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to load the audio via the Cache Manager for optimize playback.
        /// </summary>
        public bool UseCache { get; set; } = false;

        /// <summary>
        /// Gets or sets the play mode determining how the audio is sent to players.
        /// </summary>
        public SpeakerPlayMode PlayMode { get; set; } = SpeakerPlayMode.Global;

        /// <summary>
        /// Gets or sets the target player (used when PlayMode is Player).
        /// </summary>
        public Player TargetPlayer { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of target players (used when PlayMode is PlayerList).
        /// </summary>
        public HashSet<Player> TargetPlayers { get; set; } = null;

        /// <summary>
        /// Gets or sets the condition used to determine which players hear the audio.
        /// </summary>
        public Func<Player, bool> Predicate { get; set; } = null;

        /// <summary>
        /// Gets or sets an optional custom audio filter to apply to the PCM data.
        /// </summary>
        public IAudioFilter Filter { get; set; } = null;
    }
}