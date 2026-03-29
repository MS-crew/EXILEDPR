// -----------------------------------------------------------------------
// <copyright file="AudioPcmCache.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;

    using Exiled.API.Structs.Audio;

    using MEC;

    using RoundRestarting;

    using UnityEngine.Networking;

    /// <summary>
    /// Manages a global in-memory cache of decoded PCM audio data. Once cached, audio can be played using <see cref="PcmSources.CachedPcmSource"/>.
    /// </summary>
    public static class AudioPcmCache
    {
        static AudioPcmCache()
        {
            AudioCache = new();
            RoundRestart.OnRestartTriggered += OnRoundRestart;
        }

        /// <summary>
        /// Gets the underlying cache store, keyed by name.
        /// </summary>
        public static ConcurrentDictionary<string, AudioData> AudioCache { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the cache is automatically cleared when a round restart is triggered.
        /// </summary>
        public static bool ClearOnRoundRestart { get; set; } = true;

        /// <summary>
        /// Loads and caches a local .wav file under the specified name.
        /// </summary>
        /// <param name="name">The unique cache key to assign to this audio.</param>
        /// <param name="path">The absolute path to the local .wav file.</param>
        /// <returns><c>true</c> if the file was successfully loaded and cached; otherwise, <c>false</c>.</returns>
        public static bool Add(string name, string path)
        {
            if (!ValidateName(name))
                return false;

            if (path.StartsWith("http"))
            {
                Log.Error($"[AudioCache] '{path}' is a URL. Use AudioCache.AddUrl() for web sources.");
                return false;
            }

            if (AudioCache.ContainsKey(name))
            {
                Log.Debug($"[AudioCache] Key '{name}' already exists. Skipping.");
                return false;
            }

            if (!File.Exists(path))
            {
                Log.Error($"[AudioCache] Local file not found: '{path}'");
                return false;
            }

            try
            {
                AudioData parsed = WavUtility.WavToPcm(path);
                return AudioCache.TryAdd(name, parsed);
            }
            catch (Exception ex)
            {
                Log.Error($"[AudioCache] Failed to load '{path}' into cache:\n{ex}");
                return false;
            }
        }

        /// <summary>
        /// Caches raw PCM audio samples under the specified name.
        /// </summary>
        /// <param name="name">The unique cache key to assign.</param>
        /// <param name="pcm">The raw PCM float array to cache.</param>
        /// <returns><c>true</c> if successfully added; otherwise, <c>false</c>.</returns>
        public static bool Add(string name, float[] pcm)
        {
            if (pcm == null || pcm.Length == 0)
            {
                Log.Error($"[AudioCache] Cannot cache null or empty PCM array for key '{name}'.");
                return false;
            }

            TrackData trackInfo = new()
            {
                Title = name,
                Duration = (double)pcm.Length / VoiceChat.VoiceChatSettings.SampleRate,
            };

            return Add(name, new AudioData(pcm, trackInfo));
        }

        /// <summary>
        /// Caches a fully constructed <see cref="AudioData"/> under the specified name.
        /// </summary>
        /// <param name="name">The unique cache key to assign.</param>
        /// <param name="audioData">The <see cref="AudioData"/> to store.</param>
        /// <returns><c>true</c> if successfully added; otherwise, <c>false</c>.</returns>
        public static bool Add(string name, AudioData audioData)
        {
            if (!ValidateName(name))
                return false;

            if (audioData.Pcm == null || audioData.Pcm.Length == 0)
            {
                Log.Error($"[AudioCache] AudioData for key '{name}' has null or empty PCM.");
                return false;
            }

            if (AudioCache.ContainsKey(name))
            {
                Log.Debug($"[AudioCache] Key '{name}' already exists. Skipping.");
                return false;
            }

            return AudioCache.TryAdd(name, audioData);
        }

        /// <summary>
        /// Starts an asynchronous download of a .wav file from the specified URL and adds it to the cache.
        /// </summary>
        /// <param name="name">The unique cache key to assign.</param>
        /// <param name="url">The HTTP or HTTPS URL pointing to a valid .wav file.</param>
        /// <returns>A <see cref="CoroutineHandle"/> for the running download coroutine.</returns>
        public static CoroutineHandle AddUrl(string name, string url) => Timing.RunCoroutine(AddUrlCoroutine(name, url));

        /// <summary>
        /// Starts an asynchronous download of a .wav file from the specified URL and adds it to the cache.
        /// </summary>
        /// <param name="name">The unique cache key to assign.</param>
        /// <param name="url">The HTTP or HTTPS URL pointing to a valid .wav file.</param>
        /// <returns>A MEC-compatible <see cref="IEnumerator{T}"/> of <see cref="float"/>.</returns>
        public static IEnumerator<float> AddUrlCoroutine(string name, string url)
        {
            if (!ValidateName(name))
                yield break;

            if (string.IsNullOrEmpty(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"[AudioCache] Invalid URL for key '{name}': '{url}'. Must start with http/https.");
                yield break;
            }

            if (AudioCache.ContainsKey(name))
            {
                Log.Debug($"[AudioCache] Key '{name}' already exists. Skipping URL download.");
                yield break;
            }

            using UnityWebRequest www = UnityWebRequest.Get(url);
            yield return Timing.WaitUntilDone(www.SendWebRequest());

            if (www.result != UnityWebRequest.Result.Success)
            {
                Log.Error($"[AudioCache] Download failed for '{url}': {www.error}");
                yield break;
            }

            try
            {
                AudioData parsed = WavUtility.WavToPcm(www.downloadHandler.data);
                parsed.TrackInfo.Path = url;

                if (AudioCache.TryAdd(name, parsed))
                    Log.Debug($"[AudioCache] Successfully cached '{url}' as '{name}'.");
            }
            catch (Exception ex)
            {
                Log.Error($"[AudioCache] Failed to parse downloaded WAV from '{url}':\n{ex}");
            }
        }

        /// <summary>
        /// Removes a cached audio entry by name.
        /// </summary>
        /// <param name="name">The cache name/key to remove.</param>
        /// <returns><c>true</c> if the entry was found and removed; otherwise, <c>false</c>.</returns>
        public static bool Remove(string name) => AudioCache.TryRemove(name, out _);

        /// <summary>
        /// Clears all entries from the audio cache, freeing all associated memory.
        /// </summary>
        public static void Clear() => AudioCache.Clear();

        private static bool ValidateName(string name)
        {
            if (!string.IsNullOrEmpty(name))
                return true;

            Log.Error("[AudioCache] Cache name (key) cannot be null or empty.");
            return false;
        }

        private static void OnRoundRestart()
        {
            if (ClearOnRoundRestart)
                Clear();
        }
    }
}
