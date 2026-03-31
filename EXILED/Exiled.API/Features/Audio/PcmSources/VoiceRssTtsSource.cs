// -----------------------------------------------------------------------
// <copyright file="VoiceRssTtsSource.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio.PcmSources
{
    using System;
    using System.Collections.Generic;

    using Exiled.API.Features;
    using Exiled.API.Interfaces.Audio;
    using Exiled.API.Structs.Audio;

    using MEC;

    using UnityEngine.Networking;

    /// <summary>
    /// Provides a <see cref="IPcmSource"/> that converts text to speech using the <see href="https://www.voicerss.org/">VoiceRSS</see> Text-to-Speech API.
    /// </summary>
    public sealed class VoiceRssTtsSource : IPcmSource
    {
        private const string ApiEndpoint = "https://api.voicerss.org/";
        private const string AudioFormat = "48khz_16bit_mono";

        private IPcmSource internalSource;
        private UnityWebRequest webRequest;
        private CoroutineHandle downloadRoutine;

        private bool isReady = false;
        private bool isFailed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceRssTtsSource"/> class.
        /// </summary>
        /// <param name="text"> The text to convert to speech.(Length limited by 100KB).</param>
        /// <param name="apiKey"> Your VoiceRSS API key. Get a free key at <see href="https://www.voicerss.org/registration.aspx"/>.</param>
        /// <param name="language"> The language and locale code for the TTS voice. See <see href="https://www.voicerss.org/api/"/> for all supported language codes.</param>
        /// <param name="voice"> Optional specific voice name for the selected language.(See <see href="https://www.voicerss.org/api/"/> for available voices per language.)</param>
        /// <param name="rate"> Speech rate from -10 (slowest) to 10 (fastest). Defaults to 0 (normal speed).</param>
        public VoiceRssTtsSource(string text, string apiKey, string language = "en-us", string voice = null, int rate = 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                isFailed = true;
                Log.Error("[VoiceRssTtsSource] Text cannot be null or empty.");
                throw new ArgumentException("Text cannot be null or empty.", nameof(text));
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                isFailed = true;
                Log.Error("[VoiceRssTtsSource] API key cannot be null or empty. Get a free key at https://www.voicerss.org/registration.aspx");
                throw new ArgumentException("API key cannot be null or empty. Get a free key at https://www.voicerss.org/registration.aspx", nameof(apiKey));
            }

            TrackInfo = new TrackData { Path = $"VoiceRssTts: {text}", Duration = 0.0 };
            downloadRoutine = Timing.RunCoroutine(DownloadRoutine(text, apiKey, language, voice, rate));
        }

        /// <summary>
        /// Gets the metadata of the loaded track.
        /// </summary>
        public TrackData TrackInfo { get; private set; }

        /// <summary>
        /// Gets the total duration of the audio in seconds. Returns 0 while the download is in progress.
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
        /// Gets a value indicating whether playback has ended or the download has failed.
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
        /// Seeks to the specified position in seconds.
        /// </summary>
        /// <param name="seconds">The target position in seconds.</param>
        public void Seek(double seconds)
        {
            if (isReady && internalSource != null)
                internalSource.Seek(seconds);
        }

        /// <summary>
        /// Resets playback to the beginning.
        /// </summary>
        public void Reset()
        {
            if (isReady && internalSource != null)
                internalSource.Reset();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="VoiceRssTtsSource"/>.
        /// </summary>
        public void Dispose()
        {
            if (downloadRoutine.IsRunning)
                downloadRoutine.IsRunning = false;

            webRequest?.Abort();
            webRequest?.Dispose();
            internalSource?.Dispose();
        }

        private IEnumerator<float> DownloadRoutine(string text, string apiKey, string language, string voice, int rate)
        {
            webRequest = null;
            string clampedRate = Math.Clamp(rate, -10, 10).ToString();
            string url = $"{ApiEndpoint}?key={Uri.EscapeDataString(apiKey)}&hl={Uri.EscapeDataString(language)}&c=WAV&f={AudioFormat}&r={clampedRate}&src={Uri.EscapeDataString(text)}";

            if (!string.IsNullOrEmpty(voice))
                url += $"&v={Uri.EscapeDataString(voice)}";

            try
            {
                webRequest = UnityWebRequest.Get(url);
            }
            catch (Exception ex)
            {
                Log.Error($"[VoiceRssTtsSource] Failed to start web request! URL: {url} | Error: {ex.Message}");
                isFailed = true;
                yield break;
            }

            yield return Timing.WaitUntilDone(webRequest.SendWebRequest());

            try
            {
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Log.Error($"[VoiceRssTtsSource] Download failed! Error: {webRequest.error}");
                    isFailed = true;
                    yield break;
                }

                string contentType = webRequest.GetResponseHeader("Content-Type") ?? string.Empty;
                if (!contentType.Contains("audio") && !contentType.Contains("wav") && !contentType.Contains("octet-stream"))
                {
                    string apiError = webRequest.downloadHandler.text;
                    Log.Error($"[VoiceRssTtsSource] API Error: {apiError}");
                    isFailed = true;
                    yield break;
                }

                byte[] rawBytes = webRequest.downloadHandler.data;
                AudioData audioData = WavUtility.WavToPcm(rawBytes);
                audioData.TrackInfo.Path = $"VoiceRSS: {text}";

                internalSource = new PreloadedPcmSource(audioData.Pcm);
                TrackInfo = audioData.TrackInfo;
                isReady = true;
            }
            catch (Exception e)
            {
                Log.Error($"[VoiceRssTtsSource] Parsing Error! Ensure the API returns a valid PCM16 WAV.\nDetails: {e.Message}");
                isFailed = true;
            }
            finally
            {
                webRequest?.Dispose();
                webRequest = null;
            }
        }
    }
}