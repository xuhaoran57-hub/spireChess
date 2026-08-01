using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpireChess.Config;
using SpireChess.UI;
using SpireChess.Utils;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Editor
{
    public static class LightStorybookRuntimePromotionBuilder
    {
        public const string RuntimeCatalogPath =
            "Assets/Configs/Presentation/PresentationSpriteCatalog.asset";

        public const string RuntimeArtRoot =
            "Assets/Art/Presentation/Runtime/LightStorybookV033";

        public const string PromotionManifestRelativePath =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "runtime-promotion-v0.3.3/promotion-manifest.json";

        private const string CalibrationPrefix =
            "Assets/Art/Presentation/Calibration/";

        private const string FormalCalibrationPrefix =
            CalibrationPrefix + "LightStorybookFormalCatalogV032/";

        private const string ProductionCalibrationPrefix =
            CalibrationPrefix +
            "LightStorybookProductionV033Batch";

        [MenuItem(
            "Spire Chess/Release/Promote Phase 9C v0.3.3 to Runtime")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static RuntimePromotionPlan CreatePlan()
        {
            var candidate = LoadCatalog(
                LightStorybookProductionBatch6Builder.CatalogPath);
            var entries = ReadCatalogEntries(candidate);
            if (entries.Count != 86 ||
                entries.Select(value => value.ArtId)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != entries.Count)
            {
                throw new InvalidOperationException(
                    "The promotion candidate must contain 86 unique " +
                    "artwork entries.");
            }

            var planEntries = entries
                .Where(value => value.SourcePath.StartsWith(
                    CalibrationPrefix,
                    StringComparison.Ordinal))
                .Select(value => new RuntimePromotionPlanEntry
                {
                    ArtId = value.ArtId,
                    SourcePath = value.SourcePath,
                    RuntimePath = GetRuntimeAssetPath(value.ArtId),
                    Sha256 = ComputeSha256(
                        ResolveProjectAssetPath(value.SourcePath))
                })
                .OrderBy(value => value.ArtId, StringComparer.Ordinal)
                .ToArray();

            if (planEntries.Length != 66 ||
                planEntries.Count(value => value.SourcePath.StartsWith(
                    FormalCalibrationPrefix,
                    StringComparison.Ordinal)) != 15 ||
                planEntries.Count(value => value.SourcePath.StartsWith(
                    ProductionCalibrationPrefix,
                    StringComparison.Ordinal)) != 51)
            {
                throw new InvalidOperationException(
                    "The candidate must contain 15 formal-baseline and " +
                    "51 production Calibration artworks.");
            }
            if (planEntries.Select(value => value.RuntimePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() != planEntries.Length)
            {
                throw new InvalidOperationException(
                    "Promotion target paths are not unique.");
            }

            return new RuntimePromotionPlan
            {
                CandidateCatalogPath =
                    LightStorybookProductionBatch6Builder.CatalogPath,
                RuntimeCatalogPath = RuntimeCatalogPath,
                Entries = planEntries
            };
        }

        public static string GetRuntimeAssetPath(string artId)
        {
            if (string.IsNullOrWhiteSpace(artId) ||
                artId == "." ||
                artId == ".." ||
                artId.Any(value =>
                    !char.IsLetterOrDigit(value) &&
                    value != '_' &&
                    value != '-' &&
                    value != '.'))
            {
                throw new ArgumentException(
                    "ArtId cannot be mapped to a safe Runtime path.",
                    nameof(artId));
            }
            return RuntimeArtRoot + "/" + artId + ".png";
        }

        public static bool IsPromoted()
        {
            return ValidatePromotedState().Length == 0;
        }

        public static string[] ValidatePromotedState()
        {
            try
            {
                var contract = LoadContract();
                ValidateApprovalAndPolicy(contract);
                ValidatePostPromotion(contract, CreatePlan());
                return Array.Empty<string>();
            }
            catch (Exception exception)
            {
                return new[] { exception.Message };
            }
        }

        private static void Build()
        {
            var contract = LoadContract();
            ValidateApprovalAndPolicy(contract);
            var plan = CreatePlan();

            var promotedFailures = ValidatePromotedState();
            if (promotedFailures.Length == 0)
            {
                WritePromotionManifest(contract, plan);
                Debug.Log(
                    "[LightStorybook] Phase 9C v0.3.3 Runtime is " +
                    "already promoted; deterministic manifest refreshed.");
                return;
            }

            LightStorybookRuntimePromotionGate.ValidateFromMenu();

            var runtimeGuidBefore =
                AssetDatabase.AssetPathToGUID(RuntimeCatalogPath);
            if (runtimeGuidBefore !=
                contract.RuntimeBeforePromotion.CatalogGuid)
            {
                throw new InvalidOperationException(
                    "Runtime catalog GUID drifted before promotion.");
            }

            var runtimeRootExisted =
                AssetDatabase.IsValidFolder(RuntimeArtRoot);
            var createdRuntimeAssets = new List<string>();
            try
            {
                CopyAndConfigureRuntimeArt(
                    plan,
                    contract.TargetPolicy,
                    createdRuntimeAssets);
                PromoteCatalogTransactionally(
                    plan,
                    contract,
                    runtimeGuidBefore);
            }
            catch
            {
                RollBackCreatedRuntimeArt(
                    createdRuntimeAssets,
                    runtimeRootExisted);
                throw;
            }
            WritePromotionManifest(contract, plan);

            Debug.Log(
                "[LightStorybook] Phase 9C v0.3.3 promoted to Runtime. " +
                "Catalog GUID preserved; copied " +
                plan.Entries.Length + " Calibration artworks.");
        }

        private static void CopyAndConfigureRuntimeArt(
            RuntimePromotionPlan plan,
            RuntimePromotionTargetPolicy policy,
            ICollection<string> createdRuntimeAssets)
        {
            EnsureAssetFolder(RuntimeArtRoot);
            foreach (var entry in plan.Entries)
            {
                var source = ResolveProjectAssetPath(entry.SourcePath);
                var destination =
                    ResolveProjectAssetPath(entry.RuntimePath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination));
                var destinationExisted = File.Exists(destination);
                if (destinationExisted &&
                    !string.Equals(
                        ComputeSha256(destination),
                        entry.Sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "A conflicting Runtime asset already exists: " +
                        entry.RuntimePath);
                }
                if (!destinationExisted)
                {
                    File.Copy(source, destination, false);
                    createdRuntimeAssets.Add(entry.RuntimePath);
                }

                AssetDatabase.ImportAsset(
                    entry.RuntimePath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                ConfigureRuntimeImporter(entry.RuntimePath, policy);
                if (!string.Equals(
                        ComputeSha256(destination),
                        entry.Sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Runtime copy hash mismatch: " + entry.ArtId);
                }
            }
        }

        private static void RollBackCreatedRuntimeArt(
            IEnumerable<string> createdRuntimeAssets,
            bool runtimeRootExisted)
        {
            foreach (var assetPath in createdRuntimeAssets.Reverse())
            {
                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    var absolutePath =
                        ResolveProjectAssetPath(assetPath);
                    if (File.Exists(absolutePath))
                    {
                        File.Delete(absolutePath);
                    }
                    var metaPath = absolutePath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                }
            }
            if (!runtimeRootExisted &&
                AssetDatabase.IsValidFolder(RuntimeArtRoot))
            {
                AssetDatabase.DeleteAsset(RuntimeArtRoot);
            }
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureRuntimeImporter(
            string assetPath,
            RuntimePromotionTargetPolicy policy)
        {
            var importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "TextureImporter is unavailable: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = policy.Mipmaps;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.isReadable = policy.Readable;
            importer.textureCompression =
                TextureImporterCompression.Compressed;
            importer.maxTextureSize = policy.MaxTextureSize;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;

            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);

            var standalone =
                importer.GetPlatformTextureSettings("Standalone");
            standalone.name = "Standalone";
            standalone.overridden = true;
            standalone.maxTextureSize = policy.MaxTextureSize;
            standalone.format = TextureImporterFormat.DXT1;
            standalone.compressionQuality = policy.CompressionQuality;
            standalone.crunchedCompression = false;
            importer.SetPlatformTextureSettings(standalone);
            importer.SaveAndReimport();
        }

        private static void PromoteCatalogTransactionally(
            RuntimePromotionPlan plan,
            RuntimePromotionContract contract,
            string runtimeGuidBefore)
        {
            var runtimeAbsolute =
                ResolveProjectAssetPath(RuntimeCatalogPath);
            var backup = File.ReadAllBytes(runtimeAbsolute);
            var catalogChanged = false;
            try
            {
                var candidate = LoadCatalog(
                    LightStorybookProductionBatch6Builder.CatalogPath);
                var runtime = LoadCatalog(RuntimeCatalogPath);
                EditorUtility.CopySerialized(candidate, runtime);
                catalogChanged = true;
                runtime.name =
                    Path.GetFileNameWithoutExtension(RuntimeCatalogPath);

                var runtimeSprites = plan.Entries.ToDictionary(
                    value => value.ArtId,
                    value =>
                        AssetDatabase.LoadAssetAtPath<Sprite>(
                            value.RuntimePath),
                    StringComparer.Ordinal);
                if (runtimeSprites.Values.Any(value => value == null))
                {
                    throw new InvalidOperationException(
                        "A promoted Runtime sprite failed to import.");
                }

                var serialized = new SerializedObject(runtime);
                var artworks = RequireArtworks(serialized);
                for (var index = 0;
                     index < artworks.arraySize;
                     index++)
                {
                    var entry =
                        artworks.GetArrayElementAtIndex(index);
                    var artId = entry
                        .FindPropertyRelative("id")
                        .stringValue;
                    if (runtimeSprites.TryGetValue(
                            artId,
                            out var sprite))
                    {
                        entry.FindPropertyRelative("sprite")
                            .objectReferenceValue = sprite;
                    }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(runtime);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    RuntimeCatalogPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);

                if (AssetDatabase.AssetPathToGUID(RuntimeCatalogPath) !=
                    runtimeGuidBefore)
                {
                    throw new InvalidOperationException(
                        "Runtime catalog GUID changed during promotion.");
                }
                ValidatePostPromotion(contract, plan);
            }
            catch
            {
                if (catalogChanged)
                {
                    File.WriteAllBytes(runtimeAbsolute, backup);
                    AssetDatabase.ImportAsset(
                        RuntimeCatalogPath,
                        ImportAssetOptions.ForceSynchronousImport |
                        ImportAssetOptions.ForceUpdate);
                }
                throw;
            }
        }

        private static void ValidatePostPromotion(
            RuntimePromotionContract contract,
            RuntimePromotionPlan plan)
        {
            if (AssetDatabase.AssetPathToGUID(
                    LightStorybookProductionBatch6Builder.CatalogPath) !=
                    contract.Candidate.CatalogGuid ||
                AssetDatabase.AssetPathToGUID(RuntimeCatalogPath) !=
                    contract.RuntimeBeforePromotion.CatalogGuid)
            {
                throw new InvalidOperationException(
                    "Candidate or Runtime catalog GUID drifted.");
            }

            var candidate = LoadCatalog(
                LightStorybookProductionBatch6Builder.CatalogPath);
            var runtime = LoadCatalog(RuntimeCatalogPath);
            var candidateEntries = ReadCatalogEntries(candidate);
            var runtimeEntries = ReadCatalogEntries(runtime);
            if (candidateEntries.Count !=
                    contract.Candidate.CatalogEntryCount ||
                runtimeEntries.Count !=
                    contract.Candidate.CatalogEntryCount)
            {
                throw new InvalidOperationException(
                    "Promoted catalog must contain exactly 86 entries.");
            }

            var runtimeById = runtimeEntries.ToDictionary(
                value => value.ArtId,
                StringComparer.Ordinal);
            var planById = plan.Entries.ToDictionary(
                value => value.ArtId,
                StringComparer.Ordinal);
            var approvedRefreshArtIds =
                new HashSet<string>(StringComparer.Ordinal);
            if (LightStorybookArtRefreshV034Builder.IsPromoted())
            {
                approvedRefreshArtIds.UnionWith(
                    LightStorybookArtRefreshV034Builder.RefreshArtIds());
            }
            foreach (var candidateEntry in candidateEntries)
            {
                if (!runtimeById.TryGetValue(
                        candidateEntry.ArtId,
                        out var runtimeEntry))
                {
                    throw new InvalidOperationException(
                        "Runtime catalog is missing candidate artwork: " +
                        candidateEntry.ArtId);
                }
                var expectedPath = planById.TryGetValue(
                    candidateEntry.ArtId,
                    out var planned)
                        ? planned.RuntimePath
                        : candidateEntry.SourcePath;
                var expectedFocalPointY =
                    approvedRefreshArtIds.Contains(
                        candidateEntry.ArtId)
                        ? 0.5f
                        : candidateEntry.FocalPointY;
                if (runtimeEntry.SourcePath != expectedPath ||
                    Math.Abs(
                        runtimeEntry.FocalPointY -
                        expectedFocalPointY) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Runtime catalog entry drifted: " +
                        candidateEntry.ArtId);
                }
                if (runtimeEntry.SourcePath.StartsWith(
                        CalibrationPrefix,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Runtime catalog still references Calibration: " +
                        candidateEntry.ArtId);
                }
            }

            ValidateNoCalibrationReferences(runtime);
            ValidateConfiguredArtworks(runtimeById.Keys, contract);
            ValidateProductionHashes(runtimeById, contract);
            foreach (var entry in plan.Entries)
            {
                ValidateRuntimeImporter(
                    entry.RuntimePath,
                    contract.TargetPolicy);
            }
        }

        private static void ValidateNoCalibrationReferences(
            PresentationSpriteCatalog runtime)
        {
            var serialized = new SerializedObject(runtime);
            var property = serialized.GetIterator();
            var enterChildren = true;
            while (property.Next(enterChildren))
            {
                enterChildren = true;
                if (property.propertyType !=
                    SerializedPropertyType.ObjectReference ||
                    property.objectReferenceValue == null)
                {
                    continue;
                }
                var path = AssetDatabase.GetAssetPath(
                    property.objectReferenceValue);
                if (path.StartsWith(
                        CalibrationPrefix,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Runtime catalog contains a Calibration " +
                        "reference at " + property.propertyPath + ".");
                }
            }
        }

        private static void ValidateConfiguredArtworks(
            IEnumerable<string> runtimeArtIds,
            RuntimePromotionContract contract)
        {
            var configs =
                new ConfigService(new NewtonsoftJsonSerializer());
            configs.LoadFromResources().ThrowIfInvalid();
            var artIds = configs.MinionsById.Values
                .Select(value => value.ArtId)
                .Concat(
                    configs.SpellsById.Values.Select(
                        value => value.ArtId))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (artIds.Length !=
                contract.Candidate.ConfiguredArtworkCount)
            {
                throw new InvalidOperationException(
                    "Configured artwork count drifted.");
            }
            var runtimeIds = new HashSet<string>(
                runtimeArtIds,
                StringComparer.Ordinal);
            foreach (var artId in artIds)
            {
                if (!runtimeIds.Contains(artId))
                {
                    throw new InvalidOperationException(
                        "Runtime catalog is missing configured artwork: " +
                        artId);
                }
            }
        }

        private static void ValidateProductionHashes(
            IReadOnlyDictionary<string, CatalogEntrySnapshot> runtimeById,
            RuntimePromotionContract contract)
        {
            var manifest = LoadJson(
                contract.Candidate.ProductionManifestPath);
            var items = manifest["items"] as JArray;
            if (items == null ||
                items.Count !=
                    contract.Candidate.ProductionArtworkCount)
            {
                throw new InvalidOperationException(
                    "Production manifest count drifted.");
            }
            foreach (var item in items.OfType<JObject>())
            {
                var artId = (string)item["artId"];
                var expectedHash = (string)item["sha256"];
                if (string.IsNullOrWhiteSpace(artId) ||
                    string.IsNullOrWhiteSpace(expectedHash) ||
                    !runtimeById.TryGetValue(
                        artId,
                        out var runtimeEntry))
                {
                    throw new InvalidOperationException(
                        "Production artwork is incomplete: " + artId);
                }
                var runtimePath = runtimeEntry.SourcePath;
                if (!runtimePath.StartsWith(
                        RuntimeArtRoot + "/",
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        ComputeSha256(
                            ResolveProjectAssetPath(runtimePath)),
                        expectedHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Promoted production hash/path drifted: " +
                        artId);
                }
            }
        }

        private static void ValidateRuntimeImporter(
            string assetPath,
            RuntimePromotionTargetPolicy policy)
        {
            var importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Runtime TextureImporter is missing: " + assetPath);
            }
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            var standalone =
                importer.GetPlatformTextureSettings("Standalone");
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.mipmapEnabled != policy.Mipmaps ||
                !importer.sRGBTexture ||
                importer.alphaSource !=
                    TextureImporterAlphaSource.None ||
                importer.alphaIsTransparency ||
                importer.isReadable != policy.Readable ||
                importer.textureCompression !=
                    TextureImporterCompression.Compressed ||
                importer.maxTextureSize != policy.MaxTextureSize ||
                importer.filterMode != FilterMode.Bilinear ||
                importer.wrapMode != TextureWrapMode.Clamp ||
                Math.Abs(importer.spritePixelsPerUnit - 100f) > 0.001f ||
                textureSettings.spriteMeshType !=
                    SpriteMeshType.FullRect ||
                !standalone.overridden ||
                standalone.maxTextureSize != policy.MaxTextureSize ||
                standalone.format != TextureImporterFormat.DXT1 ||
                standalone.compressionQuality !=
                    policy.CompressionQuality ||
                standalone.crunchedCompression)
            {
                throw new InvalidOperationException(
                    "Runtime importer policy drifted: " + assetPath);
            }
        }

        private static RuntimePromotionContract LoadContract()
        {
            var path = ResolveRepositoryPath(
                LightStorybookRuntimePromotionGate.ContractRelativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Runtime promotion contract is missing.",
                    path);
            }
            var contract =
                JsonConvert.DeserializeObject<RuntimePromotionContract>(
                    File.ReadAllText(path));
            if (contract == null)
            {
                throw new InvalidOperationException(
                    "Runtime promotion contract could not be parsed.");
            }
            return contract;
        }

        private static void ValidateApprovalAndPolicy(
            RuntimePromotionContract contract)
        {
            var failures =
                LightStorybookRuntimePromotionGate.ValidateApproval(
                    contract.Approval);
            if (failures.Length != 0)
            {
                throw new InvalidOperationException(
                    "Runtime promotion approval is invalid: " +
                    string.Join("; ", failures));
            }
            LightStorybookRuntimePromotionGate.ValidateTargetPolicy(
                contract.TargetPolicy);
            if (contract.Version != "0.3.3" ||
                contract.GateVersion != "1" ||
                contract.Candidate == null ||
                contract.RuntimeBeforePromotion == null ||
                contract.Candidate.CatalogPath !=
                    LightStorybookProductionBatch6Builder.CatalogPath ||
                contract.Candidate.CatalogGuid !=
                    "1600000000000000000000000000000a" ||
                contract.Candidate.CatalogEntryCount != 86 ||
                contract.Candidate.ConfiguredArtworkCount != 83 ||
                contract.Candidate.ProductionArtworkCount != 51 ||
                contract.RuntimeBeforePromotion.CatalogPath !=
                    RuntimeCatalogPath ||
                contract.RuntimeBeforePromotion.CatalogGuid !=
                    "75d638606a8084146524a35a317a2cca" ||
                contract.RuntimeBeforePromotion.CatalogEntryCount != 24 ||
                contract.RuntimeBeforePromotion
                    .ProductionArtworkCount != 0 ||
                contract.TargetPolicy.RuntimeArtRoot != RuntimeArtRoot)
            {
                throw new InvalidOperationException(
                    "Runtime promotion contract identity drifted.");
            }
        }

        private static PresentationSpriteCatalog LoadCatalog(
            string path)
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    path);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Presentation catalog is missing: " + path);
            }
            return catalog;
        }

        private static List<CatalogEntrySnapshot> ReadCatalogEntries(
            PresentationSpriteCatalog catalog)
        {
            var serialized = new SerializedObject(catalog);
            var artworks = RequireArtworks(serialized);
            var result = new List<CatalogEntrySnapshot>(
                artworks.arraySize);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < artworks.arraySize;
                 index++)
            {
                var entry = artworks.GetArrayElementAtIndex(index);
                var artId =
                    entry.FindPropertyRelative("id").stringValue;
                var sprite = entry.FindPropertyRelative("sprite")
                    .objectReferenceValue as Sprite;
                if (string.IsNullOrWhiteSpace(artId) ||
                    sprite == null ||
                    !ids.Add(artId))
                {
                    throw new InvalidOperationException(
                        "Catalog contains an incomplete or duplicate " +
                        "artwork entry.");
                }
                result.Add(new CatalogEntrySnapshot
                {
                    ArtId = artId,
                    SourcePath = AssetDatabase.GetAssetPath(sprite),
                    FocalPointY = entry
                        .FindPropertyRelative("focalPointY")
                        .floatValue
                });
            }
            return result;
        }

        private static SerializedProperty RequireArtworks(
            SerializedObject catalog)
        {
            var artworks = catalog.FindProperty("artworks");
            if (artworks == null || !artworks.isArray)
            {
                throw new InvalidOperationException(
                    "PresentationSpriteCatalog.artworks is unavailable.");
            }
            return artworks;
        }

        private static JObject LoadJson(string repositoryRelativePath)
        {
            var path =
                ResolveRepositoryPath(repositoryRelativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Promotion input is missing.",
                    path);
            }
            return JObject.Parse(File.ReadAllText(path));
        }

        private static void WritePromotionManifest(
            RuntimePromotionContract contract,
            RuntimePromotionPlan plan)
        {
            var manifest = new
            {
                version = contract.Version,
                status = "PROMOTED",
                approvedBy = contract.Approval.ApprovedBy,
                approvedAt = contract.Approval.ApprovedAt,
                candidateCatalogPath = plan.CandidateCatalogPath,
                candidateCatalogGuid = contract.Candidate.CatalogGuid,
                runtimeCatalogPath = plan.RuntimeCatalogPath,
                runtimeCatalogGuid =
                    contract.RuntimeBeforePromotion.CatalogGuid,
                runtimeCatalogEntryCount =
                    contract.Candidate.CatalogEntryCount,
                configuredArtworkCount =
                    contract.Candidate.ConfiguredArtworkCount,
                productionArtworkCount =
                    contract.Candidate.ProductionArtworkCount,
                copiedCalibrationArtworkCount = plan.Entries.Length,
                policy = new
                {
                    runtimeArtRoot =
                        contract.TargetPolicy.RuntimeArtRoot,
                    standaloneTextureFormat =
                        contract.TargetPolicy.StandaloneTextureFormat,
                    maxTextureSize =
                        contract.TargetPolicy.MaxTextureSize,
                    compressionQuality =
                        contract.TargetPolicy.CompressionQuality,
                    mipmaps = contract.TargetPolicy.Mipmaps,
                    readable = contract.TargetPolicy.Readable,
                    calibrationReferences = 0,
                    runtimeCatalogGuidPreserved = true
                },
                entries = plan.Entries.Select(value => new
                {
                    artId = value.ArtId,
                    sourcePath = value.SourcePath,
                    runtimePath = value.RuntimePath,
                    sha256 = value.Sha256
                }).ToArray()
            };

            var path = ResolveRepositoryPath(
                PromotionManifestRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(
                path,
                JsonConvert.SerializeObject(
                    manifest,
                    Formatting.Indented) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }
                current = next;
            }
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
            {
                return string.Concat(
                    algorithm.ComputeHash(stream)
                        .Select(value => value.ToString("x2")));
            }
        }

        private static string ResolveProjectAssetPath(
            string assetPath)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                assetPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));
        }

        private static string ResolveRepositoryPath(
            string relativePath)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            var repositoryRoot =
                Directory.GetParent(projectRoot).FullName;
            return Path.GetFullPath(Path.Combine(
                repositoryRoot,
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));
        }

        private sealed class CatalogEntrySnapshot
        {
            public string ArtId { get; set; }
            public string SourcePath { get; set; }
            public float FocalPointY { get; set; }
        }
    }

    [Serializable]
    public sealed class RuntimePromotionPlan
    {
        public string CandidateCatalogPath { get; set; }
        public string RuntimeCatalogPath { get; set; }
        public RuntimePromotionPlanEntry[] Entries { get; set; } =
            Array.Empty<RuntimePromotionPlanEntry>();
    }

    [Serializable]
    public sealed class RuntimePromotionPlanEntry
    {
        public string ArtId { get; set; }
        public string SourcePath { get; set; }
        public string RuntimePath { get; set; }
        public string Sha256 { get; set; }
    }
}
