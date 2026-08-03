using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.UI;
using SpireChess.UI.Run;
using SpireChess.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpireChess.Tests.EditMode
{
    public sealed class RunUiPrefabTests
    {
        private const string RootPath = "Assets/Prefabs/UI/Run/";
        private const string ScreenPath = RootPath + "PF_RunScreen.prefab";
        private const string ScenePath = "Assets/Scenes/RunTest.unity";
        private const string ThemePath =
            "Assets/Configs/Presentation/PresentationTheme.asset";

        private static readonly string[] RequiredScreenPaths =
        {
            "SafeArea/TopBar/Title",
            "SafeArea/TopBar/Resources",
            "SafeArea/TopBar/Progress",
            "SafeArea/TopBar/Status",
            "SafeArea/Body/MapPanel/RouteHint",
            "SafeArea/Body/MapPanel/MapScroll/Viewport/Content/Backdrop",
            "SafeArea/Body/MapPanel/MapScroll/Viewport/Content/EdgeLayer",
            "SafeArea/Body/MapPanel/MapScroll/Viewport/Content/NodeLayer",
            "SafeArea/Body/RelicPanel/RelicCount",
            "SafeArea/Body/RelicPanel/RelicScroll/Viewport/Content",
            "SafeArea/SummaryPanel/Summary",
            "SafeArea/SummaryPanel/ActionButton",
            "SafeArea/ChoiceOverlay/Dialog/Title",
            "SafeArea/ChoiceOverlay/Dialog/Description",
            "SafeArea/ChoiceOverlay/Dialog/OptionsScroll/Viewport/Content"
        };

        private GameObject screen;
        private RectTransform root;
        private RunScreenView view;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath);
            Assert.That(prefab, Is.Not.Null, "PF_RunScreen could not be loaded.");
            screen = Object.Instantiate(prefab);
            root = screen.GetComponent<RectTransform>();
            view = screen.GetComponent<RunScreenView>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(screen);
        }

        [Test]
        public void Prefabs_HaveStableHierarchyAndCompleteBindings()
        {
            AssertPrefabBinding<RunMapNodeView>(
                "PF_RunMapNode.prefab", value => value.HasCompleteBindings);
            AssertPrefabBinding<RunRelicEntryView>(
                "PF_RunRelicEntry.prefab", value => value.HasCompleteBindings);
            AssertPrefabBinding<RunChoiceOptionView>(
                "PF_RunChoiceOption.prefab", value => value.HasCompleteBindings);
            var edge = AssetDatabase.LoadAssetAtPath<GameObject>(
                RootPath + "PF_RunMapEdge.prefab");
            Assert.That(edge, Is.Not.Null);
            Assert.That(edge.GetComponent<Image>(), Is.Not.Null);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasCompleteBindings, Is.True);
            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                RootPath + "PF_RunMapNode.prefab");
            Assert.That(nodePrefab.transform.Find("TypeIcon/Glyph"), Is.Not.Null);
            Assert.That(nodePrefab.transform.Find("StateOverlay"), Is.Not.Null);
            Assert.That(nodePrefab.transform.Find("CurrentPulse"), Is.Not.Null);
            foreach (var path in RequiredScreenPaths)
            {
                Assert.That(root.Find(path), Is.Not.Null,
                    "Missing stable PF_RunScreen path: " + path);
            }
        }

        [Test]
        public void JournalPage_RuntimeFallbackShowsChapterAndOneTimeUnlockNotice()
        {
            var state = CreateState();
            state.Choice = null;
            state.JournalPage = new RunJournalPageState
            {
                Kind = RunJournalPageKind.ChapterComplete,
                Title = "荒野 · 章节完成",
                Body = "Boss 已击败，遗珍已结算。",
                UnlockNotification = "新角色已解锁：法师",
                ArtworkId = "map_wilderness",
                ActionLabel = "进入下一章",
                Action = RunUiActionType.ContinueToNextFloor
            };

            view.Render(state);

            Assert.That(view.IsJournalPageVisible, Is.True);
            Assert.That(
                root.Find("SafeArea/JournalPageOverlay/JournalPage/NeutralJournalArtwork"),
                Is.Not.Null);
            Assert.That(
                root.Find("SafeArea/JournalPageOverlay/JournalPage/NeutralJournalArtwork")
                    .GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(
                root.Find("SafeArea/JournalPageOverlay/JournalPage/NeutralJournalArtwork")
                    .Find("Label").gameObject.activeSelf,
                Is.False);
            Assert.That(
                root.Find("SafeArea/JournalPageOverlay/JournalPage/UnlockNotice")
                    .GetComponent<Text>().text,
                Is.EqualTo("新角色已解锁：法师"));

            state.JournalPage.UnlockNotification = string.Empty;
            view.Render(state);
            Assert.That(
                root.Find("SafeArea/JournalPageOverlay/JournalPage/UnlockNotice")
                    .gameObject.activeSelf,
                Is.False,
                "A read unlock notification must not render a second time.");

            state.JournalPage = new RunJournalPageState
            {
                Kind = RunJournalPageKind.Ending,
                Title = "旅团日记 · 完结",
                Body = "三章旅程完成。",
                ActionLabel = "返回目录",
                Action = RunUiActionType.ReturnToMainMenu
            };
            view.Render(state);
            Assert.That(
                root.Find("SafeArea/JournalPageOverlay/JournalPage/Title")
                    .GetComponent<Text>().text,
                Does.Contain("完结"));
            Assert.That(
                root.Find("SafeArea/JournalPageOverlay/JournalPage/NeutralJournalArtwork")
                    .GetComponent<Image>().sprite,
                Is.Not.Null);
        }

        [Test]
        public void Theme_DefinesCrossScreenSevenTypeFiveStateAndFourEdgeContracts()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(ThemePath);
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.ScreenBackground.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(theme.PanelBackground, Is.Not.EqualTo(theme.PanelRaised));
            Assert.That(theme.ButtonNormal, Is.Not.EqualTo(theme.ButtonHighlighted));
            Assert.That(theme.ButtonNormal, Is.Not.EqualTo(theme.ButtonPressed));
            Assert.That(theme.TextPrimary, Is.Not.EqualTo(theme.TextSecondary));
            Assert.That(theme.Accent, Is.Not.EqualTo(theme.Success));

            var nodeTypes = (RunNodeType[])Enum.GetValues(typeof(RunNodeType));
            Assert.That(nodeTypes, Has.Length.EqualTo(7));
            Assert.That(
                nodeTypes.Select(theme.GetMapTypeColor).Distinct().ToArray(),
                Has.Length.EqualTo(7));

            var nodeStatuses = (RunMapPresentationStatus[])Enum.GetValues(
                typeof(RunMapPresentationStatus));
            Assert.That(nodeStatuses, Has.Length.EqualTo(5));
            Assert.That(
                nodeStatuses.Select(status =>
                        theme.GetMapNodeColor(RunNodeType.Normal, status))
                    .Distinct()
                    .ToArray(),
                Has.Length.EqualTo(5));
            Assert.That(
                nodeStatuses
                    .Select(theme.GetMapStatusColor)
                    .Distinct()
                    .ToArray(),
                Has.Length.EqualTo(5));

            var edgeStatuses = (RunMapEdgePresentationStatus[])Enum.GetValues(
                typeof(RunMapEdgePresentationStatus));
            Assert.That(edgeStatuses, Has.Length.EqualTo(4));
            Assert.That(
                edgeStatuses
                    .Select(theme.GetMapEdgeColor)
                    .Distinct()
                    .ToArray(),
                Has.Length.EqualTo(4));
        }

        [Test]
        public void MapNode_RendersSevenTypeGlyphsAndFiveDistinctStatuses()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                RootPath + "PF_RunMapNode.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var nodeView = instance.GetComponent<RunMapNodeView>();
                var iconGlyphs = new HashSet<string>();
                foreach (RunNodeType type in Enum.GetValues(typeof(RunNodeType)))
                {
                    var iconId = "icon_map_" + type.ToString().ToLowerInvariant();
                    nodeView.Render(new RunMapNodeState
                    {
                        NodeId = type.ToString(),
                        IconId = iconId,
                        Type = type,
                        Status = RunNodeStatus.Reachable,
                        PresentationStatus = RunMapPresentationStatus.Reachable
                    });
                    Assert.That(nodeView.IconId, Is.EqualTo(iconId));
                    Assert.That(nodeView.IconGlyph, Is.Not.Empty);
                    iconGlyphs.Add(nodeView.IconGlyph);
                }
                Assert.That(iconGlyphs, Has.Count.EqualTo(7));

                var backgrounds = new HashSet<Color>();
                var outlines = new HashSet<Color>();
                var labels = new HashSet<string>();
                foreach (RunMapPresentationStatus status in Enum.GetValues(
                             typeof(RunMapPresentationStatus)))
                {
                    nodeView.Render(new RunMapNodeState
                    {
                        NodeId = status.ToString(),
                        IconId = "icon_map_normal",
                        Type = RunNodeType.Normal,
                        Status = RunNodeStatus.Locked,
                        PresentationStatus = status
                    });
                    backgrounds.Add(instance.GetComponent<Image>().color);
                    outlines.Add(instance.GetComponent<Outline>().effectColor);
                    labels.Add(instance.transform.Find("Status")
                        .GetComponent<Text>().text);
                    Assert.That(
                        nodeView.IsCurrentPulseVisible,
                        Is.EqualTo(status == RunMapPresentationStatus.Current));
                }
                Assert.That(backgrounds, Has.Count.EqualTo(5));
                Assert.That(outlines, Has.Count.EqualTo(5));
                Assert.That(labels, Has.Count.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Render_UsesFourThemeEdgeStyles()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(ThemePath);
            var state = CreateState();
            state.Nodes = state.Nodes.Take(5).ToArray();
            var statuses = new[]
            {
                RunMapEdgePresentationStatus.Locked,
                RunMapEdgePresentationStatus.Reachable,
                RunMapEdgePresentationStatus.Resolved,
                RunMapEdgePresentationStatus.Abandoned
            };
            state.Edges = statuses.Select((status, index) => new RunMapEdgeState
            {
                FromNodeId = "node_" + index,
                ToNodeId = "node_" + (index + 1),
                PresentationStatus = status
            }).ToArray();
            state.MaximumColumn = 4;

            view.Render(state);

            var expectedThickness = new[] { 2f, 5f, 7f, 3f };
            var edgeLayer = root.Find(
                "SafeArea/Body/MapPanel/MapScroll/Viewport/Content/EdgeLayer");
            for (var index = 0; index < statuses.Length; index++)
            {
                var edge = edgeLayer.Find(
                    $"Edge_node_{index}_node_{index + 1}");
                Assert.That(edge, Is.Not.Null);
                Assert.That(
                    edge.GetComponent<Image>().color,
                    Is.EqualTo(theme.GetMapEdgeColor(statuses[index])));
                Assert.That(
                    ((RectTransform)edge).sizeDelta.y,
                    Is.EqualTo(expectedThickness[index]).Within(0.001f));
            }
        }

        [Test]
        public void CanvasAndBody_UseResponsiveFrozenLayoutContract()
        {
            var scaler = screen.GetComponent<CanvasScaler>();
            Assert.That(scaler.referenceResolution,
                Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f));

            var body = root.Find("SafeArea/Body") as RectTransform;
            Assert.That(body.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(body.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(body.offsetMin, Is.EqualTo(new Vector2(20f, 190f)));
            Assert.That(body.offsetMax, Is.EqualTo(new Vector2(-20f, -140f)));
            var edgeLayer = root.Find(
                "SafeArea/Body/MapPanel/MapScroll/Viewport/Content/EdgeLayer");
            var nodeLayer = root.Find(
                "SafeArea/Body/MapPanel/MapScroll/Viewport/Content/NodeLayer");
            Assert.That(edgeLayer.GetSiblingIndex(), Is.LessThan(nodeLayer.GetSiblingIndex()));

            var mapScrollTransform = root.Find(
                "SafeArea/Body/MapPanel/MapScroll");
            var viewport = mapScrollTransform.Find("Viewport") as RectTransform;
            var content = viewport.Find("Content") as RectTransform;
            var mapScroll = mapScrollTransform.GetComponent<ScrollRect>();
            Assert.That(mapScroll.horizontal, Is.True);
            Assert.That(mapScroll.vertical, Is.False);
            Assert.That(mapScroll.viewport, Is.SameAs(viewport));
            Assert.That(mapScroll.content, Is.SameAs(content));
            Assert.That(content.anchorMin, Is.EqualTo(new Vector2(0f, 0.5f)));
            Assert.That(content.anchorMax, Is.EqualTo(new Vector2(0f, 0.5f)));
            Assert.That(content.pivot, Is.EqualTo(new Vector2(0f, 0.5f)));
        }

        [Test]
        public void RelicViews_HideBlankIconAndRenderDiagnosticForUnknownIcon()
        {
            var texture = new Texture2D(2, 2);
            var diagnostic = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            diagnostic.name = "diagnostic-relic-icon";
            var catalog = ScriptableObject.CreateInstance<PresentationSpriteCatalog>();
            var catalogSerialized = new SerializedObject(catalog);
            catalogSerialized.FindProperty("missingArtwork").objectReferenceValue = diagnostic;
            catalogSerialized.ApplyModifiedPropertiesWithoutUndo();

            var relic = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                RootPath + "PF_RunRelicEntry.prefab"));
            var choice = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                RootPath + "PF_RunChoiceOption.prefab"));
            try
            {
                AssertDiagnosticIcon(
                    relic.GetComponent<RunRelicEntryView>(),
                    relic.transform.Find("Icon").GetComponent<Image>(),
                    catalog,
                    diagnostic);
                AssertDiagnosticIcon(
                    choice.GetComponent<RunChoiceOptionView>(),
                    choice.transform.Find("Icon").GetComponent<Image>(),
                    catalog,
                    diagnostic);
            }
            finally
            {
                Object.DestroyImmediate(relic);
                Object.DestroyImmediate(choice);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(diagnostic);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Render_CreatesNineteenNodesCorrectEdgesRelicsAndChoicesWithoutLeaks()
        {
            var state = CreateState();
            view.Render(state);

            Assert.That(view.RenderedNodeCount, Is.EqualTo(19));
            Assert.That(view.RenderedEdgeCount, Is.EqualTo(18));
            Assert.That(view.RenderedRelicCount, Is.EqualTo(2));
            Assert.That(view.RenderedChoiceCount, Is.EqualTo(3));
            Assert.That(view.IsChoiceVisible, Is.True);
            Assert.That(view.FindNode("node_0"), Is.Not.Null);
            Assert.That(view.FindNode("node_18"), Is.Not.Null);
            var renderedChoices = view
                .GetComponentsInChildren<RunChoiceOptionView>(true);
            Assert.That(renderedChoices, Has.Length.EqualTo(3));
            Assert.That(
                renderedChoices.Select(choice => choice.Action),
                Is.All.EqualTo(RunUiActionType.SelectRelic));
            Assert.That(
                renderedChoices.Select(choice => choice.PrimaryId),
                Is.EqualTo(new[]
                {
                    "choice_0",
                    "choice_1",
                    "choice_2"
                }));
            Assert.That(
                renderedChoices.All(choice => choice.IsInteractable),
                Is.True);

            state.Nodes = System.Array.Empty<RunMapNodeState>();
            state.Edges = System.Array.Empty<RunMapEdgeState>();
            state.Relics = System.Array.Empty<RunRelicState>();
            state.Choice = null;
            view.Render(state);

            Assert.That(view.RenderedNodeCount, Is.Zero);
            Assert.That(view.RenderedEdgeCount, Is.Zero);
            Assert.That(view.RenderedRelicCount, Is.Zero);
            Assert.That(view.RenderedChoiceCount, Is.Zero);
            Assert.That(view.IsChoiceVisible, Is.False);
        }

        [Test]
        public void MapViewportApi_FullyCoversProductionFloorOneInThreeSegments()
        {
            var state = CreateProductionFloorOneState();
            Assert.That(state.Nodes, Has.Count.EqualTo(19));
            Assert.That(state.MaximumColumn, Is.EqualTo(12));
            Assert.That(state.Choice, Is.Null);
            view.Render(state);
            Assert.That(view.IsChoiceVisible, Is.False);

            var clampedLeft =
                view.SetMapViewportNormalizedPosition(-1f);
            Assert.That(
                clampedLeft.HorizontalNormalizedPosition,
                Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                view.MapHorizontalNormalizedPosition,
                Is.EqualTo(0f).Within(0.001f));

            var left = view.SetMapViewportSegment(
                RunMapViewportSegment.Left);
            var center = view.SetMapViewportSegment(
                RunMapViewportSegment.Center);
            var right = view.SetMapViewportSegment(
                RunMapViewportSegment.Right);
            var clampedRight =
                view.SetMapViewportNormalizedPosition(2f);

            Assert.That(
                center.HorizontalNormalizedPosition,
                Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(
                right.HorizontalNormalizedPosition,
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                clampedRight.HorizontalNormalizedPosition,
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                left.FullyVisibleNodeIds,
                Does.Contain("f1_shop_start"));
            Assert.That(
                right.FullyVisibleNodeIds,
                Does.Contain("f1_boss"));
            Assert.That(left.FullyVisibleNodeIds, Is.Not.Empty);
            Assert.That(center.FullyVisibleNodeIds, Is.Not.Empty);
            Assert.That(right.FullyVisibleNodeIds, Is.Not.Empty);
            Assert.That(
                left.IntersectingNodeIds,
                Is.SupersetOf(left.FullyVisibleNodeIds));
            Assert.That(
                center.IntersectingNodeIds,
                Is.SupersetOf(center.FullyVisibleNodeIds));
            Assert.That(
                right.IntersectingNodeIds,
                Is.SupersetOf(right.FullyVisibleNodeIds));

            var allFullyVisibleNodeIds = left.FullyVisibleNodeIds
                .Concat(center.FullyVisibleNodeIds)
                .Concat(right.FullyVisibleNodeIds)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.That(
                allFullyVisibleNodeIds,
                Is.EquivalentTo(state.Nodes.Select(node => node.NodeId)));

            Assert.That(
                left.ContentBoundsInViewport.width,
                Is.GreaterThan(left.ViewportBounds.width));
            Assert.That(
                left.ContentBoundsInViewport.width,
                Is.EqualTo(2400f).Within(0.01f));
            Assert.That(
                left.ContentBoundsInViewport.xMin,
                Is.EqualTo(left.ViewportBounds.xMin).Within(0.01f));
            Assert.That(
                right.ContentBoundsInViewport.xMax,
                Is.EqualTo(right.ViewportBounds.xMax).Within(0.01f));
        }

        [Test]
        public void RunScene_HasOneSerializedFormalRuntimePath()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var controllers = roots.SelectMany(value =>
                    value.GetComponentsInChildren<RunTestController>(true)).ToArray();
                var views = roots.SelectMany(value =>
                    value.GetComponentsInChildren<RunScreenView>(true)).ToArray();
                var canvases = roots.SelectMany(value =>
                    value.GetComponentsInChildren<Canvas>(true)).ToArray();
                var eventSystems = roots.SelectMany(value =>
                    value.GetComponentsInChildren<EventSystem>(true)).ToArray();

                Assert.That(controllers, Has.Length.EqualTo(1));
                Assert.That(views, Has.Length.EqualTo(1));
                Assert.That(canvases, Has.Length.EqualTo(1));
                Assert.That(eventSystems, Has.Length.EqualTo(1));
                var serialized = new SerializedObject(controllers[0]);
                Assert.That(serialized.FindProperty("screenView").objectReferenceValue,
                    Is.SameAs(views[0]));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertPrefabBinding<T>(
            string fileName,
            System.Func<T, bool> isComplete) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPath + fileName);
            Assert.That(prefab, Is.Not.Null);
            var component = prefab.GetComponent<T>();
            Assert.That(component, Is.Not.Null);
            Assert.That(isComplete(component), Is.True);
        }

        private static void AssertDiagnosticIcon(
            Component view,
            Image icon,
            PresentationSpriteCatalog catalog,
            Sprite diagnostic)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("spriteCatalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (view is RunRelicEntryView relic)
            {
                relic.Render(new RunRelicState());
                Assert.That(icon.gameObject.activeSelf, Is.False);
                LogAssert.Expect(
                    LogType.Warning,
                    "Presentation artwork 'missing-relic-icon' is missing. " +
                    "Fallback: '<none>'.");
                relic.Render(new RunRelicState { IconId = "missing-relic-icon" });
            }
            else
            {
                var choice = (RunChoiceOptionView)view;
                choice.Render(new RunChoiceOptionState());
                Assert.That(icon.gameObject.activeSelf, Is.False);
                LogAssert.Expect(
                    LogType.Warning,
                    "Presentation artwork 'missing-choice-icon' is missing. " +
                    "Fallback: '<none>'.");
                choice.Render(new RunChoiceOptionState
                {
                    IconId = "missing-choice-icon"
                });
            }

            Assert.That(icon.gameObject.activeSelf, Is.True);
            Assert.That(icon.sprite, Is.SameAs(diagnostic));
            Assert.That(icon.preserveAspect, Is.True);
        }

        private static RunScreenState CreateState()
        {
            var nodes = Enumerable.Range(0, 19).Select(index => new RunMapNodeState
            {
                NodeId = "node_" + index,
                IconId = index == 0 ? "icon_map_shop" : "icon_map_normal",
                Title = "节点 " + index,
                Subtitle = "验证地图节点",
                Column = index,
                Row = index % 3 - 1,
                Type = index == 0 ? RunNodeType.Shop : RunNodeType.Normal,
                Status = index == 0 ? RunNodeStatus.Reachable : RunNodeStatus.Locked,
                PresentationStatus = index == 0
                    ? RunMapPresentationStatus.Reachable
                    : RunMapPresentationStatus.Locked,
                IsInteractable = index == 0
            }).ToArray();
            var edges = Enumerable.Range(0, 18).Select(index => new RunMapEdgeState
            {
                FromNodeId = "node_" + index,
                ToNodeId = "node_" + (index + 1),
                FromStatus = nodes[index].Status,
                ToStatus = nodes[index + 1].Status,
                PresentationStatus = index == 0
                    ? RunMapEdgePresentationStatus.Reachable
                    : RunMapEdgePresentationStatus.Locked
            }).ToArray();
            return new RunScreenState
            {
                Title = "正式远征",
                ResourceSummary = "生命 20/20",
                ProgressSummary = "地图步数 0",
                Status = "测试状态",
                RouteHint = "测试路线",
                MaximumColumn = 18,
                Nodes = nodes,
                Edges = edges,
                Relics = new[]
                {
                    new RunRelicState { RelicId = "a", Name = "冠冕", GradeText = "冠冕" },
                    new RunRelicState { RelicId = "b", Name = "奇物", GradeText = "奇物" }
                },
                Choice = new RunChoiceOverlayState
                {
                    Title = "三选一",
                    Options = Enumerable.Range(0, 3).Select(index =>
                        new RunChoiceOptionState
                        {
                            Label = "选项 " + index,
                            Action = RunUiActionType.SelectRelic,
                            PrimaryId = "choice_" + index,
                            IsInteractable = true
                        }).ToArray()
                },
                Summary = new RunSummaryState { Text = "等待选择" }
            };
        }

        private static RunScreenState CreateProductionFloorOneState()
        {
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();
            var run = new RunSession(configs, 9201);
            return RunScreenStateBuilder.Build(
                run,
                configs,
                "Map viewport geometry test");
        }
    }
}
