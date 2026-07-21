using System.Linq;
using NUnit.Framework;
using SpireChess.UI;
using SpireChess.UI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class BattleUiPrefabTests
    {
        private const string SlotPrefabPath =
            "Assets/Prefabs/UI/Battle/PF_BattleSlot.prefab";
        private const string ScreenPrefabPath =
            "Assets/Prefabs/UI/Battle/PF_BattleScreen.prefab";
        private const string BattleScenePath =
            "Assets/Scenes/BattleTest.unity";

        private static readonly string[] RequiredScreenPaths =
        {
            "SafeArea/TopBar/Title",
            "SafeArea/TopBar/Status",
            "SafeArea/TopBar/Round",
            "SafeArea/TopBar/Actions/Start",
            "SafeArea/TopBar/Actions/Speed",
            "SafeArea/TopBar/Actions/Skip",
            "SafeArea/TopBar/Actions/Preset",
            "SafeArea/TopBar/Actions/Reset",
            "SafeArea/TopBar/Actions/Return",
            "SafeArea/Board/EnemyRow/Slots/Slot1",
            "SafeArea/Board/EnemyRow/Slots/Slot5",
            "SafeArea/Board/PlayerRow/Slots/Slot1",
            "SafeArea/Board/PlayerRow/Slots/Slot5",
            "SafeArea/LogPanel/LogScroll/Viewport/LogText",
            "SafeArea/FeedbackLayer/Feedback"
        };

        private GameObject screen;
        private RectTransform root;
        private BattleScreenView view;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);
            Assert.That(prefab, Is.Not.Null, "PF_BattleScreen could not be loaded.");
            screen = Object.Instantiate(prefab);
            root = screen.GetComponent<RectTransform>();
            view = screen.GetComponent<BattleScreenView>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(screen);
        }

        [Test]
        public void Prefabs_HaveStableHierarchyAndCompleteBindings()
        {
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
            Assert.That(slotPrefab, Is.Not.Null);
            Assert.That(slotPrefab.GetComponent<BattleSlotView>(), Is.Not.Null);
            Assert.That(slotPrefab.GetComponent<BattleSlotView>().HasCompleteBindings,
                Is.True);
            Assert.That(slotPrefab.transform.Find("EmptyHint"), Is.Not.Null);
            Assert.That(slotPrefab.transform.Find("Content"), Is.Not.Null);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasCompleteBindings, Is.True);
            foreach (var path in RequiredScreenPaths)
            {
                Assert.That(root.Find(path), Is.Not.Null,
                    "Missing stable PF_BattleScreen path: " + path);
            }
        }

        [Test]
        public void CanvasAndRows_UseFrozenLayoutContract()
        {
            var scaler = screen.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution,
                Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.screenMatchMode,
                Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f));

            var enemyRow = root.Find("SafeArea/Board/EnemyRow/Slots")
                .GetComponent<HorizontalLayoutGroup>();
            var playerRow = root.Find("SafeArea/Board/PlayerRow/Slots")
                .GetComponent<HorizontalLayoutGroup>();
            Assert.That(enemyRow, Is.Not.Null);
            Assert.That(playerRow, Is.Not.Null);
            Assert.That(enemyRow.spacing, Is.EqualTo(28f).Within(0.01f));
            Assert.That(playerRow.spacing, Is.EqualTo(28f).Within(0.01f));
            AssertSlotSize("SafeArea/Board/EnemyRow/Slots/Slot1");
            AssertSlotSize("SafeArea/Board/PlayerRow/Slots/Slot1");

            var topBar = root.Find("SafeArea/TopBar") as RectTransform;
            var logPanel = root.Find("SafeArea/LogPanel") as RectTransform;
            Assert.That(topBar, Is.Not.Null);
            Assert.That(logPanel, Is.Not.Null);
            Assert.That(logPanel.GetComponent<Image>().raycastTarget, Is.False);
            Canvas.ForceUpdateCanvases();
            var topCorners = new Vector3[4];
            var logCorners = new Vector3[4];
            topBar.GetWorldCorners(topCorners);
            logPanel.GetWorldCorners(logCorners);
            Assert.That(logCorners[2].y,
                Is.LessThan(topCorners[0].y),
                "LogPanel must not overlap the TopBar action buttons.");
        }

        [Test]
        public void Render_PopulatesRowsAtSlotOriginAndClearsWithoutLeaks()
        {
            var state = CreateState();
            view.Render(state);

            Assert.That(view.RenderedCardCount, Is.EqualTo(3));
            Assert.That(TextAt("SafeArea/TopBar/Title"), Is.EqualTo("正式战斗"));
            Assert.That(view.LogContents, Is.EqualTo("第一条\n第二条"));
            AssertCardAnchoredAtTopLeft(
                "SafeArea/Board/EnemyRow/Slots/Slot2/Content");
            AssertCardAnchoredAtTopLeft(
                "SafeArea/Board/PlayerRow/Slots/Slot1/Content");

            state.EnemyCards = new CardViewModel[5];
            state.PlayerCards = new CardViewModel[5];
            view.Render(state);

            Assert.That(view.RenderedCardCount, Is.Zero);
            Assert.That(ContentAt(
                "SafeArea/Board/EnemyRow/Slots/Slot2/Content").childCount,
                Is.Zero);
            Assert.That(ContentAt(
                "SafeArea/Board/PlayerRow/Slots/Slot1/Content").childCount,
                Is.Zero);
        }

        [Test]
        public void BattleScene_HasOneSerializedFormalRuntimePath()
        {
            var scene = EditorSceneManager.OpenScene(
                BattleScenePath,
                OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var controllers = roots.SelectMany(rootObject =>
                    rootObject.GetComponentsInChildren<BattleTestController>(true))
                    .ToArray();
                var views = roots.SelectMany(rootObject =>
                    rootObject.GetComponentsInChildren<BattleScreenView>(true))
                    .ToArray();
                var canvases = roots.SelectMany(rootObject =>
                    rootObject.GetComponentsInChildren<Canvas>(true))
                    .ToArray();
                var eventSystems = roots.SelectMany(rootObject =>
                    rootObject.GetComponentsInChildren<EventSystem>(true))
                    .ToArray();

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

        private void AssertSlotSize(string path)
        {
            var element = root.Find(path).GetComponent<LayoutElement>();
            Assert.That(element, Is.Not.Null);
            Assert.That(element.preferredWidth, Is.EqualTo(176f).Within(0.01f));
            Assert.That(element.preferredHeight, Is.EqualTo(256f).Within(0.01f));
        }

        private void AssertCardAnchoredAtTopLeft(string path)
        {
            var content = ContentAt(path);
            Assert.That(content.childCount, Is.EqualTo(1));
            var card = content.GetChild(0) as RectTransform;
            Assert.That(card, Is.Not.Null);
            Assert.That(card.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(card.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(card.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(card.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        private RectTransform ContentAt(string path)
        {
            var content = root.Find(path) as RectTransform;
            Assert.That(content, Is.Not.Null, "Missing content path " + path);
            return content;
        }

        private string TextAt(string path)
        {
            var target = root.Find(path);
            Assert.That(target, Is.Not.Null);
            return target.GetComponent<Text>().text;
        }

        private static BattleScreenState CreateState()
        {
            var enemy = new CardViewModel[5];
            enemy[1] = CreateCard("enemy-1", "敌方一");
            var player = new CardViewModel[5];
            player[0] = CreateCard("player-1", "玩家一");
            player[3] = CreateCard("player-2", "玩家二");
            return new BattleScreenState
            {
                Title = "正式战斗",
                Status = "播放中",
                RoundText = "第 2 轮",
                LogText = "第一条\n第二条",
                EnemyCards = enemy,
                PlayerCards = player,
                Start = Button("开始战斗", true, true),
                Speed = Button("速度 2×", true, true),
                Skip = Button("跳过表现", true, true),
                Preset = Button("切换预设", false, false),
                Reset = Button("重置", false, false),
                Return = Button("查看结算", false, false)
            };
        }

        private static CardViewModel CreateCard(string id, string name)
        {
            return new CardViewModel
            {
                InstanceId = id,
                Name = name,
                Description = "用于验证正式战斗界面。",
                RaceText = "旅团",
                Tier = 2,
                Attack = 3,
                Health = 4,
                BaseAttack = 3,
                BaseHealth = 4,
                DisplayMode = CardDisplayMode.Compact,
                IsMinion = true,
                IsInteractable = true
            };
        }

        private static BattleButtonState Button(
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
    }
}
