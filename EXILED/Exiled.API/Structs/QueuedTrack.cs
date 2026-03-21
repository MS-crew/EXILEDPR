// -----------------------------------------------------------------------
// <copyright file="QueuedTrack.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Structs
{
    /// <summary>
    /// Represents a track waiting in the queue, along with its specific playback options.
    /// </summary>
    public readonly struct QueuedTrack
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueuedTrack"/> struct.
        /// </summary>
        /// <param name="path">The path to the .wav file.</param>
        /// <param name="options">The specific playback configuration for this track.</param>
        public QueuedTrack(string path, AudioPlaybackOptions options = default)
        {
            Path = path;
            Options = options;
        }

        /// <summary>
        /// Gets the absolute path to the .wav file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the playback options configured for this specific track.
        /// </summary>
        public AudioPlaybackOptions Options { get; }
    }
}
