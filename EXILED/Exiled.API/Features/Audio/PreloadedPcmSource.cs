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
    public class PreloadedPcmSource : IPcmSource
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
        /// <param name="pcm">The PCM data to preload.</param>
        public PreloadedPcmSource(float[] pcm)
        {
            data = pcm;
        }

        /// <inheritdoc/>
        public bool Ended
        {
            get
            {
                return pos >= data.Length;
            }
        }

        /// <inheritdoc/>
        public int Read(float[] buffer, int offset, int count)
        {
            int read = Math.Min(count, data.Length - pos);
            Array.Copy(data, pos, buffer, offset, read);
            pos += read;

            return read;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            pos = 0;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
