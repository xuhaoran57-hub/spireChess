using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.Shop;
using SpireChess.UI.Run;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class HeroJourneyAcceptanceTests
    {
        private ConfigService configs;
        private RunSnapshotMapper mapper;

        [SetUp]
        public void SetUp()
        {
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(
                validation.IsValid,
                Is.True,
                string.Join("\n", validation.Errors));
            mapper = new RunSnapshotMapper(configs);
        }

        [TestCase(HeroIds.Warrior, 40501)]
        [TestCase(HeroIds.Mage, 40502)]
        [TestCase(HeroIds.Rogue, 40503)]
        public void ThreeChapterJourney_PreservesHeroPassivesAcrossEveryDurablePhase(
            string heroId,
            int seed)
        {
            var run = new RunSession(configs, seed, heroId);
            var mageGrants = 0;
            var rogueSteals = 0;

            run = CompleteFloor(
                run,
                1,
                "f1_safe_normal",
                "f1_route_safe",
                "f1_rest",
                "f1_late_shield",
                true,
                ref mageGrants,
                ref rogueSteals);
            run = SelectFirstRelicAndAdvance(run);
            Assert.That(run.State.CurrentMap.Id, Is.EqualTo("map_startrail_highlands"));
            Assert.That(
                RunScreenStateBuilder.Build(run, configs, string.Empty).RouteHint,
                Does.Contain("星轨大司辰"));

            run = CompleteFloor(
                run,
                2,
                "f2_normal",
                "f2_route_safe",
                "f2_rest",
                "f2_late_break",
                false,
                ref mageGrants,
                ref rogueSteals);
            run = SelectFirstRelicAndAdvance(run);
            Assert.That(run.State.CurrentMap.Id, Is.EqualTo("map_soulforge_city"));
            Assert.That(
                RunScreenStateBuilder.Build(run, configs, string.Empty).RouteHint,
                Does.Contain("铸魂不灭王"));

            run = CompleteFloor(
                run,
                3,
                "f3_normal",
                "f3_route_safe",
                "f3_rest",
                "f3_late_wild",
                false,
                ref mageGrants,
                ref rogueSteals);

            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RunWon));
            Assert.That(run.State.HeroId, Is.EqualTo(heroId));
            Assert.That(run.State.ShopTurn, Is.EqualTo(18));
            Assert.That(run.State.MapStep, Is.EqualTo(39));
            Assert.That(run.State.Statistics.BattlesWon, Is.EqualTo(18));
            Assert.That(run.State.Statistics.BattlesNotWon, Is.EqualTo(1));
            Assert.That(run.State.Statistics.BossesDefeated, Is.EqualTo(3));
            Assert.That(run.State.Statistics.CompletedAtUtc, Is.Not.Null);

            var runtime = run.State.HeroRuntime;
            var heroDraws = mapper.Capture(run).RandomStreams.Hero.Entries.Count;
            switch (heroId)
            {
                case HeroIds.Warrior:
                    Assert.That(run.State.Armor, Is.LessThanOrEqualTo(
                        HeroPassiveRules.WarriorStartingArmor));
                    Assert.That(runtime.ProcessedShopStartTurns, Is.Empty);
                    Assert.That(runtime.ProcessedShopEndTurns, Is.Empty);
                    Assert.That(heroDraws, Is.Zero);
                    break;
                case HeroIds.Mage:
                    Assert.That(runtime.ProcessedShopStartTurns.Count, Is.EqualTo(18));
                    Assert.That(runtime.ProcessedShopEndTurns.Count, Is.EqualTo(18));
                    Assert.That(mageGrants, Is.GreaterThan(0));
                    Assert.That(heroDraws, Is.EqualTo(mageGrants));
                    Assert.That(
                        run.Shop.Collection.Bench.Any(card =>
                            card != null && card.ExpiresAtShopEnd),
                        Is.False);
                    break;
                case HeroIds.Rogue:
                    Assert.That(runtime.ProcessedShopStartTurns, Is.Empty);
                    Assert.That(runtime.ProcessedShopEndTurns.Count, Is.EqualTo(18));
                    Assert.That(rogueSteals, Is.GreaterThan(0));
                    Assert.That(heroDraws, Is.EqualTo(rogueSteals));
                    break;
            }

            var screen = RunScreenStateBuilder.Build(run, configs, string.Empty);
            var hero = HeroCatalog.GetRequired(heroId);
            Assert.That(screen.ResourceSummary, Does.Contain(hero.DisplayName));
            Assert.That(screen.ResourceSummary, Does.Contain(hero.PassiveName));
            Assert.That(screen.Summary.Text, Does.Contain(hero.DisplayName));
            AssertRoundTripEquivalent(run);
        }

        [Test]
        public void RunScreenState_AlwaysIdentifiesSelectedHeroAndPassive()
        {
            foreach (var hero in HeroCatalog.All)
            {
                var run = new RunSession(configs, 40510, hero.Id);
                var state = RunScreenStateBuilder.Build(
                    run,
                    configs,
                    "职业验收");

                Assert.That(state.ResourceSummary, Does.Contain(hero.DisplayName));
                Assert.That(state.ResourceSummary, Does.Contain(hero.PassiveName));
                Assert.That(state.Status, Is.EqualTo("职业验收"));
            }
        }

        private RunSession CompleteFloor(
            RunSession run,
            int floor,
            string earlyCombat,
            string routeCombat,
            string utility,
            string lateCombat,
            bool retryBoss,
            ref int mageGrants,
            ref int rogueSteals)
        {
            run = CompleteShop(
                run,
                $"f{floor}_shop_start",
                ref mageGrants,
                ref rogueSteals);
            run = CompleteCombat(run, $"f{floor}_opening_normal");
            run = CompleteShop(
                run,
                $"f{floor}_shop_2",
                ref mageGrants,
                ref rogueSteals);
            run = CompleteCombat(run, earlyCombat);
            run = CompleteShop(
                run,
                $"f{floor}_shop_3",
                ref mageGrants,
                ref rogueSteals);
            run = CompleteCombat(run, $"f{floor}_mid_mechanic");
            run = CompleteShop(
                run,
                $"f{floor}_shop_4",
                ref mageGrants,
                ref rogueSteals);
            run = CompleteCombat(run, routeCombat);

            Assert.That(run.EnterNode(utility).Success, Is.True, utility);
            Assert.That(run.SelectRestOption("leave").Success, Is.True, utility);
            run = RoundTrip(run);

            run = CompleteShop(
                run,
                $"f{floor}_shop_5",
                ref mageGrants,
                ref rogueSteals);
            run = CompleteCombat(run, lateCombat);
            run = CompleteShop(
                run,
                $"f{floor}_shop_boss",
                ref mageGrants,
                ref rogueSteals);
            Assert.That(run.EnterNode($"f{floor}_boss").Success, Is.True);
            run = RoundTrip(run);
            if (retryBoss)
            {
                run = LoseAndRetryBoss(run);
            }

            ResolveWin(run);
            return RoundTrip(run);
        }

        private RunSession CompleteShop(
            RunSession run,
            string nodeId,
            ref int mageGrants,
            ref int rogueSteals)
        {
            var heroCallsBefore = HeroDrawCount(run);
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Shop), nodeId);

            if (run.State.HeroId == HeroIds.Mage)
            {
                Assert.That(
                    run.State.HeroRuntime.ProcessedShopStartTurns,
                    Does.Contain(run.State.ShopTurn));
                if (run.State.HeroRuntime.LastShopStartOutcome ==
                    HeroPassiveShopStartOutcome.GrantedTemporarySpell)
                {
                    mageGrants++;
                    Assert.That(HeroDrawCount(run), Is.EqualTo(heroCallsBefore + 1));
                    Assert.That(
                        run.Shop.Collection.Bench.Count(card =>
                            card != null && card.ExpiresAtShopEnd),
                        Is.EqualTo(1));
                }
                else
                {
                    Assert.That(HeroDrawCount(run), Is.EqualTo(heroCallsBefore));
                }
            }
            else
            {
                Assert.That(HeroDrawCount(run), Is.EqualTo(heroCallsBefore));
            }

            var openHeroCalls = HeroDrawCount(run);
            var openTemporaryCount = run.Shop.Collection.Bench.Count(card =>
                card != null && card.ExpiresAtShopEnd);
            run = RoundTrip(run);
            Assert.That(run.EnsureShopOpen().Success, Is.True);
            Assert.That(HeroDrawCount(run), Is.EqualTo(openHeroCalls));
            Assert.That(
                run.Shop.Collection.Bench.Count(card =>
                    card != null && card.ExpiresAtShopEnd),
                Is.EqualTo(openTemporaryCount));

            ClaimAllRewards(run);
            var beforeCloseHeroCalls = HeroDrawCount(run);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True, nodeId);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.MapSelection), nodeId);

            if (run.State.HeroId == HeroIds.Mage)
            {
                Assert.That(
                    run.State.HeroRuntime.ProcessedShopEndTurns,
                    Does.Contain(run.State.ShopTurn));
                Assert.That(HeroDrawCount(run), Is.EqualTo(beforeCloseHeroCalls));
                Assert.That(
                    run.Shop.Collection.Bench.Any(card =>
                        card != null && card.ExpiresAtShopEnd),
                    Is.False);
            }
            else if (run.State.HeroId == HeroIds.Rogue)
            {
                Assert.That(
                    run.State.HeroRuntime.ProcessedShopEndTurns,
                    Does.Contain(run.State.ShopTurn));
                if (run.State.HeroRuntime.LastShopEndOutcome ==
                    HeroPassiveShopEndOutcome.StoleMinion)
                {
                    rogueSteals++;
                    Assert.That(
                        HeroDrawCount(run),
                        Is.EqualTo(beforeCloseHeroCalls + 1));
                }
                else
                {
                    Assert.That(HeroDrawCount(run), Is.EqualTo(beforeCloseHeroCalls));
                }
            }
            else
            {
                Assert.That(HeroDrawCount(run), Is.EqualTo(beforeCloseHeroCalls));
            }

            run = RoundTrip(run);
            var closedHeroCalls = HeroDrawCount(run);
            Assert.That(
                run.EndShopAndPrepareBattle("RunTest").Success,
                Is.False);
            Assert.That(HeroDrawCount(run), Is.EqualTo(closedHeroCalls));
            return run;
        }

        private RunSession CompleteCombat(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle), nodeId);
            run = RoundTrip(run);
            ResolveWin(run);
            run = RoundTrip(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True, nodeId);
            return RoundTrip(run);
        }

        private RunSession LoseAndRetryBoss(RunSession run)
        {
            var originalAttemptId = run.State.CurrentAttempt.NodeAttemptId;
            var originalBattleSeed = run.PendingBattle.BattleSeed;
            var shopTurn = run.State.ShopTurn;
            var mapStep = run.State.MapStep;
            var startTurns = run.State.HeroRuntime.ProcessedShopStartTurns.ToArray();
            var endTurns = run.State.HeroRuntime.ProcessedShopEndTurns.ToArray();
            var heroDraws = HeroDrawCount(run);
            var finalState = new BattleBoardState();
            var tierOne = configs.Minions.First(minion =>
                minion.Enabled &&
                !minion.IsToken &&
                minion.Tier == 1);
            finalState.Enemy[0] = new BattleMinionRuntime(tierOne);
            var loss = new BattleSimulationResult(
                finalState,
                BattleSide.Enemy,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());
            Assert.That(run.TryCompleteBattle(loss, out _), Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.BattleResult));
            Assert.That(run.State.Health, Is.GreaterThan(0));
            var health = run.State.Health;
            var armor = run.State.Armor;

            run = RoundTrip(run);
            Assert.That(run.RetryBoss().Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(
                run.State.CurrentAttempt.NodeAttemptId,
                Is.Not.EqualTo(originalAttemptId));
            Assert.That(run.PendingBattle.BattleSeed, Is.EqualTo(originalBattleSeed));
            Assert.That(run.State.ShopTurn, Is.EqualTo(shopTurn));
            Assert.That(run.State.MapStep, Is.EqualTo(mapStep));
            Assert.That(run.State.Health, Is.EqualTo(health));
            Assert.That(run.State.Armor, Is.EqualTo(armor));
            Assert.That(
                run.State.HeroRuntime.ProcessedShopStartTurns,
                Is.EquivalentTo(startTurns));
            Assert.That(
                run.State.HeroRuntime.ProcessedShopEndTurns,
                Is.EquivalentTo(endTurns));
            Assert.That(HeroDrawCount(run), Is.EqualTo(heroDraws));
            return RoundTrip(run);
        }

        private RunSession SelectFirstRelicAndAdvance(RunSession run)
        {
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RelicChoice));
            run = RoundTrip(run);
            Assert.That(
                run.SelectRelicCandidate(
                    run.State.PendingRelicChoice.Candidates[0].CandidateId).Success,
                Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
            run = RoundTrip(run);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            return RoundTrip(run);
        }

        private void ClaimAllRewards(RunSession run)
        {
            while (run.State.PendingCardRewards.Count > 0)
            {
                var result = run.ClaimNextCardReward();
                if (result.Success)
                {
                    continue;
                }

                Assert.That(result.Error, Is.EqualTo(RunOperationError.BenchFull));
                Assert.That(run.SkipNextCardReward().Success, Is.True);
            }
        }

        private static void ResolveWin(RunSession run)
        {
            var result = new BattleSimulationResult(
                new BattleBoardState(),
                BattleSide.Player,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());
            Assert.That(run.TryCompleteBattle(result, out var scene), Is.True);
            Assert.That(scene, Is.EqualTo("RunTest"));
        }

        private int HeroDrawCount(RunSession run)
        {
            return mapper.Capture(run).RandomStreams.Hero.Entries.Count;
        }

        private RunSession RoundTrip(RunSession run)
        {
            var before = mapper.Capture(run);
            var validation = new RunSnapshotValidator(configs).ValidateDto(before);
            Assert.That(
                validation.IsValid,
                Is.True,
                string.Join("\n", validation.Errors));
            var fingerprint = RunStateFingerprint.Compute(before);
            var restored = mapper.Restore(before);
            Assert.That(
                RunStateFingerprint.Compute(mapper.Capture(restored)),
                Is.EqualTo(fingerprint));
            return restored;
        }

        private void AssertRoundTripEquivalent(RunSession run)
        {
            var restored = RoundTrip(run);
            Assert.That(restored.State.HeroId, Is.EqualTo(run.State.HeroId));
            Assert.That(restored.State.Phase, Is.EqualTo(run.State.Phase));
            Assert.That(restored.State.ShopTurn, Is.EqualTo(run.State.ShopTurn));
        }
    }
}
