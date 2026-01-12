// -----------------------------------------------------------------------
// <copyright file="WavUtility.cs" company="ExMod Team">
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

    using VoiceChat;

    /// <summary>
    /// Provides utility methods for working with WAV audio files.
    /// </summary>
    public static class WavUtility
    {
        /// <summary>
        /// Converts a WAV file at the specified path to a PCM float array.
        /// </summary>
        /// <param name="path">The file path of the WAV file to convert.</param>
        /// <returns>An array of floats representing the PCM data.</returns>
        public static float[] WavToPcm(string path)
        {
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            int length = (int)fs.Length;

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                int bytesRead = fs.Read(rentedBuffer, 0, length);

                using MemoryStream ms = new(rentedBuffer, 0, bytesRead);
                using BinaryReader br = new(ms);

                SkipHeader(br);

                int headerOffset = (int)ms.Position;
                int dataLength = bytesRead - headerOffset;

                Span<byte> audioDataSpan = rentedBuffer.AsSpan(headerOffset, dataLength);
                Span<short> samples = MemoryMarshal.Cast<byte, short>(audioDataSpan);

                float[] pcm = new float[samples.Length];
                for (int i = 0; i < samples.Length; i++)
                    pcm[i] = samples[i] / 32768f;

                return pcm;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        /// <summary>
        /// Skips the WAV file header and validates that the format is PCM16 mono with the specified sample rate.
        /// </summary>
        /// <param name="br">The <see cref="BinaryReader"/> to read from.</param>
        public static void SkipHeader(BinaryReader br)
        {
            br.ReadBytes(12);

            while (true)
            {
                uint chunk = br.ReadUInt32();
                int size = br.ReadInt32();

                // 'fmt ' chunk
                if (chunk == 0x20746D66)
                {
                    short format = br.ReadInt16();
                    short channels = br.ReadInt16();
                    int rate = br.ReadInt32();
                    br.ReadInt32();
                    br.ReadInt16();
                    short bits = br.ReadInt16();

                    if (format != 1 || channels != 1 || rate != VoiceChatSettings.SampleRate || bits != 16)
                        Log.Error($"Invalid WAV format (format={format}, channels={channels}, rate={rate}, bits={bits}). Expected PCM16, mono and {VoiceChatSettings.SampleRate}Hz.");

                    br.BaseStream.Position += size - 16;
                }

                // 'data' chunk
                else if (chunk == 0x61746164)
                {
                    return;
                }
                else
                {
                    br.BaseStream.Position += size;
                }

                if (br.BaseStream.Position >= br.BaseStream.Length)
                    throw new InvalidDataException("WAV file does not contain a 'data' chunk. File might be corrupted or empty.");
            }
        }
    }
}
