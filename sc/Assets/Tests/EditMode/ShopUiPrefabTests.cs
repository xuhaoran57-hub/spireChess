using System;
using System.Linq;
using NUnit.Framework;
using SpireChess.UI;
using SpireChess.UI.Shop;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class ShopUiPrefabTests
    {
        private const string SlotPrefabPath =
            "Assets/Prefabs/UI/Shop/PF_ShopSlot.prefab";
        private const string ScreenPrefabPath =
            "Assets/Prefabs/UI/Shop/PF_ShopScreen.prefab";
        private const string ChoicePrefabPath =
            "Assets/Prefabs/UI/Shop/PF_ChoiceOverlay.prefab";

        private static readonly string[] RequiredScreenPaths =
        {
            "SafeArea/Background",
            "SafeArea/TopBar/RoundText",
            "SafeArea/TopBar/GoldText",
            "SafeArea/TopBar/TavernTierText",
            "SafeArea/TopBar/UpgradeCostText",
            "SafeArea/TopBar/StatusText",
            "SafeArea/Content/OfferPanel/Title",
            "SafeArea/Content/OfferPanel/OfferSlots/MinionSlot0",
            "SafeArea/Content/OfferPanel/OfferSlots/MinionSlot3",
            "SafeArea/Content/OfferPanel/OfferSlots/SpellSlot",
            "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0",
            "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot4",
            "SafeArea/Content/HandPanel/HandSlots/HandSlot0",
            "SafeArea/Content/HandPanel/HandSlots/HandSlot4",
            "SafeArea/Content/HandPanel/PageControls",
            "SafeArea/ActionRail/CardDetailPanel",
            "SafeArea/ActionRail/RefreshButton",
            "SafeArea/ActionRail/FreezeButton",
            "SafeArea/ActionRail/UpgradeButton",
            "SafeArea/ActionRail/SellButton",
            "SafeArea/ActionRail/EndButton",
            "SafeArea/FeedbackLayer/StatusToast",
            "SafeArea/FeedbackLayer/FloatingTextRoot",
            "SafeArea/ModalLayer/Blocker",
            "SafeArea/ModalLayer/ChoiceOverlay",
            "SafeArea/ModalLayer/ChoiceOverlay/Dialog/Candidates/Candidate3",
            "SafeArea/ModalLayer/RewardOverlay",
            "SafeArea/ModalLayer/RewardOverlay/RewardText",
            "SafeArea/ModalLayer/RewardOverlay/ClaimButton",
            "SafeArea/ModalLayer/RewardOverlay/SkipButton"
        };

        private GameObject screen;
        private RectTransform root;
        private ShopScreenView view;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);
            Assert.That(prefab, Is.Not.Null, "PF_ShopScreen could not be loaded.");
            screen = UnityEngine.Object.Instantiate(prefab);
            root = screen.GetComponent<RectTransform>();
            view = screen.GetComponent<ShopScreenView>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(screen);
        }

        [Test]
        public void Prefabs_HaveStableHierarchyAndCompleteBindings()
        {
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SlotPrefabPath);
            var choicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ChoicePrefabPath);
            Assert.That(slotPrefab, Is.Not.Null);
            Assert.That(choicePrefab, Is.Not.Null);
            Assert.That(slotPrefab.GetComponent<ShopSlotView>(), Is.Not.Null);
            Assert.That(
                slotPrefab.GetComponent<ShopSlotView>().HasCompleteBindings,
                Is.True);
            Assert.That(slotPrefab.transform.Find("Background"), Is.Not.Null);
            Assert.That(slotPrefab.transform.Find("EmptyHint"), Is.Not.Null);
            Assert.That(slotPrefab.transform.Find("SelectionFrame"), Is.Not.Null);
            Assert.That(slotPrefab.transform.Find("Content"), Is.Not.Null);
            Assert.That(
                choicePrefab.GetComponent<ChoiceOverlayView>().HasCompleteBindings,
                Is.True);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasCompleteBindings, Is.True);
            foreach (var path in RequiredScreenPaths)
            {
                Assert.That(
                    root.Find(path),
                    Is.Not.Null,
                    "Missing stable PF_ShopScreen path: " + path);
            }
        }

        [Test]
        public void CanvasAndRows_UseFrozenLayoutContract()
        {
            var scaler = screen.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.screenMatchMode,
                Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(root.Find("SafeArea/Content").GetComponent<VerticalLayoutGroup>(),
                Is.Not.Null);
            Assert.That(
                root.Find("SafeArea/Content/OfferPanel/OfferSlots")
                    .GetComponent<HorizontalLayoutGroup>().spacing,
                Is.EqualTo(16f).Within(0.01f));

            AssertSlotSize(
                "SafeArea/Content/OfferPanel/OfferSlots/MinionSlot0",
                240f,
                360f);
            AssertSlotSize(
                "SafeArea/Content/OfferPanel/OfferSlots/SpellSlot",
                240f,
                360f);
            AssertSlotSize(
                "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0",
                160f,
                240f);
            AssertSlotSize(
                "SafeArea/Content/HandPanel/HandSlots/HandSlot0",
                160f,
                240f);
        }

        [Test]
        public void Render_TemporaryStatePopulatesAllZonesAndClearsWithoutLeaks()
        {
            var state = CreateState();
            view.Render(state);

            Assert.That(view.RenderedCardCount, Is.EqualTo(8));
            Assert.That(TextAt("SafeArea/TopBar/RoundText"), Is.EqualTo("第 3 回合"));
            Assert.That(TextAt("SafeArea/TopBar/GoldText"), Is.EqualTo("金币：8"));
            AssertCardMode(
                "SafeArea/Content/OfferPanel/OfferSlots/MinionSlot0/Content",
                CardDisplayMode.Full);
            AssertCardMode(
                "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0/Content",
                CardDisplayMode.Compact);
            AssertCardMode(
                "SafeArea/Content/HandPanel/HandSlots/HandSlot0/Content",
                CardDisplayMode.Compact);
            AssertCardAnchoredAtTopLeft(
                "SafeArea/Content/OfferPanel/OfferSlots/MinionSlot0/Content");
            AssertCardAnchoredAtTopLeft(
                "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0/Content");
            Assert.That(
                root.Find("SafeArea/Content/BattlePanel/BattleSlots/BattleSlot4/EmptyHint")
                    .gameObject.activeSelf,
                Is.True);
            Assert.That(
                root.Find("SafeArea/ActionRail/CardDetailPanel/Content")
                    .gameObject.activeSelf,
                Is.True);

            state.MinionOffers = new CardViewModel[4];
            state.SpellOffer = null;
            state.BattleCards = new CardViewModel[5];
            state.HandCards.VisibleSlots = Enumerable.Range(0, 5)
                .Select(index => new HandCardSlotState { SlotIndex = index })
                .ToArray();
            view.Render(state);

            Assert.That(view.RenderedCardCount, Is.Zero);
            Assert.That(ContentAt(
                "SafeArea/Content/OfferPanel/OfferSlots/MinionSlot0/Content")
                .childCount, Is.Zero);
            Assert.That(ContentAt(
                "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0/Content")
                .childCount, Is.Zero);
            Assert.That(ContentAt(
                "SafeArea/Content/HandPanel/HandSlots/HandSlot0/Content")
                .childCount, Is.Zero);
        }

        [Test]
        public void ChoiceOverlay_RendersCardAndPlainCandidatesThenClosesCleanly()
        {
            view.Render(CreateState());
            var choice = new ChoiceViewModel
            {
                Title = "选择奖励",
                Description = "必须选择一个结果",
                CanCancel = false,
                Candidates = new[]
                {
                    new ChoiceCandidateViewModel
                    {
                        Label = "卡牌候选",
                        Card = CreateCard(CardDisplayMode.Full, "候选卡")
                    },
                    new ChoiceCandidateViewModel
                    {
                        Label = "种族候选",
                        Description = "选择星契"
                    },
                    new ChoiceCandidateViewModel
                    {
                        Label = "铸魂",
                        Description = "选择铸魂"
                    },
                    new ChoiceCandidateViewModel
                    {
                        Label = "荒灵",
                        Description = "选择荒灵"
                    }
                }
            };
            view.RenderChoice(choice);

            var overlayTransform = root.Find("SafeArea/ModalLayer/ChoiceOverlay");
            var overlay = overlayTransform.GetComponent<ChoiceOverlayView>();
            Assert.That(overlayTransform.gameObject.activeSelf, Is.True);
            Assert.That(overlay.RenderedCandidateCount, Is.EqualTo(4));
            Assert.That(
                overlayTransform.Find("Dialog/Candidates/Candidate0/CardContent")
                    .childCount,
                Is.EqualTo(1));
            Assert.That(
                overlayTransform.Find("Dialog/Candidates/Candidate1/CardContent")
                    .gameObject.activeSelf,
                Is.False);
            Assert.That(
                overlayTransform.Find("Dialog/CancelButton")
                    .GetComponent<Button>().interactable,
                Is.False);
            Assert.That(
                root.Find("SafeArea/ModalLayer/Blocker").gameObject.activeSelf,
                Is.True);

            view.RenderChoice(null);
            Assert.That(overlayTransform.gameObject.activeSelf, Is.False);
            Assert.That(
                overlayTransform.Find("Dialog/Candidates/Candidate0/CardContent")
                    .childCount,
                Is.Zero);
            Assert.That(
                root.Find("SafeArea/ModalLayer/Blocker").gameObject.activeSelf,
                Is.False);
        }

        [Test]
        public void ReRender_PlaysOwnedCardFreezeAndErrorFeedbackFromStateDelta()
        {
            var state = CreateState();
            view.Render(state);
            var battleCard = state.BattleCards[0];
            battleCard.Attack += 2;
            battleCard.Health += 1;
            battleCard.HasShield = true;
            state.IsFrozen = true;
            state.Buttons.Freeze.IsActive = true;

            view.Render(state);

            Assert.That(view.LastRenderFeedbackCount, Is.EqualTo(2));
            var renderedCard = root.Find(
                    "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0/Content")
                .GetChild(0);
            Assert.That(
                renderedCard.Find("GrowthFeedbackRoot").gameObject.activeSelf,
                Is.True);
            Assert.That(
                renderedCard.Find("GrowthFeedbackRoot/FeedbackText")
                    .GetComponent<Text>().text,
                Is.EqualTo("+2/+1"));
            Assert.That(
                renderedCard.Find("StateBadgeRow/ShieldBadge").localScale.x,
                Is.GreaterThan(1f));
            Assert.That(
                root.Find("SafeArea/ActionRail/FreezeButton").localScale.x,
                Is.GreaterThan(1f));

            view.ShowStatus("金币不足", true);
            var toast = root.Find("SafeArea/FeedbackLayer/StatusToast");
            Assert.That(toast.gameObject.activeSelf, Is.True);
            var toastColor = toast.GetComponent<Image>().color;
            Assert.That(toastColor.r, Is.GreaterThan(toastColor.g));
            view.ShowStatus(null);
            Assert.That(toast.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Render_RejectsNullStateExplicitly()
        {
            Assert.Throws<ArgumentNullException>(() => view.Render(null));
        }

        private void AssertSlotSize(string path, float width, float height)
        {
            var element = root.Find(path).GetComponent<LayoutElement>();
            Assert.That(element.preferredWidth, Is.EqualTo(width).Within(0.01f));
            Assert.That(element.preferredHeight, Is.EqualTo(height).Within(0.01f));
        }

        private void AssertCardMode(string path, CardDisplayMode mode)
        {
            var content = ContentAt(path);
            Assert.That(content.childCount, Is.EqualTo(1));
            Assert.That(
                content.GetChild(0).GetComponent<CardView>().CurrentDisplayMode,
                Is.EqualTo(mode));
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

        private static ShopScreenState CreateState()
        {
            var offers = Enumerable.Range(0, 4)
                .Select(index => CreateCard(
                    CardDisplayMode.Full,
                    "商品" + index))
                .ToArray();
            var battle = new CardViewModel[5];
            battle[0] = CreateCard(CardDisplayMode.Compact, "战斗随从");
            var handCards = new CardViewModel[5];
            handCards[0] = CreateCard(CardDisplayMode.Compact, "手牌一");
            handCards[1] = CreateCard(CardDisplayMode.Compact, "手牌二");
            return new ShopScreenState
            {
                Round = 3,
                Gold = 8,
                TavernTier = 5,
                UpgradeCost = 5,
                IsShopOpen = true,
                MinionOffers = offers,
                SpellOffer = CreateSpell(CardDisplayMode.Full, "法术商品"),
                BattleCards = battle,
                HandCards = new HandCardsState
                {
                    Count = 2,
                    Limit = 5,
                    PageSize = 5,
                    PageCount = 1,
                    VisibleSlots = handCards.Select((card, index) =>
                        new HandCardSlotState
                        {
                            SlotIndex = index,
                            Card = card
                        }).ToArray()
                },
                Buttons = new ShopButtonStates
                {
                    Refresh = Action("刷新", true),
                    Freeze = Action("冻结", true),
                    Upgrade = Action("升级", true),
                    Sell = Action("出售", true),
                    EndShop = Action("结束", true)
                },
                DetailPanel = new CardDetailPanelState
                {
                    Card = battle[0],
                    Location = ShopCardLocation.Battle,
                    SlotIndex = 0
                }
            };
        }

        private static CardViewModel CreateCard(CardDisplayMode mode, string name)
        {
            return new CardViewModel
            {
                InstanceId = "test_" + name,
                Name = name,
                Description = "用于验证正式商店静态渲染。",
                RaceText = "星契",
                AbilityLabels = new[] { "成长" },
                Tier = 2,
                Attack = 3,
                Health = 4,
                BaseAttack = 3,
                BaseHealth = 4,
                Cost = 3,
                DisplayMode = mode,
                IsMinion = true,
                ShowCost = mode == CardDisplayMode.Full,
                IsInteractable = true,
                IsAffordable = true
            };
        }

        private static CardViewModel CreateSpell(CardDisplayMode mode, string name)
        {
            var model = CreateCard(mode, name);
            model.IsMinion = false;
            model.Attack = model.Health = 0;
            model.BaseAttack = model.BaseHealth = 0;
            return model;
        }

        private static ShopActionButtonState Action(string text, bool interactable)
        {
            return new ShopActionButtonState
            {
                Text = text,
                IsVisible = true,
                IsInteractable = interactable
            };
        }
    }
}
