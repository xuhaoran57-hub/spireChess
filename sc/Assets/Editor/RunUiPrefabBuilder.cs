using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpireChess.Config;
using SpireChess.Run;
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

            BuildMapNode(font);
            BuildMapEdge();
            BuildRelicEntry(font);
            BuildChoiceOption(font);
            BuildScreen(
                font,
                LoadPrefab(NodePrefabPath),
                LoadPrefab(EdgePrefabPath),
                LoadPrefab(RelicPrefabPath),
                LoadPrefab(ChoicePrefabPath));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WireRunTestScene();
            Debug.Log("[RunUI] Rebuilt formal run prefabs.");
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
                "pf-run-screen-v0.1");
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
                    PreviewChoice("双生战号", "你的战吼额外触发一次。", "冠冕 · 触发"),
                    PreviewChoice("无价金券", "每个商店阶段第一次购买随从免费。", "冠冕 · 经济"),
                    PreviewChoice("星海秘囊", "商店开始时获得一张随机普通法术。", "冠冕 · 法术")
                }
            };
            view.Render(state);
            Capture(camera, canvasRect, 1920, 1080,
                Path.Combine(outputDirectory, "run-choice-1920x1080.png"));
            AssetDatabase.SaveAssets();
            Debug.Log("[RunUI] Captured validation screenshots to " + outputDirectory);
        }

        private static void BuildMapNode(Font font)
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
                rect.sizeDelta = new Vector2(154f, 104f);
                var image = root.GetComponent<Image>();
                image.color = new Color(0.12f, 0.30f, 0.38f, 1f);
                var outline = root.GetComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
                outline.effectDistance = new Vector2(2f, -2f);
                var button = root.GetComponent<Button>();
                button.targetGraphic = image;
                var element = root.GetComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = 154f;
                element.minHeight = element.preferredHeight = 104f;

                var route = CreateText("Route", root.transform, font, "强攻", 13,
                    TextAnchor.MiddleCenter);
                SetRect(route.rectTransform, 8f, 82f, 138f, 18f);
                route.color = new Color(1f, 0.78f, 0.28f, 1f);
                var title = CreateText("Title", root.transform, font, "第 4 战 · 精英", 17,
                    TextAnchor.MiddleCenter);
                title.fontStyle = FontStyle.Bold;
                SetRect(title.rectTransform, 8f, 51f, 138f, 30f);
                var subtitle = CreateText("Subtitle", root.transform, font, "铜墙守卫", 13,
                    TextAnchor.MiddleCenter);
                SetRect(subtitle.rectTransform, 8f, 28f, 138f, 23f);
                var status = CreateText("Status", root.transform, font, "可进入", 12,
                    TextAnchor.MiddleCenter);
                SetRect(status.rectTransform, 8f, 5f, 138f, 22f);
                status.color = new Color(0.62f, 1f, 0.82f, 1f);

                var serialized = new SerializedObject(root.GetComponent<RunMapNodeView>());
                SetReference(serialized, "background", image);
                SetReference(serialized, "outline", outline);
                SetReference(serialized, "button", button);
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

        private static void BuildMapEdge()
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
                image.color = new Color(1f, 1f, 1f, 0.12f);
                image.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(root, EdgePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildRelicEntry(Font font)
        {
            var root = new GameObject(
                "PF_RunRelicEntry",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                typeof(RunRelicEntryView));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(330f, 128f);
                var image = root.GetComponent<Image>();
                image.color = new Color(0.10f, 0.22f, 0.28f, 1f);
                var element = root.GetComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = 128f;
                element.flexibleWidth = 1f;
                var grade = CreateText("Grade", root.transform, font, "冠冕", 13,
                    TextAnchor.MiddleLeft);
                SetRect(grade.rectTransform, 12f, 98f, 70f, 22f);
                grade.color = new Color(1f, 0.78f, 0.28f, 1f);
                var name = CreateText("Name", root.transform, font, "双生战号", 18,
                    TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Bold;
                SetRect(name.rectTransform, 82f, 96f, 236f, 26f);
                var meta = CreateText("Meta", root.transform, font, "触发 · 持续生效", 12,
                    TextAnchor.MiddleLeft);
                SetRect(meta.rectTransform, 12f, 70f, 306f, 22f);
                meta.color = new Color(0.58f, 0.82f, 0.92f, 1f);
                var description = CreateText("Description", root.transform, font,
                    "你的战吼额外触发一次。", 13, TextAnchor.UpperLeft);
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                description.verticalOverflow = VerticalWrapMode.Truncate;
                SetRect(description.rectTransform, 12f, 8f, 306f, 58f);

                var serialized = new SerializedObject(root.GetComponent<RunRelicEntryView>());
                SetReference(serialized, "background", image);
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

        private static void BuildChoiceOption(Font font)
        {
            var root = new GameObject(
                "PF_RunChoiceOption",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(RunChoiceOptionView));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(442f, 166f);
                var image = root.GetComponent<Image>();
                image.color = new Color(0.13f, 0.20f, 0.28f, 1f);
                var button = root.GetComponent<Button>();
                button.targetGraphic = image;
                var badge = CreateText("Badge", root.transform, font, "冠冕 · 触发", 13,
                    TextAnchor.MiddleLeft);
                SetRect(badge.rectTransform, 14f, 132f, 414f, 24f);
                badge.color = new Color(1f, 0.78f, 0.28f, 1f);
                var title = CreateText("Title", root.transform, font, "双生战号", 21,
                    TextAnchor.MiddleLeft);
                title.fontStyle = FontStyle.Bold;
                SetRect(title.rectTransform, 14f, 92f, 414f, 38f);
                var description = CreateText("Description", root.transform, font,
                    "你的战吼额外触发一次。", 14, TextAnchor.UpperLeft);
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                description.verticalOverflow = VerticalWrapMode.Truncate;
                SetRect(description.rectTransform, 14f, 14f, 414f, 74f);

                var serialized = new SerializedObject(root.GetComponent<RunChoiceOptionView>());
                SetReference(serialized, "button", button);
                SetReference(serialized, "background", image);
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
            GameObject choicePrefab)
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

                var safeArea = CreateImage("SafeArea", root.transform, Background).rectTransform;
                Stretch(safeArea, Vector2.zero, Vector2.zero);
                var top = CreateImage("TopBar", safeArea, Panel).rectTransform;
                SetRect(top, 20f, 20f, 1880f, 92f, true);
                var title = CreateText("Title", top, font, "第 1 层 · 三层远征", 28,
                    TextAnchor.MiddleLeft);
                SetRect(title.rectTransform, 18f, 12f, 330f, 68f);
                var resources = CreateText("Resources", top, font,
                    "生命 20/20   商店回合 0   战绩 0胜/0未胜", 18,
                    TextAnchor.MiddleCenter);
                SetRect(resources.rectTransform, 350f, 12f, 650f, 68f);
                var progress = CreateText("Progress", top, font,
                    "本层商店 0/6   固定战斗 0/6   地图步数 0", 17,
                    TextAnchor.MiddleCenter);
                SetRect(progress.rectTransform, 1000f, 12f, 520f, 68f);
                var status = CreateText("Status", top, font, "选择可达节点继续三层单局", 16,
                    TextAnchor.MiddleRight);
                SetRect(status.rectTransform, 1520f, 12f, 342f, 68f);

                var body = CreateRect("Body", safeArea);
                Stretch(body, new Vector2(20f, 190f), new Vector2(-20f, -140f));
                var mapPanel = CreateImage("MapPanel", body, Panel).rectTransform;
                Stretch(mapPanel, Vector2.zero, new Vector2(-410f, 0f));
                var routeHint = CreateText("RouteHint", mapPanel, font,
                    "C2/C5 选择机制 · C4 选择路线 · 事件可能触发额外战斗", 17,
                    TextAnchor.MiddleLeft);
                SetRect(routeHint.rectTransform, 18f, 12f, 1434f, 38f, true);
                var mapScroll = BuildMapScroll(mapPanel, out var mapContent,
                    out var edgeLayer, out var nodeLayer);

                var relicPanel = CreateImage("RelicPanel", body, Panel).rectTransform;
                relicPanel.anchorMin = new Vector2(1f, 0f);
                relicPanel.anchorMax = Vector2.one;
                relicPanel.pivot = new Vector2(1f, 0f);
                relicPanel.offsetMin = new Vector2(-390f, 0f);
                relicPanel.offsetMax = Vector2.zero;
                var relicCount = CreateText("RelicCount", relicPanel, font, "遗珍 0", 23,
                    TextAnchor.MiddleLeft);
                SetRect(relicCount.rectTransform, 18f, 12f, 354f, 42f, true);
                var relicEmpty = CreateText("Empty", relicPanel, font,
                    "尚未获得遗珍\n第一、二层 Boss 会提供冠冕级遗珍。", 15,
                    TextAnchor.UpperCenter);
                SetRect(relicEmpty.rectTransform, 24f, 74f, 342f, 116f, true);
                relicEmpty.color = new Color(1f, 1f, 1f, 0.46f);
                var relicScroll = BuildVerticalScroll(
                    "RelicScroll", relicPanel, 16f, 20f, 358f, 660f,
                    out var relicContent, 10f);
                Stretch(
                    relicScroll.GetComponent<RectTransform>(),
                    new Vector2(16f, 20f),
                    new Vector2(-16f, -70f));

                var summaryPanel = CreateImage("SummaryPanel", safeArea, Panel).rectTransform;
                SetRect(summaryPanel, 20f, 24f, 1880f, 138f);
                var summary = CreateText("Summary", summaryPanel, font,
                    "选择高亮节点继续；未选择的互斥路线会在进入后锁定。", 18,
                    TextAnchor.MiddleLeft);
                SetRect(summary.rectTransform, 22f, 16f, 1510f, 106f);
                var summaryButtonImage = CreateImage(
                    "ActionButton", summaryPanel, ButtonColor);
                SetRect(summaryButtonImage.rectTransform, 1550f, 30f, 300f, 78f);
                var summaryButton = summaryButtonImage.gameObject.AddComponent<Button>();
                summaryButton.targetGraphic = summaryButtonImage;
                var summaryButtonText = CreateText("Label", summaryButtonImage.transform,
                    font, "继续前进", 20, TextAnchor.MiddleCenter);
                Stretch(summaryButtonText.rectTransform, new Vector2(8f, 6f),
                    new Vector2(-8f, -6f));

                var choiceOverlay = CreateImage(
                    "ChoiceOverlay", safeArea, new Color(0f, 0f, 0f, 0.78f)).gameObject;
                Stretch(choiceOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
                var dialog = CreateImage("Dialog", choiceOverlay.transform,
                    new Color(0.065f, 0.08f, 0.12f, 1f)).rectTransform;
                dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f);
                dialog.pivot = new Vector2(0.5f, 0.5f);
                dialog.anchoredPosition = Vector2.zero;
                dialog.sizeDelta = new Vector2(1500f, 620f);
                var choiceTitle = CreateText("Title", dialog, font,
                    "选择一件 Boss 遗珍", 30, TextAnchor.MiddleCenter);
                SetRect(choiceTitle.rectTransform, 36f, 540f, 1428f, 58f);
                var choiceDescription = CreateText("Description", dialog, font,
                    "冠冕级遗珍会在后续楼层持续改变规则。", 18,
                    TextAnchor.UpperCenter);
                choiceDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
                SetRect(choiceDescription.rectTransform, 60f, 465f, 1380f, 66f);
                var choiceScroll = BuildChoiceScroll(
                    dialog, out var choiceContent);
                choiceOverlay.SetActive(false);

                var view = root.GetComponent<RunScreenView>();
                var serialized = new SerializedObject(view);
                SetReference(serialized, "rootCanvas", canvas);
                SetReference(serialized, "safeArea", safeArea);
                SetReference(serialized, "titleText", title);
                SetReference(serialized, "resourceText", resources);
                SetReference(serialized, "progressText", progress);
                SetReference(serialized, "statusText", status);
                SetReference(serialized, "routeHintText", routeHint);
                SetReference(serialized, "mapScrollRect", mapScroll);
                SetReference(serialized, "mapContent", mapContent);
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
            out RectTransform content,
            out RectTransform edgeLayer,
            out RectTransform nodeLayer)
        {
            var scroll = CreateImage("MapScroll", parent,
                new Color(0.025f, 0.04f, 0.06f, 0.9f)).rectTransform;
            Stretch(scroll, new Vector2(16f, 20f), new Vector2(-16f, -62f));
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
            float spacing)
        {
            var scroll = CreateImage(name, parent,
                new Color(0f, 0f, 0f, 0.08f)).rectTransform;
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
            out RectTransform content)
        {
            var scroll = CreateImage("OptionsScroll", parent,
                new Color(0f, 0f, 0f, 0.08f)).rectTransform;
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
                    RelicId = "preview-crown",
                    Name = "双生战号",
                    Description = "你的战吼额外触发一次。",
                    GradeText = "冠冕",
                    CategoryText = "触发",
                    ProgressText = "持续生效"
                },
                new RunRelicState
                {
                    RelicId = "preview-curio",
                    Name = "灵感墨瓶",
                    Description = "每经过两个商店阶段，获得一张随机普通法术。",
                    GradeText = "奇物",
                    CategoryText = "法术",
                    ProgressText = "进度 1/2"
                }
            };
            return state;
        }

        private static RunChoiceOptionState PreviewChoice(
            string label,
            string description,
            string badge)
        {
            return new RunChoiceOptionState
            {
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
