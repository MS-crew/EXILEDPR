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
    using System.Buffers.Binary;
    using System.IO;
    using System.Runtime.InteropServices;

    using VoiceChat;

    /// <summary>
    /// Provides utility methods for working with WAV audio files.
    /// </summary>
    public static class WavUtility
    {
        private const float Divide = 1f / 32768f;

        /// <summary>
        /// Converts a WAV file at the specified path to a PCM float array.
        /// </summary>
        /// <param name="path">The file path of the WAV file to convert.</param>
        /// <returns>A tuple containing an array of floats representing the PCM data and its TrackData.</returns>
        public static (float[] PcmData, TrackData TrackInfo) WavToPcm(string path)
        {
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            int length = (int)fs.Length;

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                int bytesRead = fs.Read(rentedBuffer, 0, length);

                using MemoryStream ms = new(rentedBuffer, 0, bytesRead);

                TrackData metaData = SkipHeader(ms);

                int headerOffset = (int)ms.Position;
                int dataLength = bytesRead - headerOffset;

                Span<byte> audioDataSpan = rentedBuffer.AsSpan(headerOffset, dataLength);
                Span<short> samples = MemoryMarshal.Cast<byte, short>(audioDataSpan);

                float[] pcm = new float[samples.Length];

                for (int i = 0; i < samples.Length; i++)
                    pcm[i] = samples[i] * Divide;

                metaData.Path = path;
                return (pcm, metaData);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        /// <summary>
        /// Skips the WAV file header and validates that the format is PCM16 mono with the specified sample rate.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="TrackData"/> struct containing the parsed file information.</returns>
        public static TrackData SkipHeader(Stream stream)
        {
            TrackData trackData = new();

            Span<byte> headerBuffer = stackalloc byte[12];
            stream.Read(headerBuffer);

            int rate = 0;
            int bits = 0;
            int channels = 0;

            Span<byte> chunkHeader = stackalloc byte[8];
            while (true)
            {
                int read = stream.Read(chunkHeader);
                if (read < 8)
                    break;

                uint chunkId = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader[..4]);
                int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(chunkHeader.Slice(4, 4));

                // 'fmt ' chunk
                if (chunkId == 0x20746D66)
                {
                    Span<byte> fmtData = stackalloc byte[16];
                    stream.Read(fmtData);

                    short format = BinaryPrimitives.ReadInt16LittleEndian(fmtData[..2]);
                    channels = BinaryPrimitives.ReadInt16LittleEndian(fmtData.Slice(2, 2));
                    rate = BinaryPrimitives.ReadInt32LittleEndian(fmtData.Slice(4, 4));
                    bits = BinaryPrimitives.ReadInt16LittleEndian(fmtData.Slice(14, 2));

                    if (format != 1 || channels != 1 || rate != VoiceChatSettings.SampleRate || bits != 16)
                    {
                        Log.Error($"[Speaker] Invalid WAV format (format={format}, channels={channels}, rate={rate}, bits={bits}). Expected PCM16, mono and {VoiceChatSettings.SampleRate}Hz.");
                        throw new InvalidDataException("Unsupported WAV format.");
                    }

                    if (chunkSize > 16)
                        stream.Seek(chunkSize - 16, SeekOrigin.Current);
                }

                // 'LIST' chunk
                else if (chunkId == 0x5453494C)
                {
                    Span<byte> listType = stackalloc byte[4];
                    stream.Read(listType);
                    uint type = BinaryPrimitives.ReadUInt32LittleEndian(listType);

                    // 'INFO' chunk
                    if (type == 0x4F464E49)
                    {
                        int bytesToRead = chunkSize - 4;
                        byte[] infoBytes = ArrayPool<byte>.Shared.Rent(bytesToRead);
                        stream.Read(infoBytes, 0, bytesToRead);

                        int offset = 0;
                        while (offset < bytesToRead - 8)
                        {
                            uint infoId = BinaryPrimitives.ReadUInt32LittleEndian(infoBytes.AsSpan(offset, 4));
                            int infoSize = BinaryPrimitives.ReadInt32LittleEndian(infoBytes.AsSpan(offset + 4, 4));
                            offset += 8;

                            if (infoSize > 0 && offset + infoSize <= bytesToRead)
                            {
                                string value = System.Text.Encoding.UTF8.GetString(infoBytes, offset, infoSize).TrimEnd('\0');

                                if (infoId == 0x4D414E49)
                                    trackData.Title = value;
                                else if (infoId == 0x54524149)
                                    trackData.Artist = value;
                            }

                            offset += infoSize;
                            if (infoSize % 2 != 0)
                                offset++;
                        }

                        ArrayPool<byte>.Shared.Return(infoBytes);
                    }
                    else
                    {
                        stream.Seek(chunkSize - 4, SeekOrigin.Current);
                    }
                }

                // 'data' chunk
                else if (chunkId == 0x61746164)
                {
                    int bytesPerSample = bits / 8;
                    if (bytesPerSample > 0 && channels > 0 && rate > 0)
                        trackData.Duration = (double)chunkSize / (rate * channels * bytesPerSample);

                    return trackData;
                }
                else
                {
                    stream.Seek(chunkSize, SeekOrigin.Current);
                }

                if (stream.Position >= stream.Length)
                {
                    Log.Error("[Speaker] WAV file does not contain a 'data' chunk.");
                    throw new InvalidDataException("Missing 'data' chunk in WAV file.");
                }
            }

            return trackData;
        }
    }
}