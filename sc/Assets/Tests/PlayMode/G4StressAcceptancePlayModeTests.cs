using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.App;
using SpireChess.Battle;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.Simulation;
using SpireChess.Shop;
using SpireChess.UI;
using SpireChess.UI.Battle;
using SpireChess.UI.Shop;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpireChess.Tests
{
    /// <summary>
    /// G4 presentation stress regressions. These tests exercise the formal
    /// prefabs and legal domain operations, but the custom battle board is a
    /// deterministic test fixture and is not evidence for the frozen player
    /// progression route.
    /// </summary>
    public sealed class G4StressAcceptancePlayModeTests
    {
        private const string FoxRuntimeId = "g4-stress:fox-matriarch";
        private const string DeerRuntimeId = "g4-stress:young-deer";
        private const string TombGuardianRuntimeId =
            "g4-stress:tomb-guardian";
        private const string EnemyRuntimeId = "g4-stress:enemy";

        private string saveRoot;
        private RunSaveRepository originalRepository;
        private RunPersistenceCoordinator originalPersistence;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EnsureGameApp();

            var app = GameApp.Instance;
            app.ClearInMemoryRunForAutomatedTests();
            originalRepository = app.RunSaves;
            originalPersistence = app.Persistence;

            saveRoot = Path.Combine(
                Path.GetTempPath(),
                "spire-chess-g4-stress-tests",
                Guid.NewGuid().ToString("N"));
            var repository = new RunSaveRepository(
                app.Configs,
                new AtomicFileSaveStorage(saveRoot));
            SetGameAppProperty(nameof(GameApp.RunSaves), repository);
            SetGameAppProperty(
                nameof(GameApp.Persistence),
                new RunPersistenceCoordinator(repository, true));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameApp.Instance != null)
            {
                GameApp.Instance.ClearInMemoryRunForAutomatedTests();
                SetGameAppProperty(nameof(GameApp.RunSaves), originalRepository);
                SetGameAppProperty(nameof(GameApp.Persistence), originalPersistence);
            }

            if (!string.IsNullOrWhiteSpace(saveRoot) &&
                Directory.Exists(saveRoot))
            {
                Directory.Delete(saveRoot, true);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FormalShop_LegalEconomyRendersTenCompactCardsWithoutLayoutLeak()
        {
            GameApp.Instance.StartNewRun(940401);
            SceneManager.LoadScene(GameSceneNames.Shop);
            yield return null;
            yield return null;

            var controller = Object.FindObjectOfType<ShopTestController>();
            var screen = Object.FindObjectOfType<ShopScreenView>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(controller.FormalScreenView, Is.SameAs(screen));
            Assert.That(screen.HasCompleteBindings, Is.True);

            var session = controller.Session;
            Assert.That(session, Is.SameAs(GameApp.Instance.Run.Shop));

            // The schedule uses only normal round income, purchases and
            // discounted upgrades. It deliberately never calls GrantGold or
            // writes RunState; it reaches tier 5, then fills both owned zones.
            for (var round = 1; round <= 8; round++)
            {
                Assert.That(session.Round, Is.EqualTo(round));
                Assert.That(session.IsShopOpen, Is.True);
                Assert.That(
                    session.Gold,
                    Is.EqualTo(ShopEconomyRules.GetRoundBudget(round)));

                switch (round)
                {
                    case 1:
                    case 3:
                    case 7:
                        BuyMinionsWithoutTripling(
                            controller,
                            1,
                            requireChoiceFreePlay: true);
                        break;
                    case 5:
                        BuyMinionsWithoutTripling(
                            controller,
                            2,
                            requireChoiceFreePlay: true);
                        break;
                    case 2:
                    case 4:
                    case 6:
                    case 8:
                        var goldBeforeUpgrade = session.Gold;
                        var upgradeCost = session.CurrentUpgradeCost;
                        Assert.That(
                            controller.UpgradeTavern().Success,
                            Is.True,
                            $"round {round} legal discounted upgrade");
                        Assert.That(
                            session.Gold,
                            Is.EqualTo(goldBeforeUpgrade - upgradeCost));
                        if (round == 8)
                        {
                            Assert.That(
                                controller.RefreshShop().Success,
                                Is.True,
                                "The remaining legal gold fills the newly " +
                                "unlocked fourth offer slot.");
                        }
                        break;
                }

                if (round == 8)
                {
                    break;
                }

                Assert.That(session.EndRound().Success, Is.True);
                Assert.That(session.StartNextRound().Success, Is.True);
            }

            PlayAllBenchMinions(controller);
            Assert.That(session.EndRound().Success, Is.True);
            Assert.That(session.StartNextRound().Success, Is.True);
            Assert.That(session.Round, Is.EqualTo(9));
            BuyMinionsWithoutTripling(
                controller,
                3,
                requireChoiceFreePlay: false);
            Assert.That(session.EndRound().Success, Is.True);
            Assert.That(session.StartNextRound().Success, Is.True);
            Assert.That(session.Round, Is.EqualTo(10));
            BuyMinionsWithoutTripling(
                controller,
                2,
                requireChoiceFreePlay: false);

            Assert.That(session.TavernTier, Is.EqualTo(5));
            Assert.That(
                session.Collection.Bench.Count(card => card != null),
                Is.EqualTo(5));
            Assert.That(
                session.Collection.Battle.Count(card => card != null),
                Is.EqualTo(5));
            Assert.That(
                session.Collection.Bench
                    .Concat(session.Collection.Battle)
                    .Where(card => card != null)
                    .GroupBy(card => card.ConfigId)
                    .Max(group => group.Count()),
                Is.LessThan(3),
                "The density fixture must not rely on triple replacement.");
            Assert.That(
                session.MinionOffers.Count(offer => offer != null),
                Is.EqualTo(2));
            Assert.That(session.SpellOffer, Is.Not.Null);

            // Allow the deferred destruction used by slot re-rendering to
            // complete before detecting leaked/stale card roots.
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            var expectedCardCount =
                session.MinionOffers.Count(offer => offer != null) +
                (session.SpellOffer == null ? 0 : 1) +
                session.Collection.Battle.Count(card => card != null) +
                session.Collection.Bench.Count(card => card != null);
            Assert.That(expectedCardCount, Is.EqualTo(13),
                "The stress screen contains ten compact owned cards plus " +
                "two remaining minion offers and one spell offer.");
            Assert.That(screen.RenderedCardCount, Is.EqualTo(expectedCardCount));

            var activeCards = Object.FindObjectsOfType<CardView>()
                .Where(card => card.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(activeCards.Length, Is.EqualTo(expectedCardCount));
            Assert.That(
                activeCards.All(card => card.transform.IsChildOf(screen.transform)),
                Is.True,
                "No stale card may remain detached from the formal shop.");
            var renderedCards = activeCards
                .Select(card => new
                {
                    Card = card,
                    Input = card.GetComponent<ShopCardView>()
                })
                .ToArray();
            Assert.That(renderedCards.All(value => value.Input != null), Is.True,
                "Every rendered stress card must retain its real UI input route.");
            var ownedCards = renderedCards.Where(value =>
                value.Input.Zone == ShopCardZone.Battle ||
                value.Input.Zone == ShopCardZone.Bench).ToArray();
            var offerCards = renderedCards.Where(value =>
                value.Input.Zone == ShopCardZone.MinionOffer ||
                value.Input.Zone == ShopCardZone.SpellOffer).ToArray();
            Assert.That(ownedCards.Length, Is.EqualTo(10));
            Assert.That(
                ownedCards.Count(value =>
                    value.Input.Zone == ShopCardZone.Battle),
                Is.EqualTo(5));
            Assert.That(
                ownedCards.Count(value =>
                    value.Input.Zone == ShopCardZone.Bench),
                Is.EqualTo(5));
            Assert.That(
                ownedCards.All(value =>
                    value.Card.CurrentDisplayMode ==
                    CardDisplayMode.Compact),
                Is.True,
                "Every battle and hand card must use Compact mode.");
            Assert.That(offerCards.Length, Is.EqualTo(3));
            Assert.That(
                offerCards.Count(value =>
                    value.Input.Zone == ShopCardZone.MinionOffer),
                Is.EqualTo(2));
            Assert.That(
                offerCards.Count(value =>
                    value.Input.Zone == ShopCardZone.SpellOffer),
                Is.EqualTo(1));
            Assert.That(
                offerCards.All(value =>
                    value.Card.CurrentDisplayMode == CardDisplayMode.Full),
                Is.True,
                "The two minion offers and one spell offer must remain Full.");
            Assert.That(
                renderedCards.All(value =>
                    value.Card.transform.IsChildOf(screen.transform)),
                Is.True,
                "All owned and offered cards must belong to the formal screen.");
            Assert.That(activeCards.All(card => card.HasCompleteBindings), Is.True);

            var safeArea = screen.transform.Find("SafeArea") as RectTransform;
            Assert.That(safeArea, Is.Not.Null);
            var occupiedSlots = new HashSet<ShopSlotView>();
            foreach (var card in activeCards)
            {
                var cardRect = card.GetComponent<RectTransform>();
                var slot = card.GetComponentInParent<ShopSlotView>();
                Assert.That(cardRect, Is.Not.Null);
                Assert.That(slot, Is.Not.Null);
                Assert.That(slot.HasCompleteBindings, Is.True);
                Assert.That(card.transform.parent, Is.SameAs(slot.Content));
                Assert.That(slot.Content.childCount, Is.EqualTo(1));
                Assert.That(occupiedSlots.Add(slot), Is.True,
                    "Each occupied slot must own exactly one compact card.");
                AssertFinitePositiveRect(cardRect);
                AssertWorldRectContains(slot.Content, cardRect, card.name);
                AssertWorldRectContains(safeArea, cardRect, card.name);
            }

        }

        [UnityTest]
        public IEnumerator FormalBattle_NestedDeathrattleFixtureSettlesOnceAndCleansTransients()
        {
            const int seed = 940401;
            var mapper = new RunSnapshotMapper(GameApp.Instance.Configs);
            var observations = new List<StressBattleObservation>();

            yield return RunNestedDeathrattleMode(
                seed,
                StressPlaybackMode.OneTimes,
                mapper,
                observations);
            yield return RunNestedDeathrattleMode(
                seed,
                StressPlaybackMode.TwoTimes,
                mapper,
                observations);
            yield return RunNestedDeathrattleMode(
                seed,
                StressPlaybackMode.Skip,
                mapper,
                observations);

            Assert.That(observations, Has.Count.EqualTo(3));
            var baseline = observations[0];
            foreach (var observation in observations.Skip(1))
            {
                Assert.That(
                    observation.BattleHash,
                    Is.EqualTo(baseline.BattleHash),
                    $"{observation.Mode} changed the nested battle result.");
                Assert.That(
                    observation.RunFingerprint,
                    Is.EqualTo(baseline.RunFingerprint),
                    $"{observation.Mode} changed the normalized Run result.");
            }
        }

        private static IEnumerator RunNestedDeathrattleMode(
            int seed,
            StressPlaybackMode mode,
            RunSnapshotMapper mapper,
            ICollection<StressBattleObservation> observations)
        {
            var run = PrepareNestedDeathrattleBattle(seed);
            var settlementsBefore = SettlementCount(run);
            SceneManager.LoadScene(GameSceneNames.Battle);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            var controller = Object.FindObjectOfType<BattleTestController>();
            var screen = Object.FindObjectOfType<BattleScreenView>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(controller.UsesFormalView, Is.True);
            Assert.That(controller.IsRunBattle, Is.True);
            Assert.That(screen.HasCompleteBindings, Is.True);
            AssertFormalBattleInitialRender(screen);

            Assert.That(controller.PlaybackSpeed, Is.EqualTo(1f));
            if (mode == StressPlaybackMode.TwoTimes)
            {
                controller.TogglePlaybackSpeed();
                Assert.That(controller.PlaybackSpeed, Is.EqualTo(2f));
            }

            controller.StartBattle();
            if (mode == StressPlaybackMode.Skip)
            {
                yield return null;
                controller.SkipPlayback();
            }
            yield return WaitForBattleAndTransients(controller, screen);

            var result = controller.LastResult;
            AssertNestedStressResult(result);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.BattleResult));
            Assert.That(run.LastBattleResult, Is.SameAs(result));
            Assert.That(
                SettlementCount(run),
                Is.EqualTo(settlementsBefore + 1),
                $"{mode} must settle the Run exactly once.");
            AssertPresentationClean(screen, mode);

            var returnButton = FindButton(screen, "Return");
            Assert.That(returnButton, Is.Not.Null);
            Assert.That(screen.IsResultVisible, Is.True);
            Assert.That(screen.ResultTitle, Is.Not.Empty);
            Assert.That(returnButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(returnButton.interactable, Is.True);

            var battleHash = BattleDeterminismHasher.Compute(result);
            var settledFingerprint = ComparableRunFingerprint(mapper, run);

            // Every public re-entry route must remain idempotent once the
            // result and its Run settlement have been finalized.
            controller.SkipPlayback();
            controller.StartBattle();
            Assert.That(controller.ResolveImmediately(), Is.SameAs(result));
            yield return null;
            Assert.That(
                SettlementCount(run),
                Is.EqualTo(settlementsBefore + 1));
            Assert.That(run.LastBattleResult, Is.SameAs(result));
            Assert.That(
                BattleDeterminismHasher.Compute(controller.LastResult),
                Is.EqualTo(battleHash));
            Assert.That(
                ComparableRunFingerprint(mapper, run),
                Is.EqualTo(settledFingerprint));
            Assert.That(screen.IsResultVisible, Is.True);
            Assert.That(returnButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(returnButton.interactable, Is.True);
            AssertPresentationClean(screen, mode);

            observations.Add(new StressBattleObservation(
                mode,
                battleHash,
                settledFingerprint));

            returnButton.onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(GameSceneNames.Run));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.BattleResult));
            Assert.That(
                SettlementCount(run),
                Is.EqualTo(settlementsBefore + 1));
            Assert.That(
                ComparableRunFingerprint(mapper, run),
                Is.EqualTo(settledFingerprint),
                "Returning through the formal result button must not mutate " +
                "the settled Run.");
        }

        private static RunSession PrepareNestedDeathrattleBattle(int seed)
        {
            GameApp.Instance.StartNewRun(seed);
            var run = GameApp.Instance.Run;
            Assert.That(run, Is.Not.Null);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(
                run.EndShopAndPrepareBattle(GameSceneNames.Run).Success,
                Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(run.PendingBattle, Is.Not.Null);

            var originalContext = run.PendingBattle;
            run.PrepareBattle(new BattleContext(
                BuildNestedDeathrattleFixture(),
                "G4 nested deathrattle stress fixture",
                GameSceneNames.Run,
                originalContext.NodeAttemptId,
                originalContext.EncounterId,
                seed));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle),
                "The test fixture may replace the pending board, not RunState.");
            Assert.That(run.PendingBattle, Is.Not.Null);
            return run;
        }

        private static void AssertFormalBattleInitialRender(
            BattleScreenView screen)
        {
            Assert.That(screen.RenderedCardCount, Is.EqualTo(4));
            var standees = Object.FindObjectsOfType<BattleStandeeView>()
                .Where(value =>
                    value != null &&
                    value.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(standees, Has.Length.EqualTo(4));
            Assert.That(
                standees.Select(value => value.InstanceId),
                Is.EquivalentTo(new[]
                {
                    FoxRuntimeId,
                    DeerRuntimeId,
                    TombGuardianRuntimeId,
                    EnemyRuntimeId
                }));
            Assert.That(
                standees.All(value => value.HasCompleteBindings),
                Is.True,
                "Every active stress standee must retain complete bindings.");
            Assert.That(
                standees.All(value =>
                    value.transform.IsChildOf(screen.transform)),
                Is.True,
                "Every active stress standee must belong to the formal screen.");
        }

        private static void AssertNestedStressResult(
            BattleSimulationResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Diagnostics.HitEffectLimit, Is.False);
            Assert.That(
                result.Diagnostics.ProcessedEffectCount,
                Is.GreaterThan(0));
            Assert.That(
                result.Diagnostics.Player.SummonSuccesses,
                Is.GreaterThanOrEqualTo(4));
            Assert.That(
                result.Diagnostics.Player.NonTokenDeaths,
                Is.GreaterThanOrEqualTo(3));
            Assert.That(
                result.Diagnostics.Player.TokenDeaths,
                Is.GreaterThanOrEqualTo(3));
            Assert.That(
                result.Diagnostics.Player.PermanentAttackDelta,
                Is.GreaterThanOrEqualTo(2));
            Assert.That(
                result.Diagnostics.Player.PermanentHealthDelta,
                Is.GreaterThanOrEqualTo(2));
            AssertNestedDeathrattleOrder(result.PlaybackEvents);
            AssertGroupPermanentGrowth(result);
        }

        private static void AssertPresentationClean(
            BattleScreenView screen,
            StressPlaybackMode mode)
        {
            Assert.That(screen.IsAnimationPlaying, Is.False, mode.ToString());
            Assert.That(screen.ActiveFeedbackFxCount, Is.Zero, mode.ToString());
            Assert.That(
                HasActiveNonLoopingAudio(),
                Is.False,
                mode.ToString());
            Assert.That(
                HasActiveNonLoopingAnimation(),
                Is.False,
                mode.ToString());
        }

        private static Button FindButton(Component root, string buttonName)
        {
            return root.GetComponentsInChildren<Button>(true)
                .SingleOrDefault(value =>
                    string.Equals(
                        value.name,
                        buttonName,
                        StringComparison.Ordinal));
        }

        private static void BuyMinionsWithoutTripling(
            ShopTestController controller,
            int count,
            bool requireChoiceFreePlay)
        {
            var session = controller.Session;
            for (var purchaseNumber = 0; purchaseNumber < count; purchaseNumber++)
            {
                var ownedCounts = session.Collection.Bench
                    .Concat(session.Collection.Battle)
                    .Where(card => card != null)
                    .GroupBy(card => card.ConfigId)
                    .ToDictionary(group => group.Key, group => group.Count());
                var legalOfferIndexes =
                    Enumerable.Range(0, session.MinionOffers.Count)
                    .Where(index =>
                    {
                        var offer = session.MinionOffers[index];
                        return offer != null &&
                               (!requireChoiceFreePlay ||
                                offer.Effects == null ||
                                offer.Effects.All(effect =>
                                    effect == null ||
                                    effect.Trigger != "OnPlay")) &&
                               (!ownedCounts.TryGetValue(offer.Id, out var owned) ||
                                owned < 2);
                    })
                    .ToArray();
                Assert.That(
                    legalOfferIndexes,
                    Is.Not.Empty,
                    "The deterministic stress seed must expose a legal " +
                    "non-tripling purchase.");
                var offerIndex = legalOfferIndexes[0];

                var goldBefore = session.Gold;
                var result = controller.BuyMinionAt(offerIndex);
                Assert.That(result.Success, Is.True);
                Assert.That(
                    session.Gold,
                    Is.EqualTo(goldBefore -
                               ShopEconomyRules.MinionPurchaseCost));
            }
        }

        private static void PlayAllBenchMinions(
            ShopTestController controller)
        {
            var session = controller.Session;
            for (var battleIndex = 0;
                 battleIndex < ShopEconomyRules.BattleSlotCount;
                 battleIndex++)
            {
                var benchIndex = Enumerable.Range(
                        0,
                        ShopEconomyRules.BenchSlotCount)
                    .First(index => session.Collection.Bench[index] != null);
                var play = controller.PlayBenchMinion(
                    benchIndex,
                    battleIndex);
                Assert.That(play.Success, Is.True);
                Assert.That(session.PendingDiscover, Is.Null,
                    "The five cards selected for the battle row must not " +
                    "open a discover modal.");
                Assert.That(session.PendingChoice, Is.Null,
                    "The five cards selected for the battle row must not " +
                    "open a targeted battlecry choice.");
            }
        }

        private static BattleBoardState BuildNestedDeathrattleFixture()
        {
            var configs = GameApp.Instance.Configs;
            var board = new BattleBoardState();
            board.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["fox_den_matriarch"],
                isGolden: true,
                initialHealth: 1,
                sourceInstanceId: FoxRuntimeId,
                permanentKeywords: new[] { "Shield" },
                runtimeInstanceId: FoxRuntimeId);
            board.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"],
                initialHealth: 1,
                sourceInstanceId: DeerRuntimeId,
                permanentKeywords: new[] { "Shield" },
                runtimeInstanceId: DeerRuntimeId);
            board.Player[2] = new BattleMinionRuntime(
                configs.MinionsById["thousand_ring_tomb_guardian"],
                initialHealth: 1,
                sourceInstanceId: TombGuardianRuntimeId,
                runtimeInstanceId: TombGuardianRuntimeId);
            board.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["mirrorsteel_duelist"],
                initialAttack: 100,
                initialHealth: 300,
                runtimeInstanceId: EnemyRuntimeId);
            return board;
        }

        private static void AssertNestedDeathrattleOrder(
            IReadOnlyList<BattlePlaybackEvent> events)
        {
            var foxDeath = FindEventIndex(
                events,
                0,
                value => value.Kind == BattlePlaybackEventKind.UnitDied &&
                         value.TargetInstanceId == FoxRuntimeId);
            var shadowSummon = FindEventIndex(
                events,
                foxDeath + 1,
                value => value.Kind == BattlePlaybackEventKind.UnitSummoned &&
                         EndsWithRuntimeId(
                             value.TargetInstanceId,
                             "token_two_tailed_fox_shadow"));
            var shadowRuntimeId = events[shadowSummon].TargetInstanceId;
            var shadowDeath = FindEventIndex(
                events,
                shadowSummon + 1,
                value => value.Kind == BattlePlaybackEventKind.UnitDied &&
                         value.TargetInstanceId == shadowRuntimeId);
            var firstNestedYoungSpiritSummon = FindEventIndex(
                events,
                shadowDeath + 1,
                value =>
                    value.Kind == BattlePlaybackEventKind.UnitSummoned &&
                    value.SourceInstanceId == shadowRuntimeId &&
                    EndsWithRuntimeId(
                        value.TargetInstanceId,
                        "token_young_spirit"));
            var nestedYoungSpiritSummons = events.Count(value =>
                value.Kind == BattlePlaybackEventKind.UnitSummoned &&
                value.SourceInstanceId == shadowRuntimeId &&
                EndsWithRuntimeId(
                    value.TargetInstanceId,
                    "token_young_spirit"));
            var deerDeath = FindEventIndex(
                events,
                0,
                value => value.Kind == BattlePlaybackEventKind.UnitDied &&
                         value.TargetInstanceId == DeerRuntimeId);
            var tombGuardianDeath = FindEventIndex(
                events,
                0,
                value => value.Kind == BattlePlaybackEventKind.UnitDied &&
                         value.TargetInstanceId == TombGuardianRuntimeId);

            Assert.That(foxDeath, Is.LessThan(shadowSummon));
            Assert.That(shadowSummon, Is.LessThan(shadowDeath));
            Assert.That(
                shadowDeath,
                Is.LessThan(firstNestedYoungSpiritSummon));
            Assert.That(nestedYoungSpiritSummons, Is.GreaterThanOrEqualTo(2),
                "Nested young spirits must identify the dead fox shadow as " +
                "their SourceInstanceId.");
            Assert.That(deerDeath, Is.GreaterThanOrEqualTo(0),
                "A second root deathrattle must also resolve in the same battle.");
            Assert.That(tombGuardianDeath, Is.GreaterThanOrEqualTo(0),
                "The taunt tomb guardian must die and run its group effects.");
        }

        private static void AssertGroupPermanentGrowth(
            BattleSimulationResult result)
        {
            foreach (var runtimeId in new[] { FoxRuntimeId, DeerRuntimeId })
            {
                Assert.That(
                    result.PlaybackEvents.Any(value =>
                        value.Kind == BattlePlaybackEventKind.StatsChanged &&
                        value.TargetInstanceId == runtimeId &&
                        value.AttackDelta >= 1 &&
                        value.HealthDelta >= 1),
                    Is.True,
                    $"{runtimeId} did not receive the guardian's visible +1/+1.");
                Assert.That(
                    result.PermanentDeltas.Any(value =>
                        value.SourceInstanceId == runtimeId &&
                        value.Attack >= 1 &&
                        value.Health >= 1),
                    Is.True,
                    $"{runtimeId} did not retain the guardian's permanent +1/+1.");
            }
        }

        private static int FindEventIndex(
            IReadOnlyList<BattlePlaybackEvent> events,
            int startIndex,
            Func<BattlePlaybackEvent, bool> predicate)
        {
            for (var index = Math.Max(0, startIndex);
                 index < events.Count;
                 index++)
            {
                if (predicate(events[index]))
                {
                    return index;
                }
            }

            Assert.Fail("Expected battle playback event was not found.");
            return -1;
        }

        private static bool EndsWithRuntimeId(string runtimeId, string configId)
        {
            return !string.IsNullOrWhiteSpace(runtimeId) &&
                   runtimeId.EndsWith(
                       ":" + configId,
                       StringComparison.Ordinal);
        }

        private static IEnumerator WaitForBattleAndTransients(
            BattleTestController controller,
            BattleScreenView screen)
        {
            var deadline = Time.realtimeSinceStartupAsDouble + 45d;
            while (Time.realtimeSinceStartupAsDouble < deadline &&
                   (controller == null ||
                    screen == null ||
                    controller.LastResult == null ||
                    screen.IsAnimationPlaying ||
                    screen.ActiveFeedbackFxCount > 0 ||
                    HasActiveNonLoopingAudio() ||
                    HasActiveNonLoopingAnimation()))
            {
                yield return null;
            }

            Assert.That(controller, Is.Not.Null);
            Assert.That(screen, Is.Not.Null);
            Assert.That(controller.LastResult, Is.Not.Null,
                "The stress battle exceeded its 45 second watchdog.");
        }

        private static bool HasActiveNonLoopingAudio()
        {
            return Object.FindObjectsOfType<AudioSource>().Any(source =>
                source != null &&
                source.gameObject.activeInHierarchy &&
                source.isPlaying &&
                !source.loop);
        }

        private static bool HasActiveNonLoopingAnimation()
        {
            foreach (var animation in Object.FindObjectsOfType<Animation>())
            {
                if (animation == null ||
                    !animation.gameObject.activeInHierarchy ||
                    !animation.isPlaying ||
                    animation.clip == null)
                {
                    continue;
                }

                if (animation.clip.wrapMode != WrapMode.Loop &&
                    animation.clip.wrapMode != WrapMode.PingPong)
                {
                    return true;
                }
            }

            foreach (var animator in Object.FindObjectsOfType<Animator>())
            {
                if (animator == null ||
                    !animator.enabled ||
                    !animator.gameObject.activeInHierarchy ||
                    animator.runtimeAnimatorController == null)
                {
                    continue;
                }

                for (var layer = 0; layer < animator.layerCount; layer++)
                {
                    var state = animator.GetCurrentAnimatorStateInfo(layer);
                    if (animator.IsInTransition(layer) ||
                        (!state.loop && state.normalizedTime < 1f))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int SettlementCount(RunSession run)
        {
            return run.State.Statistics.BattlesWon +
                   run.State.Statistics.BattlesNotWon;
        }

        private static string ComparableRunFingerprint(
            RunSnapshotMapper mapper,
            RunSession run)
        {
            var payload = mapper.Capture(run);
            payload.RunState.Statistics.StartedAtUtc = default(DateTime);
            payload.RunState.Statistics.CompletedAtUtc = null;
            return RunStateFingerprint.Compute(payload);
        }

        private static void AssertFinitePositiveRect(RectTransform rect)
        {
            Assert.That(IsFinite(rect.rect.width), Is.True, rect.name);
            Assert.That(IsFinite(rect.rect.height), Is.True, rect.name);
            Assert.That(rect.rect.width, Is.GreaterThan(0f), rect.name);
            Assert.That(rect.rect.height, Is.GreaterThan(0f), rect.name);
            Assert.That(IsFinite(rect.position.x), Is.True, rect.name);
            Assert.That(IsFinite(rect.position.y), Is.True, rect.name);
            Assert.That(IsFinite(rect.localScale.x), Is.True, rect.name);
            Assert.That(IsFinite(rect.localScale.y), Is.True, rect.name);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void AssertWorldRectContains(
            RectTransform outer,
            RectTransform inner,
            string label)
        {
            var outerCorners = new Vector3[4];
            var innerCorners = new Vector3[4];
            outer.GetWorldCorners(outerCorners);
            inner.GetWorldCorners(innerCorners);
            const float tolerance = 1.5f;
            var outerMinX = outerCorners.Min(value => value.x) - tolerance;
            var outerMaxX = outerCorners.Max(value => value.x) + tolerance;
            var outerMinY = outerCorners.Min(value => value.y) - tolerance;
            var outerMaxY = outerCorners.Max(value => value.y) + tolerance;
            var innerDescription = string.Join(
                ", ",
                innerCorners.Select(value =>
                    $"({value.x:0.##},{value.y:0.##})"));

            Assert.That(
                innerCorners.All(value =>
                    value.x >= outerMinX &&
                    value.x <= outerMaxX &&
                    value.y >= outerMinY &&
                    value.y <= outerMaxY),
                Is.True,
                $"{label} escaped {outer.name}: " +
                $"outer=({outerMinX:0.##},{outerMinY:0.##})-" +
                $"({outerMaxX:0.##},{outerMaxY:0.##}), " +
                $"inner={innerDescription}");
        }

        private static void SetGameAppProperty<T>(string name, T value)
        {
            var property = typeof(GameApp).GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, name);
            var setter = property.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null, name);
            setter.Invoke(GameApp.Instance, new object[] { value });
        }

        private static IEnumerator EnsureGameApp()
        {
            if (GameApp.Instance == null)
            {
                yield return null;
            }

            Assert.That(GameApp.Instance, Is.Not.Null);
            Assert.That(GameApp.Instance.Configs, Is.Not.Null);
            yield return null;
        }

        private enum StressPlaybackMode
        {
            OneTimes,
            TwoTimes,
            Skip
        }

        private sealed class StressBattleObservation
        {
            public StressBattleObservation(
                StressPlaybackMode mode,
                string battleHash,
                string runFingerprint)
            {
                Mode = mode;
                BattleHash = battleHash;
                RunFingerprint = runFingerprint;
            }

            public StressPlaybackMode Mode { get; }
            public string BattleHash { get; }
            public string RunFingerprint { get; }
        }
    }
}
