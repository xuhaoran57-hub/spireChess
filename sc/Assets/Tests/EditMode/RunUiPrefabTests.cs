using System.Linq;
using NUnit.Framework;
using SpireChess.Run;
using SpireChess.UI.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class RunUiPrefabTests
    {
        private const string RootPath = "Assets/Prefabs/UI/Run/";
        private const string ScreenPath = RootPath + "PF_RunScreen.prefab";
        private const string ScenePath = "Assets/Scenes/RunTest.unity";

        private static readonly string[] RequiredScreenPaths =
        {
            "SafeArea/TopBar/Title",
            "SafeArea/TopBar/Resources",
            "SafeArea/TopBar/Progress",
            "SafeArea/TopBar/Status",
            "SafeArea/Body/MapPanel/RouteHint",
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
            foreach (var path in RequiredScreenPaths)
            {
                Assert.That(root.Find(path), Is.Not.Null,
                    "Missing stable PF_RunScreen path: " + path);
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

        private static RunScreenState CreateState()
        {
            var nodes = Enumerable.Range(0, 19).Select(index => new RunMapNodeState
            {
                NodeId = "node_" + index,
                Title = "节点 " + index,
                Subtitle = "验证地图节点",
                Column = index,
                Row = index % 3 - 1,
                Type = index == 0 ? RunNodeType.Shop : RunNodeType.Normal,
                Status = index == 0 ? RunNodeStatus.Reachable : RunNodeStatus.Locked,
                IsInteractable = index == 0
            }).ToArray();
            var edges = Enumerable.Range(0, 18).Select(index => new RunMapEdgeState
            {
                FromNodeId = "node_" + index,
                ToNodeId = "node_" + (index + 1),
                FromStatus = nodes[index].Status,
                ToStatus = nodes[index + 1].Status
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
                            IsInteractable = true
                        }).ToArray()
                },
                Summary = new RunSummaryState { Text = "等待选择" }
            };
        }
    }
}
