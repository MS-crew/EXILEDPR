// -----------------------------------------------------------------------
// <copyright file="WavUtility.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio
{
    using System.IO;

    using VoiceChat;

    /// <summary>
    /// Provides utility methods for working with WAV audio files, such as converting to PCM data and validating headers.
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
            using FileStream fs = File.OpenRead(path);
            using BinaryReader br = new(fs);

            SkipHeader(br);

            int samples = (int)((fs.Length - fs.Position) / 2);
            float[] pcm = new float[samples];

            for (int i = 0; i < samples; i++)
                pcm[i] = br.ReadInt16() / 32768f;

            return pcm;
        }

        /// <summary>
        /// Skips the WAV file header and validates that the format is PCM16 mono with the specified sample rate.
        /// </summary>
        /// <param name="br">The <see cref="BinaryReader"/> to read from.</param>
        /// <exception cref="InvalidDataException">
        /// Thrown if the WAV file is not PCM16, mono, or does not match the expected sample rate.
        /// </exception>
        public static void SkipHeader(BinaryReader br)
        {
            br.ReadBytes(12);

            while (true)
            {
                string chunk = new(br.ReadChars(4));
                int size = br.ReadInt32();

                if (chunk == "fmt ")
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
                else if (chunk == "data")
                {
                    return;
                }
                else
                {
                    br.BaseStream.Position += size;
                }
            }
        }
    }
}
