using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Audio;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Editor
{
    public static class G3PlaceholderAudioAssetBuilder
    {
        public const string PresentationAudioRoot =
            "Assets/Audio/Presentation";
        public const string ManifestPath =
            PresentationAudioRoot +
            "/Placeholder/placeholder_audio_manifest.json";

        private static readonly CueClipSpec[] CueSpecs =
        {
            Music(PresentationAudioCueIds.BgmMainMenu),
            Music(PresentationAudioCueIds.BgmRunShop),
            Music(PresentationAudioCueIds.BgmBattleNormal),
            Sfx(PresentationAudioCueIds.UiClick, "UI", 3),
            Sfx(PresentationAudioCueIds.UiConfirm, "UI", 2),
            Sfx(PresentationAudioCueIds.UiCancel, "UI", 2),
            Sfx(PresentationAudioCueIds.UiError, "UI", 2),
            Sfx(PresentationAudioCueIds.ShopRefresh, "Shop", 3),
            Sfx(PresentationAudioCueIds.ShopBuy, "Shop", 3),
            Sfx(PresentationAudioCueIds.ShopSell, "Shop", 3),
            Sfx(PresentationAudioCueIds.ShopPlay, "Shop", 3),
            Sfx(PresentationAudioCueIds.ShopSpell, "Shop", 3),
            Sfx(PresentationAudioCueIds.ShopTriple, "Shop", 1),
            Sfx(PresentationAudioCueIds.ShopDiscoverOpen, "Shop", 1),
            Sfx(PresentationAudioCueIds.ShopDiscoverPick, "Shop", 2),
            Sfx(PresentationAudioCueIds.ShopUpgrade, "Shop", 1),
            Sfx(PresentationAudioCueIds.BattleAttackLight, "Battle", 4),
            Sfx(PresentationAudioCueIds.BattleHit, "Battle", 4),
            Sfx(PresentationAudioCueIds.BattleShieldGain, "Battle", 3),
            Sfx(PresentationAudioCueIds.BattleShieldBreak, "Battle", 3),
            Sfx(PresentationAudioCueIds.BattleStatUp, "Battle", 3),
            Sfx(PresentationAudioCueIds.BattleDeath, "Battle", 4),
            Sfx(PresentationAudioCueIds.BattleTokenDeath, "Battle", 3),
            Sfx(PresentationAudioCueIds.BattleSummon, "Battle", 4),
            Sfx(PresentationAudioCueIds.BattleVictory, "Battle", 1),
            Sfx(PresentationAudioCueIds.BattleDefeat, "Battle", 1),
            Sfx(PresentationAudioCueIds.RunNodeSelect, "Run", 3),
            Sfx(PresentationAudioCueIds.RunReward, "Run", 2)
        };

        public static int ExpectedCueCount => CueSpecs.Length;
        public static int ExpectedClipCount =>
            CueSpecs.Sum(spec => spec.VariantCount);

        [MenuItem("Spire Chess/Audio/Attach G3 Local Synth Placeholders")]
        public static void Attach()
        {
            ValidateContract();
            G3AudioAssetBuilder.Build();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath) == null)
            {
                throw new InvalidOperationException(
                    "The local synth manifest is missing at " + ManifestPath +
                    ". Run tools/generate_g3_placeholder_audio.py first.");
            }

            var clipsByCueId =
                new Dictionary<string, AudioClip[]>(StringComparer.Ordinal);
            foreach (var spec in CueSpecs)
            {
                var clips = new AudioClip[spec.VariantCount];
                for (var variant = 1;
                     variant <= spec.VariantCount;
                     variant++)
                {
                    var path = GetExpectedAssetPath(spec.CueId, variant);
                    ConfigureImporter(path, spec.IsMusic);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip == null)
                    {
                        throw new InvalidOperationException(
                            $"Placeholder clip '{spec.CueId}' variant " +
                            $"{variant:00} is missing or could not be imported " +
                            $"at {path}.");
                    }

                    if (clip.frequency != 48000)
                    {
                        throw new InvalidOperationException(
                            $"Placeholder clip '{path}' imported at " +
                            $"{clip.frequency} Hz instead of 48000 Hz.");
                    }

                    clips[variant - 1] = clip;
                }

                clipsByCueId.Add(spec.CueId, clips);
            }

            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationAudioCatalog>(
                    G3AudioAssetBuilder.CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "G3 audio catalog is missing at " +
                    G3AudioAssetBuilder.CatalogPath);
            }

            var attachedPlaceholderCount =
                AttachToCatalog(catalog, clipsByCueId);
            AssetDatabase.SaveAssets();

            var commissioning = catalog.Validate(
                PresentationAudioCatalogValidationMode.Commissioning);
            if (!commissioning.IsValid)
            {
                throw new InvalidOperationException(
                    "The catalog is invalid after attaching local synth " +
                    "placeholders:\n" +
                    string.Join("\n", commissioning.Errors));
            }

            var placeholderCueCount = catalog.Cues.Count(cue =>
                cue.AssetStatus ==
                PresentationAudioCueAssetStatus.Placeholder);
            var placeholderWarningCount =
                commissioning.Warnings.Count(message =>
                    message.Contains("not production-approved"));
            if (placeholderWarningCount != placeholderCueCount)
            {
                throw new InvalidOperationException(
                    "Expected one placeholder warning per cue, but found " +
                    placeholderWarningCount + ".");
            }

            var strict = catalog.Validate(
                PresentationAudioCatalogValidationMode.ProductionStrict);
            var nonApprovedCueCount = catalog.Cues.Count(cue =>
                cue.AssetStatus !=
                PresentationAudioCueAssetStatus.ProductionApproved);
            var placeholderErrorCount = strict.Errors.Count(message =>
                message.Contains("not production-approved"));
            if (strict.IsValid != (nonApprovedCueCount == 0) ||
                placeholderErrorCount != placeholderCueCount)
            {
                throw new InvalidOperationException(
                    "ProductionStrict did not report the expected local " +
                    "placeholder state.");
            }

            var productionAssetErrors =
                G3AudioAssetBuilder.GetProductionAssetContractErrors(catalog);
            if (productionAssetErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Existing production-approved audio failed its asset " +
                    "contract and was not replaced:\n" +
                    string.Join("\n", productionAssetErrors));
            }

            Debug.Log(
                "[Audio] G3 local synth placeholder attach completed: " +
                $"{attachedPlaceholderCount} cue(s) attached, " +
                $"{ExpectedCueCount - attachedPlaceholderCount} " +
                "production-approved cue(s) preserved. " +
                $"{placeholderCueCount} placeholder cue(s) remain.");
        }

        public static void AttachFromCommandLine()
        {
            try
            {
                Attach();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        public static int GetExpectedVariantCount(string cueId)
        {
            var spec = FindSpec(cueId);
            return spec.VariantCount;
        }

        public static bool IsExpectedMusicCue(string cueId)
        {
            return FindSpec(cueId).IsMusic;
        }

        public static string GetExpectedAssetPath(
            string cueId,
            int variant)
        {
            var spec = FindSpec(cueId);
            if (variant < 1 || variant > spec.VariantCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(variant),
                    variant,
                    $"Cue '{cueId}' has {spec.VariantCount} variants.");
            }

            if (spec.IsMusic)
            {
                return PresentationAudioRoot +
                    "/Music/Placeholder/placeholder_" +
                    cueId +
                    "_v01.wav";
            }

            return PresentationAudioRoot +
                "/SFX/" +
                spec.Domain +
                "/Placeholder/placeholder_sfx_" +
                cueId +
                "_" +
                variant.ToString("00") +
                ".wav";
        }

        private static int AttachToCatalog(
            PresentationAudioCatalog catalog,
            IReadOnlyDictionary<string, AudioClip[]> clipsByCueId)
        {
            var serialized = new SerializedObject(catalog);
            serialized.Update();
            var cues = serialized.FindProperty("cues");
            var attachedCueIds =
                new HashSet<string>(StringComparer.Ordinal);
            var attachedPlaceholderCount = 0;

            for (var index = 0; index < cues.arraySize; index++)
            {
                var cue = cues.GetArrayElementAtIndex(index);
                var cueId =
                    cue.FindPropertyRelative("id").stringValue;
                if (!clipsByCueId.TryGetValue(cueId, out var clips))
                {
                    continue;
                }

                var assetStatus =
                    cue.FindPropertyRelative("assetStatus");
                if (assetStatus.enumValueIndex ==
                    (int)PresentationAudioCueAssetStatus.ProductionApproved)
                {
                    attachedCueIds.Add(cueId);
                    continue;
                }

                var serializedClips =
                    cue.FindPropertyRelative("clips");
                serializedClips.arraySize = clips.Length;
                for (var clipIndex = 0;
                     clipIndex < clips.Length;
                     clipIndex++)
                {
                    serializedClips
                        .GetArrayElementAtIndex(clipIndex)
                        .objectReferenceValue = clips[clipIndex];
                }

                assetStatus.enumValueIndex =
                    (int)PresentationAudioCueAssetStatus.Placeholder;
                attachedCueIds.Add(cueId);
                attachedPlaceholderCount++;
            }

            if (attachedCueIds.Count != clipsByCueId.Count)
            {
                var missing = clipsByCueId.Keys
                    .Where(cueId => !attachedCueIds.Contains(cueId));
                throw new InvalidOperationException(
                    "Catalog is missing required placeholder cue IDs: " +
                    string.Join(", ", missing));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return attachedPlaceholderCount;
        }

        private static void ConfigureImporter(
            string assetPath,
            bool isMusic)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "No AudioImporter is available for " + assetPath);
            }

            var settings = importer.defaultSampleSettings;
            var desiredLoadType = isMusic
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.DecompressOnLoad;
            var desiredCompression = isMusic
                ? AudioCompressionFormat.Vorbis
                : AudioCompressionFormat.PCM;
            var changed =
                importer.forceToMono ||
                importer.loadInBackground != isMusic ||
                settings.preloadAudioData == isMusic ||
                settings.loadType != desiredLoadType ||
                settings.compressionFormat != desiredCompression ||
                settings.sampleRateSetting !=
                    AudioSampleRateSetting.PreserveSampleRate ||
                isMusic && Math.Abs(settings.quality - 0.55f) > 0.0001f;

            if (!changed)
            {
                return;
            }

            importer.forceToMono = false;
            importer.loadInBackground = isMusic;
            settings.loadType = desiredLoadType;
            settings.compressionFormat = desiredCompression;
            settings.sampleRateSetting =
                AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = !isMusic;
            if (isMusic)
            {
                settings.quality = 0.55f;
            }

            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void ValidateContract()
        {
            if (CueSpecs.Length !=
                PresentationAudioCueIds.AllRequired.Count ||
                ExpectedClipCount != 67)
            {
                throw new InvalidOperationException(
                    "The G3 local synth contract must contain 28 cues and " +
                    "67 clips.");
            }

            var expected =
                new HashSet<string>(
                    PresentationAudioCueIds.AllRequired,
                    StringComparer.Ordinal);
            var actual =
                new HashSet<string>(
                    CueSpecs.Select(spec => spec.CueId),
                    StringComparer.Ordinal);
            if (actual.Count != CueSpecs.Length ||
                !expected.SetEquals(actual))
            {
                throw new InvalidOperationException(
                    "The G3 local synth cue matrix does not exactly match " +
                    "the frozen P0 cue contract.");
            }

            foreach (var spec in CueSpecs)
            {
                if (!PresentationAudioCueIds.TryGetRequiredVariantCount(
                        spec.CueId,
                        out var expectedVariantCount) ||
                    spec.VariantCount != expectedVariantCount)
                {
                    throw new InvalidOperationException(
                        $"The local synth variant count for '{spec.CueId}' " +
                        "does not match the frozen production contract.");
                }
            }
        }

        private static CueClipSpec FindSpec(string cueId)
        {
            var spec = CueSpecs.FirstOrDefault(value =>
                string.Equals(
                    value.CueId,
                    cueId,
                    StringComparison.Ordinal));
            if (spec.CueId == null)
            {
                throw new ArgumentException(
                    "Unknown G3 placeholder audio cue ID.",
                    nameof(cueId));
            }

            return spec;
        }

        private static CueClipSpec Music(string cueId)
        {
            return new CueClipSpec(cueId, "Music", 1, true);
        }

        private static CueClipSpec Sfx(
            string cueId,
            string domain,
            int variantCount)
        {
            return new CueClipSpec(
                cueId,
                domain,
                variantCount,
                false);
        }

        private readonly struct CueClipSpec
        {
            public CueClipSpec(
                string cueId,
                string domain,
                int variantCount,
                bool isMusic)
            {
                CueId = cueId;
                Domain = domain;
                VariantCount = variantCount;
                IsMusic = isMusic;
            }

            public string CueId { get; }
            public string Domain { get; }
            public int VariantCount { get; }
            public bool IsMusic { get; }
        }
    }
}
