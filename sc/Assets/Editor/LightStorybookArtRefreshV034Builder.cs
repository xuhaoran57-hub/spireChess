using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpireChess.UI;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Editor
{
    public static class LightStorybookArtRefreshV034Builder
    {
        public const string RuntimeCatalogPath =
            "Assets/Configs/Presentation/PresentationSpriteCatalog.asset";

        public const string RefreshManifestRelativePath =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "legacy-refresh-v0.3.4/ART-REFRESH-MANIFEST-v0.3.4.json";

        public const string BaselineManifestRelativePath =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "runtime-promotion-v0.3.3/promotion-manifest.json";

        public const string PromotionResultRelativePath =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "legacy-refresh-v0.3.4/" +
            "RUNTIME-PROMOTION-RESULT-v0.3.4.json";

        public const int ExpectedConfiguredArtworkCount = 83;
        public const int ExpectedBaselineArtworkCount = 66;
        public const int ExpectedRefreshArtworkCount = 17;

        private const string RuntimeCatalogGuid =
            "75d638606a8084146524a35a317a2cca";

        private const int MaxTextureSize = 1024;
        private const int CompressionQuality = 50;

        [MenuItem(
            "Spire Chess/Release/Promote Phase 9C v0.3.4 Art Refresh")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static ArtRefreshPlanV034 CreatePlan()
        {
            var manifestPath =
                ResolveRepositoryPath(RefreshManifestRelativePath);
            if (!File.Exists(manifestPath))
            {
                throw new InvalidOperationException(
                    "Art refresh manifest is missing: " +
                    RefreshManifestRelativePath);
            }

            var manifest = JsonConvert.DeserializeObject<
                ArtRefreshManifestV034>(
                File.ReadAllText(manifestPath, Encoding.UTF8));
            if (manifest == null ||
                manifest.Version != "0.3.4" ||
                (manifest.Status != "APPROVED_FOR_RUNTIME" &&
                 manifest.Status != "PROMOTED") ||
                manifest.Items == null ||
                manifest.Items.Length != ExpectedRefreshArtworkCount)
            {
                throw new InvalidOperationException(
                    "Art refresh manifest identity or approval is invalid.");
            }

            var entries = manifest.Items
                .OrderBy(value => value.ArtId, StringComparer.Ordinal)
                .ToArray();
            if (entries.Any(value =>
                    string.IsNullOrWhiteSpace(value.ArtId) ||
                    string.IsNullOrWhiteSpace(value.CandidatePath) ||
                    string.IsNullOrWhiteSpace(value.CandidateSha256) ||
                    string.IsNullOrWhiteSpace(value.RuntimePath) ||
                    string.IsNullOrWhiteSpace(value.OldRuntimeSha256) ||
                    string.IsNullOrWhiteSpace(value.RuntimeGuid)) ||
                entries.Select(value => value.ArtId)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != entries.Length ||
                entries.Select(value => value.RuntimePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() != entries.Length ||
                entries.Count(value => value.Kind == "Minion") != 10 ||
                entries.Count(value => value.Kind == "Spell") != 4 ||
                entries.Count(value => value.Kind == "Token") != 3)
            {
                throw new InvalidOperationException(
                    "Art refresh manifest entries are incomplete or " +
                    "duplicated.");
            }

            foreach (var entry in entries)
            {
                var candidatePath =
                    ResolveRepositoryPath(entry.CandidatePath);
                var runtimePath =
                    ResolveProjectAssetPath(entry.RuntimePath);
                if (!File.Exists(candidatePath) ||
                    !string.Equals(
                        ComputeSha256(candidatePath),
                        entry.CandidateSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Candidate artwork hash mismatch: " +
                        entry.ArtId);
                }
                if (!File.Exists(runtimePath))
                {
                    throw new InvalidOperationException(
                        "Runtime artwork is missing: " +
                        entry.RuntimePath);
                }

                var runtimeHash = ComputeSha256(runtimePath);
                if (!string.Equals(
                        runtimeHash,
                        entry.OldRuntimeSha256,
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        runtimeHash,
                        entry.CandidateSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Runtime artwork has an unknown pre-promotion " +
                        "hash: " + entry.ArtId);
                }
                if (!string.Equals(
                        AssetDatabase.AssetPathToGUID(entry.RuntimePath),
                        entry.RuntimeGuid,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Runtime artwork GUID drifted: " +
                        entry.ArtId);
                }
                if (Math.Abs(entry.FocalPointY - 0.5f) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Refreshed artwork must use a centered focal point: " +
                        entry.ArtId);
                }
            }

            return new ArtRefreshPlanV034
            {
                Manifest = manifest,
                Entries = entries
            };
        }

        public static string[] RefreshArtIds()
        {
            return CreatePlan().Entries
                .Select(value => value.ArtId)
                .ToArray();
        }

        public static bool IsPromoted()
        {
            return ValidatePromotedState().Length == 0;
        }

        public static string[] ValidatePromotedState()
        {
            try
            {
                var plan = CreatePlan();
                ValidateRefreshEntries(plan);
                ValidateStyleCoverageOrThrow(plan);
                return Array.Empty<string>();
            }
            catch (Exception exception)
            {
                return new[] { exception.Message };
            }
        }

        public static string[] ValidateStyleCoverage()
        {
            try
            {
                ValidateStyleCoverageOrThrow(CreatePlan());
                return Array.Empty<string>();
            }
            catch (Exception exception)
            {
                return new[] { exception.Message };
            }
        }

        private static void Build()
        {
            var plan = CreatePlan();
            var catalogGuidBefore =
                AssetDatabase.AssetPathToGUID(RuntimeCatalogPath);
            if (catalogGuidBefore != RuntimeCatalogGuid)
            {
                throw new InvalidOperationException(
                    "Runtime catalog GUID drifted before v0.3.4 refresh.");
            }

            foreach (var entry in plan.Entries)
            {
                var candidatePath =
                    ResolveRepositoryPath(entry.CandidatePath);
                var runtimePath =
                    ResolveProjectAssetPath(entry.RuntimePath);
                if (!string.Equals(
                        ComputeSha256(runtimePath),
                        entry.CandidateSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(candidatePath, runtimePath, true);
                }

                AssetDatabase.ImportAsset(
                    entry.RuntimePath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                ConfigureRuntimeImporter(entry.RuntimePath);
            }

            ApplyRuntimeCatalogBindings(plan);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            if (AssetDatabase.AssetPathToGUID(RuntimeCatalogPath) !=
                catalogGuidBefore)
            {
                throw new InvalidOperationException(
                    "Runtime catalog GUID changed during v0.3.4 refresh.");
            }

            ValidateRefreshEntries(plan);
            ValidateStyleCoverageOrThrow(plan);
            WritePromotionResult(plan);
            Debug.Log(
                "[LightStorybook] v0.3.4 art refresh promoted 17 " +
                "in-place artworks; exact approved style coverage is " +
                "83/83.");
        }

        private static void ApplyRuntimeCatalogBindings(
            ArtRefreshPlanV034 plan)
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    RuntimeCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Runtime PresentationSpriteCatalog is missing.");
            }

            var entriesByArtId = plan.Entries.ToDictionary(
                value => value.ArtId,
                StringComparer.Ordinal);
            var serialized = new SerializedObject(catalog);
            var artworks = serialized.FindProperty("artworks");
            if (artworks == null)
            {
                throw new InvalidOperationException(
                    "Runtime catalog artworks property is missing.");
            }

            var matched = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < artworks.arraySize; index++)
            {
                var artwork = artworks.GetArrayElementAtIndex(index);
                var artId = artwork
                    .FindPropertyRelative("id")
                    .stringValue;
                if (!entriesByArtId.TryGetValue(
                        artId,
                        out var planEntry))
                {
                    continue;
                }

                var sprite =
                    AssetDatabase.LoadAssetAtPath<Sprite>(
                        planEntry.RuntimePath);
                if (sprite == null)
                {
                    throw new InvalidOperationException(
                        "Refreshed Runtime sprite failed to import: " +
                        artId);
                }

                artwork.FindPropertyRelative("sprite")
                    .objectReferenceValue = sprite;
                artwork.FindPropertyRelative("focalPointY")
                    .floatValue = planEntry.FocalPointY;
                matched.Add(artId);
            }

            if (matched.Count != plan.Entries.Length)
            {
                throw new InvalidOperationException(
                    "Runtime catalog is missing one or more v0.3.4 " +
                    "refresh ArtIds.");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                RuntimeCatalogPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureRuntimeImporter(string assetPath)
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
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.isReadable = false;
            importer.textureCompression =
                TextureImporterCompression.Compressed;
            importer.maxTextureSize = MaxTextureSize;
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
            standalone.maxTextureSize = MaxTextureSize;
            standalone.format = TextureImporterFormat.DXT1;
            standalone.compressionQuality = CompressionQuality;
            standalone.crunchedCompression = false;
            importer.SetPlatformTextureSettings(standalone);
            importer.SaveAndReimport();
        }

        private static void ValidateRefreshEntries(
            ArtRefreshPlanV034 plan)
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    RuntimeCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Runtime PresentationSpriteCatalog is missing.");
            }

            foreach (var entry in plan.Entries)
            {
                var runtimePath =
                    ResolveProjectAssetPath(entry.RuntimePath);
                if (!string.Equals(
                        ComputeSha256(runtimePath),
                        entry.CandidateSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Refreshed Runtime hash mismatch: " +
                        entry.ArtId);
                }
                if (!string.Equals(
                        AssetDatabase.AssetPathToGUID(entry.RuntimePath),
                        entry.RuntimeGuid,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Refreshed Runtime GUID mismatch: " +
                        entry.ArtId);
                }
                if (!catalog.TryGetArtwork(
                        entry.ArtId,
                        out var sprite,
                        out var focalPointY) ||
                    sprite == null ||
                    AssetDatabase.GetAssetPath(sprite) !=
                        entry.RuntimePath ||
                    Math.Abs(focalPointY - entry.FocalPointY) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Refreshed Runtime catalog binding mismatch: " +
                        entry.ArtId);
                }

                ValidateRuntimeImporter(entry.RuntimePath);
            }
        }

        private static void ValidateStyleCoverageOrThrow(
            ArtRefreshPlanV034 plan)
        {
            if (AssetDatabase.AssetPathToGUID(RuntimeCatalogPath) !=
                RuntimeCatalogGuid)
            {
                throw new InvalidOperationException(
                    "Runtime catalog GUID does not match the approved " +
                    "style coverage baseline.");
            }

            var baselinePath =
                ResolveRepositoryPath(BaselineManifestRelativePath);
            if (!File.Exists(baselinePath))
            {
                throw new InvalidOperationException(
                    "v0.3.3 Runtime promotion manifest is missing.");
            }

            var baseline =
                JObject.Parse(File.ReadAllText(
                    baselinePath,
                    Encoding.UTF8));
            var baselineEntries =
                baseline["entries"] as JArray;
            if ((string)baseline["status"] != "PROMOTED" ||
                baselineEntries == null ||
                baselineEntries.Count != ExpectedBaselineArtworkCount)
            {
                throw new InvalidOperationException(
                    "v0.3.3 approved style baseline must contain 66 " +
                    "promoted artworks.");
            }

            var approved = new Dictionary<
                string,
                ApprovedRuntimeArtwork>(
                StringComparer.Ordinal);
            foreach (var token in baselineEntries.OfType<JObject>())
            {
                AddApprovedArtwork(
                    approved,
                    new ApprovedRuntimeArtwork
                    {
                        ArtId = (string)token["artId"],
                        RuntimePath = (string)token["runtimePath"],
                        Sha256 = (string)token["sha256"]
                    });
            }
            foreach (var entry in plan.Entries)
            {
                AddApprovedArtwork(
                    approved,
                    new ApprovedRuntimeArtwork
                    {
                        ArtId = entry.ArtId,
                        RuntimePath = entry.RuntimePath,
                        Sha256 = entry.CandidateSha256,
                        RuntimeGuid = entry.RuntimeGuid,
                        FocalPointY = entry.FocalPointY,
                        RequireCenteredFocalPoint = true
                    });
            }

            if (approved.Count != ExpectedConfiguredArtworkCount)
            {
                throw new InvalidOperationException(
                    "Approved style sources must cover exactly 83 unique " +
                    "configured ArtIds.");
            }

            var configuredArtIds = ReadConfiguredArtworkIds();
            if (configuredArtIds.Count !=
                    ExpectedConfiguredArtworkCount ||
                !configuredArtIds.SetEquals(approved.Keys))
            {
                var missing = configuredArtIds
                    .Except(approved.Keys, StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal);
                var extra = approved.Keys
                    .Except(configuredArtIds, StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal);
                throw new InvalidOperationException(
                    "Configured artwork style coverage drifted. Missing: " +
                    string.Join(", ", missing) +
                    "; extra: " +
                    string.Join(", ", extra));
            }

            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    RuntimeCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Runtime PresentationSpriteCatalog is missing.");
            }

            foreach (var pair in approved.OrderBy(
                         value => value.Key,
                         StringComparer.Ordinal))
            {
                var expected = pair.Value;
                if (!catalog.TryGetArtwork(
                        expected.ArtId,
                        out var sprite,
                        out var focalPointY) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "Configured artwork is not Exact in Runtime: " +
                        expected.ArtId);
                }

                var actualPath = AssetDatabase.GetAssetPath(sprite);
                if (!string.Equals(
                        actualPath,
                        expected.RuntimePath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Configured artwork path is not the approved " +
                        "style source: " + expected.ArtId);
                }
                if (!string.Equals(
                        ComputeSha256(
                            ResolveProjectAssetPath(actualPath)),
                        expected.Sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Configured artwork hash is not the approved " +
                        "style source: " + expected.ArtId);
                }
                if (!string.IsNullOrWhiteSpace(
                        expected.RuntimeGuid) &&
                    !string.Equals(
                        AssetDatabase.AssetPathToGUID(actualPath),
                        expected.RuntimeGuid,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Configured artwork GUID drifted: " +
                        expected.ArtId);
                }
                if (expected.RequireCenteredFocalPoint &&
                    Math.Abs(focalPointY - expected.FocalPointY) >
                        0.0001f)
                {
                    throw new InvalidOperationException(
                        "Configured artwork focal point is not centered: " +
                        expected.ArtId);
                }

                ValidateRuntimeImporter(actualPath);
            }
        }

        private static void AddApprovedArtwork(
            IDictionary<string, ApprovedRuntimeArtwork> approved,
            ApprovedRuntimeArtwork artwork)
        {
            if (artwork == null ||
                string.IsNullOrWhiteSpace(artwork.ArtId) ||
                string.IsNullOrWhiteSpace(artwork.RuntimePath) ||
                string.IsNullOrWhiteSpace(artwork.Sha256) ||
                approved.ContainsKey(artwork.ArtId))
            {
                throw new InvalidOperationException(
                    "Approved style manifest contains an invalid or " +
                    "duplicate artwork entry.");
            }
            approved.Add(artwork.ArtId, artwork);
        }

        private static HashSet<string> ReadConfiguredArtworkIds()
        {
            var artIds = new HashSet<string>(StringComparer.Ordinal);
            ReadConfiguredArtworkIds(
                "sc/Assets/Resources/Configs/Json/minions.v0.1.json",
                "minions",
                artIds);
            ReadConfiguredArtworkIds(
                "sc/Assets/Resources/Configs/Json/spells.v0.1.json",
                "spells",
                artIds);
            return artIds;
        }

        private static void ReadConfiguredArtworkIds(
            string relativePath,
            string collectionName,
            ISet<string> artIds)
        {
            var path = ResolveRepositoryPath(relativePath);
            var root =
                JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            var collection = root[collectionName] as JArray;
            if (collection == null)
            {
                throw new InvalidOperationException(
                    "Configured artwork collection is missing: " +
                    relativePath);
            }

            foreach (var entry in collection.OfType<JObject>())
            {
                var artId = (string)entry["artId"];
                if (string.IsNullOrWhiteSpace(artId) ||
                    !artIds.Add(artId))
                {
                    throw new InvalidOperationException(
                        "Configured ArtId is empty or duplicated: " +
                        (artId ?? "<null>"));
                }
            }
        }

        private static void ValidateRuntimeImporter(string assetPath)
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
                importer.mipmapEnabled ||
                !importer.sRGBTexture ||
                importer.alphaSource != TextureImporterAlphaSource.None ||
                importer.alphaIsTransparency ||
                importer.isReadable ||
                importer.textureCompression !=
                    TextureImporterCompression.Compressed ||
                importer.maxTextureSize != MaxTextureSize ||
                importer.filterMode != FilterMode.Bilinear ||
                importer.wrapMode != TextureWrapMode.Clamp ||
                Math.Abs(importer.spritePixelsPerUnit - 100f) >
                    0.001f ||
                textureSettings.spriteMeshType != SpriteMeshType.FullRect ||
                !standalone.overridden ||
                standalone.maxTextureSize != MaxTextureSize ||
                standalone.format != TextureImporterFormat.DXT1 ||
                standalone.compressionQuality != CompressionQuality ||
                standalone.crunchedCompression)
            {
                throw new InvalidOperationException(
                    "Runtime importer policy drifted: " + assetPath);
            }
        }

        private static void WritePromotionResult(
            ArtRefreshPlanV034 plan)
        {
            var resultPath =
                ResolveRepositoryPath(PromotionResultRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
            var result = new
            {
                version = "0.3.4",
                releaseId = "legacy-card-art-refresh-v0.3.4",
                status = "PROMOTED",
                promotedAt = DateTimeOffset.Now.ToString("o"),
                runtimeCatalogPath = RuntimeCatalogPath,
                runtimeCatalogGuid = RuntimeCatalogGuid,
                runtimeCatalogSha256 = ComputeSha256(
                    ResolveProjectAssetPath(RuntimeCatalogPath)),
                configuredArtworkCount =
                    ExpectedConfiguredArtworkCount,
                baselineApprovedArtworkCount =
                    ExpectedBaselineArtworkCount,
                refreshedArtworkCount =
                    ExpectedRefreshArtworkCount,
                exactApprovedStyleCoverage = "83/83",
                policy = new
                {
                    standaloneTextureFormat = "DXT1",
                    maxTextureSize = MaxTextureSize,
                    compressionQuality = CompressionQuality,
                    mipmaps = false,
                    readable = false,
                    focalPointY = 0.5f,
                    runtimeCatalogGuidPreserved = true
                },
                entries = plan.Entries.Select(value => new
                {
                    artId = value.ArtId,
                    candidatePath = value.CandidatePath,
                    sourceSha256 = value.CandidateSha256,
                    runtimePath = value.RuntimePath,
                    runtimeSha256 = value.CandidateSha256,
                    runtimeGuid = value.RuntimeGuid,
                    focalPointY = value.FocalPointY
                }).ToArray()
            };
            File.WriteAllText(
                resultPath,
                JsonConvert.SerializeObject(
                    result,
                    Formatting.Indented) + "\n",
                new UTF8Encoding(false));
        }

        private static string RepositoryRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null || projectRoot.Parent == null)
            {
                throw new InvalidOperationException(
                    "Unable to resolve repository root.");
            }
            return projectRoot.Parent.FullName;
        }

        private static string ResolveRepositoryPath(
            string relativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    RepositoryRoot(),
                    relativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));
        }

        private static string ResolveProjectAssetPath(
            string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                !assetPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Expected an Assets-relative path.",
                    nameof(assetPath));
            }

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException(
                    "Unable to resolve Unity project root.");
            }
            return Path.GetFullPath(
                Path.Combine(
                    projectRoot.FullName,
                    assetPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
            {
                return string.Concat(
                    hash.ComputeHash(stream)
                        .Select(value => value.ToString("x2")));
            }
        }

        private sealed class ApprovedRuntimeArtwork
        {
            public string ArtId { get; set; }
            public string RuntimePath { get; set; }
            public string Sha256 { get; set; }
            public string RuntimeGuid { get; set; }
            public float FocalPointY { get; set; }
            public bool RequireCenteredFocalPoint { get; set; }
        }
    }

    public sealed class ArtRefreshPlanV034
    {
        public ArtRefreshManifestV034 Manifest { get; set; }
        public ArtRefreshManifestEntryV034[] Entries { get; set; }
    }

    public sealed class ArtRefreshManifestV034
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("items")]
        public ArtRefreshManifestEntryV034[] Items { get; set; }
    }

    public sealed class ArtRefreshManifestEntryV034
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("artId")]
        public string ArtId { get; set; }

        [JsonProperty("candidatePath")]
        public string CandidatePath { get; set; }

        [JsonProperty("candidateSha256")]
        public string CandidateSha256 { get; set; }

        [JsonProperty("runtimePath")]
        public string RuntimePath { get; set; }

        [JsonProperty("oldRuntimeSha256")]
        public string OldRuntimeSha256 { get; set; }

        [JsonProperty("runtimeGuid")]
        public string RuntimeGuid { get; set; }

        [JsonProperty("focalPointY")]
        public float FocalPointY { get; set; }
    }
}
