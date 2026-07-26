using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpireChess.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace SpireChess.Editor
{
    public static class G3AudioAssetBuilder
    {
        public const string MixerPath =
            "Assets/Audio/Presentation/SpireChessAudio.mixer";
        public const string CatalogPath =
            "Assets/Resources/Presentation/PresentationAudioCatalog.asset";
        private const string MixerRepairPath =
            "Assets/Audio/Presentation/SpireChessAudio.repair.mixer";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Spire Chess/Audio/Rebuild G3 Commissioning Assets")]
        public static void Build()
        {
            EnsureDirectories();
            var mixer = LoadOrCreateMixer();
            if (HasDuplicateGeneratedGroups())
            {
                mixer = RecreateMixerCleanly();
            }

            var master = GetMasterGroup(mixer);
            var music = EnsureChildGroup(mixer, master, "Music");
            var sfx = EnsureChildGroup(mixer, master, "SFX");
            var ui = EnsureChildGroup(mixer, master, "UI");
            PruneUnexpectedGroups(mixer, master, music, sfx, ui);

            EnsureExposedVolume(
                mixer,
                master,
                PresentationAudioSettings.MasterMixerParameter);
            EnsureExposedVolume(
                mixer,
                music,
                PresentationAudioSettings.MusicMixerParameter);
            EnsureExposedVolume(
                mixer,
                sfx,
                PresentationAudioSettings.SfxMixerParameter);
            EnsureExposedVolume(
                mixer,
                ui,
                PresentationAudioSettings.UiMixerParameter);

            EditorUtility.SetDirty(mixer);
            EditorUtility.SetDirty(master);
            EditorUtility.SetDirty(music);
            EditorUtility.SetDirty(sfx);
            EditorUtility.SetDirty(ui);
            AssetDatabase.SaveAssets();

            var catalog = LoadOrCreateCatalog();
            ConfigureCatalog(catalog, mixer, music, sfx, ui);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var validation = catalog.Validate(
                PresentationAudioCatalogValidationMode.Commissioning);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "G3 commissioning audio catalog is invalid:\n" +
                    string.Join("\n", validation.Errors));
            }

            Debug.Log(
                $"[Audio] G3 commissioning assets rebuilt: " +
                $"{catalog.Cues.Count} cues, " +
                $"{validation.Warnings.Count} commissioning warnings.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        [MenuItem("Spire Chess/Audio/Validate G3 Production Audio")]
        public static void ValidateProductionStrict()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationAudioCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "G3 production audio catalog is missing at " +
                    CatalogPath);
            }

            var validation = catalog.Validate(
                PresentationAudioCatalogValidationMode.ProductionStrict);
            var errors = validation.Errors.ToList();
            errors.AddRange(GetProductionAssetContractErrors(catalog));
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "G3 production audio catalog is not ready:\n" +
                    string.Join("\n", errors));
            }

            Debug.Log(
                $"[Audio] G3 production audio validation passed: " +
                $"{catalog.Cues.Count} cues.");
        }

        public static IReadOnlyList<string>
            GetProductionAssetContractErrors(
                PresentationAudioCatalog catalog)
        {
            var errors = new List<string>();
            var productionAssetOwners =
                new Dictionary<string, string>(StringComparer.Ordinal);
            if (catalog == null)
            {
                errors.Add("G3 production audio catalog is null.");
                return errors;
            }

            foreach (var cue in catalog.Cues)
            {
                if (cue == null ||
                    cue.AssetStatus !=
                    PresentationAudioCueAssetStatus.ProductionApproved)
                {
                    continue;
                }

                for (var index = 0; index < cue.Clips.Count; index++)
                {
                    var clip = cue.Clips[index];
                    if (clip == null)
                    {
                        continue;
                    }

                    var variant = index + 1;
                    var assetPath = AssetDatabase.GetAssetPath(clip);
                    if (string.IsNullOrWhiteSpace(assetPath))
                    {
                        errors.Add(
                            $"Audio cue '{cue.Id}' variant {variant:00} " +
                            "is production-approved but is not a persistent " +
                            "Unity audio asset.");
                        continue;
                    }

                    var normalizedPath = assetPath.Replace('\\', '/');
                    var clipLabel =
                        $"Audio cue '{cue.Id}' variant {variant:00}";
                    if (productionAssetOwners.TryGetValue(
                            normalizedPath,
                            out var existingOwner))
                    {
                        errors.Add(
                            $"{clipLabel} reuses production asset " +
                            $"'{normalizedPath}' already assigned to " +
                            $"{existingOwner}.");
                    }
                    else
                    {
                        productionAssetOwners.Add(
                            normalizedPath,
                            clipLabel);
                    }

                    if (IsPlaceholderAssetPath(normalizedPath))
                    {
                        errors.Add(
                            $"Audio cue '{cue.Id}' variant {variant:00} " +
                            "is production-approved but still references a " +
                            $"Placeholder asset: {normalizedPath}.");
                        continue;
                    }

                    var expectedPath =
                        GetExpectedProductionAssetPath(
                            cue.Id,
                            variant);
                    if (!string.Equals(
                            normalizedPath,
                            expectedPath,
                            StringComparison.Ordinal))
                    {
                        errors.Add(
                            $"Audio cue '{cue.Id}' variant {variant:00} " +
                            $"must use '{expectedPath}', not " +
                            $"'{normalizedPath}'.");
                        continue;
                    }

                    ValidateProductionImporter(
                        cue,
                        clip,
                        normalizedPath,
                        variant,
                        errors);
                }
            }

            return errors;
        }

        public static bool IsPlaceholderAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            var normalizedPath = assetPath.Replace('\\', '/');
            return normalizedPath.IndexOf(
                "/Placeholder/",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string GetExpectedProductionAssetPath(
            string cueId,
            int variant)
        {
            if (!PresentationAudioCueIds.TryGetRequiredVariantCount(
                    cueId,
                    out var variantCount))
            {
                throw new ArgumentException(
                    "Unknown required G3 audio cue ID.",
                    nameof(cueId));
            }

            if (variant < 1 || variant > variantCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(variant),
                    variant,
                    $"Cue '{cueId}' requires {variantCount} variants.");
            }

            if (PresentationAudioCueIds.IsRequiredMusic(cueId))
            {
                return "Assets/Audio/Presentation/Music/" +
                    cueId +
                    "_v01.ogg";
            }

            return "Assets/Audio/Presentation/SFX/" +
                GetSfxDomainDirectory(cueId) +
                "/sfx_" +
                cueId +
                "_" +
                variant.ToString("00") +
                ".wav";
        }

        public static void ValidateProductionStrictFromCommandLine()
        {
            try
            {
                ValidateProductionStrict();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        private static void ValidateProductionImporter(
            PresentationAudioCueDefinition cue,
            AudioClip clip,
            string assetPath,
            int variant,
            ICollection<string> errors)
        {
            var label =
                $"Audio cue '{cue.Id}' variant {variant:00}";
            var isMusic =
                PresentationAudioCueIds.IsRequiredMusic(cue.Id);
            var expectedChannels =
                isMusic ||
                cue.Id == PresentationAudioCueIds.ShopRefresh ||
                cue.Id == PresentationAudioCueIds.ShopDiscoverOpen
                    ? 2
                    : 1;
            if (clip.frequency != 48000)
            {
                errors.Add(
                    $"{label} must import at 48000 Hz, not " +
                    $"{clip.frequency} Hz.");
            }

            if (clip.channels != expectedChannels)
            {
                errors.Add(
                    $"{label} must contain {expectedChannels} channel(s), " +
                    $"not {clip.channels}.");
            }

            var importer =
                AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                errors.Add($"{label} has no AudioImporter.");
                return;
            }

            var settings = importer.defaultSampleSettings;
            if (importer.forceToMono)
            {
                errors.Add(
                    $"{label} must not rely on force-to-mono conversion.");
            }

            if (settings.sampleRateSetting !=
                AudioSampleRateSetting.PreserveSampleRate)
            {
                errors.Add(
                    $"{label} must preserve its 48000 Hz source rate.");
            }

            if (isMusic)
            {
                if (settings.loadType != AudioClipLoadType.Streaming ||
                    settings.compressionFormat !=
                    AudioCompressionFormat.Vorbis ||
                    settings.preloadAudioData ||
                    !importer.loadInBackground)
                {
                    errors.Add(
                        $"{label} must use Streaming Vorbis, background " +
                        "loading, and no preload.");
                }

                return;
            }

            var validSfxCompression =
                settings.compressionFormat ==
                AudioCompressionFormat.PCM ||
                settings.compressionFormat ==
                AudioCompressionFormat.ADPCM;
            if (settings.loadType !=
                AudioClipLoadType.DecompressOnLoad ||
                !validSfxCompression ||
                !settings.preloadAudioData ||
                importer.loadInBackground)
            {
                errors.Add(
                    $"{label} must use preloaded Decompress On Load " +
                    "PCM/ADPCM without background loading.");
            }

            if (!TryReadWaveFormat(
                    assetPath,
                    out var waveFormat,
                    out var waveError))
            {
                errors.Add($"{label} has an invalid WAV source: {waveError}");
                return;
            }

            if (waveFormat.AudioFormat != 1 ||
                waveFormat.SampleRate != 48000 ||
                waveFormat.BitsPerSample != 24 ||
                waveFormat.Channels != expectedChannels)
            {
                errors.Add(
                    $"{label} source WAV must be 48 kHz / 24-bit PCM / " +
                    $"{expectedChannels} channel(s); found format " +
                    $"{waveFormat.AudioFormat}, {waveFormat.SampleRate} Hz, " +
                    $"{waveFormat.BitsPerSample}-bit, " +
                    $"{waveFormat.Channels} channel(s).");
            }
        }

        private static string GetSfxDomainDirectory(string cueId)
        {
            if (cueId.StartsWith("ui_", StringComparison.Ordinal))
            {
                return "UI";
            }

            if (cueId.StartsWith("shop_", StringComparison.Ordinal))
            {
                return "Shop";
            }

            if (cueId.StartsWith("battle_", StringComparison.Ordinal))
            {
                return "Battle";
            }

            if (cueId.StartsWith("run_", StringComparison.Ordinal))
            {
                return "Run";
            }

            throw new ArgumentException(
                "Required SFX cue has no production directory mapping.",
                nameof(cueId));
        }

        private static bool TryReadWaveFormat(
            string assetPath,
            out WaveFormatInfo result,
            out string error)
        {
            result = default(WaveFormatInfo);
            error = null;
            try
            {
                var projectRoot =
                    Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrWhiteSpace(projectRoot))
                {
                    error = "Unity project root could not be resolved.";
                    return false;
                }

                var fullPath = Path.GetFullPath(
                    Path.Combine(projectRoot, assetPath));
                using (var stream = File.OpenRead(fullPath))
                using (var reader = new BinaryReader(stream))
                {
                    if (ReadFourCc(reader) != "RIFF")
                    {
                        error = "missing RIFF header.";
                        return false;
                    }

                    reader.ReadUInt32();
                    if (ReadFourCc(reader) != "WAVE")
                    {
                        error = "missing WAVE signature.";
                        return false;
                    }

                    while (stream.Position + 8 <= stream.Length)
                    {
                        var chunkId = ReadFourCc(reader);
                        var chunkSize = reader.ReadUInt32();
                        var chunkEnd =
                            stream.Position +
                            chunkSize +
                            (chunkSize & 1u);
                        if (chunkEnd > stream.Length)
                        {
                            error = $"truncated '{chunkId}' chunk.";
                            return false;
                        }

                        if (chunkId == "fmt ")
                        {
                            if (chunkSize < 16)
                            {
                                error = "fmt chunk is shorter than 16 bytes.";
                                return false;
                            }

                            var audioFormat = reader.ReadUInt16();
                            var channels = reader.ReadUInt16();
                            var sampleRate = reader.ReadUInt32();
                            reader.ReadUInt32();
                            reader.ReadUInt16();
                            var bitsPerSample = reader.ReadUInt16();
                            result = new WaveFormatInfo(
                                audioFormat,
                                channels,
                                checked((int)sampleRate),
                                bitsPerSample);
                            return true;
                        }

                        stream.Position = chunkEnd;
                    }
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            error = "fmt chunk was not found.";
            return false;
        }

        private static string ReadFourCc(BinaryReader reader)
        {
            return new string(reader.ReadChars(4));
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(
                Path.Combine(Application.dataPath, "Audio/Presentation"));
            Directory.CreateDirectory(
                Path.Combine(Application.dataPath, "Resources/Presentation"));
            AssetDatabase.Refresh();
        }

        private static AudioMixer LoadOrCreateMixer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null)
            {
                return existing;
            }

            return CreateMixerAtPath(MixerPath);
        }

        private static AudioMixer CreateMixerAtPath(string path)
        {
            var controllerType = GetControllerType();
            var create = controllerType.GetMethod(
                "CreateMixerControllerAtPath",
                StaticFlags,
                null,
                new[] { typeof(string) },
                null);
            if (create == null)
            {
                throw new MissingMethodException(
                    controllerType.FullName,
                    "CreateMixerControllerAtPath");
            }

            var controller = create.Invoke(null, new object[] { path });
            var mixer = controller as AudioMixer;
            if (mixer == null)
            {
                throw new InvalidOperationException(
                    "Unity did not create an AudioMixer at " + path);
            }

            AssetDatabase.SaveAssets();
            return mixer;
        }

        private static bool HasDuplicateGeneratedGroups()
        {
            var groups = AssetDatabase.LoadAllAssetsAtPath(MixerPath)
                .OfType<AudioMixerGroup>()
                .ToArray();
            return new[] { "Master", "Music", "SFX", "UI" }
                .Any(name => groups.Count(group =>
                    string.Equals(
                        group.name,
                        name,
                        StringComparison.Ordinal)) > 1);
        }

        private static AudioMixer RecreateMixerCleanly()
        {
            AssetDatabase.DeleteAsset(MixerRepairPath);
            CreateMixerAtPath(MixerRepairPath);
            AssetDatabase.SaveAssets();

            if (!AssetDatabase.DeleteAsset(MixerPath))
            {
                throw new InvalidOperationException(
                    "Could not remove the invalid generated AudioMixer.");
            }

            var moveError = AssetDatabase.MoveAsset(
                MixerRepairPath,
                MixerPath);
            if (!string.IsNullOrEmpty(moveError))
            {
                throw new InvalidOperationException(
                    "Could not install the repaired AudioMixer: " + moveError);
            }

            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                throw new MissingReferenceException(
                    "The repaired AudioMixer could not be loaded.");
            }

            return mixer;
        }

        private static AudioMixerGroup GetMasterGroup(AudioMixer mixer)
        {
            var property = mixer.GetType().GetProperty(
                "masterGroup",
                InstanceFlags);
            var master = property?.GetValue(mixer) as AudioMixerGroup;
            if (master == null)
            {
                throw new MissingReferenceException(
                    "The generated AudioMixer has no master group.");
            }

            return master;
        }

        private static AudioMixerGroup EnsureChildGroup(
            AudioMixer mixer,
            AudioMixerGroup master,
            string groupName)
        {
            var childrenProperty = master.GetType().GetProperty(
                "children",
                InstanceFlags);
            var children = childrenProperty?.GetValue(master) as Array;
            var attached = children?
                .Cast<AudioMixerGroup>()
                .FirstOrDefault(group =>
                    string.Equals(
                        group.name,
                        groupName,
                        StringComparison.Ordinal));
            if (attached != null)
            {
                return attached;
            }

            var existing = mixer.FindMatchingGroups(groupName)
                .FirstOrDefault(group =>
                    string.Equals(
                        group.name,
                        groupName,
                        StringComparison.Ordinal));
            if (existing != null)
            {
                return existing;
            }

            var controllerType = mixer.GetType();
            var create = controllerType.GetMethod(
                "CreateNewGroup",
                InstanceFlags,
                null,
                new[] { typeof(string), typeof(bool) },
                null);
            if (create == null)
            {
                throw new MissingMethodException(
                    controllerType.FullName,
                    "CreateNewGroup");
            }

            var group = create.Invoke(
                mixer,
                new object[] { groupName, false }) as AudioMixerGroup;
            if (group == null)
            {
                throw new InvalidOperationException(
                    "Unity could not create AudioMixer group " + groupName);
            }

            var addChild = controllerType
                .GetMethods(InstanceFlags)
                .SingleOrDefault(method =>
                    method.Name == "AddChildToParent" &&
                    method.GetParameters().Length == 2);
            if (addChild == null)
            {
                throw new MissingMethodException(
                    controllerType.FullName,
                    "AddChildToParent");
            }

            addChild.Invoke(mixer, new object[] { group, master });
            return group;
        }

        private static void PruneUnexpectedGroups(
            AudioMixer mixer,
            params AudioMixerGroup[] retainedGroups)
        {
            var controllerType = mixer.GetType();
            var getAll = controllerType.GetMethod(
                "GetAllAudioGroupsSlow",
                InstanceFlags);
            var delete = controllerType.GetMethod(
                "DeleteGroups",
                InstanceFlags);
            var values =
                getAll?.Invoke(mixer, null) as System.Collections.IEnumerable;
            if (getAll == null || delete == null || values == null)
            {
                throw new MissingMethodException(
                    controllerType.FullName,
                    "GetAllAudioGroupsSlow/DeleteGroups");
            }

            var retainedIds = retainedGroups
                .Where(group => group != null)
                .Select(group => group.GetInstanceID())
                .ToHashSet();
            var unexpected = values
                .Cast<UnityEngine.Object>()
                .Where(group => !retainedIds.Contains(group.GetInstanceID()))
                .ToArray();
            if (unexpected.Length == 0)
            {
                return;
            }

            var groupType = retainedGroups
                .First(group => group != null)
                .GetType();
            var deleteArray = Array.CreateInstance(
                groupType,
                unexpected.Length);
            for (var index = 0; index < unexpected.Length; index++)
            {
                deleteArray.SetValue(unexpected[index], index);
            }

            delete.Invoke(mixer, new object[] { deleteArray });
        }

        private static void EnsureExposedVolume(
            AudioMixer mixer,
            AudioMixerGroup group,
            string parameterName)
        {
            var controllerType = mixer.GetType();
            var exposedProperty = controllerType.GetProperty(
                "exposedParameters",
                InstanceFlags);
            var exposed = exposedProperty?.GetValue(mixer) as Array;
            if (exposedProperty == null || exposed == null)
            {
                throw new MissingMemberException(
                    controllerType.FullName,
                    "exposedParameters");
            }

            var elementType = exposed.GetType().GetElementType();
            var nameField = elementType?.GetField("name", InstanceFlags);
            var guidField = elementType?.GetField("guid", InstanceFlags);
            if (elementType == null || nameField == null || guidField == null)
            {
                throw new MissingMemberException(
                    "Unity exposed audio parameter metadata is unavailable.");
            }

            foreach (var entry in exposed)
            {
                if (string.Equals(
                        nameField.GetValue(entry) as string,
                        parameterName,
                        StringComparison.Ordinal))
                {
                    return;
                }
            }

            var groupType = group.GetType();
            var getVolumeGuid = groupType.GetMethod(
                "GetGUIDForVolume",
                InstanceFlags,
                null,
                Type.EmptyTypes,
                null);
            var volumeGuid = getVolumeGuid?.Invoke(group, null);
            if (volumeGuid == null)
            {
                throw new MissingMethodException(
                    groupType.FullName,
                    "GetGUIDForVolume");
            }

            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var parameterPathType = editorAssembly.GetType(
                "UnityEditor.Audio.AudioParameterPath",
                true);
            var groupParameterPathType = editorAssembly.GetType(
                "UnityEditor.Audio.AudioGroupParameterPath",
                true);
            var parameterPath = Activator.CreateInstance(
                groupParameterPathType,
                InstanceFlags,
                null,
                new[] { (object)group, volumeGuid },
                null);

            var addExposed = controllerType.GetMethod(
                "AddExposedParameter",
                InstanceFlags,
                null,
                new[] { parameterPathType },
                null);
            if (addExposed == null || parameterPath == null)
            {
                throw new MissingMethodException(
                    controllerType.FullName,
                    "AddExposedParameter");
            }

            addExposed.Invoke(mixer, new[] { parameterPath });
            exposed = exposedProperty.GetValue(mixer) as Array;
            if (exposed == null)
            {
                throw new InvalidOperationException(
                    "Unity did not expose the requested mixer parameter.");
            }

            var targetGuid = volumeGuid.ToString();
            var renamed = false;
            for (var index = 0; index < exposed.Length; index++)
            {
                var entry = exposed.GetValue(index);
                var entryGuid = guidField.GetValue(entry);
                if (!string.Equals(
                        entryGuid?.ToString(),
                        targetGuid,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                nameField.SetValue(entry, parameterName);
                exposed.SetValue(entry, index);
                renamed = true;
                break;
            }

            if (!renamed)
            {
                throw new InvalidOperationException(
                    "Unity exposed a volume parameter but its GUID could not " +
                    "be resolved for " + group.name);
            }

            exposedProperty.SetValue(mixer, exposed);
        }

        private static PresentationAudioCatalog LoadOrCreateCatalog()
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationAudioCatalog>(
                    CatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<PresentationAudioCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static void ConfigureCatalog(
            PresentationAudioCatalog catalog,
            AudioMixer mixer,
            AudioMixerGroup music,
            AudioMixerGroup sfx,
            AudioMixerGroup ui)
        {
            var assetsByCueId = catalog.Cues
                .Where(cue =>
                    cue != null &&
                    !string.IsNullOrWhiteSpace(cue.Id))
                .GroupBy(cue => cue.Id, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var existing = group.First();
                        return new CueAssetSnapshot(
                            existing.Clips.ToArray(),
                            existing.AssetStatus);
                    },
                    StringComparer.Ordinal);
            var serialized = new SerializedObject(catalog);
            serialized.Update();
            SetReference(serialized, "audioMixer", mixer);
            SetReference(serialized, "musicOutput", music);
            SetReference(serialized, "sfxOutput", sfx);
            SetReference(serialized, "uiOutput", ui);

            var cues = serialized.FindProperty("cues");
            cues.arraySize = PresentationAudioCueIds.AllRequired.Count;
            for (var index = 0;
                 index < PresentationAudioCueIds.AllRequired.Count;
                 index++)
            {
                var cueId = PresentationAudioCueIds.AllRequired[index];
                if (!PresentationAudioCueIds.TryGetExpectedBus(
                        cueId,
                        out var bus))
                {
                    throw new InvalidOperationException(
                        "No expected audio bus for " + cueId);
                }

                assetsByCueId.TryGetValue(
                    cueId,
                    out var preservedAssets);
                ConfigureCue(
                    cues.GetArrayElementAtIndex(index),
                    cueId,
                    bus,
                    preservedAssets.Clips,
                    preservedAssets.AssetStatus);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            ValidatePreservedCueAssets(catalog, assetsByCueId);
            EditorUtility.SetDirty(catalog);
        }

        private static void ValidatePreservedCueAssets(
            PresentationAudioCatalog catalog,
            IReadOnlyDictionary<string, CueAssetSnapshot> assetsByCueId)
        {
            foreach (var pair in assetsByCueId)
            {
                if (!PresentationAudioCueIds.TryGetRequiredVariantCount(
                        pair.Key,
                        out _))
                {
                    continue;
                }

                var rebuiltCue = catalog.Cues.SingleOrDefault(
                    cue => cue != null &&
                        string.Equals(
                            cue.Id,
                            pair.Key,
                            StringComparison.Ordinal));
                if (rebuiltCue == null)
                {
                    throw new InvalidOperationException(
                        $"Catalog rebuild lost cue '{pair.Key}'.");
                }

                var snapshot = pair.Value;
                if (rebuiltCue.AssetStatus != snapshot.AssetStatus)
                {
                    throw new InvalidOperationException(
                        $"Catalog rebuild changed asset status for " +
                        $"cue '{pair.Key}'.");
                }

                var preservedClips =
                    snapshot.Clips ?? Array.Empty<AudioClip>();
                if (rebuiltCue.Clips.Count != preservedClips.Length)
                {
                    throw new InvalidOperationException(
                        $"Catalog rebuild changed clip count for " +
                        $"cue '{pair.Key}'.");
                }

                for (var index = 0;
                     index < preservedClips.Length;
                     index++)
                {
                    if (rebuiltCue.Clips[index] != preservedClips[index])
                    {
                        throw new InvalidOperationException(
                            $"Catalog rebuild changed cue '{pair.Key}' " +
                            $"variant {index + 1:00}.");
                    }
                }
            }
        }

        private static void ConfigureCue(
            SerializedProperty cue,
            string cueId,
            PresentationAudioBus bus,
            AudioClip[] preservedClips,
            PresentationAudioCueAssetStatus preservedAssetStatus)
        {
            cue.FindPropertyRelative("id").stringValue = cueId;
            cue.FindPropertyRelative("bus").enumValueIndex = (int)bus;
            var clips = cue.FindPropertyRelative("clips");
            preservedClips = preservedClips ?? Array.Empty<AudioClip>();
            clips.arraySize = preservedClips.Length;
            for (var index = 0; index < preservedClips.Length; index++)
            {
                clips.GetArrayElementAtIndex(index).objectReferenceValue =
                    preservedClips[index];
            }
            cue.FindPropertyRelative("assetStatus").enumValueIndex =
                (int)preservedAssetStatus;
            cue.FindPropertyRelative("volume").floatValue =
                bus == PresentationAudioBus.Music ? 0.72f :
                bus == PresentationAudioBus.Ui ? 0.70f : 0.82f;

            var isMusic = bus == PresentationAudioBus.Music;
            cue.FindPropertyRelative("minPitch").floatValue =
                isMusic ? 1f : 0.97f;
            cue.FindPropertyRelative("maxPitch").floatValue =
                isMusic ? 1f : 1.03f;
            cue.FindPropertyRelative("concurrencyLimit").intValue =
                isMusic ? 1 : GetConcurrencyLimit(cueId);
            cue.FindPropertyRelative("cooldownSeconds").floatValue =
                isMusic ? 0f : GetCooldownSeconds(cueId);
            cue.FindPropertyRelative("loop").boolValue = isMusic;
        }

        private static int GetConcurrencyLimit(string cueId)
        {
            if (cueId == PresentationAudioCueIds.BattleHit ||
                cueId == PresentationAudioCueIds.BattleDeath ||
                cueId == PresentationAudioCueIds.BattleTokenDeath ||
                cueId == PresentationAudioCueIds.BattleSummon)
            {
                return 6;
            }

            if (cueId == PresentationAudioCueIds.BattleVictory ||
                cueId == PresentationAudioCueIds.BattleDefeat ||
                cueId == PresentationAudioCueIds.ShopTriple)
            {
                return 1;
            }

            return 4;
        }

        private static float GetCooldownSeconds(string cueId)
        {
            if (cueId == PresentationAudioCueIds.BattleHit)
            {
                return 0.025f;
            }

            if (cueId == PresentationAudioCueIds.BattleVictory ||
                cueId == PresentationAudioCueIds.BattleDefeat ||
                cueId == PresentationAudioCueIds.ShopTriple)
            {
                return 0.2f;
            }

            return 0.05f;
        }

        private static Type GetControllerType()
        {
            return typeof(UnityEditor.Editor).Assembly.GetType(
                "UnityEditor.Audio.AudioMixerController",
                true);
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingMemberException(
                    serialized.targetObject.GetType().FullName,
                    propertyName);
            }

            property.objectReferenceValue = value;
        }

        private readonly struct CueAssetSnapshot
        {
            public CueAssetSnapshot(
                AudioClip[] clips,
                PresentationAudioCueAssetStatus assetStatus)
            {
                Clips = clips;
                AssetStatus = assetStatus;
            }

            public AudioClip[] Clips { get; }
            public PresentationAudioCueAssetStatus AssetStatus { get; }
        }

        private readonly struct WaveFormatInfo
        {
            public WaveFormatInfo(
                ushort audioFormat,
                ushort channels,
                int sampleRate,
                ushort bitsPerSample)
            {
                AudioFormat = audioFormat;
                Channels = channels;
                SampleRate = sampleRate;
                BitsPerSample = bitsPerSample;
            }

            public ushort AudioFormat { get; }
            public ushort Channels { get; }
            public int SampleRate { get; }
            public ushort BitsPerSample { get; }
        }
    }
}
