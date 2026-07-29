using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SpireChess.Config;
using SpireChess.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.Editor
{
    public static class LightStorybookFormalCatalogBuilder
    {
        public const string CatalogPath =
            "Assets/Configs/Presentation/" +
            "PresentationSpriteCatalog_LightStorybookFormalCatalogV032.asset";
        public const string CardPrefabPath =
            "Assets/Prefabs/UI/Calibration/LightStorybook/" +
            "PF_Card_FormalCatalogV032.prefab";
        public const string ScenePath =
            "Assets/Scenes/Calibration/LightStorybook/" +
            "FormalCatalogV032.unity";

        private const string ArtFolder =
            "Assets/Art/Presentation/Calibration/" +
            "LightStorybookFormalCatalogV032";
        private const string SourceRelativeDirectory =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "validation-round-7-v0.3.2-formal-catalog";
        private const string ManifestRelativePath =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "FORMAL-CATALOG-SPECS-v0.3.2.json";
        private const string MinionConfigAssetPath =
            "Assets/Resources/Configs/Json/minions.v0.1.json";
        private const string SpellConfigAssetPath =
            "Assets/Resources/Configs/Json/spells.v0.1.json";

        private static readonly string[] ColumnKinds =
        {
            "ForgeSoul",
            "WildSpirit",
            "Starbound",
            "Wayfarer",
            "Spell"
        };

        private static readonly string[] ColumnTitles =
        {
            "铸魂",
            "荒灵",
            "星契",
            "旅团",
            "法术"
        };

        [MenuItem(
            "Spire Chess/UI/Build Light Storybook Formal Catalog v0.3.2")]
        public static void Build()
        {
            var manifest = LoadAndValidateManifest();
            EnsureAssetFolders();
            var sprites = CopyAndConfigureArtwork(manifest);
            var catalog = BuildCatalog(manifest, sprites);
            var cardPrefab = BuildCardPrefab(catalog);
            var scene = BuildValidationScene(
                cardPrefab,
                RequireFont(),
                manifest);
            EditorSceneManager.SaveScene(scene, ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[LightStorybook] Built isolated formal artwork catalog " +
                "v0.3.2 with 12 minions and 3 spells.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static FormalManifest LoadAndValidateManifest()
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
                    "Formal catalog manifest is missing.",
                    manifestPath);
            }

            var manifest = JsonConvert.DeserializeObject<FormalManifest>(
                File.ReadAllText(manifestPath));
            if (manifest?.Cards == null ||
                manifest.Cards.Length != 15)
            {
                throw new InvalidOperationException(
                    "Formal catalog manifest must contain exactly 15 cards.");
            }

            if (manifest.Cards.Any(value =>
                    value == null ||
                    string.IsNullOrWhiteSpace(value.Kind) ||
                    string.IsNullOrWhiteSpace(value.Id) ||
                    string.IsNullOrWhiteSpace(value.ArtId) ||
                    string.IsNullOrWhiteSpace(value.ArtFile) ||
                    string.IsNullOrWhiteSpace(value.Name)))
            {
                throw new InvalidOperationException(
                    "Every formal catalog entry requires kind, id, artId, " +
                    "artFile and name.");
            }

            if (manifest.Cards.Select(value => value.Id)
                    .Distinct(StringComparer.Ordinal).Count() !=
                manifest.Cards.Length ||
                manifest.Cards.Select(value => value.ArtId)
                    .Distinct(StringComparer.Ordinal).Count() !=
                manifest.Cards.Length)
            {
                throw new InvalidOperationException(
                    "Formal catalog ids and art ids must be unique.");
            }

            var minions = manifest.Cards
                .Where(value => value.Kind == "Minion")
                .ToArray();
            var spells = manifest.Cards
                .Where(value => value.Kind == "Spell")
                .ToArray();
            if (minions.Length != 12 || spells.Length != 3)
            {
                throw new InvalidOperationException(
                    "Formal catalog requires 12 minions and 3 spells.");
            }

            foreach (var race in ColumnKinds.Take(4))
            {
                if (minions.Count(value => value.Race == race) != 3)
                {
                    throw new InvalidOperationException(
                        $"Formal catalog requires exactly 3 {race} minions.");
                }
            }

            ValidateAgainstFormalConfigs(manifest);
            return manifest;
        }

        private static void ValidateAgainstFormalConfigs(
            FormalManifest manifest)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var minionFile = JsonConvert.DeserializeObject<MinionConfigFile>(
                File.ReadAllText(Path.Combine(
                    projectRoot,
                    MinionConfigAssetPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar))));
            var spellFile = JsonConvert.DeserializeObject<SpellConfigFile>(
                File.ReadAllText(Path.Combine(
                    projectRoot,
                    SpellConfigAssetPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar))));
            if (minionFile?.Minions == null || spellFile?.Spells == null)
            {
                throw new InvalidOperationException(
                    "Unable to load formal minion or spell configs.");
            }

            var minions = minionFile.Minions.ToDictionary(
                value => value.Id,
                StringComparer.Ordinal);
            var spells = spellFile.Spells.ToDictionary(
                value => value.Id,
                StringComparer.Ordinal);
            foreach (var spec in manifest.Cards)
            {
                if (spec.Kind == "Minion")
                {
                    if (!minions.TryGetValue(spec.Id, out var minion))
                    {
                        throw new InvalidOperationException(
                            "Formal minion is missing: " + spec.Id);
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
                            "Formal minion manifest drifted from config: " +
                            spec.Id);
                    }
                }
                else if (spec.Kind == "Spell")
                {
                    if (!spells.TryGetValue(spec.Id, out var spell))
                    {
                        throw new InvalidOperationException(
                            "Formal spell is missing: " + spec.Id);
                    }
                    if (spell.Name != spec.Name ||
                        spell.Tier != spec.Tier ||
                        spell.Cost != spec.Cost ||
                        spell.ArtId != spec.ArtId ||
                        spell.Description != spec.Description)
                    {
                        throw new InvalidOperationException(
                            "Formal spell manifest drifted from config: " +
                            spec.Id);
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        "Unsupported formal catalog kind: " + spec.Kind);
                }
            }
        }

        private static Sprite[] CopyAndConfigureArtwork(
            FormalManifest manifest)
        {
            var repositoryRoot = ResolveRepositoryRoot();
            var sourceRoot = Path.Combine(
                repositoryRoot,
                SourceRelativeDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var sprites = new Sprite[manifest.Cards.Length];

            for (var index = 0; index < manifest.Cards.Length; index++)
            {
                var spec = manifest.Cards[index];
                var sourcePath = Path.Combine(
                    sourceRoot,
                    spec.ArtFile.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException(
                        "Formal catalog artwork is missing.",
                        sourcePath);
                }

                var assetPath =
                    ArtFolder + "/" + Path.GetFileName(spec.ArtFile);
                File.Copy(
                    sourcePath,
                    Path.Combine(projectRoot, assetPath),
                    true);
                AssetDatabase.ImportAsset(
                    assetPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                sprites[index] = ConfigureSprite(assetPath);
            }

            return sprites;
        }

        private static PresentationSpriteCatalog BuildCatalog(
            FormalManifest manifest,
            Sprite[] sprites)
        {
            var sourcePath =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookAbBuilder.CatalogPath) != null
                    ? LightStorybookAbBuilder.CatalogPath
                    : CardUiPrefabBuilder.SpriteCatalogPath;
            CopyAssetReplacing(sourcePath, CatalogPath);

            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Failed to create isolated formal sprite catalog.");
            }

            var serialized = new SerializedObject(catalog);
            for (var index = 0; index < manifest.Cards.Length; index++)
            {
                AddOrReplaceArtwork(
                    serialized,
                    manifest.Cards[index].ArtId,
                    sprites[index],
                    manifest.Cards[index].FocalPointY);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Resources.UnloadAsset(catalog);
            catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            foreach (var spec in manifest.Cards)
            {
                if (!catalog.TryGetArtwork(
                        spec.ArtId,
                        out var sprite,
                        out _) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "Isolated catalog has no exact artwork for " +
                        spec.ArtId);
                }
            }

            return catalog;
        }

        private static GameObject BuildCardPrefab(
            PresentationSpriteCatalog catalog)
        {
            CopyAssetReplacing(
                CardUiPrefabBuilder.PrefabPath,
                CardPrefabPath);
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var view = root.GetComponent<CardView>();
                if (view == null)
                {
                    throw new InvalidOperationException(
                        "PF_Card has no CardView component.");
                }

                var serialized = new SerializedObject(view);
                var property = serialized.FindProperty("spriteCatalog");
                if (property == null)
                {
                    throw new InvalidOperationException(
                        "CardView.spriteCatalog is unavailable.");
                }
                property.objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        }

        private static UnityEngine.SceneManagement.Scene BuildValidationScene(
            GameObject prefab,
            Font font,
            FormalManifest manifest)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("FormalCatalogCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.80f, 0.85f, 0.83f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 540f;
            camera.aspect = 16f / 9f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var canvasObject = new GameObject(
                "FormalCatalogCanvas",
                typeof(RectTransform),
                typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 1;
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;

            CreateText(
                canvasRect,
                font,
                "Title",
                "LIGHT STORYBOOK · FORMAL CATALOG v0.3.2",
                28,
                60f,
                18f,
                1800f,
                38f,
                new Color(0.20f, 0.14f, 0.09f, 1f));
            CreateText(
                canvasRect,
                font,
                "Subtitle",
                "ISOLATED CATALOG · 4 RACES × 3 MINIONS + 3 SPELLS",
                15,
                60f,
                56f,
                1800f,
                26f,
                new Color(0.35f, 0.34f, 0.31f, 1f));

            var xPositions = new[] { 90f, 450f, 810f, 1170f, 1530f };
            var yPositions = new[] { 128f, 414f, 700f };
            for (var column = 0; column < ColumnKinds.Length; column++)
            {
                CreateText(
                    canvasRect,
                    font,
                    "Column_" + ColumnKinds[column],
                    ColumnTitles[column],
                    18,
                    xPositions[column],
                    90f,
                    160f,
                    28f,
                    new Color(0.39f, 0.27f, 0.16f, 1f));

                var specs = manifest.Cards
                    .Where(value => ColumnKinds[column] == "Spell"
                        ? value.Kind == "Spell"
                        : value.Kind == "Minion" &&
                          value.Race == ColumnKinds[column])
                    .OrderBy(value => value.Tier)
                    .ThenBy(value => value.Id, StringComparer.Ordinal)
                    .ToArray();
                if (specs.Length != 3)
                {
                    throw new InvalidOperationException(
                        "Every validation column must contain three cards.");
                }

                for (var row = 0; row < specs.Length; row++)
                {
                    CreatePreviewCard(
                        prefab,
                        canvasRect,
                        CreateModel(specs[row]),
                        xPositions[column],
                        yPositions[row]);
                }
            }

            CreateText(
                canvasRect,
                font,
                "Footer",
                "检查：缩略图主体、四族区分、法术物件叙事、" +
                "名称/规则可读性与精确 Artwork 命中；不覆盖 Runtime Catalog",
                14,
                60f,
                1018f,
                1800f,
                26f,
                new Color(0.35f, 0.34f, 0.31f, 1f));
            return scene;
        }

        private static CardViewModel CreateModel(FormalCardSpec spec)
        {
            var keywords = spec.Keywords ?? Array.Empty<string>();
            var labels = spec.Kind == "Minion"
                ? keywords.Select(ToAbilityLabel).ToArray()
                : (spec.Tags ?? Array.Empty<string>())
                    .Take(3)
                    .Select(ToTagLabel)
                    .ToArray();
            return new CardViewModel
            {
                InstanceId = "formal_catalog_" + spec.Id,
                ArtId = spec.ArtId,
                Name = spec.Name,
                Description = spec.Description,
                RaceText = spec.Kind == "Spell"
                    ? "商店法术"
                    : ToRaceText(spec.Race),
                AbilityLabels = labels,
                Keywords = keywords,
                Tier = spec.Tier,
                Attack = spec.Attack,
                Health = spec.Health,
                BaseAttack = spec.Attack,
                BaseHealth = spec.Health,
                Cost = spec.Kind == "Spell" ? spec.Cost : 3,
                DisplayMode = CardDisplayMode.Compact,
                IsMinion = spec.Kind == "Minion",
                ShowCost = false,
                IsGolden = false,
                IsInteractable = true,
                IsAffordable = true,
                HasShield =
                    keywords.Contains("Shield", StringComparer.Ordinal) ||
                    (spec.Tags ?? Array.Empty<string>())
                        .Contains("shield", StringComparer.Ordinal)
            };
        }

        private static void CreatePreviewCard(
            GameObject prefab,
            RectTransform canvas,
            CardViewModel model,
            float x,
            float y)
        {
            var card = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (card == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate formal catalog PF_Card.");
            }

            card.transform.SetParent(canvas, false);
            card.GetComponent<CardView>().Render(model);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.localScale = Vector3.one;
        }

        private static Text CreateText(
            RectTransform parent,
            Font font,
            string name,
            string value,
            int fontSize,
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.text = value;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return text;
        }

        private static Sprite ConfigureSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
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
            var settings = new TextureImporterSettings
            {
                spriteMeshType = SpriteMeshType.FullRect
            };
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
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            entry.FindPropertyRelative("focalPointY").floatValue =
                Mathf.Clamp01(focalPointY);
        }

        private static Font RequireFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(
                CardUiPrefabBuilder.FontPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    "Pinned card font is missing at " +
                    CardUiPrefabBuilder.FontPath);
            }
            return font;
        }

        private static string ToRaceText(string race)
        {
            switch (race)
            {
                case "ForgeSoul": return "铸魂";
                case "WildSpirit": return "荒灵";
                case "Starbound": return "星契";
                case "Wayfarer": return "旅团";
                default: return race;
            }
        }

        private static string ToAbilityLabel(string keyword)
        {
            switch (keyword)
            {
                case "Battlecry": return "战吼";
                case "Shield": return "护盾";
                case "Taunt": return "嘲讽";
                case "Deathrattle": return "亡语";
                case "Splash": return "溅射";
                default: return keyword;
            }
        }

        private static string ToTagLabel(string tag)
        {
            switch (tag)
            {
                case "shield": return "护盾";
                case "next_combat": return "下场战斗";
                case "refresh": return "刷新";
                case "economy": return "经济";
                case "discover_minion": return "随从发现";
                case "late_game": return "后期";
                default: return tag;
            }
        }

        private static string ResolveRepositoryRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Directory.GetParent(projectRoot).FullName;
        }

        private static void EnsureAssetFolders()
        {
            EnsureFolder("Assets/Prefabs/UI", "Calibration");
            EnsureFolder(
                "Assets/Prefabs/UI/Calibration",
                "LightStorybook");
            EnsureFolder("Assets/Scenes", "Calibration");
            EnsureFolder(
                "Assets/Scenes/Calibration",
                "LightStorybook");
            EnsureFolder("Assets/Art/Presentation", "Calibration");
            EnsureFolder(
                "Assets/Art/Presentation/Calibration",
                "LightStorybookFormalCatalogV032");
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

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        [Serializable]
        private sealed class FormalManifest
        {
            [JsonProperty("cards")]
            public FormalCardSpec[] Cards { get; set; } =
                Array.Empty<FormalCardSpec>();
        }

        [Serializable]
        private sealed class FormalCardSpec
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

            [JsonProperty("cost")]
            public int Cost { get; set; }

            [JsonProperty("artId")]
            public string ArtId { get; set; }

            [JsonProperty("keywords")]
            public string[] Keywords { get; set; } = Array.Empty<string>();

            [JsonProperty("tags")]
            public string[] Tags { get; set; } = Array.Empty<string>();

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("goldenDescription")]
            public string GoldenDescription { get; set; }

            [JsonProperty("artFile")]
            public string ArtFile { get; set; }

            [JsonProperty("focalPointY")]
            public float FocalPointY { get; set; } = 0.5f;
        }
    }
}
