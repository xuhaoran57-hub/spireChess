using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Audio;

namespace SpireChess.Audio
{
    public enum PresentationAudioCatalogValidationMode
    {
        Commissioning,
        ProductionStrict
    }

    public enum PresentationAudioCueAssetStatus
    {
        Pending = 0,
        Placeholder = 1,
        ProductionApproved = 2
    }

    [Serializable]
    public sealed class PresentationAudioCueDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private PresentationAudioBus bus;
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField] private PresentationAudioCueAssetStatus assetStatus =
            PresentationAudioCueAssetStatus.Pending;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0.01f, 3f)] private float minPitch = 1f;
        [SerializeField, Range(0.01f, 3f)] private float maxPitch = 1f;
        [SerializeField, Min(1)] private int concurrencyLimit = 4;
        [SerializeField, Min(0f)] private float cooldownSeconds;
        [SerializeField] private bool loop;

        public PresentationAudioCueDefinition()
        {
        }

        public PresentationAudioCueDefinition(
            string id,
            PresentationAudioBus bus,
            AudioClip[] clips = null,
            float volume = 1f,
            float minPitch = 1f,
            float maxPitch = 1f,
            int concurrencyLimit = 4,
            float cooldownSeconds = 0f,
            bool loop = false,
            PresentationAudioCueAssetStatus assetStatus =
                PresentationAudioCueAssetStatus.Pending)
        {
            this.id = id;
            this.bus = bus;
            this.clips = clips ?? Array.Empty<AudioClip>();
            this.assetStatus = assetStatus;
            this.volume = volume;
            this.minPitch = minPitch;
            this.maxPitch = maxPitch;
            this.concurrencyLimit = concurrencyLimit;
            this.cooldownSeconds = cooldownSeconds;
            this.loop = loop;
        }

        public string Id => id;
        public PresentationAudioBus Bus => bus;
        public IReadOnlyList<AudioClip> Clips =>
            clips ?? Array.Empty<AudioClip>();
        public PresentationAudioCueAssetStatus AssetStatus => assetStatus;
        public float Volume => Mathf.Clamp01(volume);
        public float MinPitch => Mathf.Clamp(minPitch, 0.01f, 3f);
        public float MaxPitch => Mathf.Clamp(maxPitch, MinPitch, 3f);
        public int ConcurrencyLimit => Mathf.Max(1, concurrencyLimit);
        public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);
        public bool Loop => loop;
        public bool HasPlayableClip => CountPlayableClips() > 0;
        public bool IsProductionReady =>
            assetStatus == PresentationAudioCueAssetStatus.ProductionApproved &&
            HasCompleteRequiredClipSet();

        internal float SerializedVolume => volume;
        internal float SerializedMinPitch => minPitch;
        internal float SerializedMaxPitch => maxPitch;
        internal int SerializedConcurrencyLimit => concurrencyLimit;
        internal float SerializedCooldownSeconds => cooldownSeconds;
        internal int PlayableClipCount => CountPlayableClips();
        internal int DistinctPlayableClipCount =>
            CountDistinctPlayableClips();

        public AudioClip SelectClip(int selectionSeed)
        {
            var values = clips ?? Array.Empty<AudioClip>();
            if (values.Length == 0)
            {
                return null;
            }

            var start = (selectionSeed & int.MaxValue) % values.Length;
            for (var offset = 0; offset < values.Length; offset++)
            {
                var candidate = values[(start + offset) % values.Length];
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private int CountPlayableClips()
        {
            var count = 0;
            foreach (var clip in clips ?? Array.Empty<AudioClip>())
            {
                if (clip != null)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountDistinctPlayableClips()
        {
            var distinct = new HashSet<AudioClip>();
            foreach (var clip in clips ?? Array.Empty<AudioClip>())
            {
                if (clip != null)
                {
                    distinct.Add(clip);
                }
            }

            return distinct.Count;
        }

        private bool HasCompleteRequiredClipSet()
        {
            var values = clips ?? Array.Empty<AudioClip>();
            if (!PresentationAudioCueIds.TryGetRequiredVariantCount(
                    id,
                    out var expectedCount))
            {
                return CountPlayableClips() == values.Length &&
                    CountDistinctPlayableClips() == values.Length &&
                    values.Length > 0;
            }

            return values.Length == expectedCount &&
                CountPlayableClips() == expectedCount &&
                CountDistinctPlayableClips() == expectedCount;
        }
    }

    public sealed class PresentationAudioCatalogValidationResult
    {
        private readonly ReadOnlyCollection<string> errors;
        private readonly ReadOnlyCollection<string> warnings;

        internal PresentationAudioCatalogValidationResult(
            IList<string> errors,
            IList<string> warnings)
        {
            this.errors = new ReadOnlyCollection<string>(
                new List<string>(errors));
            this.warnings = new ReadOnlyCollection<string>(
                new List<string>(warnings));
        }

        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
    }

    [CreateAssetMenu(
        fileName = "PresentationAudioCatalog",
        menuName = "Spire Chess/Presentation/Audio Catalog")]
    public sealed class PresentationAudioCatalog : ScriptableObject
    {
        public const string DefaultResourcesPath =
            "Presentation/PresentationAudioCatalog";

        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup musicOutput;
        [SerializeField] private AudioMixerGroup sfxOutput;
        [SerializeField] private AudioMixerGroup uiOutput;
        [SerializeField] private PresentationAudioCueDefinition[] cues =
            Array.Empty<PresentationAudioCueDefinition>();

        private Dictionary<string, PresentationAudioCueDefinition> cueById;

        public AudioMixer AudioMixer => audioMixer;
        public AudioMixerGroup MusicOutput => musicOutput;
        public AudioMixerGroup SfxOutput => sfxOutput;
        public AudioMixerGroup UiOutput => uiOutput;
        public IReadOnlyList<PresentationAudioCueDefinition> Cues =>
            cues ?? Array.Empty<PresentationAudioCueDefinition>();

        public bool TryGetCue(
            string cueId,
            out PresentationAudioCueDefinition definition)
        {
            EnsureLookup();
            if (string.IsNullOrWhiteSpace(cueId))
            {
                definition = null;
                return false;
            }

            return cueById.TryGetValue(cueId, out definition);
        }

        public AudioMixerGroup GetOutputGroup(PresentationAudioBus bus)
        {
            switch (bus)
            {
                case PresentationAudioBus.Music:
                    return musicOutput;
                case PresentationAudioBus.Sfx:
                    return sfxOutput;
                case PresentationAudioBus.Ui:
                    return uiOutput;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(bus),
                        bus,
                        null);
            }
        }

        public PresentationAudioCatalogValidationResult Validate(
            PresentationAudioCatalogValidationMode mode)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            var values = cues ?? Array.Empty<PresentationAudioCueDefinition>();
            for (var index = 0; index < values.Length; index++)
            {
                var cue = values[index];
                if (cue == null)
                {
                    errors.Add($"Audio cue entry {index} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(cue.Id))
                {
                    errors.Add($"Audio cue entry {index} has no semantic ID.");
                    continue;
                }

                if (!seen.Add(cue.Id))
                {
                    errors.Add(
                        $"Audio cue ID '{cue.Id}' is registered more than once.");
                }

                if (!PresentationAudioCueIds.IsRequired(cue.Id))
                {
                    errors.Add(
                        $"Audio cue '{cue.Id}' is not part of the frozen " +
                        "G3 P0 contract.");
                }
                else if (PresentationAudioCueIds.TryGetExpectedBus(
                        cue.Id,
                        out var expectedBus) &&
                    cue.Bus != expectedBus)
                {
                    errors.Add(
                        $"Audio cue '{cue.Id}' must use the {expectedBus} bus, " +
                        $"not {cue.Bus}.");
                }

                if (PresentationAudioCueIds.IsRequiredMusic(cue.Id) &&
                    !cue.Loop)
                {
                    errors.Add(
                        $"Required music cue '{cue.Id}' must be loopable.");
                }

                ValidateNumericFields(cue, errors);
                ValidateClips(cue, mode, errors, warnings);
            }

            foreach (var requiredCueId in PresentationAudioCueIds.AllRequired)
            {
                if (!seen.Contains(requiredCueId))
                {
                    errors.Add(
                        $"Required audio cue '{requiredCueId}' is not registered.");
                }
            }

            ValidateMixerReference(
                "AudioMixer",
                audioMixer,
                mode,
                errors,
                warnings);
            ValidateMixerReference(
                "Music output group",
                musicOutput,
                mode,
                errors,
                warnings);
            ValidateMixerReference(
                "SFX output group",
                sfxOutput,
                mode,
                errors,
                warnings);
            ValidateMixerReference(
                "UI output group",
                uiOutput,
                mode,
                errors,
                warnings);

            return new PresentationAudioCatalogValidationResult(
                errors,
                warnings);
        }

        private static void ValidateNumericFields(
            PresentationAudioCueDefinition cue,
            ICollection<string> errors)
        {
            if (float.IsNaN(cue.SerializedVolume) ||
                cue.SerializedVolume < 0f ||
                cue.SerializedVolume > 1f)
            {
                errors.Add(
                    $"Audio cue '{cue.Id}' volume must be between 0 and 1.");
            }

            if (float.IsNaN(cue.SerializedMinPitch) ||
                float.IsNaN(cue.SerializedMaxPitch) ||
                cue.SerializedMinPitch < 0.01f ||
                cue.SerializedMaxPitch > 3f ||
                cue.SerializedMinPitch > cue.SerializedMaxPitch)
            {
                errors.Add(
                    $"Audio cue '{cue.Id}' pitch range must be ordered " +
                    "between 0.01 and 3.");
            }

            if (cue.SerializedConcurrencyLimit < 1)
            {
                errors.Add(
                    $"Audio cue '{cue.Id}' concurrency limit must be at least 1.");
            }

            if (float.IsNaN(cue.SerializedCooldownSeconds) ||
                cue.SerializedCooldownSeconds < 0f)
            {
                errors.Add(
                    $"Audio cue '{cue.Id}' cooldown cannot be negative.");
            }
        }

        private static void ValidateClips(
            PresentationAudioCueDefinition cue,
            PresentationAudioCatalogValidationMode mode,
            ICollection<string> errors,
            ICollection<string> warnings)
        {
            if (!Enum.IsDefined(
                    typeof(PresentationAudioCueAssetStatus),
                    cue.AssetStatus))
            {
                errors.Add(
                    $"Audio cue '{cue.Id}' has an invalid asset status " +
                    $"{(int)cue.AssetStatus}.");
                return;
            }

            if (!cue.HasPlayableClip)
            {
                var missingMessage =
                    $"Audio cue '{cue.Id}' has no assigned AudioClip.";
                if (cue.AssetStatus ==
                    PresentationAudioCueAssetStatus.ProductionApproved)
                {
                    errors.Add(
                        missingMessage +
                        " A production-approved cue must be playable.");
                }
                else if (
                    mode ==
                    PresentationAudioCatalogValidationMode.ProductionStrict)
                {
                    errors.Add(missingMessage);
                }
                else
                {
                    warnings.Add(
                        missingMessage +
                        " This is allowed while production audio is pending.");
                }

                return;
            }

            if (cue.AssetStatus ==
                PresentationAudioCueAssetStatus.ProductionApproved)
            {
                if (PresentationAudioCueIds.TryGetRequiredVariantCount(
                        cue.Id,
                        out var expectedCount))
                {
                    var slotCount = cue.Clips.Count;
                    var playableCount = cue.PlayableClipCount;
                    var distinctPlayableCount =
                        cue.DistinctPlayableClipCount;
                    if (slotCount != expectedCount ||
                        playableCount != expectedCount ||
                        distinctPlayableCount != expectedCount)
                    {
                        errors.Add(
                            $"Audio cue '{cue.Id}' is production-approved " +
                            $"but must assign exactly {expectedCount} " +
                            $"playable AudioClip variant(s); found " +
                            $"{slotCount} slot(s) / {playableCount} playable " +
                            $"/ {distinctPlayableCount} distinct.");
                    }
                }

                return;
            }

            var statusMessage =
                $"Audio cue '{cue.Id}' uses {cue.AssetStatus} audio and is " +
                "not production-approved.";
            if (mode == PresentationAudioCatalogValidationMode.ProductionStrict)
            {
                errors.Add(statusMessage);
            }
            else
            {
                warnings.Add(statusMessage);
            }
        }

        private static void ValidateMixerReference(
            string name,
            UnityEngine.Object value,
            PresentationAudioCatalogValidationMode mode,
            ICollection<string> errors,
            ICollection<string> warnings)
        {
            if (value != null)
            {
                return;
            }

            var message = $"{name} is not assigned.";
            if (mode == PresentationAudioCatalogValidationMode.ProductionStrict)
            {
                errors.Add(message);
            }
            else
            {
                warnings.Add(
                    message + " This is allowed while production audio is pending.");
            }
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
        }
#endif

        private void EnsureLookup()
        {
            if (cueById == null)
            {
                RebuildLookup();
            }
        }

        private void RebuildLookup()
        {
            cueById =
                new Dictionary<string, PresentationAudioCueDefinition>(
                    StringComparer.Ordinal);
            foreach (var cue in cues ?? Array.Empty<PresentationAudioCueDefinition>())
            {
                if (cue == null || string.IsNullOrWhiteSpace(cue.Id))
                {
                    continue;
                }

                cueById[cue.Id] = cue;
            }
        }
    }
}
