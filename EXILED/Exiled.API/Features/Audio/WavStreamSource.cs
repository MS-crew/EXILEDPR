// -----------------------------------------------------------------------
// <copyright file="WavStreamSource.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio
{
    using System.IO;

    using Exiled.API.Features.Toys;
    using Exiled.API.Interfaces;

    /// <summary>
    /// Provides a PCM audio source from a WAV file stream.
    /// </summary>
    public class WavStreamSource : IPcmSource
    {
        private readonly long endPosition;
        private readonly long startPosition;
        private readonly BinaryReader reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="WavStreamSource"/> class.
        /// </summary>
        /// <param name="path">The path to the audio file.</param>
        public WavStreamSource(string path)
        {
            reader = new BinaryReader(File.OpenRead(path));
            Speaker.SkipWavHeader(reader);
            startPosition = reader.BaseStream.Position;
            endPosition = reader.BaseStream.Length;
        }

        /// <summary>
        /// Gets a value indicating whether the end of the stream has been reached.
        /// </summary>
        public bool Ended
        {
            get
            {
                return reader.BaseStream.Position >= endPosition;
            }
        }

        /// <summary>
        /// Reads PCM data from the stream into the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to fill with PCM data.</param>
        /// <param name="offset">The offset in the buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The number of samples read.</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int i = 0;

            while (i < count && reader.BaseStream.Position < endPosition)
            {
                buffer[offset + i] = reader.ReadInt16() / 32768f;
                i++;
            }

            return i;
        }

        /// <summary>
        /// Resets the stream position to the start.
        /// </summary>
        public void Reset() => reader.BaseStream.Position = startPosition;

        /// <summary>
        /// Releases all resources used by the <see cref="WavStreamSource"/>.
        /// </summary>
        public void Dispose() => reader.Dispose();
    }
}
