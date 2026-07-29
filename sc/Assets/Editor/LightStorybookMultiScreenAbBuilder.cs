using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;
using SpireChess.UI;
using SpireChess.UI.MainMenu;
using SpireChess.UI.Run;
using SpireChess.UI.Shop;
using SpireChess.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SpireChess.UI.Battle;

namespace SpireChess.Editor
{
    public static class LightStorybookMultiScreenAbBuilder
    {
        private const string PrefabRoot =
            "Assets/Prefabs/UI/Calibration/LightStorybook";
        private const string SceneRoot =
            "Assets/Scenes/Calibration/LightStorybook";

        private const string MainMenuPrefabPath =
            PrefabRoot + "/PF_MainMenuScreen_LightStorybook.prefab";
        private const string ShopPrefabPath =
            PrefabRoot + "/PF_ShopScreen_LightStorybook.prefab";
        private const string RunPrefabPath =
            PrefabRoot + "/PF_RunScreen_LightStorybook.prefab";
        private const string RunNodePrefabPath =
            PrefabRoot + "/PF_RunMapNode_LightStorybook.prefab";
        private const string RunEdgePrefabPath =
            PrefabRoot + "/PF_RunMapEdge_LightStorybook.prefab";
        private const string RunRelicPrefabPath =
            PrefabRoot + "/PF_RunRelicEntry_LightStorybook.prefab";
        private const string RunChoicePrefabPath =
            PrefabRoot + "/PF_RunChoiceOption_LightStorybook.prefab";

        private static readonly string[] FormalArtIds =
        {
            "placeholder_card_forge_soul_shield_squire",
            "placeholder_card_resonance_bell_guard",
            "placeholder_card_undying_furnace_king",
            "placeholder_card_young_deer_spirit",
            "placeholder_card_rootbound_soul_guide",
            "placeholder_card_ancient_mountain_spirit",
            "placeholder_card_glimmer_mage",
            "placeholder_card_fate_track_recorder",
            "placeholder_card_moonwheel_dispatcher",
            "placeholder_card_traveling_physician",
            "placeholder_card_old_tower_guide",
            "placeholder_card_mirrorsteel_duelist",
            "placeholder_spell_temporary_ward",
            "placeholder_spell_starlight_rebate",
            "placeholder_spell_legendary_recruitment"
        };

        [MenuItem("Spire Chess/UI/Build Light Storybook Multi-Screen A-B")]
        public static void Build()
        {
            LightStorybookAbBuilder.Build();
            EnsureFolder("Assets/Prefabs/UI/Calibration", "LightStorybook");
            EnsureFolder("Assets/Scenes/Calibration", "LightStorybook");

            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(
                LightStorybookAbBuilder.ThemePath);
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookAbBuilder.CatalogPath);
            if (theme == null || catalog == null)
            {
                throw new InvalidOperationException(
                    "The Light Storybook theme and catalog are required.");
            }

            var mainMenu = CopyAndStylePrefab(
                "Assets/Prefabs/UI/MainMenu/PF_MainMenuScreen.prefab",
                MainMenuPrefabPath,
                theme,
                catalog);
            var formalCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                LightStorybookFormalCatalogBuilder.CardPrefabPath);
            if (formalCardPrefab == null)
            {
                throw new InvalidOperationException(
                    "Build the Light Storybook formal catalog before the " +
                    "multi-screen validation scenes.");
            }
            var shop = BuildShopPrefab(
                theme,
                catalog,
                formalCardPrefab);
            var run = BuildRunPrefab(theme, catalog);

