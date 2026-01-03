// -----------------------------------------------------------------------
// <copyright file="WavStreamSource.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    using Exiled.API.Interfaces;

    using VoiceChat;

    /// <summary>
    /// Provides a PCM audio source from a WAV file stream.
    /// </summary>
    public sealed class WavStreamSource : IPcmSource
    {
        private readonly long endPosition;
        private readonly long startPosition;
        private readonly BinaryReader reader;
        private readonly byte[] readBuffer = new byte[VoiceChatSettings.PacketSizePerChannel * 2];

        /// <summary>
        /// Initializes a new instance of the <see cref="WavStreamSource"/> class.
        /// </summary>
        /// <param name="path">The path to the audio file.</param>
        public WavStreamSource(string path)
        {
            reader = new BinaryReader(File.OpenRead(path));
            WavUtility.SkipHeader(reader);
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
            int bytesNeeded = count * 2;

            if (bytesNeeded > readBuffer.Length)
                bytesNeeded = readBuffer.Length;

            int bytesRead = reader.Read(readBuffer, 0, bytesNeeded);
            if (bytesRead == 0)
                return 0;

            if (bytesRead % 2 != 0)
                bytesRead--;

            Span<byte> byteSpan = readBuffer.AsSpan(0, bytesRead);
            Span<short> shortSpan = MemoryMarshal.Cast<byte, short>(byteSpan);

            int samplesRead = shortSpan.Length;
            for (int i = 0; i < samplesRead; i++)
                buffer[offset + i] = shortSpan[i] / 32768f;

            return samplesRead;
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
