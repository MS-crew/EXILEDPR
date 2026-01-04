// -----------------------------------------------------------------------
// <copyright file="PreloadedPcmSource.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio
{
    using System;

    using Exiled.API.Interfaces;

    /// <summary>
    /// Represents a preloaded PCM audio source.
    /// </summary>
    public sealed class PreloadedPcmSource : IPcmSource
    {
        /// <summary>
        /// The PCM data buffer.
        /// </summary>
        private readonly float[] data;

        /// <summary>
        /// The current read position in the data buffer.
        /// </summary>
        private int pos;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreloadedPcmSource"/> class.
        /// </summary>
        /// <param name="pcmData">The raw PCM float array.</param>
        public PreloadedPcmSource(float[] pcmData)
        {
            data = pcmData;
        }

        /// <summary>
        /// Gets a value indicating whether the end of the PCM data buffer has been reached.
        /// </summary>
        public bool Ended
        {
            get
            {
                return pos >= data.Length;
            }
        }

        /// <summary>
        /// Reads a sequence of PCM samples from the preloaded buffer into the specified array.
        /// </summary>
        /// <param name="buffer">The destination array to copy the samples into.</param>
        /// <param name="offset">The zero-based index in <paramref name="buffer"/> at which to begin storing the data.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The number of samples read into <paramref name="buffer"/>.</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int read = Math.Min(count, data.Length - pos);
            Array.Copy(data, pos, buffer, offset, read);
            pos += read;

            return read;
        }

        /// <summary>
        /// Resets the read position to the beginning of the PCM data buffer.
        /// </summary>
        public void Reset()
        {
            pos = 0;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="PreloadedPcmSource"/>.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
