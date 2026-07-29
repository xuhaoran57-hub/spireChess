using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SpireChess.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SpireChess.Editor
{
    public static class LightStorybookCardStressBuilder
    {
        public const string CatalogPath =
            "Assets/Configs/Presentation/" +
            "PresentationSpriteCatalog_LightStorybookMechanicStress.asset";
        public const string CardPrefabPath =
            "Assets/Prefabs/UI/Calibration/LightStorybook/" +
            "PF_Card_MechanicStress.prefab";
        public const string ScenePath =
            "Assets/Scenes/Calibration/LightStorybook/" +
            "CardMechanicStressLightStorybookAB.unity";

        private const string ArtFolder =
            "Assets/Art/Presentation/Calibration/" +
            "LightStorybookMechanicStress";
        private const string SourceRelativeDirectory =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "mechanic-stress-test-v0.1";
        private const string ManifestFileName = "CARD-SPECS.json";
        private const string OutputRelativeDirectory =
            "ui-concepts/unity-validation/" +
            "light-storybook-card-stress-v0.1";

        [MenuItem(
            "Spire Chess/UI/Build Light Storybook Card Stress A-B")]
        public static void Build()
        {
            var manifest = LoadManifest();
            EnsureAssetsFolders();
            var sprites = CopyAndConfigureArtwork(manifest);
            var catalog = BuildCatalog(manifest, sprites);
            var cardPrefab = BuildCardPrefab(catalog);
            var context = BuildCompactScene(
                cardPrefab,
                RequireFont(),
                manifest.Cards);
            EditorSceneManager.SaveScene(context.Scene, ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[LightStorybook] Built isolated card mechanic stress scene.");
        }

        [MenuItem(
            "Spire Chess/UI/Build and Capture Light Storybook Card Stress A-B")]
        public static void BuildAndCapture()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException(
                    "Card stress capture requires a graphics device. " +
                    "Do not run Unity with -nographics.");
            }

            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Build();
                CaptureValidationScreenshots();
            }
            finally
            {
                if (sceneSetup.Any(value =>
                        value.isLoaded && value.isActive))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void BuildAndCaptureFromCommandLine()
        {
            BuildAndCapture();
        }

        private static void CaptureValidationScreenshots()
        {
            var manifest = LoadManifest();
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardPrefabPath);
            if (cardPrefab == null)
            {
                throw new InvalidOperationException(
                    "Build the stress card prefab before capture.");
            }

            var repositoryRoot = ResolveRepositoryRoot();
            var outputDirectory = Path.Combine(
                repositoryRoot,
                OutputRelativeDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            Directory.CreateDirectory(outputDirectory);

            var compactScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            var compact = FindCaptureContext(compactScene);
            CaptureBothResolutions(
                compact,
                outputDirectory,
                "mechanic-stress-compact-normal-golden");

            var full = BuildFullScene(
                cardPrefab,
                RequireFont(),
                manifest.Cards);
            CaptureBothResolutions(
                full,
                outputDirectory,
                "mechanic-stress-full-normal");

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AssetDatabase.Refresh();
            Debug.Log(
                "[LightStorybook] Captured card stress validation to " +
                outputDirectory);
        }

        private static StressManifest LoadManifest()
        {
            var path = Path.Combine(
                ResolveSourceDirectory(),
                ManifestFileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Card stress manifest is missing.",
                    path);
            }

            var manifest = JsonConvert.DeserializeObject<StressManifest>(
                File.ReadAllText(path));
            if (manifest?.Cards == null || manifest.Cards.Length != 9)
            {
                throw new InvalidOperationException(
                    "Card stress manifest must contain exactly nine cards.");
            }

            if (manifest.Cards.Any(value =>
                    value == null ||
                    string.IsNullOrWhiteSpace(value.Id) ||
                    string.IsNullOrWhiteSpace(value.ArtId) ||
                    string.IsNullOrWhiteSpace(value.ArtFile) ||
                    string.IsNullOrWhiteSpace(value.Name) ||
                    string.IsNullOrWhiteSpace(value.Race)))
            {
                throw new InvalidOperationException(
                    "Every stress card requires id, artId, artFile, name " +
                    "and race.");
            }

            if (manifest.Cards.Select(value => value.Id)
                    .Distinct(StringComparer.Ordinal).Count() !=
                manifest.Cards.Length ||
                manifest.Cards.Select(value => value.ArtId)
                    .Distinct(StringComparer.Ordinal).Count() !=
                manifest.Cards.Length)
            {
                throw new InvalidOperationException(
                    "Stress card ids and art ids must be unique.");
            }

            return manifest;
        }

        private static Sprite[] CopyAndConfigureArtwork(
            StressManifest manifest)
        {
            var sourceRoot = ResolveSourceDirectory();
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var sprites = new Sprite[manifest.Cards.Length];
            for (var index = 0; index < manifest.Cards.Length; index++)
            {
                var spec = manifest.Cards[index];
                var source = Path.Combine(
                    sourceRoot,
                    spec.ArtFile.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                if (!File.Exists(source))
                {
                    throw new FileNotFoundException(
                        "Stress card artwork is missing.",
                        source);
                }

                var assetPath =
                    ArtFolder + "/" + Path.GetFileName(spec.ArtFile);
                File.Copy(
                    source,
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
            StressManifest manifest,
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
                    "Failed to create the card stress sprite catalog.");
            }

            var serialized = new SerializedObject(catalog);
            for (var index = 0; index < manifest.Cards.Length; index++)
            {
                AddOrReplaceArtwork(
                    serialized,
                    manifest.Cards[index].ArtId,
                    sprites[index]);
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
                if (!catalog.TryGetArtwork(spec.ArtId, out var sprite) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "Stress catalog has no exact artwork for " +
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

        private static CaptureContext BuildCompactScene(
            GameObject prefab,
            Font font,
            StressCardSpec[] specs)
        {
            var context = CreateScene(
                font,
                "LIGHT STORYBOOK · MECHANIC STRESS · COMPACT 160×240",
                "NORMAL TOP / GOLDEN BOTTOM · 3 RACES × 3 MECHANICS");
            const float cardWidth = 160f;
            const float gap = 36f;
            var startX = CenteredRowStart(
                specs.Length,
                cardWidth,
                gap);

            CreateSectionLabel(
                context.Canvas,
                font,
                "普通卡面 · 9",
                startX,
                78f);
            CreateSectionLabel(
                context.Canvas,
                font,
                "金色卡面 · 9",
                startX,
                374f);
            for (var index = 0; index < specs.Length; index++)
            {
                var x = startX + index * (cardWidth + gap);
                CreatePreviewCard(
                    prefab,
                    context.Canvas,
                    CreateModel(
                        specs[index],
                        false,
                        CardDisplayMode.Compact),
                    x,
                    112f);
                CreatePreviewCard(
                    prefab,
                    context.Canvas,
                    CreateModel(
                        specs[index],
                        true,
                        CardDisplayMode.Compact),
                    x,
                    408f);
            }

            CreateFooter(
                context.Canvas,
                font,
                "检查：三族区分、缩略图主体、机制关系、文字密度、" +
                "普通/金色识别与精确 Artwork 命中");
            return context;
        }

        private static CaptureContext BuildFullScene(
            GameObject prefab,
            Font font,
            StressCardSpec[] specs)
        {
            var context = CreateScene(
                font,
                "LIGHT STORYBOOK · MECHANIC STRESS · FULL 240×360",
                "NORMAL CARDS · CHECK NAME / RULES / TAGS / STATS");
            const float cardWidth = 240f;
            const float firstGap = 60f;
            const int firstRowCount = 5;
            var firstStart = CenteredRowStart(
                firstRowCount,
                cardWidth,
                firstGap);
            for (var index = 0; index < firstRowCount; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context.Canvas,
                    CreateModel(
                        specs[index],
                        false,
                        CardDisplayMode.Full),
                    firstStart + index * (cardWidth + firstGap),
                    112f);
            }

            const float secondGap = 80f;
            var secondCount = specs.Length - firstRowCount;
            var secondStart = CenteredRowStart(
                secondCount,
                cardWidth,
                secondGap);
            for (var index = 0; index < secondCount; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context.Canvas,
                    CreateModel(
                        specs[index + firstRowCount],
                        false,
                        CardDisplayMode.Full),
                    secondStart + index * (cardWidth + secondGap),
                    530f);
            }

            CreateFooter(
                context.Canvas,
                font,
                "检查：中央裁切、名称与长规则可读性、费用/等级/攻击/生命、" +
                "关键词标签和种族信息");
            return context;
        }

        private static CaptureContext CreateScene(
            Font font,
            string title,
            string subtitle)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("CardStressCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.80f, 0.85f, 0.83f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var canvasObject = new GameObject(
                "CardStressCanvas",
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
                title,
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
                subtitle,
                15,
                60f,
                56f,
                1800f,
                26f,
                new Color(0.35f, 0.34f, 0.31f, 1f));
            return new CaptureContext(scene, camera, canvasRect);
        }

        private static CaptureContext FindCaptureContext(
            UnityEngine.SceneManagement.Scene scene)
        {
            var cameraObject = scene.GetRootGameObjects()
                .FirstOrDefault(value => value.name == "CardStressCamera");
            var canvasObject = scene.GetRootGameObjects()
                .FirstOrDefault(value => value.name == "CardStressCanvas");
            if (cameraObject == null || canvasObject == null)
            {
                throw new InvalidOperationException(
                    "Saved card stress scene has no capture context.");
            }

            return new CaptureContext(
                scene,
                cameraObject.GetComponent<Camera>(),
                canvasObject.GetComponent<RectTransform>());
        }

        private static CardViewModel CreateModel(
            StressCardSpec spec,
            bool golden,
            CardDisplayMode mode)
        {
            var keywords = spec.Keywords ?? Array.Empty<string>();
            var attack = golden ? spec.Attack * 2 : spec.Attack;
            var health = golden ? spec.Health * 2 : spec.Health;
            return new CardViewModel
            {
                InstanceId =
                    "stress_" + spec.Id + (golden ? "_golden" : string.Empty),
                ArtId = spec.ArtId,
                Name = spec.Name,
                Description = golden
                    ? spec.GoldenDescription
                    : spec.Description,
                RaceText = ToRaceText(spec.Race),
                AbilityLabels = keywords
                    .Select(ToAbilityLabel)
                    .ToArray(),
                Keywords = keywords,
                Tier = spec.Tier,
                Attack = attack,
                Health = health,
                BaseAttack = attack,
                BaseHealth = health,
                Cost = 3,
                DisplayMode = mode,
                IsMinion = true,
                ShowCost = mode == CardDisplayMode.Full,
                IsGolden = golden,
                IsInteractable = true,
                IsAffordable = true,
                HasShield = keywords.Contains(
                    "Shield",
                    StringComparer.Ordinal)
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
                    "Failed to instantiate stress PF_Card.");
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

        private static void CreateSectionLabel(
            RectTransform canvas,
            Font font,
            string value,
            float x,
            float y)
        {
            CreateText(
                canvas,
                font,
                "Section_" + value,
                value,
                17,
                x,
                y,
                480f,
                26f,
                new Color(0.39f, 0.27f, 0.16f, 1f));
        }

        private static void CreateFooter(
            RectTransform canvas,
            Font font,
            string value)
        {
            CreateText(
                canvas,
                font,
                "Footer",
                value,
                14,
                60f,
                1018f,
                1800f,
                26f,
                new Color(0.35f, 0.34f, 0.31f, 1f));
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

        private static void CaptureBothResolutions(
            CaptureContext context,
            string outputDirectory,
            string stem)
        {
            Capture(
                context.Camera,
                context.Canvas,
                1920,
                1080,
                Path.Combine(outputDirectory, stem + "-1920x1080.png"));
            Capture(
                context.Camera,
                context.Canvas,
                1920,
                1200,
                Path.Combine(outputDirectory, stem + "-1920x1200.png"));
        }

        private static void Capture(
            Camera camera,
            RectTransform canvas,
            int width,
            int height,
            string outputPath)
        {
            canvas.sizeDelta = new Vector2(width, height);
            camera.aspect = (float)width / height;
            camera.orthographicSize = height * 0.5f;
            var renderTexture = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            try
            {
                camera.targetTexture = renderTexture;
                PrepareTextForCapture(canvas);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void PrepareTextForCapture(RectTransform canvas)
        {
            var texts = canvas.GetComponentsInChildren<Text>(true)
                .Where(value =>
                    value.gameObject.activeInHierarchy &&
                    value.font != null)
                .ToArray();
            foreach (var group in texts.GroupBy(value => new
                     {
                         value.font,
                         value.fontSize,
                         value.fontStyle
                     }))
            {
                group.Key.font.RequestCharactersInTexture(
                    string.Concat(group.Select(value => value.text)),
                    group.Key.fontSize,
                    group.Key.fontStyle);
            }
            foreach (var text in texts)
            {
                text.SetAllDirty();
            }
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
            Sprite sprite)
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
            entry.FindPropertyRelative("focalPointY").floatValue = 0.5f;
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

        private static float CenteredRowStart(
            int count,
            float cardWidth,
            float gap)
        {
            var rowWidth = count * cardWidth +
                           Mathf.Max(0, count - 1) * gap;
            return (1920f - rowWidth) * 0.5f;
        }

        private static string ResolveSourceDirectory()
        {
            return Path.Combine(
                ResolveRepositoryRoot(),
                SourceRelativeDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
        }

        private static string ResolveRepositoryRoot()
        {
            var projectRoot = Directory.GetParent(
                Application.dataPath).FullName;
            return Directory.GetParent(projectRoot).FullName;
        }

        private static void EnsureAssetsFolders()
        {
            EnsureFolder("Assets/Prefabs/UI", "Calibration");
            EnsureFolder(
                "Assets/Prefabs/UI/Calibration",
                "LightStorybook");
            EnsureFolder("Assets/Scenes", "Calibration");
            EnsureFolder(
                "Assets/Scenes/Calibration",
                "LightStorybook");
            EnsureFolder(
                "Assets/Art/Presentation",
                "Calibration");
            EnsureFolder(
                "Assets/Art/Presentation/Calibration",
                "LightStorybookMechanicStress");
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
        private sealed class StressManifest
        {
            [JsonProperty("cards")]
            public StressCardSpec[] Cards { get; set; } =
                Array.Empty<StressCardSpec>();
        }

        [Serializable]
        private sealed class StressCardSpec
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("artId")]
            public string ArtId { get; set; }

            [JsonProperty("artFile")]
            public string ArtFile { get; set; }

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

            [JsonProperty("keywords")]
            public string[] Keywords { get; set; } = Array.Empty<string>();

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("goldenDescription")]
            public string GoldenDescription { get; set; }
        }

        private readonly struct CaptureContext
        {
            public CaptureContext(
                UnityEngine.SceneManagement.Scene scene,
                Camera camera,
                RectTransform canvas)
            {
                Scene = scene;
                Camera = camera;
                Canvas = canvas;
            }

            public UnityEngine.SceneManagement.Scene Scene { get; }
            public Camera Camera { get; }
            public RectTransform Canvas { get; }
        }
    }
}
