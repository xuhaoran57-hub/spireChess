using System;
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
    public static class LightStorybookProductionBatch6Builder
    {
        public const string CatalogPath =
            "Assets/Configs/Presentation/" +
            "PresentationSpriteCatalog_LightStorybookProductionV033Batch06.asset";

        private const string ArtFolder =
            "Assets/Art/Presentation/Calibration/" +
            "LightStorybookProductionV033Batch06";
        private const string ManifestRelativePath =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "PRODUCTION-MANIFEST-v0.3.3.json";
        private const string SpellConfigAssetPath =
            "Assets/Resources/Configs/Json/spells.v0.1.json";
        private const string BatchId = "batch-06-spells";

        [MenuItem(
            "Spire Chess/UI/Build Light Storybook Production " +
            "v0.3.3 Batch 06")]
        public static void Build()
        {
            var specs = LoadAndValidateSpecs();
            EnsureAssetFolder();
            var sprites = CopyAndConfigureArtwork(specs);
            BuildCatalog(specs, sprites);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[LightStorybook] Built isolated v0.3.3 Batch 06 " +
                "catalog with nine spells.");
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
            if (specs.Length != 9 ||
                specs.Any(value =>
                    value.Kind != "Spell" ||
                    value.Tier < 1 ||
                    value.Tier > 5 ||
                    value.Status != "generated" ||
                    string.IsNullOrWhiteSpace(value.Id) ||
                    string.IsNullOrWhiteSpace(value.Name) ||
                    string.IsNullOrWhiteSpace(value.ArtId) ||
                    string.IsNullOrWhiteSpace(value.ArtFile) ||
                    string.IsNullOrWhiteSpace(value.Description) ||
                    string.IsNullOrWhiteSpace(value.Sha256)))
            {
                throw new InvalidOperationException(
                    "Batch 06 requires nine generated tier 1-5 spells " +
                    "with complete identity and hash fields.");
            }

            if (specs.Select(value => value.Id)
                    .Distinct(StringComparer.Ordinal).Count() != specs.Length ||
                specs.Select(value => value.ArtId)
                    .Distinct(StringComparer.Ordinal).Count() != specs.Length)
            {
                throw new InvalidOperationException(
                    "Batch 06 ids and art ids must be unique.");
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
                SpellConfigAssetPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var configFile =
                JsonConvert.DeserializeObject<SpellConfigFile>(
                    File.ReadAllText(configPath));
            if (configFile?.Spells == null)
            {
                throw new InvalidOperationException(
                    "Unable to load spell configs.");
            }

            var spells = configFile.Spells.ToDictionary(
                value => value.Id,
                StringComparer.Ordinal);
            foreach (var spec in specs)
            {
                if (!spells.TryGetValue(spec.Id, out var spell))
                {
                    throw new InvalidOperationException(
                        "Batch 06 spell is missing: " + spec.Id);
                }
                if (spell.Name != spec.Name ||
                    spell.Tier != spec.Tier ||
                    spell.Cost != spec.Cost ||
                    spell.ArtId != spec.ArtId ||
                    spell.Description != spec.Description)
                {
                    throw new InvalidOperationException(
                        "Batch 06 manifest drifted from config: " +
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
                        "Batch 06 artwork is missing.",
                        sourcePath);
                }
                var actualSha256 = ComputeSha256(sourcePath);
                if (!string.Equals(
                        actualSha256,
                        spec.Sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Batch 06 artwork hash drifted: " + spec.Id);
                }

                var destinationAssetPath =
                    ArtFolder + "/" + Path.GetFileName(sourcePath);
                var destinationPath = Path.Combine(
                    projectRoot,
                    destinationAssetPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                var requiresCopy =
                    !File.Exists(destinationPath) ||
                    !string.Equals(
                        ComputeSha256(destinationPath),
                        actualSha256,
                        StringComparison.OrdinalIgnoreCase);
                if (requiresCopy)
                {
                    File.Copy(sourcePath, destinationPath, true);
                    AssetDatabase.ImportAsset(
                        destinationAssetPath,
                        ImportAssetOptions.ForceSynchronousImport);
                }
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
                LightStorybookProductionBatch5Builder.CatalogPath,
                CatalogPath);
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Failed to create Batch 06 isolated catalog.");
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
                        "Batch 06 catalog has no exact artwork for " +
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
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            var requiresReimport =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.mipmapEnabled ||
                !importer.sRGBTexture ||
                importer.alphaIsTransparency ||
                importer.textureCompression !=
                TextureImporterCompression.Uncompressed ||
                importer.maxTextureSize != 2048 ||
                importer.isReadable ||
                importer.filterMode != FilterMode.Bilinear ||
                importer.wrapMode != TextureWrapMode.Clamp ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, 100f) ||
                settings.spriteMeshType != SpriteMeshType.FullRect;
            if (requiresReimport)
            {
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
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
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
                "LightStorybookProductionV033Batch06");
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
            var sourceAsset = AssetDatabase.LoadMainAssetAtPath(source);
            if (sourceAsset == null)
            {
                throw new InvalidOperationException(
                    $"Source asset does not exist: '{source}'.");
            }

            var destinationAsset =
                AssetDatabase.LoadMainAssetAtPath(destination);
            if (destinationAsset == null)
            {
                if (!AssetDatabase.CopyAsset(source, destination))
                {
                    throw new InvalidOperationException(
                        $"Failed to copy '{source}' to '{destination}'.");
                }
                return;
            }

            if (sourceAsset.GetType() != destinationAsset.GetType())
            {
                throw new InvalidOperationException(
                    $"Asset type mismatch for '{destination}'.");
            }

            EditorUtility.CopySerialized(sourceAsset, destinationAsset);
            destinationAsset.name = Path.GetFileNameWithoutExtension(
                destination);
            EditorUtility.SetDirty(destinationAsset);
            AssetDatabase.SaveAssets();
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

            [JsonProperty("cost")]
            public int Cost { get; set; }

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
