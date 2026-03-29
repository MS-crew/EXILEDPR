// -----------------------------------------------------------------------
// <copyright file="CachedPcmSource.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio.PcmSources
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;

    using Exiled.API.Features.Audio;
    using Exiled.API.Interfaces.Audio;
    using Exiled.API.Structs.Audio;

    using MEC;

    using RoundRestarting;

    using UnityEngine.Networking;

    using VoiceChat;

    /// <summary>
    /// Provides an <see cref="IPcmSource"/> that caches audio data in memory for optimize, repeated playback. Also serves as the central audio cache manager for the server.
    /// </summary>
    public sealed class CachedPcmSource : IPcmSource
    {
        private readonly float[] data;
        private int pos;

        static CachedPcmSource() => RoundRestart.OnRestartTriggered += ClearCacheOnRestart;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedPcmSource"/> class or local WAV files or fetches already cached audio, assigning a custom name to a specific local file path.
        /// </summary>
        /// <para>NOTE: URLs cannot be loaded directly here. Use <see cref="AddUrlSource(string, string)"/>.</para>
        /// <param name="name">The custom name/key to assign to this audio in the cache.</param>
        /// <param name="path">The absolute path to the local audio file.</param>
        public CachedPcmSource(string name, string path)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
            {
                Log.Error($"[CachedPcmSource] Cannot initialize CachedPcmSource. Invalid name: '{name}' or path: '{path}'.");
                throw new ArgumentException("Name and path cannot be null or empty.");
            }

            if (AudioCache.TryGetValue(name, out AudioData cachedAudio))
            {
                data = cachedAudio.Pcm;
                TrackInfo = cachedAudio.TrackInfo;
                Log.Info($"[CachedPcmSource] Loaded audio from cache for key '{name}'.");
                return;
            }

            if (!AddSource(name, path))
            {
                Log.Error($"[CachedPcmSource] Failed to load local file '{path}' into cache under the name '{name}'.");
                throw new FileNotFoundException($"Failed to cache and load '{path}'.");
            }

            if (AudioCache.TryGetValue(name, out AudioData createdAudio))
            {
                data = createdAudio.Pcm;
                TrackInfo = createdAudio.TrackInfo;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedPcmSource"/> class by directly injecting raw PCM audio samples into the cache under a custom name.
        /// </summary>
        /// <param name="name">The custom name/key to assign to this audio in the cache.</param>
        /// <param name="pcm">The raw PCM audio samples (float array).</param>
        public CachedPcmSource(string name, float[] pcm)
        {
            if (string.IsNullOrEmpty(name) || pcm == null || pcm.Length == 0)
            {
                Log.Error($"[CachedPcmSource] Cannot initialize CachedPcmSource. Invalid name or empty PCM data for '{name}'.");
                throw new ArgumentException("Name cannot be null/empty and PCM data cannot be null/empty.");
            }

            if (AudioCache.TryGetValue(name, out AudioData cachedAudio))
            {
                data = cachedAudio.Pcm;
                TrackInfo = cachedAudio.TrackInfo;
                Log.Info($"[CachedPcmSource] Loaded audio from cache for key '{name}'.");
                return;
            }

            if (!AddSource(name, pcm))
            {
                Log.Error($"[CachedPcmSource] Failed to load raw PCM data into cache under the name '{name}'.");
                throw new InvalidOperationException($"Failed to cache PCM data for '{name}'.");
            }

            if (AudioCache.TryGetValue(name, out AudioData createdAudio))
            {
                data = createdAudio.Pcm;
                TrackInfo = createdAudio.TrackInfo;
                Log.Info($"[CachedPcmSource] Successfully cached raw PCM data as '{name}'.");
            }
        }

        /// <summary>
        /// Gets the global audio cache dictionary.
        /// </summary>
        public static ConcurrentDictionary<string, AudioData> AudioCache { get; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the global audio cache should be cleared when a round restart is triggered.
        /// </summary>
        public static bool ClearCacheOnRoundRestart { get; set; } = true;

        /// <summary>
        /// Gets the metadata of the loaded track.
        /// </summary>
        public TrackData TrackInfo { get; }

        /// <summary>
        /// Gets a value indicating whether the end of the PCM data buffer has been reached.
        /// </summary>
        public bool Ended => pos >= data.Length;

        /// <summary>
        /// Gets the total duration of the audio in seconds.
        /// </summary>
        public double TotalDuration => (double)data.Length / VoiceChatSettings.SampleRate;

        /// <summary>
        /// Gets or sets the current playback position in seconds.
        /// </summary>
        public double CurrentTime
        {
            get => (double)pos / VoiceChatSettings.SampleRate;
            set => Seek(value);
        }

        /// <summary>
        /// Loads a local Wav file and adds it to the cache.
        /// </summary>
        /// <param name="name">The custom name/key to assign.</param>
        /// <param name="path">The absolute path to the local Wav file.</param>
        /// <returns><c>true</c> if successfully read and cached; otherwise, <c>false</c>.</returns>
        public static bool AddSource(string name, string path)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
            {
                Log.Error($"[CachedPcmSource] Cannot add source. Invalid name: '{name}' or path: '{path}'.");
                return false;
            }

            if (path.StartsWith("http"))
            {
                Log.Error($"[CachedPcmSource] Use Timing.RunCoroutine(CachedPreloadedPcmSource.AddUrlCoroutine(...)) for URLs! Path: '{path}'");
                return false;
            }

            if (AudioCache.ContainsKey(name))
            {
                Log.Info($"[CachedPcmSource] A source with the name '{name}' already exists in the cache. Skipping addition.");
                return false;
            }

            if (!File.Exists(path))
            {
                Log.Error($"[CachedPcmSource] Local file not found: '{path}'");
                return false;
            }

            try
            {
                AudioData parsedData = WavUtility.WavToPcm(path);
                AudioCache.TryAdd(name, parsedData);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[CachedPcmSource] Failed to cache local file '{path}':\n{ex}");
                return false;
            }
        }

        /// <summary>
        /// Directly adds raw PCM data and track information to the cache.
        /// </summary>
        /// <param name="name">The custom name/key to assign.</param>
        /// <param name="pcm">The raw PCM audio samples.</param>
        /// <returns><c>true</c> if successfully added; otherwise, <c>false</c>.</returns>
        public static bool AddSource(string name, float[] pcm) => AddSource(name, new AudioData { Pcm = pcm, TrackInfo = new TrackData { Title = name, Duration = (double)pcm.Length / VoiceChatSettings.SampleRate } });

        /// <summary>
        /// Directly adds raw PCM data and track information to the cache.
        /// </summary>
        /// <param name="name">The custom name/key to assign.</param>
        /// <param name="audioData">The <see cref="AudioData"/> struct containing PCM samples and metadata.</param>
        /// <returns><c>true</c> if successfully added; otherwise, <c>false</c>.</returns>
        public static bool AddSource(string name, AudioData audioData)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.Error($"[CachedPcmSource] Cannot add source. Invalid name: '{name}'.");
                return false;
            }

            if (audioData.Equals(default(AudioData)) || audioData.Pcm == null)
            {
                Log.Error($"[CachedPcmSource] Cannot add source. AudioData is empty for name: '{name}'.");
                return false;
            }

            if (AudioCache.ContainsKey(name))
            {
                Log.Info($"[CachedPcmSource] A source with the name '{name}' already exists in the cache. Skipping addition.");
                return false;
            }

            return AudioCache.TryAdd(name, audioData);
        }

        /// <summary>
        /// Asynchronously downloads a Web URL and adds it to the cache.
        /// </summary>
        /// <param name="name">The custom name/key to assign.</param>
        /// <param name="url">The HTTP/HTTPS URL to the Wav file.</param>
        /// <returns>A float IEnumerator for MEC execution.</returns>
        public static CoroutineHandle AddUrlSource(string name, string url) => Timing.RunCoroutine(AddUrlSourceCoroutine(name, url));

        /// <summary>
        /// Asynchronously downloads a Web URL and adds it to the cache.
        /// </summary>
        /// <param name="name">The custom name/key to assign.</param>
        /// <param name="url">The HTTP/HTTPS URL to the Wav file.</param>
        /// <returns>A float IEnumerator for MEC execution.</returns>
        public static IEnumerator<float> AddUrlSourceCoroutine(string name, string url)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                Log.Error($"[CachedPcmSource] Cannot add URL source. Invalid name: '{name}' or URL: '{url}'.");
                yield break;
            }

            if (!url.StartsWith("http"))
            {
                Log.Error($"[CachedPcmSource] AddUrlCoroutine is only for web URLs! URL: '{url}'");
                yield break;
            }

            if (AudioCache.ContainsKey(name))
            {
                Log.Info($"[CachedPcmSource] A source with the name '{name}' already exists in the cache. Skipping addition.");
                yield break;
            }

            using UnityWebRequest www = UnityWebRequest.Get(url);
            yield return Timing.WaitUntilDone(www.SendWebRequest());

            if (www.result != UnityWebRequest.Result.Success)
            {
                Log.Error($"[CachedPcmSource] Web download failed for '{url}': {www.error}");
                yield break;
            }

            try
            {
                byte[] downloadedBytes = www.downloadHandler.data;
                AudioData parsedData = WavUtility.WavToPcm(downloadedBytes);

                parsedData.TrackInfo.Path = url;

                AudioCache.TryAdd(name, parsedData);
            }
            catch (Exception ex)
            {
                Log.Error($"[CachedPcmSource] Failed to parse downloaded WAV from '{url}':\n{ex}");
            }
        }

        /// <summary>
        /// Removes a specific audio track from the cache.
        /// </summary>
        /// <param name="name">The name/key of the cached audio to remove.</param>
        /// <returns><c>true</c> if the item was successfully removed; otherwise, <c>false</c>.</returns>
        public static bool Remove(string name)
        {
            return AudioCache.TryRemove(name, out _);
        }

        /// <summary>
        /// Clears the entire audio cache, freeing up RAM. Useful for RoundRestart or OnDisabled.
        /// </summary>
        public static void ClearCache()
        {
            AudioCache.Clear();
        }

        /// <summary>
        /// Reads a sequence of PCM samples from the cached buffer into the specified array.
        /// </summary>
        /// <param name="buffer">The destination array.</param>
        /// <param name="offset">The index to start writing.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The actual number of samples read.</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int read = Math.Min(count, data.Length - pos);
            Array.Copy(data, pos, buffer, offset, read);
            pos += read;

            return read;
        }

        /// <summary>
        /// Seeks to the specified position in seconds.
        /// </summary>
        /// <param name="seconds">The target position in seconds.</param>
        public void Seek(double seconds)
        {
            long targetIndex = (long)(seconds * VoiceChatSettings.SampleRate);
            pos = (int)Math.Max(0, Math.Min(targetIndex, data.Length));
        }

        /// <summary>
        /// Resets the read position to the beginning of the PCM data buffer.
        /// </summary>
        public void Reset()
        {
            pos = 0;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        private static void ClearCacheOnRestart()
        {
            if (ClearCacheOnRoundRestart)
                ClearCache();
        }
    }
}