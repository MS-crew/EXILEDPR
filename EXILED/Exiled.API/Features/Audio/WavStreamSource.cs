// -----------------------------------------------------------------------
// <copyright file="WavStreamSource.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio
{
    using System;
    using System.Buffers;
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
        /// Gets the total duration of the audio in seconds.
        /// </summary>
        public double TotalDuration => (endPosition - startPosition) / 2.0 / VoiceChatSettings.SampleRate;

        /// <summary>
        /// Gets or sets the current playback position in seconds.
        /// </summary>
        public double CurrentTime
        {
            get => (reader.BaseStream.Position - startPosition) / 2.0 / VoiceChatSettings.SampleRate;
            set => Seek(value);
        }

        /// <summary>
        /// Gets a value indicating whether the end of the stream has been reached.
        /// </summary>
        public bool Ended => reader.BaseStream.Position >= endPosition;

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

            byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(bytesNeeded);

            try
            {
                int bytesRead = reader.Read(tempBuffer, 0, bytesNeeded);

                if (bytesRead == 0)
                    return 0;

                if (bytesRead % 2 != 0)
                    bytesRead--;

                Span<byte> byteSpan = tempBuffer.AsSpan(0, bytesRead);
                Span<short> shortSpan = MemoryMarshal.Cast<byte, short>(byteSpan);

                int samplesRead = shortSpan.Length;
                for (int i = 0; i < samplesRead; i++)
                {
                    if (offset + i >= buffer.Length)
                        break;

                    buffer[offset + i] = shortSpan[i] / 32768f;
                }

                return samplesRead;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(tempBuffer);
            }
        }

        /// <summary>
        /// Seeks to the specified position in the stream.
        /// </summary>
        /// <param name="seconds">The position in seconds to seek to.</param>
        public void Seek(double seconds)
        {
            long targetSample = (long)(seconds * VoiceChatSettings.SampleRate);
            long targetByte = targetSample * 2;

            long newPos = startPosition + targetByte;
            if (newPos > endPosition)
                newPos = endPosition;

            if (newPos < startPosition)
                newPos = startPosition;

            if (newPos % 2 != 0)
                newPos--;

            reader.BaseStream.Position = newPos;
        }

        /// <summary>
        /// Resets the stream position to the start.
        /// </summary>
        public void Reset()
        {
            reader.BaseStream.Position = startPosition;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="WavStreamSource"/>.
        /// </summary>
        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