            BuildMainMenuScene(mainMenu);
            BuildShopScene(shop, catalog);
            BuildRunScene(run);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[LightStorybook] Built Main Menu, Shop and Run A/B scenes.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void ValidateGeneratedScenesFromCommandLine()
        {
            var formalCatalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookFormalCatalogBuilder.CatalogPath);
            var lightCatalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookAbBuilder.CatalogPath);
            if (formalCatalog == null || lightCatalog == null)
            {
                throw new InvalidOperationException(
                    "The Light Storybook validation catalogs are missing.");
            }

            var formalSprites = new HashSet<Sprite>();
            foreach (var artId in FormalArtIds)
            {
                if (!formalCatalog.TryGetArtwork(
                        artId,
                        out var sprite) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "The formal catalog is missing exact artwork for " +
                        artId);
                }
                formalSprites.Add(sprite);
            }
            if (formalSprites.Count != FormalArtIds.Length)
            {
                throw new InvalidOperationException(
                    "The formal catalog artwork sprites must be unique.");
            }

            ValidateSavedShopScene(formalCatalog, formalSprites);
            ValidateSavedBattleScene(lightCatalog, formalSprites);
            Debug.Log(
                "[LightStorybook] Reload validation passed: Shop=15 exact " +
                "cards, Battle=10 exact standees.");
        }

        public static void OpenShopForValidationFromCommandLine()
        {
            ValidateGeneratedScenesFromCommandLine();
            EditorSceneManager.OpenScene(
                SceneRoot + "/ShopLightStorybookAB.unity",
                OpenSceneMode.Single);
        }

        public static void OpenInteractiveBattleForValidationFromCommandLine()
        {
            ValidateGeneratedScenesFromCommandLine();
            EditorSceneManager.OpenScene(
                LightStorybookAbBuilder.ScenePath,
                OpenSceneMode.Single);
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = true;
                }
            };
        }

        private static void ValidateSavedShopScene(
            PresentationSpriteCatalog expectedCatalog,
            ISet<Sprite> formalSprites)
        {
            var scene = EditorSceneManager.OpenScene(
                SceneRoot + "/ShopLightStorybookAB.unity",
                OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            ValidateSceneCamera(roots, "shop");
            var controllers = roots
                .SelectMany(value =>
                    value.GetComponentsInChildren<ShopTestController>(true))
                .ToArray();
            if (controllers.Length != 0)
            {
                throw new InvalidOperationException(
                    "The static Light Storybook shop scene must not depend " +
                    "on ShopTestController.");
            }

            var cards = roots
                .SelectMany(value =>
                    value.GetComponentsInChildren<CardView>(true))
                .ToArray();
            if (cards.Length != 15)
            {
                throw new InvalidOperationException(
                    "The saved Light Storybook shop scene must contain " +
                    $"15 cards, found={cards.Length}.");
            }

            var renderedSprites = new HashSet<Sprite>();
            foreach (var card in cards)
            {
                var serialized = new SerializedObject(card);
                if (serialized.FindProperty("spriteCatalog")
                        .objectReferenceValue != expectedCatalog)
                {
                    throw new InvalidOperationException(
                        "A saved Light Storybook shop card uses the wrong " +
                        "sprite catalog.");
                }

                var artwork = serialized.FindProperty("artwork")
                    .objectReferenceValue as Image;
                var costBadge = serialized.FindProperty("costBadge")
                    .objectReferenceValue as Image;
                if (artwork?.sprite == null ||
                    !formalSprites.Contains(artwork.sprite))
                {
                    throw new InvalidOperationException(
                        "A saved Light Storybook shop card is not using a " +
                        "formal artwork sprite.");
                }
                if (costBadge == null || !costBadge.gameObject.activeSelf)
                {
                    throw new InvalidOperationException(
                        "A saved Light Storybook shop card hides its cost.");
                }
                renderedSprites.Add(artwork.sprite);
            }

            if (renderedSprites.Count != 15)
            {
                throw new InvalidOperationException(
                    "The saved Light Storybook shop scene must preserve all " +
                    "15 unique formal artworks.");
            }
        }

        private static void ValidateSavedBattleScene(
            PresentationSpriteCatalog expectedCatalog,
            ISet<Sprite> formalSprites)
        {
            var scene = EditorSceneManager.OpenScene(
                LightStorybookAbBuilder.ScenePath,
                OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            ValidateSceneCamera(roots, "battle");
            var controllers = roots
                .SelectMany(value =>
                    value.GetComponentsInChildren<BattleTestController>(true))
                .ToArray();
            if (controllers.Length != 1)
            {
                throw new InvalidOperationException(
                    "The interactive Light Storybook battle scene must have " +
                    $"exactly one BattleTestController, found={controllers.Length}.");
            }

            var controllerSerialized = new SerializedObject(controllers[0]);
            var screenReference = controllerSerialized.FindProperty("screenView")
                .objectReferenceValue as BattleScreenView;
            var presetName = controllerSerialized
                .FindProperty("validationPresetName").stringValue;
            var playerIds = controllerSerialized
                .FindProperty("validationPlayerIds");
            var enemyIds = controllerSerialized
                .FindProperty("validationEnemyIds");
            if (screenReference == null ||
                string.IsNullOrWhiteSpace(presetName) ||
                playerIds.arraySize != 5 ||
                enemyIds.arraySize != 5 ||
                !controllers[0].IsUsingValidationPreset ||
                controllers[0].ActivePresetName != presetName)
            {
                throw new InvalidOperationException(
                    "The interactive Light Storybook battle controller has " +
                    "an incomplete validation preset.");
            }

            var standees = roots
                .SelectMany(value =>
                    value.GetComponentsInChildren<BattleStandeeView>(true))
                .ToArray();
            if (standees.Length != 10)
            {
                throw new InvalidOperationException(
                    "The saved Light Storybook battle scene must contain " +
                    $"10 standees, found={standees.Length}.");
            }

            var renderedSprites = new HashSet<Sprite>();
            var visibleShieldCount = 0;
            var visibleShieldUnderlayCount = 0;
            foreach (var standee in standees)
            {
                var serialized = new SerializedObject(standee);
                if (serialized.FindProperty("spriteCatalog")
                        .objectReferenceValue != expectedCatalog)
                {
                    throw new InvalidOperationException(
                        "A saved Light Storybook battle standee uses the " +
                        "wrong sprite catalog.");
                }

                var portrait = serialized.FindProperty("portrait")
                    .objectReferenceValue as Image;
                var fitter = serialized.FindProperty("portraitAspectFitter")
                    .objectReferenceValue as AspectRatioFitter;
                var shieldUnderlay = serialized
                    .FindProperty("shieldContrastUnderlay")
                    .objectReferenceValue as Image;
                var shield = serialized.FindProperty("shieldOverlay")
                    .objectReferenceValue as Image;
                if (portrait?.sprite == null ||
                    !formalSprites.Contains(portrait.sprite))
                {
                    throw new InvalidOperationException(
                        "A saved Light Storybook battle standee is not using " +
                        "a formal artwork sprite.");
                }
                if (fitter == null ||
                    fitter.aspectMode !=
                    AspectRatioFitter.AspectMode.EnvelopeParent)
                {
                    throw new InvalidOperationException(
                        "A saved Light Storybook battle portrait is not " +
                        "using centered cover cropping.");
                }
                if (shieldUnderlay == null ||
                    shieldUnderlay.sprite !=
                    expectedCatalog.BattleStandeeShieldOverlay ||
                    shield == null ||
                    shield.sprite !=
                    expectedCatalog.BattleStandeeShieldOverlay ||
                    shieldUnderlay.transform.GetSiblingIndex() >=
                    shield.transform.GetSiblingIndex() ||
                    Luminance(shieldUnderlay.color) >= 0.30f ||
                    shieldUnderlay.color.a < 0.85f ||
                    shield.color.b - shield.color.r < 0.50f)
                {
                    throw new InvalidOperationException(
                        "A saved Light Storybook shield is missing its " +
                        "dark cyan contrast underlay or saturated blue glow.");
                }
                var underlaySerialized =
                    new SerializedObject(shieldUnderlay);
                if (underlaySerialized.FindProperty("m_Material")
                        .objectReferenceValue != null)
                {
                    throw new InvalidOperationException(
                        "The Light Storybook shield contrast underlay must " +
                        "use non-additive UI blending.");
                }
                if (shieldUnderlay.gameObject.activeSelf !=
                    shield.gameObject.activeSelf)
                {
                    throw new InvalidOperationException(
                        "The Light Storybook shield glow and contrast " +
                        "underlay visibility are out of sync.");
                }
                if (shield != null && shield.gameObject.activeSelf)
                {
                    visibleShieldCount++;
                }
                if (shieldUnderlay.gameObject.activeSelf)
                {
                    visibleShieldUnderlayCount++;
                }
                renderedSprites.Add(portrait.sprite);
            }

            if (renderedSprites.Count != 10)
            {
                throw new InvalidOperationException(
                    "The saved Light Storybook battle scene must preserve " +
                    "10 unique formal artworks.");
            }
            if (visibleShieldCount != 2)
            {
                throw new InvalidOperationException(
                    "The saved Light Storybook battle scene must show " +
                    $"2 shield overlays, found={visibleShieldCount}.");
            }
            if (visibleShieldUnderlayCount != 2)
            {
                throw new InvalidOperationException(
                    "The saved Light Storybook battle scene must show " +
                    "2 shield contrast underlays, found=" +
                    visibleShieldUnderlayCount + ".");
            }
        }

        private static GameObject BuildRunPrefab(
            PresentationTheme theme,
            PresentationSpriteCatalog catalog)
        {
            var node = CopyAndStylePrefab(
                RunUiPrefabBuilder.NodePrefabPath,
                RunNodePrefabPath,
                theme,
                catalog);
            var edge = CopyAndStylePrefab(
                RunUiPrefabBuilder.EdgePrefabPath,
                RunEdgePrefabPath,
                theme,
                catalog);
            var relic = CopyAndStylePrefab(
                RunUiPrefabBuilder.RelicPrefabPath,
                RunRelicPrefabPath,
                theme,
                catalog);
            var choice = CopyAndStylePrefab(
                RunUiPrefabBuilder.ChoicePrefabPath,
                RunChoicePrefabPath,
                theme,
                catalog);

            CopyAssetReplacing(
                RunUiPrefabBuilder.ScreenPrefabPath,
                RunPrefabPath);
            var root = PrefabUtility.LoadPrefabContents(RunPrefabPath);
            ReplacePresentationReferences(root, theme, catalog);
            ApplyLightPalette(root, theme);
            var view = root.GetComponent<RunScreenView>();
            var serialized = new SerializedObject(view);
            SetReference(serialized, "mapNodePrefab", node);
            SetReference(serialized, "mapEdgePrefab", edge);
            SetReference(serialized, "relicEntryPrefab", relic);
            SetReference(serialized, "choiceOptionPrefab", choice);
            serialized.FindProperty("suppressProductionBackdrop").boolValue =
                true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, RunPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(RunPrefabPath);
        }

        private static GameObject BuildShopPrefab(
            PresentationTheme theme,
            PresentationSpriteCatalog catalog,
            GameObject formalCardPrefab)
        {
            CopyAssetReplacing(
                ShopUiPrefabBuilder.ScreenPrefabPath,
                ShopPrefabPath);
            var root = PrefabUtility.LoadPrefabContents(ShopPrefabPath);
            try
            {
                ReplacePresentationReferences(root, theme, catalog);
                ApplyLightPalette(root, theme);

                var view = root.GetComponent<ShopScreenView>();
                if (view == null)
                {
                    throw new InvalidOperationException(
                        "The Light Storybook shop prefab has no " +
                        "ShopScreenView.");
                }
                var viewSerialized = new SerializedObject(view);
                SetReference(
                    viewSerialized,
                    "cardPrefab",
                    formalCardPrefab);
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                foreach (var choice in
                         root.GetComponentsInChildren<ChoiceOverlayView>(true))
                {
                    var choiceSerialized = new SerializedObject(choice);
                    SetReference(
                        choiceSerialized,
                        "cardPrefab",
                        formalCardPrefab);
                    choiceSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, ShopPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefabPath);
        }

        private static GameObject CopyAndStylePrefab(
            string source,
            string destination,
            PresentationTheme theme,
            PresentationSpriteCatalog catalog)
        {
            CopyAssetReplacing(source, destination);
            var root = PrefabUtility.LoadPrefabContents(destination);
            ReplacePresentationReferences(root, theme, catalog);
            ApplyLightPalette(root, theme);
            PrefabUtility.SaveAsPrefabAsset(root, destination);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(destination);
        }

        private static void ApplyLightPalette(
            GameObject root,
            PresentationTheme theme)
        {
            foreach (var backdrop in
                     root.GetComponentsInChildren<PresentationBackdropGraphic>(
                         true))
            {
                var serialized = new SerializedObject(backdrop);
                serialized.FindProperty("suppressProductionArtwork").boolValue =
                    true;
                serialized.FindProperty("topColor").colorValue =
                    new Color(0.72f, 0.82f, 0.82f, 1f);
                serialized.FindProperty("bottomColor").colorValue =
                    theme.ScreenBackground;
                serialized.FindProperty("accentColor").colorValue =
                    theme.Accent;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.color.a < 0.05f || image.sprite != null)
                {
                    continue;
                }
                var name = image.name.ToLowerInvariant();
                if (name.Contains("blocker") ||
                    name.Contains("scrim") ||
                    name.Contains("overlay"))
                {
                    image.color = theme.ModalScrim;
                }
                else if (name.Contains("background") ||
                         name.Contains("safearea"))
                {
                    image.color = theme.ScreenBackground;
                }
                else if (name.Contains("button"))
                {
                    image.color = name.Contains("delete") ||
                                  name.Contains("danger")
                        ? new Color(0.66f, 0.27f, 0.23f, 1f)
                        : theme.ButtonNormal;
                }
                else if (Luminance(image.color) < 0.42f)
                {
                    image.color = name.Contains("panel") ||
                                  name.Contains("card") ||
                                  name.Contains("dialog")
                        ? theme.PanelBackground
                        : theme.PanelRaised;
                }
            }

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                if (Luminance(text.color) < 0.78f)
                {
                    text.color = theme.TextPrimary;
                }
            }

            foreach (var outline in
                     root.GetComponentsInChildren<Outline>(true))
            {
                outline.effectColor = theme.PanelBorder;
            }
        }

        private static void BuildMainMenuScene(GameObject prefab)
        {
            var scene = CreateSceneWithScreen(
                prefab,
                "PF_MainMenuScreen_LightStorybook");
            var controllerObject = new GameObject(
                "MainMenuController",
                typeof(MainMenuController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            Bind(
                controllerObject.GetComponent<MainMenuController>(),
                "screenView",
                scene.GetRootGameObjects()[0]
                    .GetComponent<MainMenuScreenView>());
            SaveScene(scene, SceneRoot + "/MainMenuLightStorybookAB.unity");
        }

        private static void BuildShopScene(
            GameObject prefab,
            PresentationSpriteCatalog catalog)
        {
            var scene = CreateSceneWithScreen(
                prefab,
                "PF_ShopScreen_LightStorybook");
            var view = scene.GetRootGameObjects()[0]
                .GetComponent<ShopScreenView>();
            var state = CreateShopPreviewState(catalog);
            view.Render(state);
            if (view.RenderedCardCount != 15)
            {
                throw new InvalidOperationException(
                    "The Light Storybook shop preview must render exactly " +
                    $"15 cards, rendered={view.RenderedCardCount}.");
            }
            SaveScene(scene, SceneRoot + "/ShopLightStorybookAB.unity");
        }

        private static ShopScreenState CreateShopPreviewState(
            PresentationSpriteCatalog catalog)
        {
            var configs = LoadPreviewConfigs();
            var offers = new[]
            {
                CreateMinion(
                    configs,
                    "forge_soul_shield_squire",
                    CardDisplayMode.Full,
                    "offer-0"),
                CreateMinion(
                    configs,
                    "young_deer_spirit",
                    CardDisplayMode.Full,
                    "offer-1"),
                CreateMinion(
                    configs,
                    "glimmer_mage",
                    CardDisplayMode.Full,
                    "offer-2"),
                CreateMinion(
                    configs,
                    "traveling_physician",
                    CardDisplayMode.Full,
                    "offer-3")
            };
            var spellOffer = CreateSpell(
                configs,
                "legendary_recruitment",
                CardDisplayMode.Full,
                "spell-offer");
            var battle = new[]
            {
                CreateMinion(
                    configs,
                    "resonance_bell_guard",
                    CardDisplayMode.Compact,
                    "battle-0",
                    true),
                CreateMinion(
                    configs,
                    "rootbound_soul_guide",
                    CardDisplayMode.Compact,
                    "battle-1"),
                CreateMinion(
                    configs,
                    "fate_track_recorder",
                    CardDisplayMode.Compact,
                    "battle-2"),
                CreateMinion(
                    configs,
                    "old_tower_guide",
                    CardDisplayMode.Compact,
                    "battle-3"),
                CreateMinion(
                    configs,
                    "mirrorsteel_duelist",
                    CardDisplayMode.Compact,
                    "battle-4")
            };
            var handCards = new[]
            {
                CreateMinion(
                    configs,
                    "undying_furnace_king",
                    CardDisplayMode.Compact,
                    "hand-0"),
                CreateMinion(
                    configs,
                    "ancient_mountain_spirit",
                    CardDisplayMode.Compact,
                    "hand-1"),
                CreateMinion(
                    configs,
                    "moonwheel_dispatcher",
                    CardDisplayMode.Compact,
                    "hand-2"),
                CreateSpell(
                    configs,
                    "temporary_ward",
                    CardDisplayMode.Compact,
                    "hand-3"),
                CreateSpell(
                    configs,
                    "starlight_rebate",
                    CardDisplayMode.Compact,
                    "hand-4")
            };

            ValidateExactArtwork(
                catalog,
                offers
                    .Concat(battle)
                    .Concat(handCards)
                    .Concat(new[] { spellOffer }));

            return new ShopScreenState
            {
                Round = 3,
                Gold = 8,
                TavernTier = 5,
                UpgradeCost = 5,
                RefreshCount = 2,
                FreeRefreshes = 1,
                IsShopOpen = true,
                MinionOffers = offers,
                SpellOffer = spellOffer,
                BattleCards = battle,
                HandCards = new HandCardsState
                {
                    Count = handCards.Length,
                    Limit = 5,
                    PageSize = 5,
                    PageIndex = 0,
                    PageCount = 1,
                    VisibleSlots = handCards
                        .Select((card, index) => new HandCardSlotState
                        {
                            SlotIndex = index,
                            Card = card
                        })
                        .ToArray()
                },
                Buttons = new ShopButtonStates
                {
                    Refresh = Action("刷新（免费 1 次）"),
                    Freeze = Action("冻结"),
                    Upgrade = Action("升级酒馆（5 金币）"),
                    Sell = Action("出售（1 金币）"),
                    EndShop = Action("锁定阵容并进入战斗")
                },
                DetailPanel = new CardDetailPanelState
                {
                    Card = battle[0],
                    Location = ShopCardLocation.Battle,
                    SlotIndex = 0
                },
                StatusMessage = "明亮绘本正式卡池 · 静态视觉验收"
            };
        }

        private static ConfigService LoadPreviewConfigs()
        {
            var configs = new ConfigService(
                new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();
            return configs;
        }

        private static CardViewModel CreateMinion(
            ConfigService configs,
            string id,
            CardDisplayMode mode,
            string instanceId,
            bool selected = false)
        {
            if (!configs.TryGetMinion(id, out var config))
            {
                throw new InvalidOperationException(
                    "The Light Storybook shop preview minion is missing: " +
                    id);
            }

            var model = ShopCardViewModelFactory.FromOffer(
                config,
                int.MaxValue);
            model.InstanceId = "light-storybook-" + instanceId;
            model.DisplayMode = mode;
            model.ShowCost = true;
            model.IsSelected = selected;
            return model;
        }

        private static CardViewModel CreateSpell(
            ConfigService configs,
            string id,
            CardDisplayMode mode,
            string instanceId)
        {
            if (!configs.TryGetSpell(id, out var config))
            {
                throw new InvalidOperationException(
                    "The Light Storybook shop preview spell is missing: " +
                    id);
            }

            var model = ShopCardViewModelFactory.FromOffer(
                config,
                int.MaxValue);
            model.InstanceId = "light-storybook-" + instanceId;
            model.DisplayMode = mode;
            model.ShowCost = true;
            return model;
        }

        private static void ValidateExactArtwork(
            PresentationSpriteCatalog catalog,
            IEnumerable<CardViewModel> models)
        {
            foreach (var model in models)
            {
                if (model == null ||
                    string.IsNullOrWhiteSpace(model.ArtId) ||
                    !catalog.TryGetArtwork(model.ArtId, out var sprite) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "The Light Storybook shop preview requires exact " +
                        "artwork for " + (model?.ArtId ?? "<null>"));
                }
            }
        }

        private static ShopActionButtonState Action(string text)
        {
            return new ShopActionButtonState
            {
                Text = text,
                IsVisible = true,
                IsInteractable = false
            };
        }

        private static void BuildRunScene(GameObject prefab)
        {
            var scene = CreateSceneWithScreen(
                prefab,
                "PF_RunScreen_LightStorybook");
            var controllerObject = new GameObject(
                "RunTestController",
                typeof(RunTestController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            Bind(
                controllerObject.GetComponent<RunTestController>(),
                "screenView",
                scene.GetRootGameObjects()[0].GetComponent<RunScreenView>());
            SaveScene(scene, SceneRoot + "/RunLightStorybookAB.unity");
        }

        private static Scene CreateSceneWithScreen(
            GameObject prefab,
            string instanceName)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var screen = PrefabUtility.InstantiatePrefab(
                prefab,
                scene) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate " + instanceName);
            }
            screen.name = instanceName;
            var eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            CreateValidationCamera(scene);
            return scene;
        }

        private static void CreateValidationCamera(Scene scene)
        {
            var cameraObject = new GameObject(
                "ValidationCamera",
                typeof(Camera),
                typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.80f, 0.85f, 0.83f, 1f);
            camera.cullingMask = 0;
        }

        private static void ValidateSceneCamera(
            IEnumerable<GameObject> roots,
            string sceneLabel)
        {
            var cameras = roots
                .SelectMany(value =>
                    value.GetComponentsInChildren<Camera>(true))
                .ToArray();
            if (cameras.Length != 1 ||
                !cameras[0].gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException(
                    $"The saved Light Storybook {sceneLabel} scene must " +
                    "contain exactly one active validation camera.");
            }
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void Bind(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SetReference(serialized, propertyName, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReplacePresentationReferences(
            GameObject root,
            PresentationTheme theme,
            PresentationSpriteCatalog catalog)
        {
            foreach (var component in
                     root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null)
                {
                    continue;
                }
                var serialized = new SerializedObject(component);
                var changed = false;
                var themeProperty = serialized.FindProperty("theme");
                if (themeProperty != null)
                {
                    themeProperty.objectReferenceValue = theme;
                    changed = true;
                }
                var catalogProperty = serialized.FindProperty("spriteCatalog");
                if (catalogProperty != null)
                {
                    catalogProperty.objectReferenceValue = catalog;
                    changed = true;
                }
                if (changed)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Serialized property is missing: " + propertyName);
            }
            property.objectReferenceValue = value;
        }

        private static float Luminance(Color color)
        {
            return color.r * 0.2126f +
                   color.g * 0.7152f +
                   color.b * 0.0722f;
        }

        private static void CopyAssetReplacing(string source, string destination)
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
    }
}
