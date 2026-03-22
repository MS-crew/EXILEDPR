// -----------------------------------------------------------------------
// <copyright file="Speaker.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Toys
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using AdminToys;

    using Enums;

    using Exiled.API.Features.Audio;
    using Exiled.API.Structs;

    using Interfaces;

    using MEC;

    using Mirror;

    using NorthwoodLib.Pools;

    using RoundRestarting;

    using UnityEngine;

    using VoiceChat;
    using VoiceChat.Codec;
    using VoiceChat.Codec.Enums;
    using VoiceChat.Networking;
    using VoiceChat.Playbacks;

    using Object = UnityEngine.Object;
    using Random = UnityEngine.Random;

    /// <summary>
    /// A wrapper class for <see cref="SpeakerToy"/>.
    /// </summary>
    public class Speaker : AdminToy, IWrapper<SpeakerToy>
    {
        /// <summary>
        /// A queue used for object pooling of <see cref="Speaker"/> instances.
        /// Reusing idle speakers instead of constantly creating and destroying them significantly improves server performance, especially for frequent audio events.
        /// </summary>
        internal static readonly Queue<Speaker> Pool = new();

        private const float DefaultVolume = 1f;
        private const float DefaultMinDistance = 1f;
        private const float DefaultMaxDistance = 15f;
        private const byte DefaultControllerId = 0;
        private const bool DefaultSpatial = true;

        private const int FrameSize = VoiceChatSettings.PacketSizePerChannel;
        private const float FrameTime = (float)FrameSize / VoiceChatSettings.SampleRate;

        private static readonly Vector3 SpeakerParkPosition = Vector3.down * 999;

        private IPcmSource source;
        private OpusEncoder encoder;

        private float[] frame;
        private byte[] encoded;
        private float[] resampleBuffer;

        private CoroutineHandle playBackRoutine;
        private CoroutineHandle fadeRoutine;

        private double resampleTime;
        private int resampleBufferFilled;
        private int nextScheduledEventIndex = 0;
        private int idChangeFrame;

        private bool isPlayBackInitialized = false;
        private bool isPitchDefault = true;
        private bool needsSyncWait = false;

        static Speaker() => RoundRestart.OnRestartTriggered += Pool.Clear;

        /// <summary>
        /// Initializes a new instance of the <see cref="Speaker"/> class.
        /// </summary>
        /// <param name="speakerToy">The <see cref="SpeakerToy"/> of the toy.</param>
        internal Speaker(SpeakerToy speakerToy)
            : base(speakerToy, AdminToyType.Speaker) => Base = speakerToy;

        /// <summary>
        /// Invoked when the audio playback starts.
        /// </summary>
        public event Action OnPlaybackStarted;

        /// <summary>
        /// Invoked when the audio playback is paused.
        /// </summary>
        public event Action OnPlaybackPaused;

        /// <summary>
        /// Invoked when the audio playback is resumed from a paused state.
        /// </summary>
        public event Action OnPlaybackResumed;

        /// <summary>
        /// Invoked when the audio playback loops back to the beginning.
        /// </summary>
        public event Action OnPlaybackLooped;

        /// <summary>
        /// Invoked when the audio track finishes playing.
        /// If looping is enabled, this triggers every time the track finished.
        /// </summary>
        public event Action OnPlaybackFinished;

        /// <summary>
        /// Invoked when the audio playback stops completely (either manually or end of file).
        /// </summary>
        public event Action OnPlaybackStopped;

        /// <summary>
        /// Invoked just before the speaker switches to the next track in the queue.
        /// Passes the upcoming <see cref="QueuedTrack"/> as an argument.
        /// </summary>
        public event Action<QueuedTrack> OnTrackSwitching;

        /// <summary>
        /// Gets the prefab.
        /// </summary>
        public static SpeakerToy Prefab => PrefabHelper.GetPrefab<SpeakerToy>(PrefabType.SpeakerToy);

        /// <summary>
        /// Gets the base <see cref="SpeakerToy"/>.
        /// </summary>
        public SpeakerToy Base { get; }

        /// <summary>
        /// Gets or sets the network channel used for sending audio packets from this speaker <see cref="Channels"/>.
        /// </summary>
        public int Channel { get; set; } = Channels.ReliableOrdered2;

        /// <summary>
        /// Gets or sets a value indicating whether the audio playback should loop when it reaches the end.
        /// </summary>
        public bool Loop { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the speaker should be destroyed after playback finishes.
        /// </summary>
        public bool DestroyAfter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the speaker should return to the pool after playback finishes.
        /// </summary>
        public bool ReturnToPoolAfter { get; set; }

        /// <summary>
        /// Gets or sets the play mode for this speaker, determining how audio is sent to players.
        /// </summary>
        public SpeakerPlayMode PlayMode { get; set; }

        /// <summary>
        /// Gets or sets the target player who will hear the audio played by this speaker when <see cref="PlayMode"/> is set to <see cref="SpeakerPlayMode.Player"/>.
        /// </summary>
        public Player TargetPlayer { get; set; }

        /// <summary>
        /// Gets or sets the list of target players who will hear the audio played by this speaker when <see cref="PlayMode"/> is set to <see cref="SpeakerPlayMode.PlayerList"/>.
        /// </summary>
        public HashSet<Player> TargetPlayers { get; set; }

        /// <summary>
        /// Gets or sets the predicate used to determine which players will hear the audio when <see cref="PlayMode"/> is set to <see cref="SpeakerPlayMode.Predicate"/>.
        /// The predicate should return <c>true</c> for players who should receive the audio.
        /// </summary>
        public Func<Player, bool> Predicate { get; set; }

        /// <summary>
        /// Gets a value indicating whether a sound is currently playing on this speaker.
        /// </summary>
        public bool IsPlaying => playBackRoutine.IsRunning && !IsPaused;

        /// <summary>
        /// Gets or sets a value indicating whether the playback is paused.
        /// </summary>
        /// <value>
        /// A <see cref="bool"/> where <c>true</c> means the playback is paused; <c>false</c> means it is not paused.
        /// </value>
        public bool IsPaused
        {
            get => playBackRoutine.IsAliveAndPaused;
            set
            {
                if (!playBackRoutine.IsRunning)
                    return;

                if (playBackRoutine.IsAliveAndPaused == value)
                    return;

                playBackRoutine.IsAliveAndPaused = value;
                if (value)
                {
                    OnPlaybackPaused?.Invoke();
                    SpeakerEvents.OnPlaybackPaused(this);
                }
                else
                {
                    OnPlaybackResumed?.Invoke();
                    SpeakerEvents.OnPlaybackResumed(this);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current playback time in seconds.
        /// Returns 0 if not playing.
        /// </summary>
        public double CurrentTime
        {
            get => source?.CurrentTime ?? 0.0;
            set
            {
                if (source == null)
                    return;

                source.CurrentTime = value;
                resampleTime = 0.0;
                resampleBufferFilled = 0;

                ResetEncoder();
                UpdateNextScheduledEventIndex();
            }
        }

        /// <summary>
        /// Gets the total duration of the current track in seconds.
        /// Returns 0 if not playing.
        /// </summary>
        public double TotalDuration => source?.TotalDuration ?? 0.0;

        /// <summary>
        /// Gets the remaining playback time in seconds.
        /// </summary>
        public double TimeLeft => Math.Max(0.0, TotalDuration - CurrentTime);

        /// <summary>
        /// Gets or sets the current playback progress as a value between 0.0 and 1.0.
        /// Returns 0 if not playing.
        /// </summary>
        public float PlaybackProgress
        {
            get => TotalDuration > 0.0 ? (float)(CurrentTime / TotalDuration) : 0f;
            set
            {
                if (TotalDuration > 0.0)
                    CurrentTime = TotalDuration * Mathf.Clamp01(value);
            }
        }

        /// <summary>
        /// Gets the metadata information (Title, Artist, Duration) of the last played audio track.
        /// </summary>
        public TrackData LastTrackInfo { get; private set; }

        /// <summary>
        /// Gets the queue of audio tracks to be played sequentially.
        /// </summary>
        public List<QueuedTrack> TrackQueue => field ??= new();

        /// <summary>
        /// Gets the list of time-based events for the current audio track.
        /// </summary>
        public List<ScheduledEvent> ScheduledEvents => field ??= new();

        /// <summary>
        /// Gets or sets the playback pitch.
        /// </summary>
        /// <value>
        /// A <see cref="float"/> representing the pitch level of the audio source,
        /// where 1.0 is normal pitch, less than 1.0 is lower pitch (slower), and greater than 1.0 is higher pitch (faster).
        /// </value>
        public float Pitch
        {
            get;
            set
            {
                field = Mathf.Max(0.1f, Mathf.Abs(value));
                isPitchDefault = Mathf.Abs(field - 1.0f) < 0.0001f;
                if (isPitchDefault)
                {
                    resampleTime = 0.0;
                    resampleBufferFilled = 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the volume of the audio source.
        /// </summary>
        /// <value>
        /// A <see cref="float"/> representing the volume level of the audio source,
        /// where 0.0 is silent and 1.0 is full volume if it's more it's will amplify it.
        /// </value>
        public float Volume
        {
            get => Base.NetworkVolume;
            set
            {
                StopFade();
                Base.NetworkVolume = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the audio source is spatialized.
        /// </summary>
        /// <value>
        /// A <see cref="bool"/> where <c>true</c> means the audio source is spatial, allowing
        /// for 3D audio positioning relative to the listener; <c>false</c> means it is non-spatial.
        /// </value>
        public bool IsSpatial
        {
            get => Base.NetworkIsSpatial;
            set => Base.NetworkIsSpatial = value;
        }

        /// <summary>
        /// Gets or sets the maximum distance at which the audio source can be heard.
        /// </summary>
        /// <value>
        /// A <see cref="float"/> representing the maximum hearing distance for the audio source.
        /// Beyond this distance, the audio will not be audible.
        /// </value>
        public float MaxDistance
        {
            get => Base.NetworkMaxDistance;
            set => Base.NetworkMaxDistance = value;
        }

        /// <summary>
        /// Gets or sets the minimum distance at which the audio source reaches full volume.
        /// </summary>
        /// <value>
        /// A <see cref="float"/> representing the distance from the source at which the audio is heard at full volume.
        /// Within this range, volume will not decrease with proximity.
        /// </value>
        public float MinDistance
        {
            get => Base.NetworkMinDistance;
            set => Base.NetworkMinDistance = value;
        }

        /// <summary>
        /// Gets or sets the controller ID of speaker.
        /// </summary>
        public byte ControllerId
        {
            get => Base.NetworkControllerId;
            set
            {
                if (Base.NetworkControllerId == value)
                    return;

                Base.NetworkControllerId = value;
                needsSyncWait = true;
                idChangeFrame = Time.frameCount;
            }
        }

        /// <summary>
        /// Creates a new <see cref="Speaker"/>.
        /// </summary>
        /// <param name="parent">The parent transform to attach the <see cref="Speaker"/> to.</param>
        /// <param name="position">The local position of the <see cref="Speaker"/>.</param>
        /// <param name="volume">The volume level of the audio source.</param>
        /// <param name="isSpatial">Whether the audio source is spatialized (3D sound).</param>
        /// <param name="minDistance">The minimum distance at which the audio reaches full volume.</param>
        /// <param name="maxDistance">The maximum distance at which the audio can be heard.</param>
        /// <param name="controllerId">The specific controller ID to assign. If null, the next available ID is used.</param>
        /// <param name="spawn">Whether the <see cref="Speaker"/> should be initially spawned.</param>
        /// <returns>The new <see cref="Speaker"/>.</returns>
        public static Speaker Create(Transform parent = null, Vector3? position = null, float volume = DefaultVolume, bool isSpatial = DefaultSpatial, float minDistance = DefaultMinDistance, float maxDistance = DefaultMaxDistance, byte? controllerId = null, bool spawn = true)
        {
            Speaker speaker = new(Object.Instantiate(Prefab, parent))
            {
                Volume = volume,
                IsSpatial = isSpatial,
                MinDistance = minDistance,
                MaxDistance = maxDistance,
                ControllerId = controllerId ?? GetNextFreeControllerId(),
                LocalPosition = position ?? Vector3.zero,
            };

            if (spawn)
                speaker.Spawn();

            return speaker;
        }

        /// <summary>
        /// Rents an available speaker from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <param name="parent">The parent transform to attach the <see cref="Speaker"/> to.</param>
        /// <param name="position">The local position of the <see cref="Speaker"/>.</param>
        /// <returns>A clean <see cref="Speaker"/> instance ready for use.</returns>
        public static Speaker Rent(Transform parent = null, Vector3? position = null)
        {
            Speaker speaker = null;

            while (Pool.Count > 0)
            {
                speaker = Pool.Dequeue();

                if (speaker != null && speaker.Base != null)
                    break;

                speaker = null;
            }

            if (speaker == null)
            {
                speaker = Create(parent, position);
            }
            else
            {
                speaker.IsStatic = false;

                if (parent != null)
                    speaker.Transform.parent = parent;

                speaker.LocalPosition = position ?? Vector3.zero;
                speaker.ControllerId = GetNextFreeControllerId(speaker.ControllerId);
                SpeakerToyPlaybackBase.AllInstances.Add(speaker.Base.Playback);
            }

            return speaker;
        }

        /// <summary>
        /// Rents a speaker from the pool, plays a wav file one time, and automatically returns it to the pool afterwards. (File must be 16 bit, mono and 48khz.)
        /// </summary>
        /// <param name="path">The path to the wav file.</param>
        /// <param name="position">The local position of the speaker.</param>
        /// <param name="parent">The parent transform, if any.</param>
        /// <param name="isSpatial">Whether the audio source is spatialized.</param>
        /// <param name="volume">The volume level of the audio source.</param>
        /// <param name="minDistance">The minimum distance at which the audio reaches full volume.</param>
        /// <param name="maxDistance">The maximum distance at which the audio can be heard.</param>
        /// <param name="pitch">The playback pitch level of the audio source.</param>
        /// <param name="playMode">The play mode determining how audio is sent to players.</param>
        /// <param name="stream">Whether to stream the audio or preload it.</param>
        /// <param name="targetPlayer">The target player if PlayMode is Player.</param>
        /// <param name="targetPlayers">The list of target players if PlayMode is PlayerList.</param>
        /// <param name="predicate">The condition if PlayMode is Predicate.</param>
        /// <returns><c>true</c> if the audio file was successfully found, loaded, and playback started; otherwise, <c>false</c>.</returns>
        public static bool PlayFromPool(string path, Vector3 position, Transform parent = null, bool isSpatial = DefaultSpatial, float volume = DefaultVolume, float minDistance = DefaultMinDistance, float maxDistance = DefaultMaxDistance, float pitch = 1f, SpeakerPlayMode playMode = SpeakerPlayMode.Global, bool stream = false, Player targetPlayer = null, HashSet<Player> targetPlayers = null, Func<Player, bool> predicate = null)
        {
            Speaker speaker = Rent(parent, position);

            speaker.Volume = volume;
            speaker.IsSpatial = isSpatial;
            speaker.MinDistance = minDistance;
            speaker.MaxDistance = maxDistance;

            speaker.Pitch = pitch;
            speaker.PlayMode = playMode;
            speaker.Predicate = predicate;
            speaker.TargetPlayer = targetPlayer;
            speaker.TargetPlayers = targetPlayers;

            speaker.ReturnToPoolAfter = true;

            if (!speaker.Play(path, new() { Stream = stream }))
            {
                speaker.ReturnToPool();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the next available controller ID for a <see cref="Speaker"/>.
        /// </summary>
        /// <param name="preferredId">An optional ID to check first.</param>
        /// <returns>The next available byte ID. If all IDs are currently in use, returns a default of 0.</returns>
        public static byte GetNextFreeControllerId(byte? preferredId = null)
        {
            HashSet<byte> usedIds = HashSetPool<byte>.Shared.Rent(byte.MaxValue + 1);

            foreach (SpeakerToyPlaybackBase playbackBase in SpeakerToyPlaybackBase.AllInstances)
            {
                usedIds.Add(playbackBase.ControllerId);
            }

            if (usedIds.Count >= byte.MaxValue + 1)
            {
                HashSetPool<byte>.Shared.Return(usedIds);
                Log.Warn("[Speaker] All controller IDs are in use. Default Controll Id will be use, Audio may conflict!");
                return DefaultControllerId;
            }

            if (preferredId.HasValue && !usedIds.Contains(preferredId.Value))
            {
                HashSetPool<byte>.Shared.Return(usedIds);
                return preferredId.Value;
            }

            byte id = 0;
            while (usedIds.Contains(id))
            {
                id++;
            }

            HashSetPool<byte>.Shared.Return(usedIds);
            return id;
        }

        /// <summary>
        /// Plays a wav file through this speaker. (File must be 16-bit, mono, and 48kHz.)
        /// </summary>
        /// <param name="path">The path to the wav file.</param>
        /// <param name="options">The configuration options for playback.</param>
        /// <returns><c>true</c> if the audio file was successfully found, loaded, and playback started; otherwise, <c>false</c>.</returns>
        public bool Play(string path, AudioPlaybackOptions options = default)
        {
            if (!File.Exists(path))
            {
                Log.Error($"[Speaker] The specified file does not exist, path: `{path}`.");
                return false;
            }

            if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"[Speaker] The file type '{Path.GetExtension(path)}' is not supported. Please use .wav file.");
                return false;
            }

            TryInitializePlayBack();
            Stop(options.ClearQueue);

            try
            {
                source = options.Stream ? new WavStreamSource(path) : new PreloadedPcmSource(path);
            }
            catch (Exception ex)
            {
                Log.Error($"[Speaker] Failed to initialize audio source for file at path: '{path}'.\nException Details: {ex}");
                return false;
            }

            LastTrackInfo = source.TrackInfo;

            playBackRoutine = Timing.RunCoroutine(PlayBackCoroutine().CancelWith(GameObject));
            return true;
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        /// <param name="clearQueue">If true, clears the upcoming tracks in the playlist.</param>
        public void Stop(bool clearQueue = true)
        {
            if (!isPlayBackInitialized)
                return;

            if (playBackRoutine.IsRunning)
            {
                playBackRoutine.IsRunning = false;

                OnPlaybackStopped?.Invoke();
                SpeakerEvents.OnPlaybackStopped(this);
            }

            if (clearQueue)
                TrackQueue.Clear();

            StopFade();
            ResetEncoder();
            ClearScheduledEvents();
            source?.Dispose();
            source = null;
        }

        /// <summary>
        /// Fades the volume to a specific target over a given duration.
        /// <para><c>IMPORTANT:</c> If the <see cref="Volume"/> property is manually changed while a fade is in progress, the fade operation will be immediately aborted.</para>
        /// </summary>
        /// <param name="startVolume">The initial volume level when the fade begins.</param>
        /// <param name="targetVolume">The final volume level to reach at the end of the fade.</param>
        /// <param name="duration">The time in seconds the fading process should take to complete.</param>
        /// <param name="linear">If <c>true</c>, uses linear interpolation; if <c>false</c>, uses natural easing (ease-in for fade-in, ease-out for fade-out).</param>
        /// <param name="onComplete">An optional action to invoke when the fade process is fully finished.</param>
        public void FadeVolume(float startVolume, float targetVolume, float duration = 3, bool linear = false, Action onComplete = null)
        {
            if (fadeRoutine.IsRunning)
                fadeRoutine.IsRunning = false;

            fadeRoutine = Timing.RunCoroutine(FadeCoroutine(startVolume, targetVolume, duration, linear, onComplete).CancelWith(GameObject));
        }

        /// <summary>
        /// Stops currently active volume fading process, leaving the volume at its exact current level.
        /// </summary>
        public void StopFade()
        {
            if (fadeRoutine.IsRunning)
                fadeRoutine.IsRunning = false;
        }

        /// <summary>
        /// Restarts the currently playing track from the beginning.
        /// </summary>
        public void RestartTrack()
        {
            if (!playBackRoutine.IsRunning)
                return;

            CurrentTime = 0.0;
        }

        /// <summary>
        /// Adds an audio file to the playback queue with specific options. If nothing is playing, playback starts immediately.
        /// </summary>
        /// <param name="path">The path to the wav file to enqueue.</param>
        /// <param name="options">The specific playback configuration for this track.</param>
        /// <returns><c>true</c> if successfully queued or started.</returns>
        public bool QueueTrack(string path, AudioPlaybackOptions options = default)
        {
            if (!playBackRoutine.IsRunning && !IsPaused)
                return Play(path, options);

            TrackQueue.Add(new QueuedTrack(path, options));
            return true;
        }

        /// <summary>
        /// Skips the currently playing track and starts playing the next one in the queue.
        /// </summary>
        public void SkipTrack()
        {
            if (TrySwitchToNextTrack())
            {
                IsPaused = false;

                if (!playBackRoutine.IsRunning)
                    playBackRoutine = Timing.RunCoroutine(PlayBackCoroutine().CancelWith(GameObject));
            }
            else
            {
                Stop();
            }
        }

        /// <summary>
        /// Removes a specific track from the playback queue by its file path.
        /// </summary>
        /// <param name="path">The exact file path of the track to remove.</param>
        /// <param name="findFirst">If <c>true</c>, removes the first occurrence; if <c>false</c>, removes the last occurrence.</param>
        /// <returns><c>true</c> if the track was successfully found and removed; otherwise, <c>false</c>.</returns>
        public bool RemoveTrack(string path, bool findFirst = true)
        {
            int index = findFirst ? TrackQueue.FindIndex(t => t.Path == path) : TrackQueue.FindLastIndex(t => t.Path == path);

            if (index == -1)
                return false;

            TrackQueue.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Shuffles the tracks in the <see cref="TrackQueue"/> into a random order with Fisher-Yates algorithm.
        /// </summary>
        public void ShuffleTracks()
        {
            if (TrackQueue.Count <= 1)
                return;

            for (int i = TrackQueue.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (TrackQueue[i], TrackQueue[j]) = (TrackQueue[j], TrackQueue[i]);
            }
        }

        /// <summary>
        /// Adds an action to be executed at a specific time in seconds during the current playback.
        /// <para><c>WARNING:</c> Heavy operations can cause audio interruptions. If you need to perform heavy operations, start a MEC Coroutine inside the action.</para>
        /// </summary>
        /// <param name="timeInSeconds">The exact time in seconds to trigger the action.</param>
        /// <param name="action">The action to invoke when the specified time is reached.</param>
        /// <param name="id">An optional unique string identifier for this event. If not provided, a random GUID will be assigned.</param>
        /// <returns>The unique string ID of the created time event, which can be used to remove it later via <see cref="RemoveScheduledEvent"/>.</returns>
        public string AddScheduledEvent(double timeInSeconds, Action action, string id = null)
        {
            ScheduledEvent timeEvent = new(timeInSeconds, action, id);

            ScheduledEvents.Add(timeEvent);
            ScheduledEvents.Sort();
            UpdateNextScheduledEventIndex();

            return timeEvent.Id;
        }

        /// <summary>
        /// Removes a specific time-based event using its ID.
        /// </summary>
        /// <param name="id">The unique string identifier of the event to remove.</param>
        /// <returns><c>true</c> if the event was successfully found and removed; otherwise, <c>false</c>.</returns>
        public bool RemoveScheduledEvent(string id)
        {
            int removed = ScheduledEvents.RemoveAll(e => e.Id == id);

            if (removed <= 0)
                return false;

            UpdateNextScheduledEventIndex();
            return true;
        }

        /// <summary>
        /// Clears all time-based events for the current playback.
        /// </summary>
        public void ClearScheduledEvents()
        {
            ScheduledEvents.Clear();
            nextScheduledEventIndex = 0;
        }

        /// <summary>
        /// Stops the current playback, resets all properties of the <see cref="Speaker"/>, and returns the instance to the object pool for future reuse.
        /// </summary>
        public void ReturnToPool()
        {
            if (Base == null)
                return;

            Stop();

            if (Transform.parent != null || AdminToyBase._clientParentId != 0)
            {
                Transform.parent = null;
                Base.RpcChangeParent(0);
            }

            LocalPosition = SpeakerParkPosition;

            Volume = DefaultVolume;
            IsSpatial = DefaultSpatial;
            MinDistance = DefaultMinDistance;
            MaxDistance = DefaultMaxDistance;

            IsStatic = true;
            Loop = false;
            DestroyAfter = false;
            ReturnToPoolAfter = false;
            PlayMode = SpeakerPlayMode.Global;
            Channel = Channels.ReliableOrdered2;

            LastTrackInfo = default;

            Predicate = null;
            TargetPlayer = null;
            TargetPlayers = null;

            Pitch = 1f;
            resampleTime = 0.0;
            resampleBufferFilled = 0;
            isPitchDefault = true;
            needsSyncWait = false;

            OnPlaybackStarted = null;
            OnPlaybackPaused = null;
            OnPlaybackResumed = null;
            OnPlaybackLooped = null;
            OnTrackSwitching = null;
            OnPlaybackFinished = null;
            OnPlaybackStopped = null;

            SpeakerToyPlaybackBase.AllInstances.Remove(Base.Playback);

            Pool.Enqueue(this);
        }

        private void TryInitializePlayBack()
        {
            if (isPlayBackInitialized)
                return;

            isPlayBackInitialized = true;

            frame = new float[FrameSize];
            resampleBuffer = Array.Empty<float>();
            encoder = new(OpusApplicationType.Audio);
            encoded = new byte[VoiceChatSettings.MaxEncodedSize];

            // 3002 => OPUS_SIGNAL_MUSIC (https://github.com/xiph/opus/blob/2d862ea14b233e5a3f3afaf74d96050691af3cd5/include/opus_defines.h#L229)
            OpusWrapper.SetEncoderSetting(encoder._handle, OpusCtlSetRequest.Signal, 3002);

            AdminToyBase.OnRemoved += OnToyRemoved;
        }

        private bool TrySwitchToNextTrack()
        {
            while (TrackQueue.Count > 0)
            {
                QueuedTrack nextTrack = TrackQueue[0];
                OnTrackSwitching?.Invoke(nextTrack);
                SpeakerEvents.OnTrackSwitching(this, nextTrack);

                TrackQueue.RemoveAt(0);

                IPcmSource newSource;
                try
                {
                    bool useStream = nextTrack.Options.Stream;
                    newSource = useStream ? new WavStreamSource(nextTrack.Path) : new PreloadedPcmSource(nextTrack.Path);
                }
                catch (Exception ex)
                {
                    Log.Error($"[Speaker] Playlist next track failed: '{nextTrack}'.\n{ex}");
                    continue;
                }

                source?.Dispose();
                source = newSource;

                LastTrackInfo = source.TrackInfo;

                ResetEncoder();
                ClearScheduledEvents();
                resampleTime = 0.0;
                resampleBufferFilled = 0;

                OnPlaybackStarted?.Invoke();
                SpeakerEvents.OnPlaybackStarted(this);
                return true;
            }

            return false;
        }

        private void UpdateNextScheduledEventIndex()
        {
            nextScheduledEventIndex = 0;
            double current = CurrentTime;

            while (nextScheduledEventIndex < ScheduledEvents.Count && ScheduledEvents[nextScheduledEventIndex].Time <= current)
            {
                nextScheduledEventIndex++;
            }
        }

        private void ResetEncoder()
        {
            if (encoder != null && encoder._handle != IntPtr.Zero)
            {
                // 4028 => OPUS_RESET_STATE (https://github.com/xiph/opus/blob/2d862ea14b233e5a3f3afaf74d96050691af3cd5/include/opus_defines.h#L710)
                OpusWrapper.SetEncoderSetting(encoder._handle, (OpusCtlSetRequest)4028, 0);
            }
        }

        private void SendPacket(int len)
        {
            AudioMessage msg = new(ControllerId, encoded, len);

            switch (PlayMode)
            {
                case SpeakerPlayMode.Global:
                    NetworkServer.SendToReady(msg, Channel);
                    break;

                case SpeakerPlayMode.Player:
                    TargetPlayer?.Connection?.Send(msg, Channel);
                    break;

                case SpeakerPlayMode.PlayerList:

                    if (TargetPlayers is null)
                        break;

                    using (NetworkWriterPooled writer = NetworkWriterPool.Get())
                    {
                        NetworkMessages.Pack(msg, writer);
                        ArraySegment<byte> segment = writer.ToArraySegment();

                        foreach (Player ply in TargetPlayers)
                        {
                            ply?.Connection?.Send(segment, Channel);
                        }
                    }

                    break;

                case SpeakerPlayMode.Predicate:
                    if (Predicate is null)
                        break;

                    using (NetworkWriterPooled writer = NetworkWriterPool.Get())
                    {
                        NetworkMessages.Pack(msg, writer);
                        ArraySegment<byte> segment = writer.ToArraySegment();

                        foreach (Player ply in Player.List)
                        {
                            if (Predicate(ply))
                                ply.Connection?.Send(segment, Channel);
                        }
                    }

                    break;
            }
        }

        private void ResampleFrame()
        {
            int requiredSize = (int)(FrameSize * Mathf.Abs(Pitch) * 2) + 10;

            if (resampleBuffer.Length < requiredSize)
            {
                resampleBuffer = new float[requiredSize];
                resampleTime = 0.0;
                resampleBufferFilled = 0;
            }

            int outputIdx = 0;

            while (outputIdx < FrameSize)
            {
                if (resampleBufferFilled == 0)
                {
                    int toRead = resampleBuffer.Length - 4;
                    int actualRead = source.Read(resampleBuffer, 0, toRead);

                    if (actualRead == 0)
                    {
                        while (outputIdx < FrameSize)
                            frame[outputIdx++] = 0f;
                        return;
                    }

                    resampleBufferFilled = actualRead;
                    resampleTime = 0.0;
                }

                int currentSample = (int)resampleTime;

                if (currentSample >= resampleBufferFilled - 1)
                {
                    if (resampleBufferFilled > 0)
                    {
                        resampleBuffer[0] = resampleBuffer[resampleBufferFilled - 1];

                        int toRead = resampleBuffer.Length - 5;
                        int actualRead = source.Read(resampleBuffer, 1, toRead);

                        if (actualRead == 0)
                        {
                            while (outputIdx < FrameSize)
                                frame[outputIdx++] = 0f;
                            return;
                        }

                        resampleBufferFilled = actualRead + 1;
                        resampleTime -= currentSample;
                    }
                    else
                    {
                        resampleBufferFilled = 0;
                    }

                    continue;
                }

                double frac = resampleTime - currentSample;
                float sample1 = resampleBuffer[currentSample];
                float sample2 = resampleBuffer[currentSample + 1];

                frame[outputIdx++] = (float)(sample1 + ((sample2 - sample1) * frac));

                resampleTime += Pitch;
            }
        }

        private void OnToyRemoved(AdminToyBase toy)
        {
            if (toy != Base)
                return;

            AdminToyBase.OnRemoved -= OnToyRemoved;

            Stop();
            encoder?.Dispose();

            OnPlaybackStarted = null;
            OnPlaybackPaused = null;
            OnPlaybackResumed = null;
            OnPlaybackLooped = null;
            OnTrackSwitching = null;
            OnPlaybackFinished = null;
            OnPlaybackStopped = null;
        }

        private IEnumerator<float> PlayBackCoroutine()
        {
            if (needsSyncWait)
            {
                int framesPassed = Time.frameCount - idChangeFrame;

                while (framesPassed < 2)
                {
                    yield return Timing.WaitForOneFrame;
                    framesPassed = Time.frameCount - idChangeFrame;
                }

                needsSyncWait = false;
            }

            OnPlaybackStarted?.Invoke();
            SpeakerEvents.OnPlaybackStarted(this);

            resampleTime = 0.0;
            resampleBufferFilled = 0;

            float timeAccumulator = 0f;

            while (true)
            {
                timeAccumulator += Time.deltaTime;

                while (timeAccumulator >= FrameTime)
                {
                    timeAccumulator -= FrameTime;

                    if (isPitchDefault)
                    {
                        int read = source.Read(frame, 0, FrameSize);
                        if (read < FrameSize)
                            Array.Clear(frame, read, FrameSize - read);
                    }
                    else
                    {
                        ResampleFrame();
                    }

                    int len = encoder.Encode(frame, encoded);

                    if (len > 2)
                        SendPacket(len);

                    if (!source.Ended)
                        continue;

                    yield return Timing.WaitForOneFrame;

                    OnPlaybackFinished?.Invoke();
                    SpeakerEvents.OnPlaybackFinished(this);

                    if (Loop)
                    {
                        ResetEncoder();
                        source.Reset();
                        timeAccumulator = 0;
                        resampleTime = resampleBufferFilled = 0;
                        nextScheduledEventIndex = 0;

                        OnPlaybackLooped?.Invoke();
                        SpeakerEvents.OnPlaybackLooped(this);
                        continue;
                    }

                    if (TrySwitchToNextTrack())
                    {
                        timeAccumulator = 0f;
                        continue;
                    }

                    if (ReturnToPoolAfter)
                        ReturnToPool();
                    else if (DestroyAfter)
                        Destroy();
                    else
                        Stop();

                    yield break;
                }

                while (nextScheduledEventIndex < ScheduledEvents.Count && CurrentTime >= ScheduledEvents[nextScheduledEventIndex].Time)
                {
                    try
                    {
                        ScheduledEvents[nextScheduledEventIndex].Action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[Speaker] Failed to execute scheduled time event at {ScheduledEvents[nextScheduledEventIndex].Time:F2}s.\nException Details: {ex}");
                    }

                    nextScheduledEventIndex++;
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        private IEnumerator<float> FadeCoroutine(float startVolume, float targetVolume, float duration, bool linear, Action onComplete)
        {
            float timePassed = 0f;
            bool isFadeOut = startVolume > targetVolume;

            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                float t = timePassed / duration;

                if (!linear)
                    t = isFadeOut ? 1f - ((1f - t) * (1f - t)) : t * t;

                Base.NetworkVolume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return Timing.WaitForOneFrame;
            }

            Base.NetworkVolume = targetVolume;
            onComplete?.Invoke();
        }
    }
}