using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Audio;
using SpireChess.Editor;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Tests.EditMode
{
    public sealed class PresentationAudioTests
    {
        private PlayerPrefSnapshot masterSnapshot;
        private PlayerPrefSnapshot musicSnapshot;
        private PlayerPrefSnapshot sfxSnapshot;
        private PlayerPrefSnapshot uiSnapshot;

        private static readonly string[] ExpectedP0CueIds =
        {
            "bgm_main_menu",
            "bgm_run_shop",
            "bgm_battle_normal",
            "ui_click",
            "ui_confirm",
            "ui_cancel",
            "ui_error",
            "shop_refresh",
            "shop_buy",
            "shop_sell",
            "shop_play",
            "shop_spell",
            "shop_triple",
            "shop_discover_open",
            "shop_discover_pick",
            "shop_upgrade",
            "battle_attack_light",
            "battle_hit",
            "battle_shield_gain",
            "battle_shield_break",
            "battle_stat_up",
            "battle_death",
            "battle_token_death",
            "battle_summon",
            "battle_victory",
            "battle_defeat",
            "run_node_select",
            "run_reward"
        };

        [SetUp]
        public void SetUp()
        {
            masterSnapshot = CapturePreference(
                PresentationAudioSettings.MasterPrefKey);
            musicSnapshot = CapturePreference(
                PresentationAudioSettings.MusicPrefKey);
            sfxSnapshot = CapturePreference(
                PresentationAudioSettings.SfxPrefKey);
            uiSnapshot = CapturePreference(
                PresentationAudioSettings.UiPrefKey);
        }

        [TearDown]
        public void TearDown()
        {
            RestorePreference(
                PresentationAudioSettings.MasterPrefKey,
                masterSnapshot);
            RestorePreference(
                PresentationAudioSettings.MusicPrefKey,
                musicSnapshot);
            RestorePreference(
                PresentationAudioSettings.SfxPrefKey,
                sfxSnapshot);
            RestorePreference(
                PresentationAudioSettings.UiPrefKey,
                uiSnapshot);
            PlayerPrefs.Save();
        }

        [Test]
        public void CueIds_AreUniqueAndCoverTheFrozenP0Contract()
        {
            Assert.That(
                PresentationAudioCueIds.AllRequired,
                Has.Count.EqualTo(ExpectedP0CueIds.Length));
            Assert.That(
                PresentationAudioCueIds.AllRequired.Distinct(
                    StringComparer.Ordinal).Count(),
                Is.EqualTo(ExpectedP0CueIds.Length));
            CollectionAssert.AreEquivalent(
                ExpectedP0CueIds,
                PresentationAudioCueIds.AllRequired);

            var expectedClipCount = 0;
            foreach (var cueId in PresentationAudioCueIds.AllRequired)
            {
                Assert.That(
                    PresentationAudioCueIds.TryGetExpectedBus(
                        cueId,
                        out _),
                    Is.True,
                    cueId);
                Assert.That(
                    PresentationAudioCueIds.TryGetRequiredVariantCount(
                        cueId,
                        out var variantCount),
                    Is.True,
                    cueId);
                Assert.That(variantCount, Is.GreaterThan(0), cueId);
                expectedClipCount += variantCount;
            }

            Assert.That(expectedClipCount, Is.EqualTo(67));
        }

        [Test]
        public void Catalog_CommissioningModeAllowsPendingClips()
        {
            var catalog = CreateCatalog(CreateCompleteDefinitions());
            try
            {
                var result = catalog.Validate(
                    PresentationAudioCatalogValidationMode.Commissioning);

                Assert.That(
                    result.IsValid,
                    Is.True,
                    string.Join("\n", result.Errors));
                Assert.That(
                    result.Warnings.Any(value =>
                        value.Contains("has no assigned AudioClip")),
                    Is.True);
                Assert.That(
                    catalog.TryGetCue(
                        PresentationAudioCueIds.ShopRefresh,
                        out var refresh),
                    Is.True);
                Assert.That(
                    refresh.Bus,
                    Is.EqualTo(PresentationAudioBus.Sfx));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Catalog_ProductionStrictModeRejectsPendingClips()
        {
            var catalog = CreateCatalog(CreateCompleteDefinitions());
            try
            {
                var result = catalog.Validate(
                    PresentationAudioCatalogValidationMode.ProductionStrict);

                Assert.That(result.IsValid, Is.False);
                Assert.That(
                    result.Errors.Any(value =>
                        value.Contains(
                            "Audio cue 'bgm_main_menu' has no assigned AudioClip")),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Catalog_PlayablePlaceholderRemainsOutsideProductionGate()
        {
            var clip = AudioClip.Create(
                "placeholder_test",
                4800,
                1,
                48000,
                false);
            var definitions = CreateCompleteDefinitions();
            definitions[0] = new PresentationAudioCueDefinition(
                PresentationAudioCueIds.BgmMainMenu,
                PresentationAudioBus.Music,
                new[] { clip },
                loop: true,
                assetStatus:
                    PresentationAudioCueAssetStatus.Placeholder);
            var catalog = CreateCatalog(definitions);
            try
            {
                var commissioning = catalog.Validate(
                    PresentationAudioCatalogValidationMode.Commissioning);
                Assert.That(
                    commissioning.IsValid,
                    Is.True,
                    string.Join("\n", commissioning.Errors));
                Assert.That(
                    commissioning.Warnings.Any(value =>
                        value.Contains(
                            "'bgm_main_menu' uses Placeholder audio") &&
                        value.Contains("not production-approved")),
                    Is.True);

                var strict = catalog.Validate(
                    PresentationAudioCatalogValidationMode.ProductionStrict);
                Assert.That(strict.IsValid, Is.False);
                Assert.That(
                    strict.Errors.Any(value =>
                        value.Contains(
                            "'bgm_main_menu' uses Placeholder audio") &&
                        value.Contains("not production-approved")),
                    Is.True);
                Assert.That(definitions[0].HasPlayableClip, Is.True);
                Assert.That(definitions[0].IsProductionReady, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void Catalog_ProductionApprovedCueMustRemainPlayable()
        {
            var definitions = CreateCompleteDefinitions();
            definitions[0] = new PresentationAudioCueDefinition(
                PresentationAudioCueIds.BgmMainMenu,
                PresentationAudioBus.Music,
                loop: true,
                assetStatus:
                    PresentationAudioCueAssetStatus.ProductionApproved);
            var catalog = CreateCatalog(definitions);
            try
            {
                var result = catalog.Validate(
                    PresentationAudioCatalogValidationMode.Commissioning);

                Assert.That(result.IsValid, Is.False);
                Assert.That(
                    result.Errors.Any(value =>
                        value.Contains(
                            "A production-approved cue must be playable")),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Catalog_ProductionApprovedPlayableCuesPassCueLevelGate()
        {
            var clips = new List<AudioClip>();
            var definitions =
                new List<PresentationAudioCueDefinition>();
            foreach (var cueId in PresentationAudioCueIds.AllRequired)
            {
                Assert.That(
                    PresentationAudioCueIds.TryGetExpectedBus(
                        cueId,
                        out var bus),
                    Is.True,
                    cueId);
                Assert.That(
                    PresentationAudioCueIds.TryGetRequiredVariantCount(
                        cueId,
                        out var variantCount),
                    Is.True,
                    cueId);
                var cueClips = new AudioClip[variantCount];
                for (var index = 0; index < variantCount; index++)
                {
                    cueClips[index] = AudioClip.Create(
                        $"{cueId}_{index + 1:00}",
                        4800,
                        1,
                        48000,
                        false);
                    clips.Add(cueClips[index]);
                }

                definitions.Add(new PresentationAudioCueDefinition(
                    cueId,
                    bus,
                    cueClips,
                    loop: bus == PresentationAudioBus.Music,
                    assetStatus:
                        PresentationAudioCueAssetStatus.ProductionApproved));
            }

            var catalog = CreateCatalog(definitions.ToArray());
            try
            {
                var result = catalog.Validate(
                    PresentationAudioCatalogValidationMode.ProductionStrict);

                Assert.That(
                    result.Errors.Any(value =>
                        value.StartsWith(
                            "Audio cue ",
                            StringComparison.Ordinal)),
                    Is.False,
                    string.Join("\n", result.Errors));
                Assert.That(
                    definitions.All(cue => cue.IsProductionReady),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                foreach (var clip in clips)
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                }
            }
        }

        [Test]
        public void Catalog_ProductionApprovedCueRequiresExactVariantSet()
        {
            var clipA = AudioClip.Create(
                "invalid_production_a",
                4800,
                1,
                48000,
                false);
            var clipB = AudioClip.Create(
                "invalid_production_b",
                4800,
                1,
                48000,
                false);
            try
            {
                foreach (var invalidClips in new[]
                         {
                             new[] { clipA, clipA, clipA },
                             new[] { clipA, null, clipB }
                         })
                {
                    var definitions = CreateCompleteDefinitions();
                    definitions[3] =
                        new PresentationAudioCueDefinition(
                            PresentationAudioCueIds.UiClick,
                            PresentationAudioBus.Ui,
                            invalidClips,
                            assetStatus:
                                PresentationAudioCueAssetStatus
                                    .ProductionApproved);
                    var catalog = CreateCatalog(definitions);
                    try
                    {
                        var result = catalog.Validate(
                            PresentationAudioCatalogValidationMode
                                .Commissioning);

                        Assert.That(result.IsValid, Is.False);
                        Assert.That(
                            result.Errors.Any(value =>
                                value.Contains(
                                    "'ui_click' is production-approved") &&
                                value.Contains(
                                    "exactly 3 playable AudioClip variant")),
                            Is.True,
                            string.Join("\n", result.Errors));
                        Assert.That(
                            definitions[3].IsProductionReady,
                            Is.False);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(catalog);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clipA);
                UnityEngine.Object.DestroyImmediate(clipB);
            }
        }

        [Test]
        public void ProductionAssetContract_RejectsPlaceholderPaths()
        {
            const string placeholderPath =
                "Assets/Audio/Presentation/Music/Placeholder/" +
                "placeholder_bgm_main_menu_v01.wav";
            Assert.That(
                G3AudioAssetBuilder.IsPlaceholderAssetPath(
                    placeholderPath),
                Is.True);
            Assert.That(
                G3AudioAssetBuilder.IsPlaceholderAssetPath(
                    "Assets/Audio/Presentation/Music/" +
                    "bgm_main_menu_v01.ogg"),
                Is.False);

            var clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(placeholderPath);
            if (clip == null)
            {
                return;
            }

            var catalog = CreateCatalog(
                new[]
                {
                    new PresentationAudioCueDefinition(
                        PresentationAudioCueIds.BgmMainMenu,
                        PresentationAudioBus.Music,
                        new[] { clip },
                        loop: true,
                        assetStatus:
                            PresentationAudioCueAssetStatus
                                .ProductionApproved)
                });
            try
            {
                var errors =
                    G3AudioAssetBuilder
                        .GetProductionAssetContractErrors(catalog);
                Assert.That(
                    errors.Any(value =>
                        value.Contains("still references a Placeholder asset")),
                    Is.True,
                    string.Join("\n", errors));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void LocalSynthContract_ExactlyCoversP0VariantMatrix()
        {
            Assert.That(
                G3PlaceholderAudioAssetBuilder.ExpectedCueCount,
                Is.EqualTo(ExpectedP0CueIds.Length));
            Assert.That(
                G3PlaceholderAudioAssetBuilder.ExpectedClipCount,
                Is.EqualTo(67));
            Assert.That(
                ExpectedP0CueIds.Sum(
                    G3PlaceholderAudioAssetBuilder
                        .GetExpectedVariantCount),
                Is.EqualTo(67));
            var packagePresent =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    G3PlaceholderAudioAssetBuilder.ManifestPath) != null;

            foreach (var cueId in ExpectedP0CueIds)
            {
                var expectedMusic =
                    PresentationAudioCueIds.IsRequiredMusic(cueId);
                Assert.That(
                    G3PlaceholderAudioAssetBuilder.IsExpectedMusicCue(cueId),
                    Is.EqualTo(expectedMusic),
                    cueId);
                Assert.That(
                    PresentationAudioCueIds.TryGetRequiredVariantCount(
                        cueId,
                        out var expectedVariants),
                    Is.True,
                    cueId);
                Assert.That(
                    G3PlaceholderAudioAssetBuilder
                        .GetExpectedVariantCount(cueId),
                    Is.EqualTo(expectedVariants),
                    cueId);
                for (var variant = 1;
                     variant <=
                     G3PlaceholderAudioAssetBuilder
                         .GetExpectedVariantCount(cueId);
                     variant++)
                {
                    var path =
                        G3PlaceholderAudioAssetBuilder.GetExpectedAssetPath(
                            cueId,
                            variant);
                    Assert.That(
                        path,
                        Does.Contain("/Placeholder/"),
                        cueId);
                    Assert.That(
                        G3AudioAssetBuilder
                            .GetExpectedProductionAssetPath(
                                cueId,
                                variant),
                        expectedMusic
                            ? Does.EndWith(
                                $"/Music/{cueId}_v01.ogg")
                            : Does.EndWith(
                                $"/sfx_{cueId}_{variant:00}.wav"),
                        cueId);
                    if (!packagePresent)
                    {
                        continue;
                    }

                    Assert.That(
                        AssetDatabase.LoadAssetAtPath<AudioClip>(path),
                        Is.Not.Null,
                        path);
                    var clip =
                        AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    Assert.That(clip.frequency, Is.EqualTo(48000), path);
                    var expectedStereo =
                        expectedMusic ||
                        cueId == PresentationAudioCueIds.ShopRefresh ||
                        cueId ==
                        PresentationAudioCueIds.ShopDiscoverOpen;
                    Assert.That(
                        clip.channels,
                        Is.EqualTo(expectedStereo ? 2 : 1),
                        path);

                    var importer =
                        AssetImporter.GetAtPath(path) as AudioImporter;
                    Assert.That(importer, Is.Not.Null, path);
                    Assert.That(importer.forceToMono, Is.False, path);
                    Assert.That(
                        importer.loadInBackground,
                        Is.EqualTo(expectedMusic),
                        path);
                    var settings = importer.defaultSampleSettings;
                    Assert.That(
                        settings.sampleRateSetting,
                        Is.EqualTo(
                            AudioSampleRateSetting.PreserveSampleRate),
                        path);
                    Assert.That(
                        settings.loadType,
                        Is.EqualTo(
                            expectedMusic
                                ? AudioClipLoadType.Streaming
                                : AudioClipLoadType.DecompressOnLoad),
                        path);
                    Assert.That(
                        settings.compressionFormat,
                        Is.EqualTo(
                            expectedMusic
                                ? AudioCompressionFormat.Vorbis
                                : AudioCompressionFormat.PCM),
                        path);
                    Assert.That(
                        settings.preloadAudioData,
                        Is.EqualTo(!expectedMusic),
                        path);
                }
            }
        }

        [Test]
        public void ProjectCatalog_ReportsReadinessFromExplicitAssetStatus()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationAudioCatalog>(
                    "Assets/Resources/Presentation/" +
                    "PresentationAudioCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.Cues.Count,
                Is.EqualTo(ExpectedP0CueIds.Length));
            Assert.That(catalog.AudioMixer, Is.Not.Null);
            Assert.That(catalog.MusicOutput?.name, Is.EqualTo("Music"));
            Assert.That(catalog.SfxOutput?.name, Is.EqualTo("SFX"));
            Assert.That(catalog.UiOutput?.name, Is.EqualTo("UI"));

            var commissioning = catalog.Validate(
                PresentationAudioCatalogValidationMode.Commissioning);
            Assert.That(
                commissioning.IsValid,
                Is.True,
                string.Join("\n", commissioning.Errors));
            var nonApprovedCueCount = catalog.Cues.Count(cue =>
                cue.AssetStatus !=
                PresentationAudioCueAssetStatus.ProductionApproved);
            Assert.That(
                commissioning.Warnings.Count(warning =>
                    warning.StartsWith(
                        "Audio cue '",
                        StringComparison.Ordinal)),
                Is.EqualTo(nonApprovedCueCount));

            var strict = catalog.Validate(
                PresentationAudioCatalogValidationMode.ProductionStrict);
            Assert.That(
                strict.IsValid,
                Is.EqualTo(nonApprovedCueCount == 0),
                string.Join("\n", strict.Errors));
            Assert.That(
                strict.Errors.Count(error =>
                    error.StartsWith(
                        "Audio cue '",
                        StringComparison.Ordinal)),
                Is.EqualTo(nonApprovedCueCount),
                string.Join("\n", strict.Errors));
            Assert.That(
                G3AudioAssetBuilder
                    .GetProductionAssetContractErrors(catalog),
                Is.Empty);

            foreach (var parameterName in new[]
                     {
                         PresentationAudioSettings.MasterMixerParameter,
                         PresentationAudioSettings.MusicMixerParameter,
                         PresentationAudioSettings.SfxMixerParameter,
                         PresentationAudioSettings.UiMixerParameter
                     })
            {
                Assert.That(
                    catalog.AudioMixer.GetFloat(
                        parameterName,
                        out _),
                    Is.True,
                    parameterName);
            }

            foreach (var cue in catalog.Cues)
            {
                Assert.That(cue, Is.Not.Null);
                Assert.That(
                    PresentationAudioCueIds.TryGetRequiredVariantCount(
                        cue.Id,
                        out var expectedVariantCount),
                    Is.True,
                    cue.Id);
                if (cue.AssetStatus ==
                    PresentationAudioCueAssetStatus.Pending)
                {
                    continue;
                }

                Assert.That(
                    cue.Clips.Count,
                    Is.EqualTo(expectedVariantCount),
                    cue.Id);
                for (var index = 0;
                     index < cue.Clips.Count;
                     index++)
                {
                    Assert.That(cue.Clips[index], Is.Not.Null, cue.Id);
                    var actualPath =
                        AssetDatabase.GetAssetPath(cue.Clips[index]);
                    var expectedPath =
                        cue.AssetStatus ==
                        PresentationAudioCueAssetStatus.Placeholder
                            ? G3PlaceholderAudioAssetBuilder
                                .GetExpectedAssetPath(
                                    cue.Id,
                                    index + 1)
                            : G3AudioAssetBuilder
                                .GetExpectedProductionAssetPath(
                                    cue.Id,
                                    index + 1);
                    Assert.That(
                        actualPath,
                        Is.EqualTo(expectedPath),
                        cue.Id);
                }
            }

            var localPackagePresent =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    G3PlaceholderAudioAssetBuilder.ManifestPath) != null;
            if (localPackagePresent)
            {
                Assert.That(
                    catalog.Cues.Any(cue =>
                        cue.AssetStatus ==
                        PresentationAudioCueAssetStatus.Pending),
                    Is.False,
                    "An attached local package must not silently regress " +
                    "from Placeholder to Pending.");
            }
        }

        [Test]
        public void Catalog_RejectsDuplicateAndMissingRequiredCue()
        {
            var definitions = CreateCompleteDefinitions().ToList();
            definitions[definitions.Count - 1] =
                new PresentationAudioCueDefinition(
                    PresentationAudioCueIds.BgmMainMenu,
                    PresentationAudioBus.Music,
                    loop: true);
            definitions.Add(new PresentationAudioCueDefinition(
                "unknown_extra_cue",
                PresentationAudioBus.Sfx));
            var catalog = CreateCatalog(definitions.ToArray());
            try
            {
                var result = catalog.Validate(
                    PresentationAudioCatalogValidationMode.Commissioning);

                Assert.That(result.IsValid, Is.False);
                Assert.That(
                    result.Errors.Any(value =>
                        value.Contains("registered more than once")),
                    Is.True);
                Assert.That(
                    result.Errors.Any(value =>
                        value.Contains(
                            $"'{PresentationAudioCueIds.RunReward}' is not registered")),
                    Is.True);
                Assert.That(
                    result.Errors.Any(value =>
                        value.Contains(
                            "'unknown_extra_cue' is not part of the frozen")),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Settings_PersistSeparatelyAndConvertLinearToDecibels()
        {
            var settings = new PresentationAudioSettings(
                master: 0.5f,
                music: 0.25f,
                sfx: 0.75f,
                ui: 0.1f);
            settings.Save();

            var loaded = PresentationAudioSettings.Load();
            Assert.That(loaded.Master, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(loaded.Music, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(loaded.Sfx, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(loaded.Ui, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(
                loaded.GetEffectiveLinearVolume(PresentationAudioBus.Music),
                Is.EqualTo(0.125f).Within(0.0001f));

            var decibels =
                PresentationAudioSettings.LinearToDecibels(0.5f);
            Assert.That(decibels, Is.EqualTo(-6.0206f).Within(0.001f));
            Assert.That(
                PresentationAudioSettings.DecibelsToLinear(decibels),
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                PresentationAudioSettings.LinearToDecibels(0f),
                Is.EqualTo(PresentationAudioSettings.MinimumDecibels));
            Assert.That(
                PresentationAudioSettings.DecibelsToLinear(
                    PresentationAudioSettings.MinimumDecibels),
                Is.Zero);
        }

        [Test]
        public void PlaybackLimiter_EnforcesCooldownAndConcurrency()
        {
            var limiter = new AudioPlaybackLimiter();

            Assert.That(
                limiter.TryAcquire(
                    "battle_hit",
                    0d,
                    2,
                    0.5d,
                    out var rejection),
                Is.True);
            Assert.That(rejection, Is.EqualTo(AudioPlaybackRejectionReason.None));

            Assert.That(
                limiter.TryAcquire(
                    "battle_hit",
                    0.1d,
                    2,
                    0.5d,
                    out rejection),
                Is.False);
            Assert.That(
                rejection,
                Is.EqualTo(AudioPlaybackRejectionReason.Cooldown));

            Assert.That(
                limiter.TryAcquire(
                    "battle_hit",
                    0.5d,
                    2,
                    0.5d,
                    out rejection),
                Is.True);
            Assert.That(limiter.GetActiveCount("battle_hit"), Is.EqualTo(2));

            Assert.That(
                limiter.TryAcquire(
                    "battle_hit",
                    1d,
                    2,
                    0.5d,
                    out rejection),
                Is.False);
            Assert.That(
                rejection,
                Is.EqualTo(AudioPlaybackRejectionReason.Concurrency));

            limiter.Release("battle_hit");
            Assert.That(
                limiter.TryAcquire(
                    "battle_hit",
                    1d,
                    2,
                    0.5d,
                    out rejection),
                Is.True);
            Assert.That(limiter.GetActiveCount("battle_hit"), Is.EqualTo(2));
        }

        [TestCase("MainMenu", PresentationAudioCueIds.BgmMainMenu)]
        [TestCase("MainMenuUiPreview", PresentationAudioCueIds.BgmMainMenu)]
        [TestCase("RunTest", PresentationAudioCueIds.BgmRunShop)]
        [TestCase("ShopTest", PresentationAudioCueIds.BgmRunShop)]
        [TestCase("BattleTest", PresentationAudioCueIds.BgmBattleNormal)]
        [TestCase("BattleUiPreview", PresentationAudioCueIds.BgmBattleNormal)]
        public void MusicDirector_MapsSceneContexts(
            string sceneName,
            string expectedCueId)
        {
            Assert.That(
                MusicDirector.TryGetCueForScene(sceneName, out var cueId),
                Is.True);
            Assert.That(cueId, Is.EqualTo(expectedCueId));
        }

        [Test]
        public void MusicDirector_IgnoresUnknownScenes()
        {
            Assert.That(
                MusicDirector.TryGetCueForScene(
                    "UnknownScene",
                    out var cueId),
                Is.False);
            Assert.That(cueId, Is.Null);
        }

        private static PresentationAudioCueDefinition[]
            CreateCompleteDefinitions()
        {
            var definitions =
                new List<PresentationAudioCueDefinition>();
            foreach (var cueId in PresentationAudioCueIds.AllRequired)
            {
                Assert.That(
                    PresentationAudioCueIds.TryGetExpectedBus(
                        cueId,
                        out var bus),
                    Is.True,
                    cueId);
                definitions.Add(new PresentationAudioCueDefinition(
                    cueId,
                    bus,
                    loop: bus == PresentationAudioBus.Music));
            }

            return definitions.ToArray();
        }

        private static PresentationAudioCatalog CreateCatalog(
            PresentationAudioCueDefinition[] definitions)
        {
            var catalog =
                ScriptableObject.CreateInstance<PresentationAudioCatalog>();
            SetPrivateField(catalog, "cues", definitions);
            typeof(PresentationAudioCatalog)
                .GetMethod(
                    "RebuildLookup",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(catalog, null);
            return catalog;
        }

        private static void SetPrivateField(
            object target,
            string name,
            object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static PlayerPrefSnapshot CapturePreference(string key)
        {
            return new PlayerPrefSnapshot(
                PlayerPrefs.HasKey(key),
                PlayerPrefs.GetFloat(key));
        }

        private static void RestorePreference(
            string key,
            PlayerPrefSnapshot snapshot)
        {
            if (snapshot.Exists)
            {
                PlayerPrefs.SetFloat(key, snapshot.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        private readonly struct PlayerPrefSnapshot
        {
            public PlayerPrefSnapshot(bool exists, float value)
            {
                Exists = exists;
                Value = value;
            }

            public bool Exists { get; }
            public float Value { get; }
        }
    }
}
