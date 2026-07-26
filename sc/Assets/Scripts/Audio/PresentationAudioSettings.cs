using System;
using UnityEngine;
using UnityEngine.Audio;

namespace SpireChess.Audio
{
    public sealed class PresentationAudioSettings
    {
        public const string MasterPrefKey =
            "SpireChess.Audio.v1.Master";
        public const string MusicPrefKey =
            "SpireChess.Audio.v1.Music";
        public const string SfxPrefKey =
            "SpireChess.Audio.v1.SFX";
        public const string UiPrefKey =
            "SpireChess.Audio.v1.UI";

        public const string MasterMixerParameter = "MasterVolumeDb";
        public const string MusicMixerParameter = "MusicVolumeDb";
        public const string SfxMixerParameter = "SfxVolumeDb";
        public const string UiMixerParameter = "UiVolumeDb";

        public const float MinimumDecibels = -80f;
        public const float MaximumDecibels = 0f;
        public const float DefaultLinearVolume = 1f;

        private float master;
        private float music;
        private float sfx;
        private float ui;

        public PresentationAudioSettings(
            float master = DefaultLinearVolume,
            float music = DefaultLinearVolume,
            float sfx = DefaultLinearVolume,
            float ui = DefaultLinearVolume)
        {
            Master = master;
            Music = music;
            Sfx = sfx;
            Ui = ui;
        }

        public float Master
        {
            get => master;
            set => master = ClampLinear(value);
        }

        public float Music
        {
            get => music;
            set => music = ClampLinear(value);
        }

        public float Sfx
        {
            get => sfx;
            set => sfx = ClampLinear(value);
        }

        public float Ui
        {
            get => ui;
            set => ui = ClampLinear(value);
        }

        public static PresentationAudioSettings Load()
        {
            return new PresentationAudioSettings(
                PlayerPrefs.GetFloat(MasterPrefKey, DefaultLinearVolume),
                PlayerPrefs.GetFloat(MusicPrefKey, DefaultLinearVolume),
                PlayerPrefs.GetFloat(SfxPrefKey, DefaultLinearVolume),
                PlayerPrefs.GetFloat(UiPrefKey, DefaultLinearVolume));
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(MasterPrefKey, Master);
            PlayerPrefs.SetFloat(MusicPrefKey, Music);
            PlayerPrefs.SetFloat(SfxPrefKey, Sfx);
            PlayerPrefs.SetFloat(UiPrefKey, Ui);
            PlayerPrefs.Save();
        }

        public float GetBusLinearVolume(PresentationAudioBus bus)
        {
            switch (bus)
            {
                case PresentationAudioBus.Music:
                    return Music;
                case PresentationAudioBus.Sfx:
                    return Sfx;
                case PresentationAudioBus.Ui:
                    return Ui;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(bus),
                        bus,
                        null);
            }
        }

        public void SetBusLinearVolume(
            PresentationAudioBus bus,
            float value)
        {
            switch (bus)
            {
                case PresentationAudioBus.Music:
                    Music = value;
                    break;
                case PresentationAudioBus.Sfx:
                    Sfx = value;
                    break;
                case PresentationAudioBus.Ui:
                    Ui = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(bus),
                        bus,
                        null);
            }
        }

        public float GetEffectiveLinearVolume(PresentationAudioBus bus)
        {
            return Master * GetBusLinearVolume(bus);
        }

        public bool ApplyToMixer(AudioMixer mixer)
        {
            if (mixer == null)
            {
                return false;
            }

            var applied = mixer.SetFloat(
                MasterMixerParameter,
                LinearToDecibels(Master));
            applied &= mixer.SetFloat(
                MusicMixerParameter,
                LinearToDecibels(Music));
            applied &= mixer.SetFloat(
                SfxMixerParameter,
                LinearToDecibels(Sfx));
            applied &= mixer.SetFloat(
                UiMixerParameter,
                LinearToDecibels(Ui));
            return applied;
        }

        public static float LinearToDecibels(float linear)
        {
            var clamped = ClampLinear(linear);
            if (clamped <= 0.0001f)
            {
                return MinimumDecibels;
            }

            return Mathf.Clamp(
                20f * Mathf.Log10(clamped),
                MinimumDecibels,
                MaximumDecibels);
        }

        public static float DecibelsToLinear(float decibels)
        {
            if (float.IsNaN(decibels) || decibels <= MinimumDecibels)
            {
                return 0f;
            }

            var clamped = Mathf.Clamp(
                decibels,
                MinimumDecibels,
                MaximumDecibels);
            return Mathf.Clamp01(Mathf.Pow(10f, clamped / 20f));
        }

        private static float ClampLinear(float value)
        {
            if (float.IsNaN(value))
            {
                return DefaultLinearVolume;
            }

            return Mathf.Clamp01(value);
        }
    }
}
