using System;
using System.IO;
using System.Linq;
using SpireChess.UI;
using SpireChess.UI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.Editor
{
    public static class BattleUiPrefabBuilder
    {
        public const string SlotPrefabPath =
            "Assets/Prefabs/UI/Battle/PF_BattleSlot.prefab";
        public const string StandeePrefabPath =
            "Assets/Prefabs/UI/Battle/PF_BattleStandee.prefab";
        public const string ScreenPrefabPath =
            "Assets/Prefabs/UI/Battle/PF_BattleScreen.prefab";
        public const string ThemePath =
            "Assets/Configs/Presentation/PresentationTheme.asset";
        public const string PreviewScenePath =
            "Assets/Scenes/BattleUiPreview.unity";
        public const string BattleScenePath =
            "Assets/Scenes/BattleTest.unity";

        private static readonly Color Background =
            new Color(0.025f, 0.035f, 0.055f, 1f);
        private static readonly Color Panel =
            new Color(0.075f, 0.09f, 0.13f, 0.97f);
        private static readonly Color SlotHitArea =
            Color.clear;
        private static readonly Color ButtonColor =
            new Color(0.16f, 0.19f, 0.25f, 1f);
        private const string StandeeArtRoot =
            "Assets/Art/Presentation/UI/Battle/Standee";
        private const string ShieldMaterialPath =
            StandeeArtRoot + "/M_BattleShieldAdditive.mat";
        private const string ShieldSquireArtPath =
            "Assets/Art/Presentation/Cards/Minions/ForgeSoul/" +
            "card_minion_forge_soul_shield_squire.png";

        [MenuItem("Spire Chess/UI/Rebuild Battle UI")]
        public static void Build()
        {
            CardUiPrefabBuilder.Build();
            EnsureFolder("Assets/Prefabs/UI", "Battle");
            var font = AssetDatabase.LoadAssetAtPath<Font>(
                CardUiPrefabBuilder.FontPath);
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardUiPrefabBuilder.PrefabPath);
            if (font == null || cardPrefab == null)
            {
                throw new InvalidOperationException(
                    "Battle UI requires the pinned font and PF_Card.");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                CardUiPrefabBuilder.SpriteCatalogPath);
            var theme = LoadOrCreateTheme();
            var shieldMaterial = ConfigureStandeePresentation(catalog);
            BuildStandee(font, catalog, theme, shieldMaterial);
            var standeePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                StandeePrefabPath);
            if (standeePrefab == null)
            {
                throw new InvalidOperationException(
                    "Failed to reload generated battle standee prefab.");
            }

            BuildSlot(font);
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SlotPrefabPath);
            if (slotPrefab == null)
            {
                throw new InvalidOperationException(
                    "Failed to reload generated battle slot prefab.");
            }

            BuildScreen(font, cardPrefab, standeePrefab, slotPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WireBattleTestScene();
            Debug.Log("[BattleUI] Rebuilt formal battle prefabs.");
        }

        [MenuItem("Spire Chess/UI/Rebuild and Capture Battle UI")]
        public static void BuildAndCapture()
        {
            Build();
            CaptureValidationScreenshots();
        }

        public static void WireBattleTestScene()
        {
            var screenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ScreenPrefabPath);
            if (screenPrefab == null)
            {
                throw new InvalidOperationException(
                    "Build PF_BattleScreen before wiring BattleTest.");
            }

            var scene = EditorSceneManager.OpenScene(
                BattleScenePath,
                OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<BattleScreenView>() != null ||
                    root.GetComponent<BattleTestController>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            var screen = PrefabUtility.InstantiatePrefab(
                screenPrefab,
                scene) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Failed to place PF_BattleScreen in BattleTest.");
            }
            screen.name = "PF_BattleScreen";

            var controllerObject = new GameObject(
                "BattleTestController",
                typeof(BattleTestController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            var controller = controllerObject.GetComponent<BattleTestController>();
            var serialized = new SerializedObject(controller);
            SetReference(
                serialized,
                "screenView",
                screen.GetComponent<BattleScreenView>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void CaptureValidationScreenshots()
        {
            var screenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ScreenPrefabPath);
            if (screenPrefab == null)
            {
                throw new InvalidOperationException(
                    "Build PF_BattleScreen before capturing screenshots.");
            }

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("BattleUiPreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var screen = PrefabUtility.InstantiatePrefab(screenPrefab) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate PF_BattleScreen for validation.");
            }
            screen.name = "BattleUiPreview";
            var canvas = screen.GetComponent<Canvas>();
            var canvasRect = screen.GetComponent<RectTransform>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 1;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;

            var view = screen.GetComponent<BattleScreenView>();
            view.Render(CreatePreviewState());
            EditorSceneManager.SaveScene(scene, PreviewScenePath);

            var repositoryRoot = Directory.GetParent(
                Directory.GetParent(Application.dataPath).FullName).FullName;
            var outputDirectory = Path.Combine(
                repositoryRoot,
                "ui-concepts",
                "unity-validation",
                "pf-battle-screen-v0.2");
            Directory.CreateDirectory(outputDirectory);
            Capture(
                camera,
                canvasRect,
                1920,
                1080,
                Path.Combine(outputDirectory, "battle-screen-1920x1080.png"));
            view.Render(CreatePreviewState());
            Capture(
                camera,
                canvasRect,
                1920,
                1200,
                Path.Combine(outputDirectory, "battle-screen-1920x1200.png"));
            view.Render(CreateRarityComparisonState());
            Capture(
                camera,
                canvasRect,
                1920,
                1080,
                Path.Combine(
                    outputDirectory,
                    "battle-standee-rarity-1920x1080.png"));
            view.Render(CreateRarityComparisonState());
            Capture(
                camera,
                canvasRect,
                1920,
                1200,
                Path.Combine(
                    outputDirectory,
                    "battle-standee-rarity-1920x1200.png"));
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            view.Render(CreatePreviewState());
            Canvas.ForceUpdateCanvases();
            var previewStandee = screen.transform.Find(
                    "SafeArea/Board/PlayerRow/Slots/Slot1/Content")
                .GetComponentInChildren<BattleStandeeView>();
            view.ToggleStandeeDetailLock(
                previewStandee,
                previewStandee.Model);
            Capture(
                camera,
                canvasRect,
                1920,
                1080,
                Path.Combine(
                    outputDirectory,
                    "battle-standee-detail-1920x1080.png"));
            AssetDatabase.SaveAssets();
            Debug.Log("[BattleUI] Captured validation screenshots to " +
                      outputDirectory);
        }

        private static PresentationTheme LoadOrCreateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(
                ThemePath);
            if (theme != null)
            {
                ConfigureTheme(theme);
                return theme;
            }

            theme = ScriptableObject.CreateInstance<PresentationTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            ConfigureTheme(theme);
            return theme;
        }

        private static void ConfigureTheme(PresentationTheme theme)
        {
            var serialized = new SerializedObject(theme);
            SetColor(serialized, "normalFrameTint", Color.white);
            SetColor(serialized, "goldenFrameTint",
                new Color(1f, 0.90f, 0.62f, 1f));
            SetColor(serialized, "legalTargetTint",
                new Color(0.38f, 0.82f, 0.58f, 0.78f));
            SetColor(serialized, "selectedTargetTint",
                new Color(0.98f, 0.68f, 0.22f, 0.88f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        private static Material ConfigureStandeePresentation(
            PresentationSpriteCatalog catalog)
        {
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "PresentationSpriteCatalog is required for battle standees.");
            }

            var serialized = new SerializedObject(catalog);
            SetReference(serialized, "battleNormalStandeeFrame",
                LoadSprite(StandeeArtRoot + "/standee_frame_silver_v1.png", true));
            SetReference(serialized, "battleStandeeFrame",
                LoadSprite(StandeeArtRoot + "/standee_frame.png", true));
            SetReference(serialized, "battleAttackMedallion",
                LoadSprite(StandeeArtRoot + "/attack_medallion.png", true));
            SetReference(serialized, "battleHealthMedallion",
                LoadSprite(StandeeArtRoot + "/health_medallion.png", true));
            SetReference(serialized, "battleShieldOverlay",
                LoadSprite(StandeeArtRoot + "/shield_overlay_screen.png", false));
            SetReference(serialized, "battleTauntBase",
                LoadSprite(StandeeArtRoot + "/taunt_base.png", true));
            SetReference(serialized, "battleDeathrattleSeal",
                LoadSprite(StandeeArtRoot + "/deathrattle_seal.png", true));
            SetReference(serialized, "battleSplashMark",
                LoadSprite(StandeeArtRoot + "/splash_mark.png", true));
            AddOrReplaceArtwork(
                serialized,
                "placeholder_card_forge_soul_shield_squire",
                LoadSprite(ShieldSquireArtPath, false));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);

            var material = AssetDatabase.LoadAssetAtPath<Material>(
                ShieldMaterialPath);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Mobile/Particles/Additive shader is unavailable.");
            }
            material = new Material(shader)
            {
                name = "M_BattleShieldAdditive"
            };
            AssetDatabase.CreateAsset(material, ShieldMaterialPath);
            return material;
        }

        private static Sprite LoadSprite(string path, bool alphaTransparency)
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Unable to configure presentation sprite at " + path);
            }

            var changed = importer.textureType != TextureImporterType.Sprite ||
                          importer.spriteImportMode != SpriteImportMode.Single ||
                          importer.mipmapEnabled ||
                          importer.alphaIsTransparency != alphaTransparency;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alphaTransparency;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (changed)
            {
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    "Unable to load presentation sprite at " + path);
            }
            return sprite;
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
                entry = artworks.GetArrayElementAtIndex(artworks.arraySize - 1);
            }

            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
        }

        private static void BuildStandee(
            Font font,
            PresentationSpriteCatalog catalog,
            PresentationTheme theme,
            Material shieldMaterial)
        {
            var root = new GameObject(
                "PF_BattleStandee",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(BattleStandeeView));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.pivot = new Vector2(0f, 1f);
                rootRect.sizeDelta = new Vector2(160f, 240f);
                var rootImage = root.GetComponent<Image>();
                rootImage.color = Color.clear;
                rootImage.raycastTarget = true;

                var target = CreateImage(
                    "TargetHighlight",
                    root.transform,
                    new Color(0.38f, 0.82f, 0.58f, 0.78f));
                Stretch(target.rectTransform, new Vector2(-3f, -3f),
                    new Vector2(3f, 3f));
                target.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");
                target.type = Image.Type.Sliced;
                target.fillCenter = false;
                target.pixelsPerUnitMultiplier = 2.5f;
                var targetOutline = target.gameObject.AddComponent<Outline>();
                targetOutline.effectColor = new Color(0.30f, 0.55f, 0.34f, 0.52f);
                targetOutline.effectDistance = new Vector2(1.5f, -1.5f);

                var taunt = CreateImage("TauntBase", root.transform, Color.white);
                SetRect(taunt.rectTransform, -8f, 0f, 176f, 35f);
                taunt.preserveAspect = true;

                var portraitMask = CreateRect("PortraitMask", root.transform);
                SetRect(portraitMask, 20f, 34f, 120f, 192f);
                portraitMask.gameObject.AddComponent<RectMask2D>();
                var portrait = CreateImage(
                    "Portrait",
                    portraitMask,
                    new Color(0.30f, 0.27f, 0.33f, 1f));
                Stretch(portrait.rectTransform, Vector2.zero, Vector2.zero);
                var fallback = CreateText(
                    "PortraitFallback",
                    portraitMask,
                    font,
                    "?",
                    52,
                    TextAnchor.MiddleCenter);
                fallback.fontStyle = FontStyle.Bold;
                fallback.color = new Color(1f, 0.92f, 0.72f, 0.72f);
                Stretch(fallback.rectTransform, Vector2.zero, Vector2.zero);

                var shield = CreateImage(
                    "ShieldOverlay",
                    root.transform,
                    new Color(0.78f, 0.96f, 1f, 0.78f));
                SetRect(shield.rectTransform, 14f, 10f, 132f, 222f);
                shield.material = shieldMaterial;
                shield.preserveAspect = true;

                var frame = CreateImage("Frame", root.transform, Color.white);
                SetRect(frame.rectTransform, 14f, 7f, 132f, 228f);

                var deathrattle = CreateImage(
                    "DeathrattleSeal",
                    root.transform,
                    Color.white);
                SetRect(deathrattle.rectTransform, 52f, 188f, 56f, 56f);
                deathrattle.preserveAspect = true;

                var attack = CreateImage(
                    "AttackMedallion",
                    root.transform,
                    Color.white);
                SetRect(attack.rectTransform, 1f, 0f, 56f, 56f);
                var attackText = CreateText(
                    "Value",
                    attack.transform,
                    font,
                    "0",
                    25,
                    TextAnchor.MiddleCenter);
                attackText.fontStyle = FontStyle.Bold;
                Stretch(attackText.rectTransform, new Vector2(4f, 4f),
                    new Vector2(-4f, -4f));

                var health = CreateImage(
                    "HealthMedallion",
                    root.transform,
                    Color.white);
                SetRect(health.rectTransform, 103f, 0f, 56f, 56f);
                var healthText = CreateText(
                    "Value",
                    health.transform,
                    font,
                    "0",
                    25,
                    TextAnchor.MiddleCenter);
                healthText.fontStyle = FontStyle.Bold;
                Stretch(healthText.rectTransform, new Vector2(4f, 4f),
                    new Vector2(-4f, -4f));

                var splash = CreateImage(
                    "SplashMark",
                    root.transform,
                    Color.white);
                SetRect(splash.rectTransform, 48f, 41f, 26f, 44f);
                splash.preserveAspect = true;

                var serialized = new SerializedObject(
                    root.GetComponent<BattleStandeeView>());
                SetReference(serialized, "spriteCatalog", catalog);
                SetReference(serialized, "theme", theme);
                SetReference(serialized, "portrait", portrait);
                SetReference(serialized, "portraitFallback", fallback);
                SetReference(serialized, "frame", frame);
                SetReference(serialized, "shieldOverlay", shield);
                SetReference(serialized, "tauntBase", taunt);
                SetReference(serialized, "deathrattleSeal", deathrattle);
                SetReference(serialized, "splashMark", splash);
                SetReference(serialized, "attackMedallion", attack);
                SetReference(serialized, "healthMedallion", health);
                SetReference(serialized, "attackText", attackText);
                SetReference(serialized, "healthText", healthText);
                SetReference(serialized, "targetHighlight", target);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, StandeePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildSlot(Font font)
        {
            var root = new GameObject(
                "PF_BattleSlot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(LayoutElement),
                typeof(BattleSlotView));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(176f, 256f);
                var image = root.GetComponent<Image>();
                image.color = SlotHitArea;
                image.raycastTarget = true;
                var outline = root.GetComponent<Outline>();
                outline.effectColor = Color.clear;
                outline.effectDistance = Vector2.zero;
                var element = root.GetComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = 176f;
                element.minHeight = element.preferredHeight = 256f;

                var hint = CreateText(
                    "EmptyHint",
                    root.transform,
                    font,
                    "1",
                    28,
                    TextAnchor.MiddleCenter);
                hint.color = new Color(1f, 1f, 1f, 0.25f);
                Stretch(hint.rectTransform, Vector2.zero, Vector2.zero);
                var content = CreateRect("Content", root.transform);
                Stretch(content, new Vector2(8f, 8f), new Vector2(-8f, -8f));

                var serialized = new SerializedObject(
                    root.GetComponent<BattleSlotView>());
                SetReference(serialized, "background", image);
                SetReference(serialized, "emptyHint", hint);
                SetReference(serialized, "content", content);
                SetReference(serialized, "highlight", outline);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildScreen(
            Font font,
            GameObject cardPrefab,
            GameObject standeePrefab,
            GameObject slotPrefab)
        {
            var root = new GameObject(
                "PF_BattleScreen",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(BattleScreenView));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1920f, 1080f);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                var safeArea = CreateImage(
                    "SafeArea",
                    root.transform,
                    Background).rectTransform;
                Stretch(safeArea, Vector2.zero, Vector2.zero);
                var topBar = CreateImage(
                    "TopBar",
                    safeArea,
                    Panel).rectTransform;
                SetRect(topBar, 20f, 20f, 1880f, 82f, true);

                var title = CreateText(
                    "Title",
                    topBar,
                    font,
                    "战斗",
                    28,
                    TextAnchor.MiddleLeft);
                SetRect(title.rectTransform, 18f, 10f, 330f, 62f);
                var status = CreateText(
                    "Status",
                    topBar,
                    font,
                    "准备阶段",
                    20,
                    TextAnchor.MiddleLeft);
                SetRect(status.rectTransform, 350f, 10f, 430f, 62f);
                var round = CreateText(
                    "Round",
                    topBar,
                    font,
                    "准备阶段",
                    18,
                    TextAnchor.MiddleCenter);
                SetRect(round.rectTransform, 790f, 10f, 150f, 62f);

                var actions = CreateRect("Actions", topBar);
                SetRect(actions, 950f, 10f, 912f, 62f);
                var actionLayout = actions.gameObject.AddComponent<
                    HorizontalLayoutGroup>();
                actionLayout.spacing = 8f;
                actionLayout.childAlignment = TextAnchor.MiddleRight;
                actionLayout.childControlWidth = false;
                actionLayout.childControlHeight = true;
                actionLayout.childForceExpandWidth = false;
                actionLayout.childForceExpandHeight = true;
                var start = CreateButton("Start", actions, font, "开始战斗", out var startText);
                var speed = CreateButton("Speed", actions, font, "速度 1×", out var speedText);
                var skip = CreateButton("Skip", actions, font, "跳过表现", out var skipText);
                var preset = CreateButton("Preset", actions, font, "切换预设", out var presetText);
                var reset = CreateButton("Reset", actions, font, "重置", out var resetText);
                var returnButton = CreateButton("Return", actions, font, "查看结算", out var returnText);

                var board = CreateRect("Board", safeArea);
                SetRect(board, 20f, 120f, 1490f, 930f);
                var enemySlots = BuildRow(
                    "EnemyRow",
                    board,
                    font,
                    slotPrefab,
                    "敌方",
                    485f);
                var playerSlots = BuildRow(
                    "PlayerRow",
                    board,
                    font,
                    slotPrefab,
                    "玩家",
                    95f);

                var logPanel = CreateImage(
                    "LogPanel",
                    safeArea,
                    Panel).rectTransform;
                SetRect(logPanel, 1530f, 120f, 370f, 838f);
                logPanel.GetComponent<Image>().raycastTarget = false;
                var logTitle = CreateText(
                    "LogTitle",
                    logPanel,
                    font,
                    "战斗日志",
                    22,
                    TextAnchor.MiddleLeft);
                SetRect(logTitle.rectTransform, 16f, 768f, 338f, 50f);
                var scrollRect = BuildLogScroll(logPanel, font, out var logText);

                var feedbackRoot = CreateRect("FeedbackLayer", safeArea);
                Stretch(feedbackRoot, Vector2.zero, Vector2.zero);
                var feedbackCanvas = feedbackRoot.gameObject.AddComponent<CanvasGroup>();
                feedbackCanvas.alpha = 0f;
                var feedbackText = CreateText(
                    "Feedback",
                    feedbackRoot,
                    font,
                    string.Empty,
                    34,
                    TextAnchor.MiddleCenter);
                feedbackText.fontStyle = FontStyle.Bold;
                SetRect(feedbackText.rectTransform, 720f, 500f, 480f, 80f);

                var detailLayer = CreateRect("StandeeDetailLayer", safeArea);
                SetRect(detailLayer, 20f, 120f, 1490f, 930f);
                var detailObject = PrefabUtility.InstantiatePrefab(
                    cardPrefab) as GameObject;
                if (detailObject == null)
                {
                    throw new InvalidOperationException(
                        "Failed to instantiate PF_Card for standee detail.");
                }
                detailObject.name = "DetailCard";
                detailObject.transform.SetParent(detailLayer, false);
                var detailCard = detailObject.GetComponent<CardView>();
                var detailGroup = detailObject.GetComponent<CanvasGroup>();
                detailGroup.alpha = 0f;
                detailGroup.blocksRaycasts = false;
                detailGroup.interactable = false;
                var detailMode = CreateText(
                    "DetailMode",
                    detailLayer,
                    font,
                    string.Empty,
                    15,
                    TextAnchor.MiddleCenter);
                detailMode.color = new Color(1f, 0.88f, 0.62f, 1f);
                detailMode.rectTransform.sizeDelta = new Vector2(280f, 28f);

                var view = root.GetComponent<BattleScreenView>();
                var serialized = new SerializedObject(view);
                SetReference(serialized, "rootCanvas", canvas);
                SetReference(serialized, "safeArea", safeArea);
                SetReference(serialized, "standeePrefab", standeePrefab);
                SetReference(serialized, "titleText", title);
                SetReference(serialized, "statusText", status);
                SetReference(serialized, "roundText", round);
                SetReference(serialized, "startButton", start);
                SetReference(serialized, "startButtonText", startText);
                SetReference(serialized, "speedButton", speed);
                SetReference(serialized, "speedButtonText", speedText);
                SetReference(serialized, "skipButton", skip);
                SetReference(serialized, "skipButtonText", skipText);
                SetReference(serialized, "presetButton", preset);
                SetReference(serialized, "presetButtonText", presetText);
                SetReference(serialized, "resetButton", reset);
                SetReference(serialized, "resetButtonText", resetText);
                SetReference(serialized, "returnButton", returnButton);
                SetReference(serialized, "returnButtonText", returnText);
                SetReferenceArray(serialized, "enemySlots", enemySlots);
                SetReferenceArray(serialized, "playerSlots", playerSlots);
                SetReference(serialized, "logScrollRect", scrollRect);
                SetReference(serialized, "logText", logText);
                SetReference(serialized, "feedbackCanvasGroup", feedbackCanvas);
                SetReference(serialized, "feedbackText", feedbackText);
                SetReference(serialized, "detailLayer", detailLayer);
                SetReference(serialized, "detailCard", detailCard);
                SetReference(serialized, "detailCanvasGroup", detailGroup);
                SetReference(serialized, "detailModeText", detailMode);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static BattleSlotView[] BuildRow(
            string name,
            Transform parent,
            Font font,
            GameObject slotPrefab,
            string label,
            float bottom)
        {
            var panel = CreateImage(name, parent, Panel).rectTransform;
            SetRect(panel, 0f, bottom, 1490f, 330f);
            var title = CreateText(
                "Label",
                panel,
                font,
                label,
                24,
                TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, 18f, 135f, 90f, 60f);
            var row = CreateRect("Slots", panel);
            SetRect(row, 110f, 30f, 1320f, 270f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var slots = new BattleSlotView[5];
            for (var index = 0; index < slots.Length; index++)
            {
                var instance = PrefabUtility.InstantiatePrefab(slotPrefab) as GameObject;
                instance.name = $"Slot{index + 1}";
                instance.transform.SetParent(row, false);
                slots[index] = instance.GetComponent<BattleSlotView>();
            }
            return slots;
        }

        private static ScrollRect BuildLogScroll(
            Transform parent,
            Font font,
            out Text contentText)
        {
            var scroll = CreateRect("LogScroll", parent);
            SetRect(scroll, 16f, 18f, 338f, 738f);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            var viewport = CreateImage(
                "Viewport",
                scroll,
                new Color(0f, 0f, 0f, 0.01f)).rectTransform;
            Stretch(viewport, Vector2.zero, Vector2.zero);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            contentText = CreateText(
                "LogText",
                viewport,
                font,
                string.Empty,
                17,
                TextAnchor.UpperLeft);
            contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentText.verticalOverflow = VerticalWrapMode.Overflow;
            contentText.rectTransform.anchorMin = new Vector2(0f, 1f);
            contentText.rectTransform.anchorMax = new Vector2(1f, 1f);
            contentText.rectTransform.pivot = new Vector2(0.5f, 1f);
            contentText.rectTransform.anchoredPosition = Vector2.zero;
            contentText.rectTransform.sizeDelta = Vector2.zero;
            var fitter = contentText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = contentText.rectTransform;
            return scrollRect;
        }

        private static BattleScreenState CreatePreviewState()
        {
            var state = new BattleScreenState
            {
                Title = "战斗 · 星门守卫",
                Status = "玩家 2 号位 → 敌方 3 号位",
                RoundText = "第 3 轮",
                LogText = string.Join("\n", new[]
                {
                    "战斗开始。",
                    "天穹契约者攻击星门守卫。",
                    "星门守卫的护盾抵挡了 16 点伤害。",
                    "双尾狐影在 3 号位被召唤。"
                }),
                Start = ButtonState("开始战斗", false, false),
                Speed = ButtonState("速度 2×", true, true),
                Skip = ButtonState("跳过表现", true, true),
                Preset = ButtonState("切换预设", false, false),
                Reset = ButtonState("重置", false, false),
                Return = ButtonState("查看结算", false, false)
            };
            state.PlayerCards[0] = PreviewCard(
                "player-0",
                "铸魂盾侍",
                "战斗开始时获得护盾；金色时左侧友军也获得护盾。",
                "铸魂",
                1,
                12,
                19,
                false,
                true);
            state.PlayerCards[1] = PreviewCard(
                "player-1",
                "星盘校准师",
                "每个商店阶段第一次刷新后，使攻击最低的友方星契永久 +1 攻击。",
                "星契",
                3,
                7,
                9,
                false,
                false);
            state.PlayerCards[2] = PreviewCard(
                "player-2",
                "双尾狐影",
                "亡语：依次召唤两个幼灵。",
                "荒灵",
                1,
                4,
                4,
                false,
                false);
            state.EnemyCards[1] = PreviewCard(
                "enemy-1",
                "不熄炉王",
                "嘲讽。护盾破裂与亡语反馈使用独立空间锚点。",
                "铸魂",
                5,
                11,
                18,
                true,
                true);
            state.EnemyCards[3] = PreviewCard(
                "enemy-3",
                "关键词校验立牌",
                "同时验证嘲讽、护盾、亡语和溅射。",
                "旅团",
                3,
                9,
                12,
                false,
                false);
            return state;
        }

        private static BattleScreenState CreateRarityComparisonState()
        {
            var state = CreatePreviewState();
            state.Title = "战斗 · 稀有度对照";
            state.Status = "普通 / 金色 · 同角色、同流派色";
            state.RoundText = "G1 视觉评审";
            state.LogText = string.Join("\n", new[]
            {
                "左：普通铸魂盾侍。",
                "右：金色铸魂盾侍。",
                "金色只强化局部暖金，不覆盖铸魂红与攻防数字。"
            });
            Array.Clear(state.PlayerCards, 0, state.PlayerCards.Length);
            Array.Clear(state.EnemyCards, 0, state.EnemyCards.Length);
            state.PlayerCards[0] = PreviewCard(
                "rarity-normal",
                "铸魂盾侍",
                "战斗开始时获得护盾。",
                "铸魂",
                1,
                6,
                10,
                false,
                false);
            state.PlayerCards[1] = PreviewCard(
                "rarity-golden",
                "铸魂盾侍",
                "金色：战斗开始时获得护盾。",
                "铸魂",
                1,
                12,
                20,
                true,
                false);
            return state;
        }

        private static CardViewModel PreviewCard(
            string id,
            string name,
            string description,
            string race,
            int tier,
            int attack,
            int health,
            bool golden,
            bool shield)
        {
            var keywords = ResolvePreviewKeywords(name, shield);
            return new CardViewModel
            {
                InstanceId = id,
                ArtId = ResolvePreviewArtId(name),
                Name = name,
                Description = description,
                RaceText = race,
                AbilityLabels = keywords,
                Keywords = keywords,
                Tier = tier,
                Attack = attack,
                Health = health,
                BaseAttack = golden ? attack / 2 : attack,
                BaseHealth = golden ? health / 2 : health,
                DisplayMode = CardDisplayMode.Compact,
                IsMinion = true,
                IsGolden = golden,
                HasShield = shield,
                IsInteractable = true,
                IsAffordable = true,
                IsLegalTarget = name == "关键词校验立牌"
            };
        }

        private static string[] ResolvePreviewKeywords(string name, bool shield)
        {
            if (name == "关键词校验立牌")
            {
                return new[] { "嘲讽", "护盾", "亡语", "溅射" };
            }
            if (name == "铸魂盾侍" || name == "不熄炉王")
            {
                return shield ? new[] { "嘲讽", "护盾" } : new[] { "嘲讽" };
            }
            if (name == "双尾狐影")
            {
                return new[] { "亡语" };
            }
            return shield ? new[] { "护盾" } : Array.Empty<string>();
        }

        private static string ResolvePreviewArtId(string name)
        {
            switch (name)
            {
                case "铸魂盾侍":
                    return "placeholder_card_forge_soul_shield_squire";
                case "不熄炉王":
                    return "placeholder_card_undying_furnace_king";
                default:
                    return string.Empty;
            }
        }

        private static BattleButtonState ButtonState(
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

        private static Button CreateButton(
            string name,
            Transform parent,
            Font font,
            string label,
            out Text text)
        {
            var image = CreateImage(name, parent, ButtonColor);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var element = image.gameObject.AddComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = 142f;
            text = CreateText(
                "Label",
                image.transform,
                font,
                label,
                17,
                TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, new Vector2(6f, 4f), new Vector2(-6f, -4f));
            return button;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            string value,
            int size,
            TextAnchor alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform))
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetRect(
            RectTransform rect,
            float left,
            float bottom,
            float width,
            float height,
            bool fromTop = false)
        {
            rect.anchorMin = fromTop ? new Vector2(0f, 1f) : Vector2.zero;
            rect.anchorMax = rect.anchorMin;
            rect.pivot = fromTop ? new Vector2(0f, 1f) : Vector2.zero;
            rect.anchoredPosition = fromTop
                ? new Vector2(left, -bottom)
                : new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetReference(
            SerializedObject serialized,
            string name,
            UnityEngine.Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Missing serialized property {name}.");
            }
            property.objectReferenceValue = value;
        }

        private static void SetColor(
            SerializedObject serialized,
            string name,
            Color value)
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized color property: " + name);
            }
            property.colorValue = value;
        }

        private static void SetReferenceArray<T>(
            SerializedObject serialized,
            string name,
            T[] values)
            where T : UnityEngine.Object
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Missing serialized property {name}.");
            }
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (scene.GetRootGameObjects()
                .Any(root => root.GetComponentInChildren<EventSystem>(true) != null))
            {
                return;
            }
            var value = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(value, scene);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
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
                Canvas.ForceUpdateCanvases();
                camera.Render();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
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
    }
}
