using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SpireChess.Config;
using SpireChess.UI;
using SpireChess.UI.Shop;
using SpireChess.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SpireChess.Editor
{
    public static class G2CardMatrixCapture
    {
        private const string OutputRelativeDirectory =
            "ui-concepts/unity-validation/g2-card-matrix-v0.4";

        private static readonly string[] MinionIds =
        {
            "tempering_mender",
            "cracked_armor_avenger",
            "rotleaf_heir",
            "fox_den_matriarch",
            "secret_page_refractor",
            "star_map_broker"
        };

        private static readonly string[] TokenIds =
        {
            "token_young_spirit",
            "token_two_tailed_fox_shadow",
            "token_swift_young_spirit"
        };

        private static readonly string[] SpellIds =
        {
            "minor_tempering",
            "free_refresh",
            "advanced_discovery",
            "prebattle_benediction"
        };

        private static readonly FieldInfo CardCatalogField =
            typeof(CardView).GetField(
                "spriteCatalog",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CardArtworkField =
            typeof(CardView).GetField(
                "artwork",
                BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("Spire Chess/UI/Capture G2 Card Matrix")]
        public static void CaptureValidationScreenshots()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException(
                    "G2 card matrix capture requires a graphics device. " +
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
                CaptureValidationScreenshotsCore();
            }
            finally
            {
                if (sceneSetup.Any(scene =>
                        scene.isLoaded && scene.isActive))
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

        [MenuItem("Spire Chess/UI/Rebuild and Capture G2 Card Matrix")]
        public static void RebuildAndCaptureValidationScreenshots()
        {
            CardUiPrefabBuilder.Build();
            CaptureValidationScreenshots();
        }

        private static void CaptureValidationScreenshotsCore()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardUiPrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "Build PF_Card before capturing the G2 card matrix.");
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(
                CardUiPrefabBuilder.FontPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    "Pinned card font is missing at " +
                    CardUiPrefabBuilder.FontPath);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                CardUiPrefabBuilder.SpriteCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Presentation sprite catalog is missing at " +
                    CardUiPrefabBuilder.SpriteCatalogPath);
            }

            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();

            var minions = LoadMinions(configs, MinionIds, false);
            var tokens = LoadMinions(configs, TokenIds, true);
            var spells = SpellIds.Select(id => RequireSpell(configs, id)).ToArray();
            ValidateExactArtwork(catalog, minions, tokens, spells);

            var repositoryRoot = Directory.GetParent(
                Directory.GetParent(Application.dataPath).FullName).FullName;
            var outputDirectory = Path.Combine(
                repositoryRoot,
                OutputRelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(outputDirectory);

            CaptureFullMinionMatrix(prefab, font, minions, outputDirectory);
            CaptureFullTokenSpellMatrix(
                prefab,
                font,
                tokens,
                spells,
                outputDirectory);
            CaptureCompactMatrix(
                prefab,
                font,
                minions,
                tokens,
                spells,
                outputDirectory);

            AssetDatabase.Refresh();
            Debug.Log(
                "[CardUI] Captured G2 card matrix to " + outputDirectory);
        }

        private static MinionConfig[] LoadMinions(
            ConfigService configs,
            string[] ids,
            bool expectToken)
        {
            return ids.Select(id =>
                {
                    if (!configs.MinionsById.TryGetValue(id, out var config) ||
                        config == null)
                    {
                        throw new InvalidOperationException(
                            "G2 matrix minion config is missing: " + id);
                    }

                    if (config.IsToken != expectToken)
                    {
                        throw new InvalidOperationException(
                            $"G2 matrix token identity mismatch for {id}.");
                    }

                    return config;
                })
                .ToArray();
        }

        private static SpellConfig RequireSpell(
            ConfigService configs,
            string id)
        {
            if (!configs.SpellsById.TryGetValue(id, out var config) ||
                config == null)
            {
                throw new InvalidOperationException(
                    "G2 matrix spell config is missing: " + id);
            }

            return config;
        }

        private static void ValidateExactArtwork(
            PresentationSpriteCatalog catalog,
            MinionConfig[] minions,
            MinionConfig[] tokens,
            SpellConfig[] spells)
        {
            var artIds = minions.Select(value => value.ArtId)
                .Concat(tokens.Select(value => value.ArtId))
                .Concat(spells.Select(value => value.ArtId))
                .ToArray();
            if (artIds.Length != 13 ||
                artIds.Distinct(StringComparer.Ordinal).Count() != 13)
            {
                throw new InvalidOperationException(
                    "G2 card matrix must contain 13 unique artwork IDs.");
            }

            foreach (var artId in artIds)
            {
                if (!catalog.TryGetArtwork(artId, out var sprite) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "G2 card matrix requires an exact artwork hit: " + artId);
                }
            }
        }

        private static void CaptureFullMinionMatrix(
            GameObject prefab,
            Font font,
            MinionConfig[] minions,
            string outputDirectory)
        {
            var context = CreateScene(
                font,
                "G2 CARD MATRIX - NEW CORE MINIONS - FULL 240x360",
                "CONFIG-BACKED - NORMAL TOP / GOLDEN BOTTOM - " +
                "6 EXACT ART IDS");
            const float cardWidth = 240f;
            const float gap = 30f;
            var startX = CenteredRowStart(minions.Length, cardWidth, gap);

            CreateLabel(
                context.Canvas,
                context.AnnotationFont,
                "NORMAL - 6",
                startX,
                94f,
                420f);
            for (var index = 0; index < minions.Length; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context,
                    CreateMinionModel(
                        minions[index],
                        false,
                        CardDisplayMode.Full),
                    startX + index * (cardWidth + gap),
                    126f);
            }

            CreateLabel(
                context.Canvas,
                context.AnnotationFont,
                "GOLDEN - 6",
                startX,
                510f,
                420f);
            for (var index = 0; index < minions.Length; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context,
                    CreateMinionModel(
                        minions[index],
                        true,
                        CardDisplayMode.Full),
                    startX + index * (cardWidth + gap),
                    542f);
            }

            CreateFooter(
                context.Canvas,
                context.AnnotationFont,
                "CHECK: ART CROP, ARCHETYPE, NAME, RULES, TAGS, " +
                "STATS AND GOLDEN IDENTITY");
            CaptureBothResolutions(
                context,
                outputDirectory,
                "g2-minions-full");
        }

        private static void CaptureFullTokenSpellMatrix(
            GameObject prefab,
            Font font,
            MinionConfig[] tokens,
            SpellConfig[] spells,
            string outputDirectory)
        {
            var context = CreateScene(
                font,
                "G2 CARD MATRIX - TOKENS / SPELLS - FULL 240x360",
                "CONFIG-BACKED - 3 TOKENS + 4 SPELLS - " +
                "SPELLS HAVE NO STATS OR GOLDEN STATE",
                330f);
            const float cardWidth = 240f;
            const float gap = 20f;
            var cardCount = tokens.Length + spells.Length;
            var startX = CenteredRowStart(cardCount, cardWidth, gap);
            var spellStartX = startX + tokens.Length * (cardWidth + gap);

            CreateLabel(
                context.Canvas,
                context.AnnotationFont,
                "TOKENS - 3",
                startX,
                128f,
                420f);
            CreateLabel(
                context.Canvas,
                context.AnnotationFont,
                "SPELLS - 4",
                spellStartX,
                128f,
                420f);
            for (var index = 0; index < tokens.Length; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context,
                    CreateMinionModel(
                        tokens[index],
                        false,
                        CardDisplayMode.Full),
                    startX + index * (cardWidth + gap),
                    160f);
            }

            for (var index = 0; index < spells.Length; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context,
                    CreateSpellModel(spells[index], CardDisplayMode.Full),
                    spellStartX + index * (cardWidth + gap),
                    160f);
            }

            CreateFooter(
                context.Canvas,
                context.AnnotationFont,
                "CHECK: TOKEN T0 / NO COST, SPELL TYPE / COST / " +
                "LONG RULES, EXACT ART",
                330f);
            CaptureBothResolutions(
                context,
                outputDirectory,
                "g2-token-spells-full");
        }

        private static void CaptureCompactMatrix(
            GameObject prefab,
            Font font,
            MinionConfig[] minions,
            MinionConfig[] tokens,
            SpellConfig[] spells,
            string outputDirectory)
        {
            var context = CreateScene(
                font,
                "G2 CARD MATRIX - COMPACT 160x240",
                "CONFIG-BACKED - NORMAL / GOLDEN MINIONS - " +
                "TOKENS / SPELLS - 13 EXACT ART IDS");
            const float cardWidth = 160f;
            const float gap = 36f;
            var minionStartX = CenteredRowStart(
                minions.Length,
                cardWidth,
                gap);
            var utilityCount = tokens.Length + spells.Length;
            var utilityStartX = CenteredRowStart(
                utilityCount,
                cardWidth,
                gap);
            var spellStartX = utilityStartX +
                              tokens.Length * (cardWidth + gap);

            CreateLabel(
                context.Canvas,
                context.AnnotationFont,
                "NORMAL MINIONS - 6",
                minionStartX,
                88f,
                420f);
            for (var index = 0; index < minions.Length; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context,
                    CreateMinionModel(
                        minions[index],
                        false,
                        CardDisplayMode.Compact),
                    minionStartX + index * (cardWidth + gap),
                    118f);
            }

            CreateLabel(
                context.Canvas,
                context.AnnotationFont,
                "GOLDEN MINIONS - 6",
                minionStartX,
                382f,
                420f);
            for (var index = 0; index < minions.Length; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context,
                    CreateMinionModel(
                        minions[index],
                        true,
                        CardDisplayMode.Compact),
                    minionStartX + index * (cardWidth + gap),
                    412f);
            }

            CreateLabel(
                context.Canvas,
                context.AnnotationFont,
                "TOKENS - 3",
                utilityStartX,
                676f,
                360f);
            CreateLabel(
                context.Canvas,
                context.AnnotationFont,
                "SPELLS - 4",
                spellStartX,
                676f,
                360f);
            for (var index = 0; index < tokens.Length; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context,
                    CreateMinionModel(
                        tokens[index],
                        false,
                        CardDisplayMode.Compact),
                    utilityStartX + index * (cardWidth + gap),
                    706f);
            }

            for (var index = 0; index < spells.Length; index++)
            {
                CreatePreviewCard(
                    prefab,
                    context,
                    CreateSpellModel(spells[index], CardDisplayMode.Compact),
                    spellStartX + index * (cardWidth + gap),
                    706f);
            }

            CreateFooter(
                context.Canvas,
                context.AnnotationFont,
                "CHECK: NAME, TIER, RULES, STATE AND STATS REMAIN " +
                "READABLE AT 160x240");
            CaptureBothResolutions(
                context,
                outputDirectory,
                "g2-all-compact");
        }

        internal static CaptureContext CreateScene(
            Font font,
            string title,
            string subtitle,
            float annotationX = 60f)
        {
            var fontAssetPath = AssetDatabase.GetAssetPath(font);
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("G2CardMatrixCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.07f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var canvasObject = new GameObject(
                "G2CardMatrixCanvas",
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

            var cardFont = AssetDatabase.LoadAssetAtPath<Font>(fontAssetPath);
            if (cardFont == null)
            {
                throw new InvalidOperationException(
                    "Pinned card font could not be reloaded after scene creation: " +
                    fontAssetPath);
            }

            var annotationSource =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                cardFont;
            var annotationFont = annotationSource;

            CreateText(
                canvasRect,
                annotationFont,
                "MatrixTitle",
                title,
                30,
                annotationX,
                20f,
                1920f - annotationX - 60f,
                42f,
                new Color(0.95f, 0.96f, 0.98f, 1f));
            CreateText(
                canvasRect,
                annotationFont,
                "MatrixSubtitle",
                subtitle,
                16,
                annotationX,
                60f,
                1920f - annotationX - 60f,
                28f,
                new Color(0.68f, 0.72f, 0.80f, 1f));

            return new CaptureContext(
                camera,
                canvasRect,
                cardFont,
                annotationFont);
        }

        internal static void CreateLabel(
            RectTransform canvas,
            Font font,
            string value,
            float x,
            float y,
            float width)
        {
            CreateText(
                canvas,
                font,
                "Section_" + value,
                value,
                18,
                x,
                y,
                width,
                28f,
                new Color(0.91f, 0.78f, 0.47f, 1f));
        }

        internal static void CreateFooter(
            RectTransform canvas,
            Font font,
            string value,
            float x = 60f)
        {
            CreateText(
                canvas,
                font,
                "MatrixFooter",
                value,
                14,
                x,
                1010f,
                1920f - x - 60f,
                26f,
                new Color(0.58f, 0.63f, 0.72f, 1f));
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
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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

        internal static float CenteredRowStart(
            int count,
            float cardWidth,
            float gap)
        {
            var rowWidth = count * cardWidth + Math.Max(0, count - 1) * gap;
            return (1920f - rowWidth) * 0.5f;
        }

        internal static void CreatePreviewCard(
            GameObject prefab,
            CaptureContext context,
            CardViewModel model,
            float x,
            float y,
            PresentationSpriteCatalog catalogOverride = null)
        {
            var card = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (card == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate PF_Card.");
            }

            var cardView = card.GetComponent<CardView>();
            if (!ReferenceEquals(catalogOverride, null))
            {
                if (CardCatalogField == null || CardArtworkField == null)
                {
                    throw new InvalidOperationException(
                        "CardView presentation fields are unavailable.");
                }
                CardCatalogField.SetValue(cardView, catalogOverride);
                if (!ReferenceEquals(
                        CardCatalogField.GetValue(cardView),
                        catalogOverride))
                {
                    throw new InvalidOperationException(
                        "PF_Card rejected the catalog override.");
                }
            }

            card.name = "Card_" + model.InstanceId;
            card.transform.SetParent(context.Canvas, false);
            foreach (var text in card.GetComponentsInChildren<Text>(true))
            {
                text.font = context.CardFont;
            }
            cardView.Render(model);
            if (!ReferenceEquals(catalogOverride, null))
            {
                if (cardView.LastArtworkResolution !=
                    ArtworkResolution.Exact)
                {
                    throw new InvalidOperationException(
                        "Card matrix rendered non-exact artwork: " +
                        model.ArtId);
                }
                if (!catalogOverride.TryGetArtwork(
                        model.ArtId,
                        out var expectedSprite) ||
                    expectedSprite == null)
                {
                    throw new InvalidOperationException(
                        "Card matrix catalog lookup failed: " + model.ArtId);
                }
                var artworkImage =
                    CardArtworkField.GetValue(cardView) as Image;
                if (artworkImage == null ||
                    artworkImage.sprite != expectedSprite)
                {
                    throw new InvalidOperationException(
                        "Card matrix rendered the wrong sprite: " +
                        model.ArtId);
                }
            }
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.localScale = Vector3.one;
        }

        internal static CardViewModel CreateMinionModel(
            MinionConfig config,
            bool isGolden,
            CardDisplayMode mode)
        {
            var model = ShopCardViewModelFactory.FromOffer(
                config,
                int.MaxValue);
            var attack = isGolden ? config.GoldenAttack : config.Attack;
            var health = isGolden ? config.GoldenHealth : config.Health;
            model.InstanceId =
                $"matrix_{config.Id}_{(isGolden ? "golden" : "normal")}_{mode}";
            model.Description = config.GetPrototypeDescription(isGolden);
            model.Attack = attack;
            model.Health = health;
            model.BaseAttack = attack;
            model.BaseHealth = health;
            model.DisplayMode = mode;
            model.IsGolden = isGolden;
            model.ShowCost =
                mode == CardDisplayMode.Full && !config.IsToken;
            return model;
        }

        internal static CardViewModel CreateSpellModel(
            SpellConfig config,
            CardDisplayMode mode)
        {
            var model = ShopCardViewModelFactory.FromOffer(
                config,
                int.MaxValue);
            model.InstanceId = $"matrix_{config.Id}_{mode}";
            model.DisplayMode = mode;
            model.ShowCost = mode == CardDisplayMode.Full;
            return model;
        }

        internal static void CaptureBothResolutions(
            CaptureContext context,
            string outputDirectory,
            string fileStem)
        {
            try
            {
                CaptureFrame(
                    context.Camera,
                    context.Canvas,
                    1920,
                    1080,
                    Path.Combine(
                        outputDirectory,
                        fileStem + "-1920x1080.png"));
                CaptureFrame(
                    context.Camera,
                    context.Canvas,
                    1920,
                    1200,
                    Path.Combine(
                        outputDirectory,
                        fileStem + "-1920x1200.png"));
            }
            finally
            {
                context.ReleaseTransientResources();
            }
        }

        internal static void CaptureFrame(
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
            var previousActiveTexture = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                PrepareTextForCapture(canvas);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                ValidateVisibleContent(
                    texture,
                    camera.backgroundColor,
                    outputPath);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void ValidateVisibleContent(
            Texture2D texture,
            Color backgroundColor,
            string outputPath)
        {
            var background = (Color32)backgroundColor;
            var pixels = texture.GetPixels32();
            var minimumVisiblePixels = Math.Max(1024, pixels.Length / 100);
            var visiblePixels = 0;
            foreach (var pixel in pixels)
            {
                if (Math.Abs(pixel.r - background.r) <= 12 &&
                    Math.Abs(pixel.g - background.g) <= 12 &&
                    Math.Abs(pixel.b - background.b) <= 12)
                {
                    continue;
                }

                visiblePixels++;
                if (visiblePixels >= minimumVisiblePixels)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"G2 matrix frame is blank or incomplete: {outputPath}");
        }

        private static void PrepareTextForCapture(RectTransform canvas)
        {
            var texts = canvas.GetComponentsInChildren<Text>(true)
                .Where(value => value.gameObject.activeInHierarchy &&
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

        internal sealed class CaptureContext
        {
            public CaptureContext(
                Camera camera,
                RectTransform canvas,
                Font cardFont,
                Font annotationFont)
            {
                Camera = camera;
                Canvas = canvas;
                CardFont = cardFont;
                AnnotationFont = annotationFont;
            }

            public Camera Camera { get; }
            public RectTransform Canvas { get; }
            public Font CardFont { get; }
            public Font AnnotationFont { get; }

            public void ReleaseTransientResources()
            {
                // Fonts are shared assets reloaded after the temporary scene is
                // created, so no transient font resources need destruction.
            }
        }
    }
}
