using System.Linq;
using NUnit.Framework;
using SpireChess.Audio;
using SpireChess.Battle;
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
        private const string StandeePrefabPath =
            "Assets/Prefabs/UI/Battle/PF_BattleStandee.prefab";
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
            "SafeArea/FeedbackLayer/Feedback",
            "SafeArea/VfxLayer",
            "SafeArea/VfxLayer/BoardPulse",
            "SafeArea/ResultLayer/ResultCard/Title",
            "SafeArea/ResultLayer/ResultCard/Body",
            "SafeArea/StandeeDetailLayer/DetailCard",
            "SafeArea/StandeeDetailLayer/DetailMode"
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
            var slotImage = slotPrefab.GetComponent<Image>();
            Assert.That(slotImage, Is.Not.Null);
            Assert.That(slotImage.color.a, Is.LessThanOrEqualTo(0.001f),
                "Battle slots must remain visually transparent.");
            Assert.That(slotImage.raycastTarget, Is.True,
                "Transparent slots must still accept drag-and-drop.");
            Assert.That(slotPrefab.transform.Find("EmptyHint"), Is.Not.Null);
            Assert.That(slotPrefab.transform.Find("Content"), Is.Not.Null);
            var standeePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                StandeePrefabPath);
            Assert.That(standeePrefab, Is.Not.Null);
            var standee = standeePrefab.GetComponent<BattleStandeeView>();
            Assert.That(standee, Is.Not.Null);
            Assert.That(standee.HasCompleteBindings, Is.True);
            var standeeRootImage = standeePrefab.GetComponent<Image>();
            Assert.That(standeeRootImage.color.a,
                Is.LessThanOrEqualTo(0.001f));
            Assert.That(standeeRootImage.raycastTarget, Is.True);
            Assert.That(standeePrefab.transform.Find("PortraitMask/Portrait"),
                Is.Not.Null);
            Assert.That(standeePrefab.transform.Find("ShieldOverlay"),
                Is.Not.Null);
            Assert.That(standeePrefab.transform.Find("TauntBase"), Is.Not.Null);
            Assert.That(standeePrefab.transform.Find("DeathrattleSeal"),
                Is.Not.Null);
            Assert.That(standeePrefab.transform.Find("SplashMark"), Is.Not.Null);
            var targetHighlight = standeePrefab.transform
                .Find("TargetHighlight").GetComponent<Image>();
            Assert.That(targetHighlight, Is.Not.Null);
            Assert.That(targetHighlight.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(targetHighlight.fillCenter, Is.False,
                "Legal targets must use an outline, not a cyan fill block.");
            var shieldOverlay = standeePrefab.transform
                .Find("ShieldOverlay").GetComponent<Image>();
            Assert.That(shieldOverlay.color.a, Is.InRange(0.75f, 0.82f),
                "Shield must stay clearly readable over battle portraits.");
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                "Assets/Configs/Presentation/PresentationSpriteCatalog.asset");
            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(
                "Assets/Configs/Presentation/PresentationTheme.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.HasCompleteBattleStandeeSet, Is.True);
            Assert.That(catalog.BattleNormalStandeeFrame, Is.Not.Null);
            Assert.That(catalog.BattleGoldenStandeeFrame, Is.Not.Null);
            Assert.That(catalog.BattleNormalStandeeFrame,
                Is.Not.SameAs(catalog.BattleGoldenStandeeFrame));
            Assert.That(
                catalog.BattleStandeeShieldOverlay.name,
                Is.EqualTo("shield_overlay_bright_storybook_v1"));
            Assert.That(theme, Is.Not.Null);
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
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0f).Within(0.001f));

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
            var board = root.Find("SafeArea/Board") as RectTransform;
            var logPanel = root.Find("SafeArea/LogPanel") as RectTransform;
            var boardPulse =
                root.Find("SafeArea/VfxLayer/BoardPulse") as RectTransform;
            var detailLayer =
                root.Find("SafeArea/StandeeDetailLayer") as RectTransform;
            Assert.That(topBar, Is.Not.Null);
            Assert.That(board, Is.Not.Null);
            Assert.That(logPanel, Is.Not.Null);
            Assert.That(boardPulse, Is.Not.Null);
            Assert.That(detailLayer, Is.Not.Null);
            Assert.That(logPanel.GetComponent<Image>().raycastTarget, Is.False);
            AssertVerticalBattleArea(board);
            AssertVerticalBattleArea(detailLayer);
            Assert.That(boardPulse.sizeDelta.y, Is.EqualTo(-240f).Within(0.01f));
            Canvas.ForceUpdateCanvases();
            var topCorners = new Vector3[4];
            var boardCorners = new Vector3[4];
            var logCorners = new Vector3[4];
            topBar.GetWorldCorners(topCorners);
            board.GetWorldCorners(boardCorners);
            logPanel.GetWorldCorners(logCorners);
            Assert.That(boardCorners[2].y,
                Is.LessThan(topCorners[0].y),
                "Board must not overlap and hide the TopBar at 1920x1080.");
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

        [TestCase(BattlePlaybackEventKind.CombatStarted, "battle_start")]
        [TestCase(BattlePlaybackEventKind.RoundStarted, "battle_round")]
        [TestCase(BattlePlaybackEventKind.AttackStarted, "battle_attack")]
        [TestCase(BattlePlaybackEventKind.DamageApplied, "battle_damage")]
        [TestCase(BattlePlaybackEventKind.ShieldGained, "battle_shield_gain")]
        [TestCase(BattlePlaybackEventKind.ShieldLost, "battle_shield_break")]
        [TestCase(BattlePlaybackEventKind.StatsChanged, "battle_stats")]
        [TestCase(BattlePlaybackEventKind.UnitDied, "battle_death")]
        [TestCase(BattlePlaybackEventKind.UnitSummoned, "battle_summon")]
        [TestCase(BattlePlaybackEventKind.CombatEnded, "battle_end")]
        public void PlaybackKinds_HaveDedicatedFeedbackMappings(
            BattlePlaybackEventKind kind,
            string expected)
        {
            Assert.That(
                BattleScreenView.ResolveFeedbackId(kind),
                Is.EqualTo(expected));
        }

        [Test]
        public void BlockedDamage_UsesDedicatedNonHealthFeedback()
        {
            Assert.That(
                BattleScreenView.ResolveFeedbackId(
                    BattlePlaybackEventKind.DamageApplied,
                    true),
                Is.EqualTo("battle_damage_blocked"));
        }

        [Test]
        public void BattleAudioMapping_UsesStructuredEventDataOnly()
        {
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.AttackStarted),
                Is.EqualTo(PresentationAudioCueIds.BattleAttackLight));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.DamageApplied),
                Is.EqualTo(PresentationAudioCueIds.BattleHit));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.DamageApplied,
                    wasBlocked: true),
                Is.Null);
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.ShieldGained),
                Is.EqualTo(PresentationAudioCueIds.BattleShieldGain));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.ShieldLost),
                Is.EqualTo(PresentationAudioCueIds.BattleShieldBreak));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.StatsChanged,
                    attackDelta: 1),
                Is.EqualTo(PresentationAudioCueIds.BattleStatUp));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.StatsChanged,
                    healthDelta: -1),
                Is.Null);
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.UnitDied),
                Is.EqualTo(PresentationAudioCueIds.BattleDeath));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.UnitDied,
                    targetIsToken: true),
                Is.EqualTo(PresentationAudioCueIds.BattleTokenDeath));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.UnitSummoned),
                Is.EqualTo(PresentationAudioCueIds.BattleSummon));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.CombatEnded,
                    winner: BattleSide.Player),
                Is.EqualTo(PresentationAudioCueIds.BattleVictory));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.CombatEnded,
                    winner: BattleSide.Enemy),
                Is.EqualTo(PresentationAudioCueIds.BattleDefeat));
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.CombatEnded),
                Is.Null);
            Assert.That(
                BattleScreenView.ResolveAudioCueId(
                    BattlePlaybackEventKind.RoundStarted),
                Is.Null);
        }

        [Test]
        public void VfxLayer_UsesFinitePoolAndNeverBlocksInput()
        {
            var layer = root.Find("SafeArea/VfxLayer");
            var pool = layer.GetComponent<PresentationFxPool>();
            Assert.That(pool, Is.Not.Null);
            pool.Configure(
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                3);
            for (var index = 0; index < 5; index++)
            {
                pool.Play(
                    "反馈 " + index,
                    Color.cyan,
                    Vector2.zero);
            }

            Assert.That(pool.Capacity, Is.EqualTo(3));
            Assert.That(pool.ActiveCount, Is.EqualTo(3));
            foreach (var group in layer.GetComponentsInChildren<CanvasGroup>(true))
            {
                Assert.That(group.interactable, Is.False);
                Assert.That(group.blocksRaycasts, Is.False);
            }
            foreach (var graphic in layer.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False);
            }
        }

        [Test]
        public void SnapAndClear_RestoresInterruptedStandeeAndResultState()
        {
            view.Render(CreateState());
            var standee = ContentAt(
                    "SafeArea/Board/PlayerRow/Slots/Slot1/Content")
                .GetComponentInChildren<BattleStandeeView>();
            var group = standee.GetComponent<CanvasGroup>();
            group.alpha = 0.22f;
            standee.RectTransform.anchoredPosition = new Vector2(17f, -9f);
            standee.RectTransform.localScale = Vector3.one * 0.76f;
            view.ShowCombatResult(BattleSide.Player, "测试结算");

            view.SnapAndClear();

            Assert.That(group.alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                standee.RectTransform.anchoredPosition,
                Is.EqualTo(Vector2.zero));
            Assert.That(
                standee.RectTransform.localScale,
                Is.EqualTo(Vector3.one));
            Assert.That(view.ActiveFeedbackFxCount, Is.Zero);
            Assert.That(view.IsAnimationPlaying, Is.False);
            Assert.That(view.IsResultVisible, Is.False);
        }

        [Test]
        public void ResultLayer_MapsVictoryDefeatAndDrawWithoutOwningInput()
        {
            var layer = root.Find("SafeArea/ResultLayer");
            var group = layer.GetComponent<CanvasGroup>();

            view.ShowCombatResult(BattleSide.Player, "玩家胜利");
            Assert.That(view.IsResultVisible, Is.True);
            Assert.That(view.ResultTitle, Is.EqualTo("战斗胜利"));
            view.ShowCombatResult(BattleSide.Enemy, "敌方胜利");
            Assert.That(view.ResultTitle, Is.EqualTo("战斗失利"));
            view.ShowCombatResult(null, "平局");
            Assert.That(view.ResultTitle, Is.EqualTo("战斗平局"));
            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            foreach (var graphic in layer.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False);
            }
        }

        [Test]
        public void TwoTimesPlayback_OnlyHalvesPresentationDuration()
        {
            Assert.That(
                BattleScreenView.GetDurationScale(1f),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                BattleScreenView.GetDurationScale(2f),
                Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void Standee_RendersFourKeywordsTargetStateAndSharedDetail()
        {
            var state = CreateState();
            var model = state.PlayerCards[0];
            model.ArtId = "placeholder_card_forge_soul_shield_squire";
            model.Keywords = new[] { "嘲讽", "护盾", "亡语", "溅射" };
            model.AbilityLabels = model.Keywords;
            model.HasShield = true;
            model.IsLegalTarget = true;
            view.Render(state);

            var content = ContentAt(
                "SafeArea/Board/PlayerRow/Slots/Slot1/Content");
            var standee = content.GetComponentInChildren<BattleStandeeView>();
            Assert.That(standee, Is.Not.Null);
            Assert.That(standee.HasCompleteBindings, Is.True);
            Assert.That(standee.IsShieldVisible, Is.True);
            Assert.That(standee.IsTauntVisible, Is.True);
            Assert.That(standee.IsDeathrattleVisible, Is.True);
            Assert.That(standee.IsSplashVisible, Is.True);
            Assert.That(standee.IsTargetHighlighted, Is.True);
            var portraitAspectFitter =
                standee.GetComponentInChildren<AspectRatioFitter>();
            Assert.That(portraitAspectFitter, Is.Not.Null);
            Assert.That(
                portraitAspectFitter.aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            var portraitImage = standee.transform
                .Find("PortraitMask/Portrait")
                .GetComponent<Image>();
            Assert.That(
                portraitAspectFitter.aspectRatio,
                Is.EqualTo(
                    portraitImage.sprite.rect.width /
                    portraitImage.sprite.rect.height).Within(0.001f));

            standee.OnPointerEnter(null);
            Assert.That(view.IsStandeeDetailVisible, Is.True);
            Assert.That(view.IsStandeeDetailLocked, Is.False);
            Assert.That(view.DetailInstanceId, Is.EqualTo(model.InstanceId));
            standee.OnPointerExit(null);
            Assert.That(view.IsStandeeDetailVisible, Is.False);

            standee.OnPointerClick(null);
            Assert.That(view.IsStandeeDetailVisible, Is.True);
            Assert.That(view.IsStandeeDetailLocked, Is.True);
            standee.OnPointerExit(null);
            Assert.That(view.IsStandeeDetailVisible, Is.True);
            standee.OnPointerClick(null);
            Assert.That(view.IsStandeeDetailVisible, Is.False);
            Assert.That(view.IsStandeeDetailLocked, Is.False);
        }

        [Test]
        public void StandeeDetail_PreservesArtworkFallbackResolution()
        {
            var state = CreateState();
            var model = state.PlayerCards[0];
            model.ArtId = "missing_detail_art";
            model.ArtworkFallbackId =
                "placeholder_card_forge_soul_shield_squire";
            view.Render(state);

            var content = ContentAt(
                "SafeArea/Board/PlayerRow/Slots/Slot1/Content");
            var standee = content.GetComponentInChildren<BattleStandeeView>();
            Assert.That(standee, Is.Not.Null);
            standee.OnPointerEnter(null);

            var detail = root.Find(
                    "SafeArea/StandeeDetailLayer/DetailCard")
                .GetComponent<CardView>();
            var serialized = new SerializedObject(detail);
            var artwork = serialized.FindProperty("artwork")
                .objectReferenceValue as Image;
            Assert.That(artwork, Is.Not.Null);
            Assert.That(artwork.sprite, Is.Not.Null);
            Assert.That(
                artwork.sprite.name,
                Is.EqualTo("card_minion_forge_soul_shield_squire"));
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

        private static void AssertVerticalBattleArea(RectTransform rect)
        {
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(rect.anchoredPosition.y, Is.EqualTo(120f).Within(0.01f));
            Assert.That(rect.sizeDelta.y, Is.EqualTo(-240f).Within(0.01f));
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
