using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpireChess.Audio
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class AudioService : MonoBehaviour
    {
        private sealed class AudioVoice
        {
            public AudioSource Source;
            public string CueId;
        }

        private static AudioService instance;

        [SerializeField] private PresentationAudioCatalog catalog;
        [SerializeField, Range(1, 32)] private int voicePoolSize = 12;
        [SerializeField, Min(0f)] private float defaultMusicFadeSeconds = 0.4f;

        private readonly List<AudioVoice> voices = new List<AudioVoice>();
        private readonly AudioPlaybackLimiter playbackLimiter =
            new AudioPlaybackLimiter();

        private AudioSource musicSourceA;
        private AudioSource musicSourceB;
        private AudioSource activeMusicSource;
        private PresentationAudioCueDefinition activeMusicDefinition;
        private Coroutine musicFadeRoutine;
        private PresentationAudioSettings settings;
        private System.Random variationRandom;
        private bool mixerVolumeApplied;
        private string currentMusicCueId;

        public static AudioService Instance => instance;
        public PresentationAudioCatalog Catalog => catalog;
        public PresentationAudioSettings Settings => settings;
        public string CurrentMusicCueId => currentMusicCueId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsurePresent();
        }

        public static AudioService EnsurePresent()
        {
            if (instance != null)
            {
                return instance;
            }

            var existing = FindObjectOfType<AudioService>();
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(nameof(AudioService));
            return root.AddComponent<AudioService>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            settings = PresentationAudioSettings.Load();
            variationRandom = new System.Random(
                unchecked(Environment.TickCount * 397) ^ GetInstanceID());

            if (catalog == null)
            {
                catalog = Resources.Load<PresentationAudioCatalog>(
                    PresentationAudioCatalog.DefaultResourcesPath);
            }

            CreateSources();
            ApplySettingsToRuntime();

            if (GetComponent<MusicDirector>() == null)
            {
                gameObject.AddComponent<MusicDirector>();
            }
        }

        private void Update()
        {
            RefreshVoiceStates();
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            if (musicFadeRoutine != null)
            {
                StopCoroutine(musicFadeRoutine);
                musicFadeRoutine = null;
            }

            foreach (var voice in voices)
            {
                ReleaseVoice(voice, true);
            }

            playbackLimiter.Reset();
            instance = null;
        }

        public void Configure(PresentationAudioCatalog value)
        {
            catalog = value;
            if (catalog == null)
            {
                StopMusic(0f);
            }

            ApplySettingsToRuntime();
            MusicDirector.Instance?.RefreshForActiveScene();
        }

        public bool PlayCue(string cueId)
        {
            if (!TryResolvePlayableCue(
                    cueId,
                    out var definition,
                    out var clip))
            {
                return false;
            }

            if (definition.Bus == PresentationAudioBus.Music)
            {
                return PlayMusic(cueId, defaultMusicFadeSeconds);
            }

            RefreshVoiceStates();
            var voice = FindAvailableVoice();
            if (voice == null)
            {
                return false;
            }

            if (!playbackLimiter.TryAcquire(
                    definition.Id,
                    Time.unscaledTime,
                    definition.ConcurrencyLimit,
                    definition.CooldownSeconds,
                    out _))
            {
                return false;
            }

            var source = voice.Source;
            voice.CueId = definition.Id;
            source.Stop();
            source.clip = clip;
            source.outputAudioMixerGroup =
                catalog.GetOutputGroup(definition.Bus);
            source.volume = GetSourceGain(definition);
            source.pitch = SelectPitch(definition);
            source.loop = false;
            source.ignoreListenerPause =
                definition.Bus == PresentationAudioBus.Ui;
            source.Play();
            return true;
        }

        public bool PlayMusic(string cueId)
        {
            return PlayMusic(cueId, defaultMusicFadeSeconds);
        }

        public bool PlayMusic(string cueId, float fadeSeconds)
        {
            if (string.Equals(
                    currentMusicCueId,
                    cueId,
                    StringComparison.Ordinal) &&
                activeMusicSource != null &&
                activeMusicSource.isPlaying)
            {
                return true;
            }

            if (!TryResolvePlayableCue(
                    cueId,
                    out var definition,
                    out var clip) ||
                definition.Bus != PresentationAudioBus.Music)
            {
                return false;
            }

            if (musicFadeRoutine != null)
            {
                StopCoroutine(musicFadeRoutine);
                musicFadeRoutine = null;
            }

            var outgoing = activeMusicSource;
            var incoming = ReferenceEquals(outgoing, musicSourceA)
                ? musicSourceB
                : musicSourceA;
            if (incoming == null)
            {
                return false;
            }

            incoming.Stop();
            incoming.clip = clip;
            incoming.outputAudioMixerGroup =
                catalog.GetOutputGroup(PresentationAudioBus.Music);
            incoming.pitch = SelectPitch(definition);
            incoming.loop = definition.Loop;
            incoming.ignoreListenerPause = false;
            incoming.volume = 0f;
            incoming.Play();

            activeMusicSource = incoming;
            activeMusicDefinition = definition;
            currentMusicCueId = cueId;

            var duration = Mathf.Max(0f, fadeSeconds);
            if (duration <= 0f)
            {
                StopAndClear(outgoing);
                incoming.volume = GetSourceGain(definition);
                return true;
            }

            musicFadeRoutine = StartCoroutine(
                FadeToNewMusic(
                    outgoing,
                    incoming,
                    definition,
                    duration));
            return true;
        }

        public void StopMusic()
        {
            StopMusic(defaultMusicFadeSeconds);
        }

        public void StopMusic(float fadeSeconds)
        {
            if (musicFadeRoutine != null)
            {
                StopCoroutine(musicFadeRoutine);
                musicFadeRoutine = null;
            }

            var outgoing = activeMusicSource;
            activeMusicSource = null;
            activeMusicDefinition = null;
            currentMusicCueId = null;

            // A stopped cross-fade can leave the previous source playing at a
            // partial volume. Always retire the non-active music voice before
            // handling the active one.
            var inactive = ReferenceEquals(outgoing, musicSourceA)
                ? musicSourceB
                : musicSourceA;
            StopAndClear(inactive);
            if (outgoing == null)
            {
                StopAndClear(musicSourceB);
                return;
            }

            var duration = Mathf.Max(0f, fadeSeconds);
            if (duration <= 0f)
            {
                StopAndClear(outgoing);
                return;
            }

            musicFadeRoutine = StartCoroutine(
                FadeOutMusic(outgoing, duration));
        }

        /// <summary>
        /// Stops every pooled non-music voice immediately.  Presentation
        /// callers use this when a sequence is skipped so old impacts or
        /// battle cues cannot continue after the visual state has snapped.
        /// </summary>
        public void StopAllTransientCues()
        {
            foreach (var voice in voices)
            {
                ReleaseVoice(voice, true);
            }

            playbackLimiter.Reset();
        }

        public void SetMasterVolume(float value, bool save = true)
        {
            EnsureSettings();
            settings.Master = value;
            ApplySettingsToRuntime();
            if (save)
            {
                settings.Save();
            }
        }

        public void SetBusVolume(
            PresentationAudioBus bus,
            float value,
            bool save = true)
        {
            EnsureSettings();
            settings.SetBusLinearVolume(bus, value);
            ApplySettingsToRuntime();
            if (save)
            {
                settings.Save();
            }
        }

        public void ReloadSettings()
        {
            settings = PresentationAudioSettings.Load();
            ApplySettingsToRuntime();
        }

        public void SaveSettings()
        {
            EnsureSettings();
            settings.Save();
        }

        private IEnumerator FadeToNewMusic(
            AudioSource outgoing,
            AudioSource incoming,
            PresentationAudioCueDefinition incomingDefinition,
            float duration)
        {
            var elapsed = 0f;
            var outgoingStartVolume = outgoing != null
                ? outgoing.volume
                : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                if (outgoing != null)
                {
                    outgoing.volume =
                        Mathf.Lerp(outgoingStartVolume, 0f, progress);
                }

                if (incoming != null)
                {
                    incoming.volume =
                        GetSourceGain(incomingDefinition) * progress;
                }

                yield return null;
            }

            StopAndClear(outgoing);
            if (incoming != null)
            {
                incoming.volume = GetSourceGain(incomingDefinition);
            }

            musicFadeRoutine = null;
        }

        private IEnumerator FadeOutMusic(
            AudioSource outgoing,
            float duration)
        {
            var elapsed = 0f;
            var startVolume = outgoing.volume;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                outgoing.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            StopAndClear(outgoing);
            musicFadeRoutine = null;
        }

        private void CreateSources()
        {
            musicSourceA = CreateSource();
            musicSourceB = CreateSource();

            voices.Clear();
            var count = Mathf.Clamp(voicePoolSize, 1, 32);
            for (var index = 0; index < count; index++)
            {
                voices.Add(new AudioVoice
                {
                    Source = CreateSource()
                });
            }
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        private bool TryResolvePlayableCue(
            string cueId,
            out PresentationAudioCueDefinition definition,
            out AudioClip clip)
        {
            definition = null;
            clip = null;
            if (catalog == null ||
                !catalog.TryGetCue(cueId, out definition) ||
                definition == null ||
                !definition.HasPlayableClip)
            {
                return false;
            }

            EnsureRandom();
            clip = definition.SelectClip(variationRandom.Next());
            return clip != null;
        }

        private AudioVoice FindAvailableVoice()
        {
            foreach (var voice in voices)
            {
                if (voice.CueId == null && !voice.Source.isPlaying)
                {
                    return voice;
                }
            }

            return null;
        }

        private void RefreshVoiceStates()
        {
            foreach (var voice in voices)
            {
                if (voice.CueId != null && !voice.Source.isPlaying)
                {
                    ReleaseVoice(voice, false);
                }
            }
        }

        private void ReleaseVoice(AudioVoice voice, bool stopSource)
        {
            if (voice == null || voice.Source == null)
            {
                return;
            }

            if (stopSource)
            {
                voice.Source.Stop();
            }

            if (voice.CueId != null)
            {
                playbackLimiter.Release(voice.CueId);
                voice.CueId = null;
            }

            voice.Source.clip = null;
            voice.Source.outputAudioMixerGroup = null;
            voice.Source.pitch = 1f;
            voice.Source.volume = 1f;
        }

        private float SelectPitch(PresentationAudioCueDefinition definition)
        {
            EnsureRandom();
            var progress = (float)variationRandom.NextDouble();
            return Mathf.Lerp(
                definition.MinPitch,
                definition.MaxPitch,
                progress);
        }

        private float GetSourceGain(
            PresentationAudioCueDefinition definition)
        {
            if (definition == null)
            {
                return 0f;
            }

            if (mixerVolumeApplied)
            {
                return definition.Volume;
            }

            EnsureSettings();
            return definition.Volume *
                   settings.GetEffectiveLinearVolume(definition.Bus);
        }

        private void ApplySettingsToRuntime()
        {
            EnsureSettings();
            mixerVolumeApplied =
                catalog != null &&
                settings.ApplyToMixer(catalog.AudioMixer);

            foreach (var voice in voices)
            {
                if (voice.CueId != null &&
                    catalog != null &&
                    catalog.TryGetCue(voice.CueId, out var definition))
                {
                    voice.Source.volume = GetSourceGain(definition);
                }
            }

            if (activeMusicSource != null &&
                activeMusicDefinition != null)
            {
                activeMusicSource.volume =
                    GetSourceGain(activeMusicDefinition);
            }
        }

        private void EnsureSettings()
        {
            if (settings == null)
            {
                settings = PresentationAudioSettings.Load();
            }
        }

        private void EnsureRandom()
        {
            if (variationRandom == null)
            {
                variationRandom = new System.Random(
                    unchecked(Environment.TickCount * 397) ^ GetInstanceID());
            }
        }

        private static void StopAndClear(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.pitch = 1f;
            source.volume = 0f;
        }
    }
}
