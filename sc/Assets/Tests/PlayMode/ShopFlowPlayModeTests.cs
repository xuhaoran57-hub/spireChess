using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SpireChess.App;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.UI.Battle;
using SpireChess.UI.Shop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace SpireChess.Tests
{
    public sealed class ShopFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator FormalShopScene_RendersSessionAndRoutesUiOperations()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(13);
            var run = GameApp.Instance.Run;

            SceneManager.LoadScene("ShopTest");
            yield return null;

            var controller = Object.FindObjectOfType<ShopTestController>();
            var screen = Object.FindObjectOfType<ShopScreenView>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<ShopTestController>().Length,
                Is.EqualTo(1));
            Assert.That(controller.IsUsingFormalView, Is.True);
            Assert.That(controller.FormalScreenView, Is.SameAs(screen));
            Assert.That(GameObject.Find("ShopTestCanvas"), Is.Null);

            var expected = ShopScreenStateBuilder.Build(
                run.Shop,
                run,
                statusMessage: controller.StatusMessage);
            var expectedCards = expected.MinionOffers.Count(value => value != null) +
                                (expected.SpellOffer == null ? 0 : 1) +
                                expected.BattleCards.Count(value => value != null) +
                                expected.HandCards.VisibleSlots.Count(
                                    value => value.Card != null);
            Assert.That(screen.RenderedCardCount, Is.EqualTo(expectedCards));
            Assert.That(
                screen.transform.Find("SafeArea/TopBar/GoldText")
                    .GetComponent<Text>().text,
                Is.EqualTo($"金币：{run.Shop.Gold}"));

            run.Shop.GrantGold(10);
            var freezeButton = screen.transform
                .Find("SafeArea/ActionRail/FreezeButton")
                .GetComponent<Button>();
            freezeButton.onClick.Invoke();
            Assert.That(run.Shop.IsFrozen, Is.True);
            Assert.That(controller.ResourceSummary, Does.Contain("金币 13"));

            var offerIndex = Enumerable.Range(0, run.Shop.MinionOffers.Count)
                .First(index => run.Shop.MinionOffers[index] != null);
            var offerContent = screen.transform.Find(
                "SafeArea/Content/OfferPanel/OfferSlots/MinionSlot" +
                offerIndex + "/Content");
            var offerInteraction = offerContent
                .GetComponentInChildren<ShopCardView>();
            Assert.That(offerInteraction, Is.Not.Null);
            var goldBeforePurchase = run.Shop.Gold;
            offerInteraction.OnPointerClick(
                new PointerEventData(EventSystem.current));
            Assert.That(run.Shop.Gold,
                Is.EqualTo(goldBeforePurchase - ShopEconomyRules.MinionPurchaseCost));
            Assert.That(run.Shop.Collection.Bench.Any(value => value != null),
                Is.True);
            Assert.That(
                screen.transform.Find("SafeArea/TopBar/GoldText")
                    .GetComponent<Text>().text,
                Is.EqualTo($"金币：{run.Shop.Gold}"));
        }

        [UnityTest]
        public IEnumerator FormalShopScene_ResolvesMandatoryDiscoverThroughOverlay()
        {
            yield return EnsureGameApp();
            var configs = GameApp.Instance.Configs;
            var minion = configs.Minions.First(config =>
                config.Enabled && !config.IsToken && config.Tier == 1);
            GameApp.Instance.StartNewRun(31);

            SceneManager.LoadScene("ShopTest");
            yield return null;
            var controller = Object.FindObjectOfType<ShopTestController>();
            Assert.That(controller, Is.Not.Null);
            var session = controller.Session;

            Assert.That(controller.IsUsingFormalView, Is.True);
            Assert.That(session.ClaimRewardMinion(minion).Success, Is.True);
            Assert.That(session.ClaimRewardMinion(minion).Success, Is.True);
            Assert.That(session.ClaimRewardMinion(minion).Success, Is.True);
            var goldenIndex = Enumerable.Range(0, session.Collection.Bench.Count)
                .First(index => session.Collection.Bench[index]?.IsGolden == true);
            Assert.That(controller.PlayBenchMinion(goldenIndex, 0).Success, Is.True);
            var rewardIndex = Enumerable.Range(0, session.Collection.Bench.Count)
                .First(index => session.Collection.Bench[index]?.ConfigId ==
                                ShopSession.TripleDiscoveryRewardSpellId);
            Assert.That(controller.UseBenchSpell(rewardIndex).Success, Is.True);

            var screen = controller.FormalScreenView;
            var overlay = screen.GetComponentInChildren<ChoiceOverlayView>(true);
            Assert.That(session.PendingDiscover, Is.Not.Null);
            Assert.That(controller.DiscoverModalVisible, Is.True);
            Assert.That(controller.DiscoverCancelInteractable, Is.False);
            Assert.That(overlay.RenderedCandidateCount,
                Is.EqualTo(session.PendingDiscover.Candidates.Count));

            screen.transform
                .Find("SafeArea/ModalLayer/ChoiceOverlay/Dialog/Candidates/Candidate0")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(session.PendingDiscover, Is.Null);
            Assert.That(controller.DiscoverModalVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator FormalShopScene_UpgradeDragRepositionAndSellUseUiInputs()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(43);
            var run = GameApp.Instance.Run;
            SceneManager.LoadScene("ShopTest");
            yield return null;

            var controller = Object.FindObjectOfType<ShopTestController>();
            var screen = controller.FormalScreenView;
            var session = controller.Session;
            Assert.That(controller.IsUsingFormalView, Is.True);
            session.GrantGold(30);

            var freezeButton = screen.transform
                .Find("SafeArea/ActionRail/FreezeButton")
                .GetComponent<Button>();
            freezeButton.onClick.Invoke();
            var tierBeforeUpgrade = session.TavernTier;
            var goldBeforeUpgrade = session.Gold;
            var upgradeCost = session.CurrentUpgradeCost;
            var upgradeButton = screen.transform
                .Find("SafeArea/ActionRail/UpgradeButton")
                .GetComponent<Button>();
            Assert.That(upgradeButton.interactable, Is.True);
            upgradeButton.onClick.Invoke();
            Assert.That(session.TavernTier, Is.EqualTo(tierBeforeUpgrade + 1));
            Assert.That(session.Gold, Is.EqualTo(goldBeforeUpgrade - upgradeCost));
            Assert.That(upgradeButton.interactable, Is.False);

            var offerIndex = Enumerable.Range(0, session.MinionOffers.Count)
                .First(index => session.MinionOffers[index] != null);
            var offerCard = screen.transform.Find(
                    "SafeArea/Content/OfferPanel/OfferSlots/MinionSlot" +
                    offerIndex + "/Content")
                .GetComponentInChildren<ShopCardView>();
            offerCard.OnPointerClick(new PointerEventData(EventSystem.current));
            Assert.That(controller.LastOperationResult.Success, Is.True);
            var handIndex = controller.LastOperationResult.BenchIndex;
            var instanceId = session.Collection.Bench[handIndex].InstanceId;

            var handContent = screen.transform.Find(
                "SafeArea/Content/HandPanel/HandSlots/HandSlot" +
                handIndex + "/Content");
            var handCard = handContent.GetComponentInChildren<ShopCardView>();
            var playDrag = new PointerEventData(EventSystem.current)
            {
                pointerDrag = handCard.gameObject
            };
            handCard.OnBeginDrag(playDrag);
            Assert.That(handCard.IsDragging, Is.True);
            screen.transform
                .Find("SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0")
                .GetComponent<ShopSlotView>().OnDrop(playDrag);
            handCard.OnEndDrag(playDrag);
            yield return null;

            Assert.That(session.Collection.Bench[handIndex], Is.Null);
            Assert.That(session.Collection.Battle[0].InstanceId,
                Is.EqualTo(instanceId));
            Assert.That(handContent.childCount, Is.Zero,
                "Handled drag must not restore a stale card to the hand slot.");
            Assert.That(screen.transform
                    .Find("SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0/Content")
                    .childCount,
                Is.EqualTo(1));

            var battleZeroContent = screen.transform.Find(
                "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot0/Content");
            var battleCard = battleZeroContent
                .GetComponentInChildren<ShopCardView>();
            var repositionDrag = new PointerEventData(EventSystem.current)
            {
                pointerDrag = battleCard.gameObject
            };
            battleCard.OnBeginDrag(repositionDrag);
            Assert.That(battleCard.IsDragging, Is.True);
            screen.transform
                .Find("SafeArea/Content/BattlePanel/BattleSlots/BattleSlot1")
                .GetComponent<ShopSlotView>().OnDrop(repositionDrag);
            battleCard.OnEndDrag(repositionDrag);
            yield return null;

            Assert.That(session.Collection.Battle[0], Is.Null);
            Assert.That(session.Collection.Battle[1].InstanceId,
                Is.EqualTo(instanceId));
            Assert.That(battleZeroContent.childCount, Is.Zero,
                "Handled reposition must not restore a stale battle card.");

            var movedCard = screen.transform.Find(
                    "SafeArea/Content/BattlePanel/BattleSlots/BattleSlot1/Content")
                .GetComponentInChildren<ShopCardView>();
            movedCard.OnPointerClick(new PointerEventData(EventSystem.current));
            var sellButton = screen.transform
                .Find("SafeArea/ActionRail/SellButton")
                .GetComponent<Button>();
            Assert.That(sellButton.interactable, Is.True);
            var goldBeforeSell = session.Gold;
            sellButton.onClick.Invoke();
            Assert.That(session.Collection.Battle[1], Is.Null);
            Assert.That(session.Gold,
                Is.EqualTo(goldBeforeSell + ShopEconomyRules.MinionSellValue));
            Assert.That(sellButton.interactable, Is.False);

            var failedSell = controller.SellSelectedBattleMinion();
            Assert.That(failedSell.Success, Is.False);
            var toast = screen.transform.Find(
                "SafeArea/FeedbackLayer/StatusToast");
            Assert.That(toast.gameObject.activeSelf, Is.True);
            Assert.That(toast.GetComponent<Image>().color.r,
                Is.GreaterThan(toast.GetComponent<Image>().color.g));
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.That(toast.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ShopToBattleAndBack_PreservesRunSessionAndOwnedMinion()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(17);
            var run = GameApp.Instance.Run;

            SceneManager.LoadScene("ShopTest");
            yield return null;

            var shopController = Object.FindObjectOfType<ShopTestController>();
            Assert.That(shopController, Is.Not.Null);
            Assert.That(shopController.IsInitialized, Is.True);
            Assert.That(shopController.Session, Is.SameAs(run.Shop));
            Assert.That(run.Shop.Round, Is.EqualTo(1));

            var offerIndex = Enumerable.Range(0, run.Shop.MinionOffers.Count)
                .First(index => run.Shop.MinionOffers[index] != null);
            var purchase = shopController.BuyMinionAt(offerIndex);
            Assert.That(purchase.Success, Is.True);
            var purchased = run.Shop.Collection.Bench[purchase.BenchIndex];
            Assert.That(purchased, Is.Not.Null);

            var play = shopController.PlayBenchMinion(purchase.BenchIndex, 0);
            Assert.That(play.Success, Is.True);
            var sourceInstanceId = run.Shop.Collection.Battle[0].InstanceId;

            var end = shopController.EndShopAndEnterBattle();
            Assert.That(end.Success, Is.True);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BattleTest"));
            var battleController = Object.FindObjectOfType<BattleTestController>();
            Assert.That(battleController, Is.Not.Null);
            Assert.That(battleController.IsRunBattle, Is.True);
            Assert.That(battleController.SetupState.Player[0].SourceInstanceId,
                Is.EqualTo(sourceInstanceId));
            Assert.That(battleController.SetupState.Enemy.Any(minion => minion != null), Is.True);

            var battleResult = battleController.ResolveImmediately();
            Assert.That(battleResult, Is.Not.Null);
            Assert.That(battleController.IsLogScrollable, Is.True);
            Assert.That(battleController.LogContents,
                Is.EqualTo(string.Join("\n", battleResult.Log)));
            Assert.That(run.LastBattleResult, Is.SameAs(battleResult));
            Assert.That(run.Shop.Collection.Battle[0].InstanceId, Is.EqualTo(sourceInstanceId));

            battleController.ReturnToFlow();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ShopTest"));
            var returnedController = Object.FindObjectOfType<ShopTestController>();
            Assert.That(returnedController, Is.Not.Null);
            Assert.That(returnedController.Session, Is.SameAs(run.Shop));
            Assert.That(run.Shop.Round, Is.EqualTo(2));
            Assert.That(run.Shop.Collection.Battle[0].InstanceId, Is.EqualTo(sourceInstanceId));
        }

        [UnityTest]
        public IEnumerator HeadlessTripleDiscover_BlocksOperationsAndResolvesSelection()
        {
            yield return EnsureGameApp();
            var configs = GameApp.Instance.Configs;
            var minion = configs.Minions.First(config =>
                config.Enabled && !config.IsToken && config.Tier == 1);
            var session = new ShopSession(
                new[] { minion },
                configs.Spells,
                new System.Random(3));

            var controllerObject = new GameObject("ShopControllerUnderTest");
            var controller = controllerObject.AddComponent<ShopTestController>();
            controller.InitializeForTests(session);
            Assert.That(controller.IsUsingFormalView, Is.False);
            session.GrantGold(20);
            yield return null;

            Assert.That(controller.BuyMinionAt(0).Success, Is.True);
            Assert.That(controller.BuyMinionAt(1).Success, Is.True);
            Assert.That(controller.RefreshShop().Success, Is.True);
            Assert.That(controller.BuyMinionAt(0).Success, Is.True);

            var goldenIndex = Enumerable.Range(0, session.Collection.Bench.Count)
                .First(index => session.Collection.Bench[index]?.IsGolden == true);
            Assert.That(controller.PlayBenchMinion(goldenIndex, 0).Success, Is.True);

            var rewardIndex = Enumerable.Range(0, session.Collection.Bench.Count)
                .First(index => session.Collection.Bench[index]?.ConfigId ==
                                ShopSession.TripleDiscoveryRewardSpellId);
            Assert.That(controller.UseBenchSpell(rewardIndex).Success, Is.True);
            Assert.That(session.PendingDiscover, Is.Not.Null);

            var cancel = controller.CancelDiscover();
            Assert.That(cancel.Success, Is.False);
            Assert.That(cancel.Error, Is.EqualTo(
                ShopOperationError.DiscoveryCannotBeCancelled));
            Assert.That(session.PendingDiscover, Is.Not.Null);

            var blockedRefresh = controller.RefreshShop();
            Assert.That(blockedRefresh.Success, Is.False);
            Assert.That(blockedRefresh.Error, Is.EqualTo(ShopOperationError.DiscoveryPending));

            Assert.That(controller.SelectDiscoverCandidate(0).Success, Is.True);
            Assert.That(session.PendingDiscover, Is.Null);

            Object.Destroy(controllerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FormalAndHeadless_NormalOperationsProduceIdenticalDomainTrace()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(101);
            SceneManager.LoadScene("ShopTest");
            yield return null;

            var formalScreen = CloneFormalScreenAndRemoveSceneController();
            yield return null;

            var configs = GameApp.Instance.Configs;
            var minions = new[]
            {
                configs.MinionsById["forge_soul_shield_squire"],
                configs.MinionsById["hearth_core_spark"],
                configs.MinionsById["copper_ring_apprentice"],
                configs.MinionsById["young_deer_spirit"]
            };
            var spells = new[]
            {
                configs.SpellsById["minor_tempering"],
                configs.SpellsById[ShopSession.TripleDiscoveryRewardSpellId]
            };
            var poolIds = minions.Select(value => value.Id).ToArray();
            var headlessSession = new ShopSession(
                minions,
                spells,
                new System.Random(211));
            var formalSession = new ShopSession(
                minions,
                spells,
                new System.Random(211));
            var headlessObject = new GameObject("HeadlessParityController");
            var formalObject = new GameObject("FormalParityController");
            var headless = headlessObject.AddComponent<ShopTestController>();
            var formal = formalObject.AddComponent<ShopTestController>();

            try
            {
                formal.ConfigureFormalViewForTests(formalScreen);
                headless.InitializeForTests(headlessSession);
                formal.InitializeForTests(formalSession);
                Assert.That(headless.IsUsingFormalView, Is.False);
                Assert.That(formal.IsUsingFormalView, Is.True);
                AssertEquivalentDomain("initial render", headless, formal, poolIds);

                headlessSession.GrantGold(50);
                formalSession.GrantGold(50);
                AssertEquivalentDomain("grant gold", headless, formal, poolIds);

                ApplyEquivalentOperation(
                    "freeze",
                    headless,
                    formal,
                    controller => controller.ToggleFreeze(),
                    poolIds);
                ApplyEquivalentOperation(
                    "refresh after freeze",
                    headless,
                    formal,
                    controller => controller.RefreshShop(),
                    poolIds);
                ApplyEquivalentOperation(
                    "upgrade",
                    headless,
                    formal,
                    controller => controller.UpgradeTavern(),
                    poolIds);

                var minionPurchase = ApplyEquivalentOperation(
                    "buy minion",
                    headless,
                    formal,
                    controller => controller.BuyMinionAt(0),
                    poolIds);
                Assert.That(minionPurchase.Success, Is.True);
                ApplyEquivalentOperation(
                    "play minion",
                    headless,
                    formal,
                    controller => controller.PlayBenchMinion(
                        minionPurchase.BenchIndex,
                        0),
                    poolIds);

                var spellPurchase = ApplyEquivalentOperation(
                    "buy targeted spell",
                    headless,
                    formal,
                    controller => controller.BuySpellOffer(),
                    poolIds);
                Assert.That(spellPurchase.Success, Is.True);
                ApplyEquivalentOperation(
                    "use targeted spell",
                    headless,
                    formal,
                    controller => controller.UseBenchSpell(
                        spellPurchase.BenchIndex,
                        0),
                    poolIds);
                ApplyEquivalentOperation(
                    "reposition minion",
                    headless,
                    formal,
                    controller => controller.RepositionBattleMinion(0, 1),
                    poolIds);

                headless.HandleCardClick(ShopCardZone.Battle, 1);
                formal.HandleCardClick(ShopCardZone.Battle, 1);
                AssertEquivalentDomain("select battle minion", headless, formal, poolIds);
                ApplyEquivalentOperation(
                    "sell selected minion",
                    headless,
                    formal,
                    controller => controller.SellSelectedBattleMinion(),
                    poolIds);

                // This extra RNG-consuming action detects a formal Render/Builder
                // path that accidentally advances the ShopSession random source.
                ApplyEquivalentOperation(
                    "rng probe refresh",
                    headless,
                    formal,
                    controller => controller.RefreshShop(),
                    poolIds);
                var invalidSell = ApplyEquivalentOperation(
                    "invalid sell",
                    headless,
                    formal,
                    controller => controller.SellSelectedBattleMinion(),
                    poolIds);
                Assert.That(invalidSell.Error,
                    Is.EqualTo(ShopOperationError.InvalidIndex));
            }
            finally
            {
                Object.Destroy(headlessObject);
                Object.Destroy(formalObject);
                Object.Destroy(formalScreen.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FormalAndHeadless_TripleDiscoverProduceIdenticalDomainTrace()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(102);
            SceneManager.LoadScene("ShopTest");
            yield return null;

            var formalScreen = CloneFormalScreenAndRemoveSceneController();
            yield return null;

            var configs = GameApp.Instance.Configs;
            var minion = configs.MinionsById["forge_soul_shield_squire"];
            var minions = new[] { minion };
            var spells = new[]
            {
                configs.SpellsById["minor_tempering"],
                configs.SpellsById[ShopSession.TripleDiscoveryRewardSpellId]
            };
            var poolIds = new[] { minion.Id };
            var headlessSession = new ShopSession(
                minions,
                spells,
                new System.Random(307));
            var formalSession = new ShopSession(
                minions,
                spells,
                new System.Random(307));
            var headlessObject = new GameObject("HeadlessDiscoverParityController");
            var formalObject = new GameObject("FormalDiscoverParityController");
            var headless = headlessObject.AddComponent<ShopTestController>();
            var formal = formalObject.AddComponent<ShopTestController>();

            try
            {
                formal.ConfigureFormalViewForTests(formalScreen);
                headless.InitializeForTests(headlessSession);
                formal.InitializeForTests(formalSession);
                headlessSession.GrantGold(40);
                formalSession.GrantGold(40);
                AssertEquivalentDomain("discover initial", headless, formal, poolIds);

                ApplyEquivalentOperation(
                    "triple buy one",
                    headless,
                    formal,
                    controller => controller.BuyMinionAt(0),
                    poolIds);
                ApplyEquivalentOperation(
                    "triple buy two",
                    headless,
                    formal,
                    controller => controller.BuyMinionAt(1),
                    poolIds);
                ApplyEquivalentOperation(
                    "triple refresh",
                    headless,
                    formal,
                    controller => controller.RefreshShop(),
                    poolIds);
                ApplyEquivalentOperation(
                    "triple buy three",
                    headless,
                    formal,
                    controller => controller.BuyMinionAt(0),
                    poolIds);

                var goldenIndex = Enumerable.Range(
                        0,
                        headlessSession.Collection.Bench.Count)
                    .First(index =>
                        headlessSession.Collection.Bench[index]?.IsGolden == true);
                Assert.That(formalSession.Collection.Bench[goldenIndex]?.IsGolden,
                    Is.True);
                ApplyEquivalentOperation(
                    "play golden minion",
                    headless,
                    formal,
                    controller => controller.PlayBenchMinion(goldenIndex, 0),
                    poolIds);

                var rewardIndex = Enumerable.Range(
                        0,
                        headlessSession.Collection.Bench.Count)
                    .First(index => headlessSession.Collection.Bench[index]?.ConfigId ==
                                    ShopSession.TripleDiscoveryRewardSpellId);
                Assert.That(formalSession.Collection.Bench[rewardIndex]?.ConfigId,
                    Is.EqualTo(ShopSession.TripleDiscoveryRewardSpellId));
                ApplyEquivalentOperation(
                    "start mandatory discover",
                    headless,
                    formal,
                    controller => controller.UseBenchSpell(rewardIndex),
                    poolIds);
                Assert.That(headlessSession.PendingDiscover, Is.Not.Null);
                Assert.That(formalSession.PendingDiscover, Is.Not.Null);

                var cancel = ApplyEquivalentOperation(
                    "reject mandatory discover cancellation",
                    headless,
                    formal,
                    controller => controller.CancelDiscover(),
                    poolIds);
                Assert.That(cancel.Error,
                    Is.EqualTo(ShopOperationError.DiscoveryCannotBeCancelled));
                var blockedRefresh = ApplyEquivalentOperation(
                    "block refresh while discover is pending",
                    headless,
                    formal,
                    controller => controller.RefreshShop(),
                    poolIds);
                Assert.That(blockedRefresh.Error,
                    Is.EqualTo(ShopOperationError.DiscoveryPending));
                ApplyEquivalentOperation(
                    "resolve mandatory discover",
                    headless,
                    formal,
                    controller => controller.SelectDiscoverCandidate(0),
                    poolIds);

                Assert.That(headlessSession.PendingDiscover, Is.Null);
                Assert.That(formalSession.PendingDiscover, Is.Null);
                ApplyEquivalentOperation(
                    "post-discover rng probe",
                    headless,
                    formal,
                    controller => controller.RefreshShop(),
                    poolIds);
            }
            finally
            {
                Object.Destroy(headlessObject);
                Object.Destroy(formalObject);
                Object.Destroy(formalScreen.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReloadingShopScene_DoesNotDuplicateControllerOrEventSubscription()
        {
            yield return EnsureGameApp();
            GameApp.Instance.StartNewRun(29);
            var run = GameApp.Instance.Run;

            SceneManager.LoadScene("ShopTest");
            yield return null;
            var firstController = Object.FindObjectOfType<ShopTestController>();
            run.Shop.GrantGold(2);
            var firstEventCount = firstController.EventLogCount;
            Assert.That(firstController.RefreshShop().Success, Is.True);
            Assert.That(firstController.EventLogCount, Is.EqualTo(firstEventCount + 1));

            SceneManager.LoadScene("ShopTest");
            yield return null;
            var controllers = Object.FindObjectsOfType<ShopTestController>();
            Assert.That(controllers.Length, Is.EqualTo(1));
            run.Shop.GrantGold(2);
            var reloadedEventCount = controllers[0].EventLogCount;
            Assert.That(controllers[0].RefreshShop().Success, Is.True);
            Assert.That(controllers[0].EventLogCount, Is.EqualTo(reloadedEventCount + 1));
        }

        [UnityTest]
        public IEnumerator ConfiguredShopSpells_AllHaveExecutableEffects()
        {
            yield return EnsureGameApp();

            var shopSpells = GameApp.Instance.Configs.Spells
                .Where(spell => spell.Enabled && spell.ShopEligible)
                .ToList();

            Assert.That(shopSpells.Count, Is.EqualTo(15));
            Assert.That(shopSpells.All(spell => spell.Effects != null && spell.Effects.Count > 0),
                Is.True);
        }

        [UnityTest]
        public IEnumerator TargetedSpell_SecondClickCancelsSelectionAndFreeRefreshesAreVisible()
        {
            yield return EnsureGameApp();
            var spell = GameApp.Instance.Configs.Spells.Single(
                config => config.Id == "minor_tempering");
            var session = new ShopSession(
                System.Array.Empty<MinionConfig>(),
                new[] { spell },
                new System.Random(7));
            var controllerObject = new GameObject("ShopSelectionControllerUnderTest");
            var controller = controllerObject.AddComponent<ShopTestController>();
            controller.InitializeForTests(session);
            yield return null;

            var purchase = controller.BuySpellOffer();
            Assert.That(purchase.Success, Is.True);

            controller.HandleCardClick(ShopCardZone.Bench, purchase.BenchIndex);
            Assert.That(controller.SelectedBenchIndex, Is.EqualTo(purchase.BenchIndex));
            Assert.That(controller.LastOperationResult.Error, Is.EqualTo(ShopOperationError.NoBenefit));

            controller.HandleCardClick(ShopCardZone.Bench, purchase.BenchIndex);
            Assert.That(controller.SelectedBenchIndex, Is.EqualTo(-1));
            Assert.That(controller.StatusMessage, Is.EqualTo("已取消选择"));

            session.GrantFreeRefreshes(2);
            controller.ToggleFreeze();
            Assert.That(controller.ResourceSummary, Does.Contain("免费刷新 2"));

            Object.Destroy(controllerObject);
            yield return null;
        }

        private static IEnumerator EnsureGameApp()
        {
            if (GameApp.Instance == null)
            {
                yield return null;
            }

            Assert.That(GameApp.Instance, Is.Not.Null);
            Assert.That(GameApp.Instance.Configs, Is.Not.Null);
        }

        private static ShopScreenView CloneFormalScreenAndRemoveSceneController()
        {
            var sceneController = Object.FindObjectOfType<ShopTestController>();
            var sceneScreen = Object.FindObjectOfType<ShopScreenView>();
            Assert.That(sceneController, Is.Not.Null);
            Assert.That(sceneScreen, Is.Not.Null);

            var clone = Object.Instantiate(sceneScreen);
            clone.gameObject.name = "FormalParityScreen";
            Object.Destroy(sceneController.gameObject);
            Object.Destroy(sceneScreen.gameObject);
            return clone;
        }

        private static ShopOperationResult ApplyEquivalentOperation(
            string step,
            ShopTestController headless,
            ShopTestController formal,
            System.Func<ShopTestController, ShopOperationResult> operation,
            IEnumerable<string> poolIds)
        {
            var headlessResult = operation(headless);
            var formalResult = operation(formal);
            Assert.That(formalResult.Success,
                Is.EqualTo(headlessResult.Success),
                step + " success");
            Assert.That(formalResult.Error,
                Is.EqualTo(headlessResult.Error),
                step + " error");
            Assert.That(formalResult.BenchIndex,
                Is.EqualTo(headlessResult.BenchIndex),
                step + " bench index");
            AssertEquivalentDomain(step, headless, formal, poolIds);
            return headlessResult;
        }

        private static void AssertEquivalentDomain(
            string step,
            ShopTestController headless,
            ShopTestController formal,
            IEnumerable<string> poolIds)
        {
            var headlessSnapshot = CaptureDomainSnapshot(headless.Session, poolIds);
            var formalSnapshot = CaptureDomainSnapshot(formal.Session, poolIds);
            Assert.That(formalSnapshot,
                Is.EqualTo(headlessSnapshot),
                step + " domain snapshot");
            Assert.That(formal.EventLogCount,
                Is.EqualTo(headless.EventLogCount),
                step + " event count");
            Assert.That(formal.SelectedBenchIndex,
                Is.EqualTo(headless.SelectedBenchIndex),
                step + " selected hand index");
            Assert.That(formal.StatusMessage,
                Is.EqualTo(headless.StatusMessage),
                step + " status");
        }

        private static string CaptureDomainSnapshot(
            ShopSession session,
            IEnumerable<string> poolIds)
        {
            var builder = new StringBuilder();
            builder.Append("round=").Append(session.Round)
                .Append("|gold=").Append(session.Gold)
                .Append("|tier=").Append(session.TavernTier)
                .Append("|upgradeCost=").Append(session.CurrentUpgradeCost)
                .Append("|refresh=").Append(session.RefreshCount)
                .Append("|freeRefresh=").Append(session.FreeRefreshes)
                .Append("|open=").Append(session.IsShopOpen)
                .Append("|frozen=").Append(session.IsFrozen)
                .Append("|upgraded=").Append(session.UpgradedThisRound)
                .Append("|scheduledGold=").Append(session.ScheduledGold)
                .Append("|flourish=").Append(session.FlourishStacks)
                .Append("|offers=")
                .Append(string.Join(",", session.MinionOffers
                    .Select(value => value?.Id ?? "-")))
                .Append("|spellOffer=").Append(session.SpellOffer?.Id ?? "-")
                .Append("|phaseStats=")
                .Append(session.PhaseStats.RefreshCount).Append(',')
                .Append(session.PhaseStats.MinionBoughtCount).Append(',')
                .Append(session.PhaseStats.SpellBoughtCount).Append(',')
                .Append(session.PhaseStats.SpellUsedCount);

            AppendCards(builder, "battle", session.Collection.Battle);
            AppendCards(builder, "bench", session.Collection.Bench);

            var discover = session.PendingDiscover;
            builder.Append("|discover=");
            if (discover == null)
            {
                builder.Append('-');
            }
            else
            {
                builder.Append(discover.SourceSpell.InstanceId).Append(',')
                    .Append(discover.BenchIndex).Append(',')
                    .Append(discover.CanCancel).Append(',')
                    .Append(string.Join(",", discover.Candidates
                        .Select(value => value.Id)));
            }

            var choice = session.PendingChoice;
            builder.Append("|choice=");
            if (choice == null)
            {
                builder.Append('-');
            }
            else
            {
                builder.Append(choice.ChoiceType).Append(',')
                    .Append(choice.SourceCard.InstanceId).Append(',')
                    .Append(choice.BenchIndex).Append(',')
                    .Append(choice.Effect.Id).Append(',')
                    .Append(choice.ReplaceSourceCard).Append(',')
                    .Append(string.Join(",", choice.Candidates.Select(candidate =>
                        candidate.Id + ":" +
                        (candidate.Minion?.Id ??
                         candidate.Spell?.Id ??
                         candidate.Target?.InstanceId ?? "-"))));
            }

            builder.Append("|pool=");
            foreach (var id in poolIds.OrderBy(value => value))
            {
                builder.Append(id).Append(':')
                    .Append(session.MinionPool.GetRemainingCopies(id))
                    .Append(',');
            }

            return builder.ToString();
        }

        private static void AppendCards(
            StringBuilder builder,
            string zone,
            IReadOnlyList<ShopCardInstance> cards)
        {
            builder.Append('|').Append(zone).Append('=');
            for (var slot = 0; slot < cards.Count; slot++)
            {
                if (slot > 0)
                {
                    builder.Append(';');
                }

                var card = cards[slot];
                if (card == null)
                {
                    builder.Append('-');
                    continue;
                }

                builder.Append(slot).Append(':')
                    .Append(card.InstanceId).Append(',')
                    .Append(card.ConfigId).Append(',')
                    .Append(card.CardType).Append(',')
                    .Append(card.IsGolden).Append(',')
                    .Append(card.CurrentAttack).Append(',')
                    .Append(card.CurrentHealth).Append(',')
                    .Append(card.PermanentAttackBonus).Append(',')
                    .Append(card.PermanentHealthBonus).Append(',')
                    .Append(card.FlourishAttackBonus).Append(',')
                    .Append(card.TripleDiscoveryPending).Append(',')
                    .Append(card.ExpiresAtShopEnd).Append(',')
                    .Append('[')
                    .Append(string.Join(",", card.PermanentKeywords
                        .OrderBy(value => value)))
                    .Append("],[")
                    .Append(string.Join(",", card.PendingCombatModifiers.Select(
                        modifier => modifier.EffectId + ":" +
                                    modifier.Attack + ":" +
                                    modifier.Health + ":" +
                                    modifier.Keyword + ":" +
                                    modifier.AddShield)))
                    .Append(']');
            }
        }
    }
}
