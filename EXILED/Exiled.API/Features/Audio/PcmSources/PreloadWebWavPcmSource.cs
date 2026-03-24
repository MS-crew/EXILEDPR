// -----------------------------------------------------------------------
// <copyright file="PreloadWebWavPcmSource.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio.PcmSources
{
    using System;
    using System.Collections.Generic;

    using Exiled.API.Features;
    using Exiled.API.Interfaces;
    using Exiled.API.Structs;

    using MEC;

    using UnityEngine.Networking;

    /// <summary>
    /// Provides a <see cref="IPcmSource"/> that downloads a .wav file from a URL and preloads it for playback.
    /// </summary>
    public sealed class PreloadWebWavPcmSource : IPcmSource
    {
        private IPcmSource internalSource;

        private bool isReady = false;
        private bool isFailed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreloadWebWavPcmSource"/> class.
        /// </summary>
        /// <param name="url">The direct URL to the .wav file.</param>
        public PreloadWebWavPcmSource(string url)
        {
            TrackInfo = default;
            Timing.RunCoroutine(Download(url));
        }

        /// <summary>
        /// Gets the metadata of the preloaded track.
        /// </summary>
        public TrackData TrackInfo { get; private set; }

        /// <summary>
        /// Gets the total duration of the audio in seconds.
        /// </summary>
        public double TotalDuration => isReady && internalSource != null ? internalSource.TotalDuration : 0.0;

        /// <summary>
        /// Gets or sets the current playback position in seconds.
        /// </summary>
        public double CurrentTime
        {
            get => isReady && internalSource != null ? internalSource.CurrentTime : 0.0;
            set => Seek(value);
        }

        /// <summary>
        /// Gets a value indicating whether the end of the playback has been reached.
        /// </summary>
        public bool Ended => isFailed || (isReady && internalSource != null && internalSource.Ended);

        /// <summary>
        /// Reads PCM data from the audio source into the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to fill with PCM data.</param>
        /// <param name="offset">The offset in the buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The number of samples read.</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            if (isFailed)
                return 0;

            if (!isReady || internalSource == null)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            return internalSource.Read(buffer, offset, count);
        }

        /// <summary>
        /// Seeks to the specified position in the playback.
        /// </summary>
        /// <param name="seconds">The position in seconds to seek to.</param>
        public void Seek(double seconds)
        {
            if (isReady && internalSource != null)
                internalSource.CurrentTime = seconds;
        }

        /// <summary>
        /// Resets the playback position to the start.
        /// </summary>
        public void Reset()
        {
            if (isReady && internalSource != null)
                internalSource.Reset();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="PreloadWebWavPcmSource"/>.
        /// </summary>
        public void Dispose() => internalSource?.Dispose();

        private IEnumerator<float> Download(string url)
        {
            using UnityWebRequest www = UnityWebRequest.Get(url);
            yield return Timing.WaitUntilDone(www.SendWebRequest());

            if (www.result != UnityWebRequest.Result.Success)
            {
                Log.Error($"[WebPreloadWavPcmSource] Failed to download audio! URL: {url} | Error: {www.error}");
                isFailed = true;
                yield break;
            }

            try
            {
                byte[] rawBytes = www.downloadHandler.data;
                (float[] pcmData, TrackData trackInfo) = WavUtility.WavToPcm(rawBytes);
                trackInfo.Path = url;

                internalSource = new PreloadedPcmSource(pcmData);
                TrackInfo = trackInfo;
                isReady = true;
            }
            catch (Exception e)
            {
                Log.Error($"[WebPreloadWavPcmSource] Failed to read the downloaded file! Ensure the link points to a valid .WAV file.\nException Details: {e.Message}");
                isFailed = true;
            }
        }
    }
}