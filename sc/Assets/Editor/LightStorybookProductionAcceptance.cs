using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.UI;
using SpireChess.UI.Battle;
using SpireChess.UI.Shop;
using SpireChess.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpireChess.Editor
{
    public static class LightStorybookProductionAcceptance
    {
        public const string ReleaseRelativeDirectory =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "unity-batch-release-v0.3.3";

        private const string ManifestRelativePath =
            "ui-concepts/phase-9c/light-storybook-production-v0.1/" +
            "PRODUCTION-MANIFEST-v0.3.3.json";

        private const string RuntimeCatalogPath =
            "Assets/Configs/Presentation/PresentationSpriteCatalog.asset";

        private const int ExpectedCatalogEntryCount = 86;
        private const int ExpectedConfiguredArtworkCount = 83;
        private const int ExpectedProductionArtworkCount = 51;
        private const int ExpectedScreenshotCount = 42;

        private static readonly string[] BatchOrder =
        {
            "batch-01-tier1",
            "batch-02-tier2",
            "batch-03-tier3",
            "batch-04-tier4",
            "batch-05-tier5",
            "batch-06-spells"
        };

        private static readonly IReadOnlyDictionary<string, int>
            ExpectedBatchCounts = new Dictionary<string, int>
            {
                { "batch-01-tier1", 7 },
                { "batch-02-tier2", 11 },
                { "batch-03-tier3", 7 },
                { "batch-04-tier4", 11 },
                { "batch-05-tier5", 6 },
                { "batch-06-spells", 9 }
            };

        [MenuItem(
            "Spire Chess/UI/Build and Capture Light Storybook " +
            "Production v0.3.3 Acceptance")]
        public static void BuildAndCapture()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException(
                    "Phase 9C acceptance capture requires a graphics device. " +
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
                BuildAllBatches();
                CaptureAcceptanceEvidence();
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

        public static void BuildAndCaptureFromCommandLine()
        {
            BuildAndCapture();
        }

        private static void BuildAllBatches()
        {
            LightStorybookProductionBatch1Builder.Build();
            LightStorybookProductionBatch2Builder.Build();
            LightStorybookProductionBatch3Builder.Build();
            LightStorybookProductionBatch4Builder.Build();
            LightStorybookProductionBatch5Builder.Build();
            LightStorybookProductionBatch6Builder.Build();
        }

        private static void CaptureAcceptanceEvidence()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<
                PresentationSpriteCatalog>(
                LightStorybookProductionBatch6Builder.CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "The final Batch 06 catalog was not built.");
            }

            var runtimeCatalog = AssetDatabase.LoadAssetAtPath<
                PresentationSpriteCatalog>(RuntimeCatalogPath);
            if (runtimeCatalog == null)
            {
                throw new InvalidOperationException(
                    "The Runtime presentation catalog is missing.");
            }

            var manifest = LoadAndValidateManifest();
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            configs.LoadFromResources().ThrowIfInvalid();
            ValidateCatalogCandidate(
                catalog,
                runtimeCatalog,
                configs,
                manifest.Items);

            var font = AssetDatabase.LoadAssetAtPath<Font>(
                CardUiPrefabBuilder.FontPath);
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardUiPrefabBuilder.PrefabPath);
            if (font == null || cardPrefab == null)
            {
                throw new InvalidOperationException(
                    "PF_Card and its pinned font must exist before Phase 9C " +
                    "acceptance capture.");
            }

            var repositoryRoot = ResolveRepositoryRoot();
            var releaseDirectory = Path.Combine(
                repositoryRoot,
                ReleaseRelativeDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var screenshotDirectory = Path.Combine(
                releaseDirectory,
                "screenshots");
            Directory.CreateDirectory(screenshotDirectory);
            foreach (var staleScreenshot in Directory.GetFiles(
                         screenshotDirectory,
                         "*.png",
                         SearchOption.TopDirectoryOnly))
            {
                File.Delete(staleScreenshot);
            }

            CaptureMatrices(
                manifest.Items,
                configs,
                catalog,
                cardPrefab,
                font,
                screenshotDirectory);
            CaptureShopCrop(
                manifest.Items,
                configs,
                catalog,
                screenshotDirectory);
            CaptureBattleCrop(
                manifest.Items,
                configs,
                catalog,
                screenshotDirectory);
            WriteCaptureIndex(
                releaseDirectory,
                screenshotDirectory,
                catalog,
                manifest.Items);

            AssetDatabase.Refresh();
            Debug.Log(
                "[LightStorybook] Phase 9C acceptance captured " +
                $"{ExpectedScreenshotCount} screenshots to " +
                screenshotDirectory);
        }

        private static ProductionManifest LoadAndValidateManifest()
        {
            var manifestPath = Path.Combine(
                ResolveRepositoryRoot(),
                ManifestRelativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var manifest = JsonConvert.DeserializeObject<ProductionManifest>(
                File.ReadAllText(manifestPath));
            if (manifest?.Items == null ||
                manifest.Items.Length != ExpectedProductionArtworkCount)
            {
                throw new InvalidOperationException(
                    "Phase 9C production manifest must contain exactly 51 " +
                    "items.");
            }

            foreach (var batchId in BatchOrder)
            {
                var count = manifest.Items.Count(value =>
                    value.BatchId == batchId);
                if (count != ExpectedBatchCounts[batchId])
                {
                    throw new InvalidOperationException(
                        $"{batchId} must contain " +
                        $"{ExpectedBatchCounts[batchId]} items; found {count}.");
                }
            }

            if (manifest.Items.Any(value =>
                    value.Status != "generated" ||
                    string.IsNullOrWhiteSpace(value.Id) ||
                    string.IsNullOrWhiteSpace(value.Kind) ||
                    string.IsNullOrWhiteSpace(value.ArtId)) ||
                manifest.Items.Select(value => value.Id)
                    .Distinct(StringComparer.Ordinal).Count() !=
                ExpectedProductionArtworkCount ||
                manifest.Items.Select(value => value.ArtId)
                    .Distinct(StringComparer.Ordinal).Count() !=
                ExpectedProductionArtworkCount)
            {
                throw new InvalidOperationException(
                    "Phase 9C production manifest identities are incomplete " +
                    "or duplicated.");
            }

            return manifest;
        }

        private static void ValidateCatalogCandidate(
            PresentationSpriteCatalog catalog,
            PresentationSpriteCatalog runtimeCatalog,
            ConfigService configs,
            ProductionItem[] productionItems)
        {
            var configuredArtIds = configs.MinionsById.Values
                .Select(value => value.ArtId)
                .Concat(configs.SpellsById.Values.Select(value => value.ArtId))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (configuredArtIds.Length != ExpectedConfiguredArtworkCount)
            {
                throw new InvalidOperationException(
                    "The current card configuration must expose 83 unique " +
                    "artwork IDs.");
            }

            foreach (var artId in configuredArtIds)
            {
                if (!catalog.TryGetArtwork(artId, out var sprite) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "The final Batch 06 catalog is missing exact artwork: " +
                        artId);
                }
            }

            var serializedCatalog = new SerializedObject(catalog);
            var artworks = serializedCatalog.FindProperty("artworks");
            if (artworks == null ||
                artworks.arraySize != ExpectedCatalogEntryCount)
            {
                throw new InvalidOperationException(
                    "The final Batch 06 catalog must contain exactly 86 " +
                    "artwork entries.");
            }

            foreach (var item in productionItems)
            {
                if (runtimeCatalog.TryGetArtwork(item.ArtId, out _))
                {
                    throw new InvalidOperationException(
                        "Runtime catalog isolation failed for " + item.ArtId);
                }
            }
        }

        private static void CaptureMatrices(
            ProductionItem[] items,
            ConfigService configs,
            PresentationSpriteCatalog catalog,
            GameObject cardPrefab,
            Font font,
            string outputDirectory)
        {
            foreach (var batchId in BatchOrder)
            {
                var batchItems = items
                    .Where(value => value.BatchId == batchId)
                    .ToArray();
                var minions = batchItems
                    .Where(value => value.Kind == "Minion")
                    .Select(value => RequireMinion(configs, value.Id))
                    .ToArray();
                var spells = batchItems
                    .Where(value => value.Kind == "Spell")
                    .Select(value => RequireSpell(configs, value.Id))
                    .ToArray();

                if (minions.Length > 0)
                {
                    CaptureMinionPages(
                        batchId,
                        minions,
                        CardDisplayMode.Full,
                        6,
                        cardPrefab,
                        font,
                        catalog,
                        outputDirectory);
                    CaptureMinionPages(
                        batchId,
                        minions,
                        CardDisplayMode.Compact,
                        9,
                        cardPrefab,
                        font,
                        catalog,
                        outputDirectory);
                }

                if (spells.Length > 0)
                {
                    CaptureSpellPages(
                        batchId,
                        spells,
                        CardDisplayMode.Full,
                        6,
                        cardPrefab,
                        font,
                        catalog,
                        outputDirectory);
                    CaptureSpellPages(
                        batchId,
                        spells,
                        CardDisplayMode.Compact,
                        9,
                        cardPrefab,
                        font,
                        catalog,
                        outputDirectory);
                }
            }
        }

        private static void CaptureMinionPages(
            string batchId,
            MinionConfig[] minions,
            CardDisplayMode mode,
            int pageSize,
            GameObject prefab,
            Font font,
            PresentationSpriteCatalog catalog,
            string outputDirectory)
        {
            var pageCount = (minions.Length + pageSize - 1) / pageSize;
            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var page = minions
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToArray();
                var width = mode == CardDisplayMode.Full ? 240f : 160f;
                var gap = mode == CardDisplayMode.Full ? 30f : 32f;
                var startX = G2CardMatrixCapture.CenteredRowStart(
                    page.Length,
                    width,
                    gap);
                var context = G2CardMatrixCapture.CreateScene(
                    font,
                    $"PHASE 9C - {batchId.ToUpperInvariant()} - {mode}",
                    $"CONFIG-BACKED - NORMAL / GOLDEN - PAGE " +
                    $"{pageIndex + 1}/{pageCount} - EXACT BATCH 06 CATALOG");
                catalog = ReloadCatalogCandidate();
                var normalLabelY =
                    mode == CardDisplayMode.Full ? 94f : 100f;
                var normalCardY =
                    mode == CardDisplayMode.Full ? 126f : 132f;
                var goldenLabelY =
                    mode == CardDisplayMode.Full ? 510f : 430f;
                var goldenCardY =
                    mode == CardDisplayMode.Full ? 542f : 462f;

                G2CardMatrixCapture.CreateLabel(
                    context.Canvas,
                    context.AnnotationFont,
                    $"NORMAL - {page.Length}",
                    startX,
                    normalLabelY,
                    420f);
                for (var index = 0; index < page.Length; index++)
                {
                    G2CardMatrixCapture.CreatePreviewCard(
                        prefab,
                        context,
                        G2CardMatrixCapture.CreateMinionModel(
                            page[index],
                            false,
                            mode),
                        startX + index * (width + gap),
                        normalCardY,
                        catalog);
                }

                G2CardMatrixCapture.CreateLabel(
                    context.Canvas,
                    context.AnnotationFont,
                    $"GOLDEN - {page.Length}",
                    startX,
                    goldenLabelY,
                    420f);
                for (var index = 0; index < page.Length; index++)
                {
                    G2CardMatrixCapture.CreatePreviewCard(
                        prefab,
                        context,
                        G2CardMatrixCapture.CreateMinionModel(
                            page[index],
                            true,
                            mode),
                        startX + index * (width + gap),
                        goldenCardY,
                        catalog);
                }

                G2CardMatrixCapture.CreateFooter(
                    context.Canvas,
                    context.AnnotationFont,
                    "CHECK: EXACT ART, CROP, NAME, RULES, STATS, " +
                    "KEYWORDS AND GOLDEN IDENTITY");
                G2CardMatrixCapture.CaptureBothResolutions(
                    context,
                    outputDirectory,
                    $"{batchId}-{mode.ToString().ToLowerInvariant()}-" +
                    $"page-{pageIndex + 1:00}");
            }
        }

        private static void CaptureSpellPages(
            string batchId,
            SpellConfig[] spells,
            CardDisplayMode mode,
            int pageSize,
            GameObject prefab,
            Font font,
            PresentationSpriteCatalog catalog,
            string outputDirectory)
        {
            var pageCount = (spells.Length + pageSize - 1) / pageSize;
            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var page = spells
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToArray();
                var width = mode == CardDisplayMode.Full ? 240f : 160f;
                var gap = mode == CardDisplayMode.Full ? 30f : 32f;
                var startX = G2CardMatrixCapture.CenteredRowStart(
                    page.Length,
                    width,
                    gap);
                var context = G2CardMatrixCapture.CreateScene(
                    font,
                    $"PHASE 9C - {batchId.ToUpperInvariant()} - {mode}",
                    $"CONFIG-BACKED SPELLS - PAGE {pageIndex + 1}/" +
                    $"{pageCount} - EXACT BATCH 06 CATALOG");
                catalog = ReloadCatalogCandidate();

                G2CardMatrixCapture.CreateLabel(
                    context.Canvas,
                    context.AnnotationFont,
                    $"SPELLS - {page.Length}",
                    startX,
                    132f,
                    420f);
                for (var index = 0; index < page.Length; index++)
                {
                    G2CardMatrixCapture.CreatePreviewCard(
                        prefab,
                        context,
                        G2CardMatrixCapture.CreateSpellModel(
                            page[index],
                            mode),
                        startX + index * (width + gap),
                        170f,
                        catalog);
                }

                G2CardMatrixCapture.CreateFooter(
                    context.Canvas,
                    context.AnnotationFont,
                    "CHECK: EXACT ART, COST, TIER, SPELL TYPE AND " +
                    "LONG-RULE TEXT CROP");
                G2CardMatrixCapture.CaptureBothResolutions(
                    context,
                    outputDirectory,
                    $"{batchId}-{mode.ToString().ToLowerInvariant()}-" +
                    $"page-{pageIndex + 1:00}");
            }
        }

        private static void CaptureShopCrop(
            ProductionItem[] items,
            ConfigService configs,
            PresentationSpriteCatalog catalog,
            string outputDirectory)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ShopUiPrefabBuilder.ScreenPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "PF_ShopScreen is missing for Phase 9C acceptance.");
            }

            var minions = SelectCrossTierMinions(items, configs, 3);
            var spells = items
                .Where(value => value.Kind == "Spell")
                .Select(value => RequireSpell(configs, value.Id))
                .ToArray();
            var state = CreateShopState(minions, spells);
            var intendedArtIds = state.MinionOffers
                .Concat(state.BattleCards)
                .Concat(state.HandCards.VisibleSlots
                    .Where(value => value.Card != null)
                    .Select(value => value.Card))
                .Concat(new[] { state.SpellOffer })
                .Select(value => value.ArtId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var context = CreateUiCaptureContext(
                prefab,
                "Phase9CShopCrop",
                new Color(0.035f, 0.045f, 0.07f, 1f),
                catalog);
            var view = context.Root.GetComponent<ShopScreenView>();

            context.Canvas.sizeDelta = new Vector2(1920f, 1080f);
            view.Render(state);
            AssertExactRenderedCards(
                context.Root,
                intendedArtIds,
                "shop crop");
            G2CardMatrixCapture.CaptureFrame(
                context.Camera,
                context.Canvas,
                1920,
                1080,
                Path.Combine(
                    outputDirectory,
                    "shop-production-crop-1920x1080.png"));

            context.Canvas.sizeDelta = new Vector2(1920f, 1200f);
            view.Render(state);
            AssertExactRenderedCards(
                context.Root,
                intendedArtIds,
                "shop crop");
            G2CardMatrixCapture.CaptureFrame(
                context.Camera,
                context.Canvas,
                1920,
                1200,
                Path.Combine(
                    outputDirectory,
                    "shop-production-crop-1920x1200.png"));
        }

        private static void CaptureBattleCrop(
            ProductionItem[] items,
            ConfigService configs,
            PresentationSpriteCatalog catalog,
            string outputDirectory)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                BattleUiPrefabBuilder.ScreenPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "PF_BattleScreen is missing for Phase 9C acceptance.");
            }

            var minions = SelectCrossTierMinions(items, configs, 2);
            if (minions.Length != 10)
            {
                throw new InvalidOperationException(
                    "Battle crop requires two production minions per tier.");
            }
            var state = CreateBattleState(minions);
            var intendedArtIds = state.PlayerCards
                .Concat(state.EnemyCards)
                .Where(value => value != null)
                .Select(value => value.ArtId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var context = CreateUiCaptureContext(
                prefab,
                "Phase9CBattleCrop",
                new Color(0.035f, 0.045f, 0.07f, 1f),
                catalog);
            var view = context.Root.GetComponent<BattleScreenView>();

            context.Canvas.sizeDelta = new Vector2(1920f, 1080f);
            view.Render(state);
            AssertExactRenderedStandees(
                context.Root,
                intendedArtIds,
                "battle crop");
            G2CardMatrixCapture.CaptureFrame(
                context.Camera,
                context.Canvas,
                1920,
                1080,
                Path.Combine(
                    outputDirectory,
                    "battle-production-crop-1920x1080.png"));

            context.Canvas.sizeDelta = new Vector2(1920f, 1200f);
            view.Render(state);
            AssertExactRenderedStandees(
                context.Root,
                intendedArtIds,
                "battle crop");
            G2CardMatrixCapture.CaptureFrame(
                context.Camera,
                context.Canvas,
                1920,
                1200,
                Path.Combine(
                    outputDirectory,
                    "battle-production-crop-1920x1200.png"));
        }

        private static MinionConfig[] SelectCrossTierMinions(
            ProductionItem[] items,
            ConfigService configs,
            int perTier)
        {
            return items
                .Where(value => value.Kind == "Minion")
                .Select(value => RequireMinion(configs, value.Id))
                .GroupBy(value => value.Tier)
                .OrderBy(value => value.Key)
                .SelectMany(value => value.Take(perTier))
                .ToArray();
        }

        private static ShopScreenState CreateShopState(
            MinionConfig[] minions,
            SpellConfig[] spells)
        {
            if (minions.Length < 14 || spells.Length < 2)
            {
                throw new InvalidOperationException(
                    "Shop crop requires fourteen minions and two spells.");
            }

            var offers = minions.Take(4)
                .Select((value, index) =>
                    CreateMinionModel(
                        value,
                        index == 2,
                        CardDisplayMode.Full,
                        "shop_offer"))
                .ToArray();
            var battle = minions.Skip(4).Take(5)
                .Select((value, index) =>
                    CreateMinionModel(
                        value,
                        index == 1,
                        CardDisplayMode.Compact,
                        "shop_battle"))
                .ToArray();
            battle[1].IsSelected = true;
            var hand = minions.Skip(9).Take(4)
                .Select((value, index) =>
                    CreateMinionModel(
                        value,
                        index == 3,
                        CardDisplayMode.Compact,
                        "shop_hand"))
                .Concat(new[]
                {
                    CreateSpellModel(
                        spells[1],
                        CardDisplayMode.Compact,
                        "shop_hand")
                })
                .ToArray();
            var spellOffer = CreateSpellModel(
                spells[0],
                CardDisplayMode.Full,
                "shop_offer");

            return new ShopScreenState
            {
                Round = 9,
                Gold = 10,
                TavernTier = 5,
                UpgradeCost = 0,
                RefreshCount = 3,
                FreeRefreshes = 1,
                IsShopOpen = true,
                MinionOffers = offers,
                SpellOffer = spellOffer,
                BattleCards = battle,
                HandCards = new HandCardsState
                {
                    Count = hand.Length,
                    Limit = 5,
                    PageSize = 5,
                    PageIndex = 0,
                    PageCount = 1,
                    VisibleSlots = hand.Select((card, index) =>
                        new HandCardSlotState
                        {
                            SlotIndex = index,
                            Card = card
                        }).ToArray()
                },
                Buttons = new ShopButtonStates
                {
                    Refresh = Action("Refresh (free 1)", true),
                    Freeze = Action("Freeze", true),
                    Upgrade = Action("Tavern max", false),
                    Sell = Action("Sell (1 gold)", true),
                    EndShop = Action("Enter battle", true)
                },
                DetailPanel = new CardDetailPanelState
                {
                    Card = battle[1],
                    Location = ShopCardLocation.Battle,
                    SlotIndex = 1,
                    Statuses = new[]
                    {
                        new CardDetailStatusState
                        {
                            Type = CardDetailStatusType.PermanentShield,
                            Label = "Phase 9C",
                            Description = "Batch 06 exact-art crop validation"
                        }
                    }
                },
                StatusMessage = "Phase 9C production candidate - Runtime unchanged"
            };
        }

        private static BattleScreenState CreateBattleState(
            MinionConfig[] minions)
        {
            var state = new BattleScreenState
            {
                Title = "Phase 9C - Production Battle Crop",
                Status = "Batch 06 exact artwork - normal / golden",
                RoundText = "Unity batch release",
                LogText = string.Join("\n", new[]
                {
                    "Ten production minions sampled across tiers 1-5.",
                    "Standee portraits must remain centered and uncropped.",
                    "Runtime catalog remains unchanged."
                }),
                Start = BattleButton("Start battle", false, false),
                Speed = BattleButton("Speed 2x", true, true),
                Skip = BattleButton("Skip", true, true),
                Preset = BattleButton("Preset", false, false),
                Reset = BattleButton("Reset", false, false),
                Return = BattleButton("Return", false, false)
            };

            for (var index = 0; index < 5; index++)
            {
                state.PlayerCards[index] = CreateMinionModel(
                    minions[index * 2],
                    index % 2 == 1,
                    CardDisplayMode.Compact,
                    "battle_player");
                state.EnemyCards[index] = CreateMinionModel(
                    minions[index * 2 + 1],
                    index % 2 == 0,
                    CardDisplayMode.Compact,
                    "battle_enemy");
            }

            return state;
        }

        private static CardViewModel CreateMinionModel(
            MinionConfig config,
            bool golden,
            CardDisplayMode mode,
            string prefix)
        {
            var model = G2CardMatrixCapture.CreateMinionModel(
                config,
                golden,
                mode);
            model.InstanceId =
                $"{prefix}_{config.Id}_{(golden ? "golden" : "normal")}";
            return model;
        }

        private static CardViewModel CreateSpellModel(
            SpellConfig config,
            CardDisplayMode mode,
            string prefix)
        {
            var model = G2CardMatrixCapture.CreateSpellModel(config, mode);
            model.InstanceId = $"{prefix}_{config.Id}";
            return model;
        }

        private static ShopActionButtonState Action(
            string text,
            bool interactable)
        {
            return new ShopActionButtonState
            {
                Text = text,
                IsVisible = true,
                IsInteractable = interactable
            };
        }

        private static BattleButtonState BattleButton(
            string label,
            bool visible,
            bool interactable)
        {
            return new BattleButtonState
            {
                Label = label,
                IsVisible = visible,
                IsInteractable = interactable
            };
        }

        private static UiCaptureContext CreateUiCaptureContext(
            GameObject prefab,
            string name,
            Color background,
            PresentationSpriteCatalog catalog)
        {
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            catalog = ReloadCatalogCandidate();
            var cameraObject = new GameObject(name + "Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate " + prefab.name);
            }

            root.name = name;
            var canvas = root.GetComponent<Canvas>();
            var canvasRect = root.GetComponent<RectTransform>();
            if (canvas == null || canvasRect == null)
            {
                throw new InvalidOperationException(
                    prefab.name + " must have a root Canvas and RectTransform.");
            }
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 1;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;
            AssignCatalog(root, catalog);
            var shopView = root.GetComponent<ShopScreenView>();
            if (shopView != null)
            {
                OverrideSpawnPrefab(
                    shopView,
                    "cardPrefab",
                    catalog);
            }
            var battleView = root.GetComponent<BattleScreenView>();
            if (battleView != null)
            {
                OverrideSpawnPrefab(
                    battleView,
                    "standeePrefab",
                    catalog);
            }
            return new UiCaptureContext(camera, canvasRect, root);
        }

        private static void OverrideSpawnPrefab(
            Component owner,
            string propertyName,
            PresentationSpriteCatalog catalog)
        {
            var serializedOwner = new SerializedObject(owner);
            var property = serializedOwner.FindProperty(propertyName);
            var sourcePrefab = property?.objectReferenceValue as GameObject;
            if (sourcePrefab == null)
            {
                throw new InvalidOperationException(
                    owner.GetType().Name + "." + propertyName +
                    " is missing.");
            }

            var transientPrefab = UnityEngine.Object.Instantiate(sourcePrefab);
            transientPrefab.name =
                sourcePrefab.name + "_Phase9CCatalogOverride";
            AssignCatalog(transientPrefab, catalog);
            property.objectReferenceValue = transientPrefab;
            serializedOwner.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignCatalog(
            GameObject root,
            PresentationSpriteCatalog catalog)
        {
            foreach (var card in root.GetComponentsInChildren<CardView>(true))
            {
                AssignCatalog(card, catalog);
            }
            foreach (var standee in root.GetComponentsInChildren<
                         BattleStandeeView>(true))
            {
                AssignCatalog(standee, catalog);
            }
        }

        private static void AssignCatalog(
            Component component,
            PresentationSpriteCatalog catalog)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty("spriteCatalog");
            if (property == null)
            {
                throw new InvalidOperationException(
                    component.GetType().Name +
                    " has no serialized spriteCatalog field.");
            }
            property.objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertExactRenderedCards(
            GameObject root,
            string[] expectedArtIds,
            string context)
        {
            var rendered = root.GetComponentsInChildren<CardView>(true)
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value.LastArtId))
                .ToArray();
            var renderedIds = new HashSet<string>(
                rendered.Select(value => value.LastArtId),
                StringComparer.Ordinal);
            if (rendered.Any(value =>
                    value.LastArtworkResolution != ArtworkResolution.Exact) ||
                expectedArtIds.Any(value => !renderedIds.Contains(value)))
            {
                throw new InvalidOperationException(
                    $"Phase 9C {context} rendered fallback or missing artwork.");
            }
        }

        private static void AssertExactRenderedStandees(
            GameObject root,
            string[] expectedArtIds,
            string context)
        {
            var rendered = root.GetComponentsInChildren<
                    BattleStandeeView>(true)
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value.LastArtId))
                .ToArray();
            var renderedIds = new HashSet<string>(
                rendered.Select(value => value.LastArtId),
                StringComparer.Ordinal);
            if (rendered.Any(value =>
                    value.LastArtworkResolution != ArtworkResolution.Exact) ||
                expectedArtIds.Any(value => !renderedIds.Contains(value)))
            {
                throw new InvalidOperationException(
                    $"Phase 9C {context} rendered fallback or missing artwork.");
            }
        }

        private static MinionConfig RequireMinion(
            ConfigService configs,
            string id)
        {
            if (!configs.MinionsById.TryGetValue(id, out var config) ||
                config == null)
            {
                throw new InvalidOperationException(
                    "Phase 9C minion config is missing: " + id);
            }
            return config;
        }

        private static PresentationSpriteCatalog ReloadCatalogCandidate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<
                PresentationSpriteCatalog>(
                LightStorybookProductionBatch6Builder.CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "The final Batch 06 catalog could not be reloaded.");
            }
            return catalog;
        }

        private static SpellConfig RequireSpell(
            ConfigService configs,
            string id)
        {
            if (!configs.SpellsById.TryGetValue(id, out var config) ||
                config == null)
            {
                throw new InvalidOperationException(
                    "Phase 9C spell config is missing: " + id);
            }
            return config;
        }

        private static void WriteCaptureIndex(
            string releaseDirectory,
            string screenshotDirectory,
            PresentationSpriteCatalog catalog,
            ProductionItem[] productionItems)
        {
            var screenshotPaths = Directory.GetFiles(
                    screenshotDirectory,
                    "*.png",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (screenshotPaths.Length != ExpectedScreenshotCount)
            {
                throw new InvalidOperationException(
                    $"Phase 9C acceptance requires {ExpectedScreenshotCount} " +
                    $"screenshots; found {screenshotPaths.Length}.");
            }

            var index = new CaptureIndex
            {
                Version = "0.3.3",
                Status = "UNITY_CAPTURE_COMPLETE",
                GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                UnityVersion = Application.unityVersion,
                CatalogPath =
                    LightStorybookProductionBatch6Builder.CatalogPath,
                CatalogGuid = AssetDatabase.AssetPathToGUID(
                    LightStorybookProductionBatch6Builder.CatalogPath),
                RuntimeCatalogPath = RuntimeCatalogPath,
                RuntimeCatalogGuid = AssetDatabase.AssetPathToGUID(
                    RuntimeCatalogPath),
                CatalogEntryCount = ExpectedCatalogEntryCount,
                ConfiguredArtworkCount = ExpectedConfiguredArtworkCount,
                ProductionArtworkCount = productionItems.Length,
                ScreenshotCount = screenshotPaths.Length,
                Screenshots = screenshotPaths.Select(value =>
                    new CaptureFile
                    {
                        Path = "screenshots/" + Path.GetFileName(value),
                        Sha256 = ComputeSha256(value),
                        Bytes = new FileInfo(value).Length
                    }).ToArray()
            };
            var indexPath = Path.Combine(
                releaseDirectory,
                "capture-index.json");
            File.WriteAllText(
                indexPath,
                JsonConvert.SerializeObject(index, Formatting.Indented) +
                Environment.NewLine,
                new UTF8Encoding(false));
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

        private static string ResolveRepositoryRoot()
        {
            return Directory.GetParent(
                Directory.GetParent(Application.dataPath).FullName).FullName;
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

            [JsonProperty("artId")]
            public string ArtId { get; set; }

            [JsonProperty("batchId")]
            public string BatchId { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }
        }

        [Serializable]
        private sealed class CaptureIndex
        {
            [JsonProperty("version")]
            public string Version { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("generatedAtUtc")]
            public string GeneratedAtUtc { get; set; }

            [JsonProperty("unityVersion")]
            public string UnityVersion { get; set; }

            [JsonProperty("catalogPath")]
            public string CatalogPath { get; set; }

            [JsonProperty("catalogGuid")]
            public string CatalogGuid { get; set; }

            [JsonProperty("runtimeCatalogPath")]
            public string RuntimeCatalogPath { get; set; }

            [JsonProperty("runtimeCatalogGuid")]
            public string RuntimeCatalogGuid { get; set; }

            [JsonProperty("catalogEntryCount")]
            public int CatalogEntryCount { get; set; }

            [JsonProperty("configuredArtworkCount")]
            public int ConfiguredArtworkCount { get; set; }

            [JsonProperty("productionArtworkCount")]
            public int ProductionArtworkCount { get; set; }

            [JsonProperty("screenshotCount")]
            public int ScreenshotCount { get; set; }

            [JsonProperty("screenshots")]
            public CaptureFile[] Screenshots { get; set; } =
                Array.Empty<CaptureFile>();
        }

        [Serializable]
        private sealed class CaptureFile
        {
            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("sha256")]
            public string Sha256 { get; set; }

            [JsonProperty("bytes")]
            public long Bytes { get; set; }
        }

        private sealed class UiCaptureContext
        {
            public UiCaptureContext(
                Camera camera,
                RectTransform canvas,
                GameObject root)
            {
                Camera = camera;
                Canvas = canvas;
                Root = root;
            }

            public Camera Camera { get; }
            public RectTransform Canvas { get; }
            public GameObject Root { get; }
        }
    }
}
