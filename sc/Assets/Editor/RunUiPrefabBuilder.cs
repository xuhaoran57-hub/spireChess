using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.UI;
using SpireChess.UI.Common;
using SpireChess.UI.Run;
using SpireChess.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.Editor
{
    public static class RunUiPrefabBuilder
    {
        public const string NodePrefabPath =
            "Assets/Prefabs/UI/Run/PF_RunMapNode.prefab";
        public const string EdgePrefabPath =
            "Assets/Prefabs/UI/Run/PF_RunMapEdge.prefab";
        public const string RelicPrefabPath =
            "Assets/Prefabs/UI/Run/PF_RunRelicEntry.prefab";
        public const string ChoicePrefabPath =
            "Assets/Prefabs/UI/Run/PF_RunChoiceOption.prefab";
        public const string ScreenPrefabPath =
            "Assets/Prefabs/UI/Run/PF_RunScreen.prefab";
        public const string PreviewScenePath =
            "Assets/Scenes/RunUiPreview.unity";
        public const string RunScenePath =
            "Assets/Scenes/RunTest.unity";

        private static readonly Color Background =
            new Color(0.025f, 0.035f, 0.055f, 1f);
        private static readonly Color Panel =
            new Color(0.075f, 0.09f, 0.13f, 0.98f);
        private static readonly Color ButtonColor =
            new Color(0.14f, 0.24f, 0.31f, 1f);

        [MenuItem("Spire Chess/UI/Rebuild Run UI")]
        public static void Build()
        {
            EnsureFolder("Assets/Prefabs/UI", "Run");
            var font = AssetDatabase.LoadAssetAtPath<Font>(
                CardUiPrefabBuilder.FontPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    "Run UI requires the pinned Noto Sans CJK font.");
            }
            var spriteCatalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CardUiPrefabBuilder.SpriteCatalogPath);
            if (spriteCatalog == null)
            {
                throw new InvalidOperationException(
                    "Run UI requires PresentationSpriteCatalog.");
            }
            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(
                BattleUiPrefabBuilder.ThemePath);
            if (theme == null)
            {
                throw new InvalidOperationException(
                    "Run UI requires PresentationTheme.");
            }

            BuildMapNode(font, theme);
            BuildMapEdge(theme);
            BuildRelicEntry(font, spriteCatalog, theme);
            BuildChoiceOption(font, spriteCatalog, theme);
            BuildScreen(
                font,
                LoadPrefab(NodePrefabPath),
                LoadPrefab(EdgePrefabPath),
                LoadPrefab(RelicPrefabPath),
                LoadPrefab(ChoicePrefabPath),
                theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WireRunTestScene();
            Debug.Log("[RunUI] Rebuilt formal run prefabs.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        [MenuItem("Spire Chess/UI/Rebuild and Capture Run UI")]
        public static void BuildAndCapture()
        {
            Build();
            CaptureValidationScreenshots();
        }

        public static void WireRunTestScene()
        {
            var screenPrefab = LoadPrefab(ScreenPrefabPath);
            var scene = EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<RunScreenView>() != null ||
                    root.GetComponent<RunTestController>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            var screen = PrefabUtility.InstantiatePrefab(screenPrefab, scene) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException("Failed to place PF_RunScreen in RunTest.");
            }
            screen.name = "PF_RunScreen";
            var controllerObject = new GameObject(
                "RunTestController",
                typeof(RunTestController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            var serialized = new SerializedObject(
                controllerObject.GetComponent<RunTestController>());
            SetReference(serialized, "screenView", screen.GetComponent<RunScreenView>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void CaptureValidationScreenshots()
        {
            var screenPrefab = LoadPrefab(ScreenPrefabPath);
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("RunUiPreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var screen = PrefabUtility.InstantiatePrefab(screenPrefab) as GameObject;
            screen.name = "RunUiPreview";
            var canvas = screen.GetComponent<Canvas>();
            var canvasRect = screen.GetComponent<RectTransform>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 1;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;

            var view = screen.GetComponent<RunScreenView>();
            var state = CreatePreviewState();
            view.Render(state);
            EditorSceneManager.SaveScene(scene, PreviewScenePath);

            var repositoryRoot = Directory.GetParent(
                Directory.GetParent(Application.dataPath).FullName).FullName;
            var outputDirectory = Path.Combine(
                repositoryRoot,
                "ui-concepts",
                "unity-validation",
                "g3-run-screen-v0.1");
            Directory.CreateDirectory(outputDirectory);
            Capture(camera, canvasRect, 1920, 1080,
                Path.Combine(outputDirectory, "run-screen-1920x1080.png"));
            view.Render(state);
            Capture(camera, canvasRect, 1920, 1200,
                Path.Combine(outputDirectory, "run-screen-1920x1200.png"));

            state.Choice = new RunChoiceOverlayState
            {
                Title = "选择一件 Boss 遗珍",
                Description = "冠冕级遗珍会在后续楼层持续改变规则。",
                Options = new[]
                {
                    PreviewChoice(
                        "回魂丧钟",
                        "你的随从的亡语额外触发一次。",
                        "冠冕 · 亡语",
                        "icon_relic_crown_echo_bell"),
                    PreviewChoice(
                        "千盾王冠",
                        "战斗开始时为生命最低的友方随从赋予护盾。",
                        "冠冕 · 护盾",
                        "icon_relic_crown_thousand_shields"),
                    PreviewChoice(
                        "漏刻齿轮",
                        "每个商店阶段第一次付费刷新免费。",
                        "奇物 · 刷新",
                        "icon_relic_curio_refresh_gear")
                }
            };
            view.Render(state);
            Capture(camera, canvasRect, 1920, 1080,
                Path.Combine(outputDirectory, "run-choice-1920x1080.png"));
            Capture(camera, canvasRect, 1920, 1200,
                Path.Combine(outputDirectory, "run-choice-1920x1200.png"));

            state.Choice = null;
            view.Render(state);
            var systemMenu = RunSystemMenuView.Attach(view, () => true);
            var menuButton = systemMenu
                .GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "MenuButton");
            menuButton.onClick.Invoke();
            Capture(camera, canvasRect, 1920, 1080,
                Path.Combine(outputDirectory, "system-menu-1920x1080.png"));
            Capture(camera, canvasRect, 1920, 1200,
                Path.Combine(outputDirectory, "system-menu-1920x1200.png"));

            var audioSettingsButton = systemMenu
                .GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "AudioSettingsButton");
            audioSettingsButton.onClick.Invoke();
            Capture(camera, canvasRect, 1920, 1080,
                Path.Combine(
                    outputDirectory,
                    "system-audio-settings-1920x1080.png"));
            Capture(camera, canvasRect, 1920, 1200,
                Path.Combine(
                    outputDirectory,
                    "system-audio-settings-1920x1200.png"));
            AssetDatabase.SaveAssets();
            Debug.Log("[RunUI] Captured validation screenshots to " + outputDirectory);
        }

        private static void BuildMapNode(
            Font font,
            PresentationTheme theme)
        {
            var root = new GameObject(
                "PF_RunMapNode",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(Button),
                typeof(LayoutElement),
                typeof(RunMapNodeView));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(166f, 116f);
                var image = root.GetComponent<Image>();
                image.color = theme.GetMapNodeColor(
                    RunNodeType.Shop,
                    RunMapPresentationStatus.Reachable);
                var outline = root.GetComponent<Outline>();
                outline.effectColor = theme.GetMapStatusColor(
                    RunMapPresentationStatus.Reachable);
                outline.effectDistance = new Vector2(2f, -2f);
                var button = root.GetComponent<Button>();
                button.targetGraphic = image;
                ConfigureButton(button, theme);
                var element = root.GetComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = 166f;
                element.minHeight = element.preferredHeight = 116f;

                var stateOverlay = CreateImage(
                    "StateOverlay",
                    root.transform,
                    theme.GetMapStatusOverlayColor(
                        RunMapPresentationStatus.Reachable));
                Stretch(stateOverlay.rectTransform, Vector2.zero, Vector2.zero);
                stateOverlay.raycastTarget = false;

                var currentPulse = CreateImage(
                    "CurrentPulse",
                    root.transform,
                    theme.GetMapStatusColor(RunMapPresentationStatus.Current));
                Stretch(
                    currentPulse.rectTransform,
                    new Vector2(-5f, -5f),
                    new Vector2(5f, 5f));
                currentPulse.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");
                currentPulse.type = Image.Type.Sliced;
                currentPulse.fillCenter = false;
                currentPulse.raycastTarget = false;
                currentPulse.gameObject.SetActive(false);

                var typeIcon = CreateImage(
                    "TypeIcon",
                    root.transform,
                    theme.GetMapTypeColor(RunNodeType.Shop));
                SetRect(typeIcon.rectTransform, 10f, 35f, 42f, 42f);
                typeIcon.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/Knob.psd");
                typeIcon.raycastTarget = false;
                var typeIconLabel = CreateText(
                    "Glyph",
                    typeIcon.transform,
                    font,
                    "\u5546",
                    22,
                    TextAnchor.MiddleCenter);
                Stretch(
                    typeIconLabel.rectTransform,
                    new Vector2(2f, 2f),
                    new Vector2(-2f, -2f));
                typeIconLabel.fontStyle = FontStyle.Bold;
                typeIconLabel.color = theme.TextPrimary;

                var route = CreateText("Route", root.transform, font, "强攻", 13,
                    TextAnchor.MiddleCenter);
                SetRect(route.rectTransform, 8f, 94f, 150f, 18f);
                route.color = theme.Accent;
                var title = CreateText("Title", root.transform, font, "第 4 战 · 精英", 17,
                    TextAnchor.MiddleCenter);
                title.fontStyle = FontStyle.Bold;
                title.color = theme.TextPrimary;
                SetRect(title.rectTransform, 58f, 60f, 100f, 30f);
                var subtitle = CreateText("Subtitle", root.transform, font, "铜墙守卫", 13,
                    TextAnchor.MiddleCenter);
                subtitle.color = theme.TextSecondary;
                SetRect(subtitle.rectTransform, 58f, 34f, 100f, 23f);
                var status = CreateText("Status", root.transform, font, "可进入", 12,
                    TextAnchor.MiddleCenter);
                SetRect(status.rectTransform, 58f, 7f, 100f, 22f);
                status.color = theme.GetMapStatusColor(
                    RunMapPresentationStatus.Reachable);

                var serialized = new SerializedObject(root.GetComponent<RunMapNodeView>());
                SetReference(serialized, "theme", theme);
                SetReference(serialized, "background", image);
                SetReference(serialized, "outline", outline);
                SetReference(serialized, "button", button);
                SetReference(serialized, "typeIconBackground", typeIcon);
                SetReference(serialized, "typeIconText", typeIconLabel);
                SetReference(serialized, "stateOverlay", stateOverlay);
                SetReference(serialized, "currentPulse", currentPulse);
                SetReference(serialized, "routeText", route);
                SetReference(serialized, "titleText", title);
                SetReference(serialized, "subtitleText", subtitle);
                SetReference(serialized, "statusText", status);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildMapEdge(PresentationTheme theme)
        {
            var root = new GameObject(
                "PF_RunMapEdge",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(100f, 4f);
                var image = root.GetComponent<Image>();
                image.color = theme.GetMapEdgeColor(
                    RunMapEdgePresentationStatus.Locked);
                image.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(root, EdgePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildRelicEntry(
            Font font,
            PresentationSpriteCatalog spriteCatalog,
            PresentationTheme theme)
        {
            var root = new GameObject(
                "PF_RunRelicEntry",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(LayoutElement),
                typeof(RunRelicEntryView));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(330f, 128f);
                var image = root.GetComponent<Image>();
                image.color = theme.PanelRaised;
                var outline = root.GetComponent<Outline>();
                outline.effectColor = theme.PanelBorder;
                outline.effectDistance = new Vector2(1f, -1f);
                var element = root.GetComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = 128f;
                element.flexibleWidth = 1f;
                var icon = CreateImage(
                    "Icon",
                    root.transform,
                    Color.white);
                SetRect(icon.rectTransform, 12f, 12f, 68f, 68f);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.gameObject.SetActive(false);
                var grade = CreateText("Grade", root.transform, font, "冠冕", 13,
                    TextAnchor.MiddleLeft);
                SetRect(grade.rectTransform, 12f, 98f, 70f, 22f);
                grade.color = theme.Accent;
                var name = CreateText("Name", root.transform, font, "双生战号", 18,
                    TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Bold;
                name.color = theme.TextPrimary;
                SetRect(name.rectTransform, 92f, 96f, 226f, 26f);
                var meta = CreateText("Meta", root.transform, font, "触发 · 持续生效", 12,
                    TextAnchor.MiddleLeft);
                SetRect(meta.rectTransform, 92f, 70f, 226f, 22f);
                meta.color = theme.Success;
                var description = CreateText("Description", root.transform, font,
                    "你的战吼额外触发一次。", 13, TextAnchor.UpperLeft);
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                description.verticalOverflow = VerticalWrapMode.Truncate;
                description.color = theme.TextSecondary;
                SetRect(description.rectTransform, 92f, 8f, 226f, 58f);

                var serialized = new SerializedObject(root.GetComponent<RunRelicEntryView>());
                SetReference(serialized, "theme", theme);
                SetReference(serialized, "spriteCatalog", spriteCatalog);
                SetReference(serialized, "background", image);
                SetReference(serialized, "iconImage", icon);
                SetReference(serialized, "gradeText", grade);
                SetReference(serialized, "nameText", name);
                SetReference(serialized, "metaText", meta);
                SetReference(serialized, "descriptionText", description);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RelicPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildChoiceOption(
            Font font,
            PresentationSpriteCatalog spriteCatalog,
            PresentationTheme theme)
        {
            var root = new GameObject(
                "PF_RunChoiceOption",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline),
                typeof(RunChoiceOptionView));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(442f, 166f);
                var image = root.GetComponent<Image>();
                image.color = theme.ButtonNormal;
                var outline = root.GetComponent<Outline>();
                outline.effectColor = theme.PanelBorder;
                outline.effectDistance = new Vector2(1f, -1f);
                var button = root.GetComponent<Button>();
                button.targetGraphic = image;
                ConfigureButton(button, theme);
                var icon = CreateImage(
                    "Icon",
                    root.transform,
                    Color.white);
                SetRect(icon.rectTransform, 14f, 18f, 82f, 82f);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.gameObject.SetActive(false);
                var badge = CreateText("Badge", root.transform, font, "冠冕 · 触发", 13,
                    TextAnchor.MiddleLeft);
                SetRect(badge.rectTransform, 14f, 132f, 414f, 24f);
                badge.color = theme.Accent;
                var title = CreateText("Title", root.transform, font, "双生战号", 21,
                    TextAnchor.MiddleLeft);
                title.fontStyle = FontStyle.Bold;
                title.color = theme.TextPrimary;
                SetRect(title.rectTransform, 112f, 92f, 316f, 38f);
                var description = CreateText("Description", root.transform, font,
                    "你的战吼额外触发一次。", 14, TextAnchor.UpperLeft);
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                description.verticalOverflow = VerticalWrapMode.Truncate;
                description.color = theme.TextSecondary;
                SetRect(description.rectTransform, 112f, 14f, 316f, 74f);

                var serialized = new SerializedObject(root.GetComponent<RunChoiceOptionView>());
                SetReference(serialized, "theme", theme);
                SetReference(serialized, "spriteCatalog", spriteCatalog);
                SetReference(serialized, "button", button);
                SetReference(serialized, "background", image);
                SetReference(serialized, "iconImage", icon);
                SetReference(serialized, "badgeText", badge);
                SetReference(serialized, "titleText", title);
                SetReference(serialized, "descriptionText", description);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ChoicePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildScreen(
            Font font,
            GameObject nodePrefab,
            GameObject edgePrefab,
            GameObject relicPrefab,
            GameObject choicePrefab,
            PresentationTheme theme)
        {
            var root = new GameObject(
                "PF_RunScreen",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(RunScreenView));
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
                    theme.ScreenBackground).rectTransform;
                Stretch(safeArea, Vector2.zero, Vector2.zero);
                var top = CreateImage(
                    "TopBar",
                    safeArea,
                    theme.PanelBackground).rectTransform;
                SetRect(top, 20f, 20f, 1880f, 92f, true);
                AddPanelOutline(top, theme);
                var topAccent = CreateImage("AccentRule", top, theme.Accent);
                SetRect(topAccent.rectTransform, 0f, 0f, 1880f, 3f);
                topAccent.raycastTarget = false;
                var title = CreateText("Title", top, font, "第 1 层 · 三层远征", 28,
                    TextAnchor.MiddleLeft);
                SetRect(title.rectTransform, 18f, 12f, 330f, 68f);
                title.color = theme.TextPrimary;
                var resources = CreateText("Resources", top, font,
                    "生命 20/20   商店回合 0   战绩 0胜/0未胜", 18,
                    TextAnchor.MiddleCenter);
                SetRect(resources.rectTransform, 350f, 12f, 650f, 68f);
                resources.color = theme.TextSecondary;
                var progress = CreateText("Progress", top, font,
                    "本层商店 0/6   固定战斗 0/6   地图步数 0", 17,
                    TextAnchor.MiddleCenter);
                SetRect(progress.rectTransform, 1000f, 12f, 520f, 68f);
                progress.color = theme.TextSecondary;
                var status = CreateText("Status", top, font, "选择可达节点继续三层单局", 16,
                    TextAnchor.MiddleRight);
                SetRect(status.rectTransform, 1490f, 12f, 230f, 68f);
                status.color = theme.Success;

                var body = CreateRect("Body", safeArea);
                Stretch(body, new Vector2(20f, 190f), new Vector2(-20f, -140f));
                var mapPanel = CreateImage(
                    "MapPanel",
                    body,
                    theme.PanelBackground).rectTransform;
                Stretch(mapPanel, Vector2.zero, new Vector2(-410f, 0f));
                AddPanelOutline(mapPanel, theme);
                var routeHint = CreateText("RouteHint", mapPanel, font,
                    "C2/C5 选择机制 · C4 选择路线 · 事件可能触发额外战斗", 17,
                    TextAnchor.MiddleLeft);
                SetRect(routeHint.rectTransform, 18f, 12f, 1434f, 38f, true);
                routeHint.color = theme.TextSecondary;
                var mapScroll = BuildMapScroll(
                    mapPanel,
                    theme,
                    out var mapContent,
                    out var mapBackdrop,
                    out var edgeLayer,
                    out var nodeLayer);

                var relicPanel = CreateImage(
                    "RelicPanel",
                    body,
                    theme.PanelBackground).rectTransform;
                relicPanel.anchorMin = new Vector2(1f, 0f);
                relicPanel.anchorMax = Vector2.one;
                relicPanel.pivot = new Vector2(1f, 0f);
                relicPanel.offsetMin = new Vector2(-390f, 0f);
                relicPanel.offsetMax = Vector2.zero;
                AddPanelOutline(relicPanel, theme);
                var relicCount = CreateText("RelicCount", relicPanel, font, "遗珍 0", 23,
                    TextAnchor.MiddleLeft);
                SetRect(relicCount.rectTransform, 18f, 12f, 354f, 42f, true);
                relicCount.color = theme.TextPrimary;
                var relicEmpty = CreateText("Empty", relicPanel, font,
                    "尚未获得遗珍\n第一、二层 Boss 会提供冠冕级遗珍。", 15,
                    TextAnchor.UpperCenter);
                SetRect(relicEmpty.rectTransform, 24f, 74f, 342f, 116f, true);
                relicEmpty.color = WithAlpha(theme.TextSecondary, 0.58f);
                var relicScroll = BuildVerticalScroll(
                    "RelicScroll", relicPanel, 16f, 20f, 358f, 660f,
                    out var relicContent, 10f, theme);
                Stretch(
                    relicScroll.GetComponent<RectTransform>(),
                    new Vector2(16f, 20f),
                    new Vector2(-16f, -70f));

                var summaryPanel = CreateImage(
                    "SummaryPanel",
                    safeArea,
                    theme.PanelBackground).rectTransform;
                SetRect(summaryPanel, 20f, 24f, 1880f, 138f);
                AddPanelOutline(summaryPanel, theme);
                var summary = CreateText("Summary", summaryPanel, font,
                    "选择高亮节点继续；未选择的互斥路线会在进入后锁定。", 18,
                    TextAnchor.MiddleLeft);
                SetRect(summary.rectTransform, 22f, 16f, 1510f, 106f);
                summary.color = theme.TextPrimary;
                var summaryButtonImage = CreateImage(
                    "ActionButton", summaryPanel, theme.ButtonNormal);
                SetRect(summaryButtonImage.rectTransform, 1550f, 30f, 300f, 78f);
                var summaryButton = summaryButtonImage.gameObject.AddComponent<Button>();
                summaryButton.targetGraphic = summaryButtonImage;
                ConfigureButton(summaryButton, theme);
                var summaryButtonText = CreateText("Label", summaryButtonImage.transform,
                    font, "继续前进", 20, TextAnchor.MiddleCenter);
                Stretch(summaryButtonText.rectTransform, new Vector2(8f, 6f),
                    new Vector2(-8f, -6f));
                summaryButtonText.color = theme.TextPrimary;

                var choiceOverlay = CreateImage(
                    "ChoiceOverlay", safeArea, theme.ModalScrim).gameObject;
                Stretch(choiceOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
                var dialog = CreateImage("Dialog", choiceOverlay.transform,
                    theme.PanelBackground).rectTransform;
                dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f);
                dialog.pivot = new Vector2(0.5f, 0.5f);
                dialog.anchoredPosition = Vector2.zero;
                dialog.sizeDelta = new Vector2(1500f, 620f);
                AddPanelOutline(dialog, theme);
                var choiceTitle = CreateText("Title", dialog, font,
                    "选择一件 Boss 遗珍", 30, TextAnchor.MiddleCenter);
                SetRect(choiceTitle.rectTransform, 36f, 540f, 1428f, 58f);
                choiceTitle.color = theme.TextPrimary;
                var choiceDescription = CreateText("Description", dialog, font,
                    "冠冕级遗珍会在后续楼层持续改变规则。", 18,
                    TextAnchor.UpperCenter);
                choiceDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
                choiceDescription.color = theme.TextSecondary;
                SetRect(choiceDescription.rectTransform, 60f, 465f, 1380f, 66f);
                var choiceScroll = BuildChoiceScroll(
                    dialog, out var choiceContent, theme);
                choiceOverlay.SetActive(false);

                var view = root.GetComponent<RunScreenView>();
                var serialized = new SerializedObject(view);
                SetReference(serialized, "theme", theme);
                SetReference(serialized, "rootCanvas", canvas);
                SetReference(serialized, "safeArea", safeArea);
                SetReference(serialized, "titleText", title);
                SetReference(serialized, "resourceText", resources);
                SetReference(serialized, "progressText", progress);
                SetReference(serialized, "statusText", status);
                SetReference(serialized, "routeHintText", routeHint);
                SetReference(serialized, "mapScrollRect", mapScroll);
                SetReference(serialized, "mapContent", mapContent);
                SetReference(serialized, "mapBackdrop", mapBackdrop);
                SetReference(serialized, "edgeLayer", edgeLayer);
                SetReference(serialized, "nodeLayer", nodeLayer);
                SetReference(serialized, "mapNodePrefab", nodePrefab);
                SetReference(serialized, "mapEdgePrefab", edgePrefab);
                SetReference(serialized, "relicCountText", relicCount);
                SetReference(serialized, "relicEmptyText", relicEmpty);
                SetReference(serialized, "relicScrollRect", relicScroll);
                SetReference(serialized, "relicContent", relicContent);
                SetReference(serialized, "relicEntryPrefab", relicPrefab);
                SetReference(serialized, "summaryText", summary);
                SetReference(serialized, "summaryActionButton", summaryButton);
                SetReference(serialized, "summaryActionText", summaryButtonText);
                SetReference(serialized, "choiceOverlay", choiceOverlay);
                SetReference(serialized, "choiceTitleText", choiceTitle);
                SetReference(serialized, "choiceDescriptionText", choiceDescription);
                SetReference(serialized, "choiceScrollRect", choiceScroll);
                SetReference(serialized, "choiceContent", choiceContent);
                SetReference(serialized, "choiceOptionPrefab", choicePrefab);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ScreenPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static ScrollRect BuildMapScroll(
            Transform parent,
            PresentationTheme theme,
            out RectTransform content,
            out Image backdrop,
            out RectTransform edgeLayer,
            out RectTransform nodeLayer)
        {
            var scroll = CreateImage("MapScroll", parent,
                theme.MapCanvasBackground).rectTransform;
            Stretch(scroll, new Vector2(16f, 20f), new Vector2(-16f, -62f));
            AddPanelOutline(scroll, theme);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 44f;
            var viewport = CreateImage("Viewport", scroll,
                new Color(0f, 0f, 0f, 0.01f)).rectTransform;
            Stretch(viewport, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            content = CreateRect("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0f, 0.5f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(2400f, 620f);
            backdrop = CreateImage(
                "Backdrop",
                content,
                theme.MapCanvasBackground);
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            backdrop.raycastTarget = false;
            BuildMapDecorations(backdrop.rectTransform, theme);
            edgeLayer = CreateRect("EdgeLayer", content);
            Stretch(edgeLayer, Vector2.zero, Vector2.zero);
            nodeLayer = CreateRect("NodeLayer", content);
            Stretch(nodeLayer, Vector2.zero, Vector2.zero);
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return scrollRect;
        }

        private static ScrollRect BuildVerticalScroll(
            string name,
            Transform parent,
            float left,
            float bottom,
            float width,
            float height,
            out RectTransform content,
            float spacing,
            PresentationTheme theme)
        {
            var scroll = CreateImage(name, parent,
                WithAlpha(theme.ScreenBackground, 0.44f)).rectTransform;
            SetRect(scroll, left, bottom, width, height);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            var viewport = CreateImage("Viewport", scroll,
                new Color(0f, 0f, 0f, 0.01f)).rectTransform;
            Stretch(viewport, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return scrollRect;
        }

        private static ScrollRect BuildChoiceScroll(
            Transform parent,
            out RectTransform content,
            PresentationTheme theme)
        {
            var scroll = CreateImage("OptionsScroll", parent,
                WithAlpha(theme.ScreenBackground, 0.44f)).rectTransform;
            SetRect(scroll, 48f, 42f, 1404f, 400f);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            var viewport = CreateImage("Viewport", scroll,
                new Color(0f, 0f, 0f, 0.01f)).rectTransform;
            Stretch(viewport, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(442f, 166f);
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(22, 22, 18, 18);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return scrollRect;
        }

        private static void BuildMapDecorations(
            RectTransform parent,
            PresentationTheme theme)
        {
            CreateMapRule("UpperRule", parent, 0.84f, theme);
            CreateMapRule("LowerRule", parent, 0.16f, theme);

            var anchors = new[]
            {
                new Vector2(0.05f, 0.62f),
                new Vector2(0.13f, 0.22f),
                new Vector2(0.24f, 0.78f),
                new Vector2(0.38f, 0.18f),
                new Vector2(0.52f, 0.72f),
                new Vector2(0.66f, 0.28f),
                new Vector2(0.77f, 0.82f),
                new Vector2(0.88f, 0.38f),
                new Vector2(0.95f, 0.68f)
            };
            for (var index = 0; index < anchors.Length; index++)
            {
                var mark = CreateImage(
                    "WayfinderMark_" + index,
                    parent,
                    theme.MapDecorationTint);
                var rect = mark.rectTransform;
                rect.anchorMin = rect.anchorMax = anchors[index];
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                var size = index % 3 == 0 ? 8f : 5f;
                rect.sizeDelta = new Vector2(size, size);
                rect.localEulerAngles = new Vector3(0f, 0f, 45f);
                mark.raycastTarget = false;
            }

            var compass = CreateImage(
                "CompassRose",
                parent,
                WithAlpha(theme.MapDecorationTint, 0.24f));
            compass.rectTransform.anchorMin = compass.rectTransform.anchorMax =
                new Vector2(0.5f, 0.5f);
            compass.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            compass.rectTransform.anchoredPosition = Vector2.zero;
            compass.rectTransform.sizeDelta = new Vector2(54f, 54f);
            compass.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            compass.raycastTarget = false;
            var inset = CreateImage(
                "Inset",
                compass.transform,
                theme.MapCanvasBackground);
            Stretch(
                inset.rectTransform,
                new Vector2(8f, 8f),
                new Vector2(-8f, -8f));
            inset.raycastTarget = false;
        }

        private static void CreateMapRule(
            string name,
            Transform parent,
            float normalizedY,
            PresentationTheme theme)
        {
            var rule = CreateImage(
                name,
                parent,
                WithAlpha(theme.MapDecorationTint, 0.15f));
            var rect = rule.rectTransform;
            rect.anchorMin = new Vector2(0.04f, normalizedY);
            rect.anchorMax = new Vector2(0.96f, normalizedY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 2f);
            rule.raycastTarget = false;
        }

        private static void AddPanelOutline(
            RectTransform rect,
            PresentationTheme theme)
        {
            var outline = rect.gameObject.GetComponent<Outline>() ??
                          rect.gameObject.AddComponent<Outline>();
            outline.effectColor = theme.PanelBorder;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        private static void ConfigureButton(
            Button button,
            PresentationTheme theme)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(
                Color.white,
                theme.ButtonHighlighted,
                0.18f);
            colors.pressedColor = Color.Lerp(
                Color.white,
                theme.ButtonPressed,
                0.32f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Color.Lerp(
                Color.white,
                theme.ButtonDisabled,
                0.52f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static RunScreenState CreatePreviewState()
        {
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();
            var run = new RunSession(configs, 8128);
            var state = RunScreenStateBuilder.Build(run, configs, "选择可达节点继续三层单局");
            state.Relics = new[]
            {
                new RunRelicState
                {
                    RelicId = "crown_echo_bell",
                    IconId = "icon_relic_crown_echo_bell",
                    Name = "回魂丧钟",
                    Description = "你的随从的亡语额外触发一次。",
                    GradeText = "冠冕",
                    CategoryText = "亡语",
                    ProgressText = "持续生效"
                },
                new RunRelicState
                {
                    RelicId = "curio_refresh_gear",
                    IconId = "icon_relic_curio_refresh_gear",
                    Name = "漏刻齿轮",
                    Description = "每个商店阶段第一次付费刷新免费。",
                    GradeText = "奇物",
                    CategoryText = "刷新",
                    ProgressText = "本阶段可用"
                }
            };
            return state;
        }

        private static RunChoiceOptionState PreviewChoice(
            string label,
            string description,
            string badge,
            string iconId)
        {
            return new RunChoiceOptionState
            {
                IconId = iconId,
                Label = label,
                Description = description,
                Badge = badge,
                IsInteractable = true,
                Action = RunUiActionType.SelectRelic
            };
        }

        private static GameObject LoadPrefab(string path)
        {
            var value = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (value == null)
            {
                throw new InvalidOperationException("Missing generated prefab " + path);
            }
            return value;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
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
                throw new InvalidOperationException("Missing serialized property " + name);
            }
            property.objectReferenceValue = value;
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
                width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(
                width, height, TextureFormat.RGBA32, false);
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
