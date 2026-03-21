// -----------------------------------------------------------------------
// <copyright file="AudioPlaybackOptions.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Structs
{
    /// <summary>
    /// Represents the configuration options for audio playback.
    /// </summary>
    public struct AudioPlaybackOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlaybackOptions"/> struct with default values.
        /// </summary>
        public AudioPlaybackOptions()
        {
            Stream = false;
            ClearQueue = false;
            FadeInDuration = 0f;
            FadeOutDuration = 0f;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to stream the audio directly from the disk (<c>true</c>) or preload it entirely into RAM (<c>false</c>).
        /// </summary>
        public bool Stream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the speaker object should be automatically destroyed after the playback finishes.
        /// </summary>
        public bool DestroyAfter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audio should loop continuously.
        /// </summary>
        public bool Loop { get; set; }

        /// <summary>
        /// Gets or sets the duration in seconds over which the volume should smoothly increase from 0 to the target volume at the start of playback.
        /// <c>0</c> means no fade-in.
        /// </summary>
        public float FadeInDuration { get; set; }

        /// <summary>
        /// Gets or sets the duration in seconds over which the volume should smoothly decrease to 0 before the track ends automatically.
        /// <c>0</c> means no fade-out.
        /// </summary>
        public float FadeOutDuration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to clear any upcoming tracks in the playlist before playing the new track.
        /// </summary>
        public bool ClearQueue { get; set; }
    }
}