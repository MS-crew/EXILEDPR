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
        }

        /// <summary>
        /// Gets or sets a value indicating whether to stream the audio directly from the disk (<c>true</c>) or preload it entirely into RAM (<c>false</c>).
        /// </summary>
        public bool Stream { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to clear any upcoming tracks in the playlist before playing the new track.
        /// </summary>
        public bool ClearQueue { get; set; }
    }
}