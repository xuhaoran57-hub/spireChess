using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SpireChess.Config;
using SpireChess.UI;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Editor
{
    public static class LightStorybookProductionBatch4Builder
    {
        public const string CatalogPath =
            "Assets/Configs/Presentation/" +
            "PresentationSpriteCatalog_LightStorybookProductionV033Batch04.asset";

        private const string ArtFolder =
            "Assets/Art/Presentation/Calibration/" +
            "LightStorybookProductionV033Batch04";
        private const string ManifestRelativePath =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "PRODUCTION-MANIFEST-v0.3.3.json";
        private const string MinionConfigAssetPath =
            "Assets/Resources/Configs/Json/minions.v0.1.json";
        private const string BatchId = "batch-04-tier4";

        private static readonly IReadOnlyDictionary<string, int>
            ExpectedRaceCounts = new Dictionary<string, int>
            {
                { "ForgeSoul", 3 },
                { "WildSpirit", 3 },
                { "Starbound", 3 },
                { "Wayfarer", 2 }
            };

        [MenuItem(
            "Spire Chess/UI/Build Light Storybook Production " +
            "v0.3.3 Batch 04")]
        public static void Build()
        {
            var specs = LoadAndValidateSpecs();
            EnsureAssetFolder();
            var sprites = CopyAndConfigureArtwork(specs);
            BuildCatalog(specs, sprites);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[LightStorybook] Built isolated v0.3.3 Batch 04 " +
                "catalog with eleven tier-4 minions.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static ProductionItem[] LoadAndValidateSpecs()
        {
            var repositoryRoot = ResolveRepositoryRoot();
            var manifestPath = Path.Combine(
                repositoryRoot,
                ManifestRelativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException(
                    "Production manifest is missing.",
                    manifestPath);
            }

            var manifest = JsonConvert.DeserializeObject<ProductionManifest>(
                File.ReadAllText(manifestPath));
            var specs = manifest?.Items?
                .Where(value => value.BatchId == BatchId)
                .ToArray() ?? Array.Empty<ProductionItem>();
            if (specs.Length != 11 ||
                specs.Any(value =>
                    value.Kind != "Minion" ||
                    value.Tier != 4 ||
                    value.Status != "generated" ||
                    string.IsNullOrWhiteSpace(value.Id) ||
                    string.IsNullOrWhiteSpace(value.Name) ||
                    string.IsNullOrWhiteSpace(value.Race) ||
                    string.IsNullOrWhiteSpace(value.ArtId) ||
                    string.IsNullOrWhiteSpace(value.ArtFile) ||
                    string.IsNullOrWhiteSpace(value.Sha256)))
            {
                throw new InvalidOperationException(
                    "Batch 04 requires eleven generated tier-4 minions " +
                    "with complete identity and hash fields.");
            }

            if (specs.Select(value => value.Id)
                    .Distinct(StringComparer.Ordinal).Count() != specs.Length ||
                specs.Select(value => value.ArtId)
                    .Distinct(StringComparer.Ordinal).Count() != specs.Length)
            {
                throw new InvalidOperationException(
                    "Batch 04 ids and art ids must be unique.");
            }

            foreach (var expected in ExpectedRaceCounts)
            {
                if (specs.Count(value => value.Race == expected.Key) !=
                    expected.Value)
                {
                    throw new InvalidOperationException(
                        $"Batch 04 requires {expected.Value} " +
                        $"{expected.Key} minions.");
                }
            }

            ValidateAgainstConfig(specs);
            return specs;
        }

        private static void ValidateAgainstConfig(ProductionItem[] specs)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            var configPath = Path.Combine(
                projectRoot,
                MinionConfigAssetPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var configFile =
                JsonConvert.DeserializeObject<MinionConfigFile>(
                    File.ReadAllText(configPath));
            if (configFile?.Minions == null)
            {
                throw new InvalidOperationException(
                    "Unable to load minion configs.");
            }

            var minions = configFile.Minions.ToDictionary(
                value => value.Id,
                StringComparer.Ordinal);
            foreach (var spec in specs)
            {
                if (!minions.TryGetValue(spec.Id, out var minion))
                {
                    throw new InvalidOperationException(
                        "Batch 04 minion is missing: " + spec.Id);
                }
                if (minion.Name != spec.Name ||
                    minion.Race != spec.Race ||
                    minion.Tier != spec.Tier ||
                    minion.Attack != spec.Attack ||
                    minion.Health != spec.Health ||
                    minion.GoldenAttack != spec.GoldenAttack ||
                    minion.GoldenHealth != spec.GoldenHealth ||
                    minion.ArtId != spec.ArtId ||
                    minion.Description != spec.Description ||
                    minion.GoldenDescription != spec.GoldenDescription)
                {
                    throw new InvalidOperationException(
                        "Batch 04 manifest drifted from config: " +
                        spec.Id);
                }
            }
        }

        private static Sprite[] CopyAndConfigureArtwork(
            ProductionItem[] specs)
        {
            var repositoryRoot = ResolveRepositoryRoot();
            var projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            var sprites = new Sprite[specs.Length];
            for (var index = 0; index < specs.Length; index++)
            {
                var spec = specs[index];
                var sourcePath = Path.Combine(
                    repositoryRoot,
                    spec.ArtFile.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException(
                        "Batch 04 artwork is missing.",
                        sourcePath);
                }
                var actualSha256 = ComputeSha256(sourcePath);
                if (!string.Equals(
                        actualSha256,
                        spec.Sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Batch 04 artwork hash drifted: " + spec.Id);
                }

                var destinationAssetPath =
                    ArtFolder + "/" + Path.GetFileName(sourcePath);
                var destinationPath = Path.Combine(
                    projectRoot,
                    destinationAssetPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                File.Copy(sourcePath, destinationPath, true);
                AssetDatabase.ImportAsset(
                    destinationAssetPath,
                    ImportAssetOptions.ForceSynchronousImport);
                sprites[index] = ConfigureSprite(destinationAssetPath);
            }
            return sprites;
        }

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(
                        algorithm.ComputeHash(stream))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static void BuildCatalog(
            ProductionItem[] specs,
            Sprite[] sprites)
        {
            CopyAssetReplacing(
                LightStorybookProductionBatch3Builder.CatalogPath,
                CatalogPath);
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Failed to create Batch 04 isolated catalog.");
            }

            var serialized = new SerializedObject(catalog);
            for (var index = 0; index < specs.Length; index++)
            {
                AddOrReplaceArtwork(
                    serialized,
                    specs[index].ArtId,
                    sprites[index],
                    specs[index].FocalPointY);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Resources.UnloadAsset(catalog);
            catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            foreach (var spec in specs)
            {
                if (catalog == null ||
                    !catalog.TryGetArtwork(
                        spec.ArtId,
                        out var sprite,
                        out _) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "Batch 04 catalog has no exact artwork for " +
                        spec.ArtId);
                }
            }
        }

        private static Sprite ConfigureSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath)
                as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Unable to configure sprite at " + assetPath);
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void AddOrReplaceArtwork(
            SerializedObject catalog,
            string id,
            Sprite sprite,
            float focalPointY)
        {
            var artworks = catalog.FindProperty("artworks");
            if (artworks == null)
            {
                throw new InvalidOperationException(
                    "PresentationSpriteCatalog.artworks is unavailable.");
            }

            SerializedProperty entry = null;
            for (var index = 0; index < artworks.arraySize; index++)
            {
                var candidate = artworks.GetArrayElementAtIndex(index);
                if (candidate.FindPropertyRelative("id").stringValue == id)
                {
                    entry = candidate;
                    break;
                }
            }
            if (entry == null)
            {
                artworks.arraySize++;
                entry = artworks.GetArrayElementAtIndex(
                    artworks.arraySize - 1);
            }
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("sprite").objectReferenceValue =
                sprite;
            entry.FindPropertyRelative("focalPointY").floatValue =
                Mathf.Clamp01(focalPointY);
        }

        private static void EnsureAssetFolder()
        {
            EnsureFolder("Assets/Art/Presentation", "Calibration");
            EnsureFolder(
                "Assets/Art/Presentation/Calibration",
                "LightStorybookProductionV033Batch04");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CopyAssetReplacing(
            string source,
            string destination)
        {
            if (AssetDatabase.LoadMainAssetAtPath(destination) != null)
            {
                AssetDatabase.DeleteAsset(destination);
            }
            if (!AssetDatabase.CopyAsset(source, destination))
            {
                throw new InvalidOperationException(
                    $"Failed to copy '{source}' to '{destination}'.");
            }
        }

        private static string ResolveRepositoryRoot()
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            return Directory.GetParent(projectRoot).FullName;
        }

        [Serializable]
        private sealed class ProductionManifest
        {
            [JsonProperty("items")]
            public ProductionItem[] Items { get; set; } =
                Array.Empty<ProductionItem>();
        }

        [Serializable]
        private sealed class ProductionItem
        {
            [JsonProperty("kind")]
            public string Kind { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("race")]
            public string Race { get; set; }

            [JsonProperty("tier")]
            public int Tier { get; set; }

            [JsonProperty("attack")]
            public int Attack { get; set; }

            [JsonProperty("health")]
            public int Health { get; set; }

            [JsonProperty("goldenAttack")]
            public int GoldenAttack { get; set; }

            [JsonProperty("goldenHealth")]
            public int GoldenHealth { get; set; }

            [JsonProperty("artId")]
            public string ArtId { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("goldenDescription")]
            public string GoldenDescription { get; set; }

            [JsonProperty("batchId")]
            public string BatchId { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("artFile")]
            public string ArtFile { get; set; }

            [JsonProperty("sha256")]
            public string Sha256 { get; set; }

            [JsonProperty("focalPointY")]
            public float FocalPointY { get; set; } = 0.5f;
        }
    }
}
