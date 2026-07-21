using System.Collections.Generic;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;
using SpireChess.Utils;

namespace SpireChess.Tests
{
    public sealed class RunFlowTests
    {
        [Test]
        public void FixedMap_StartsAtExplicitShopAndLocksCombatUntilShopResolves()
        {
            var run = CreateRun(11);

            Assert.That(run.State.CurrentMap.Id, Is.EqualTo("phase4d_floor1"));
            Assert.That(run.State.CurrentMap.Nodes.Count, Is.EqualTo(15));
            Assert.That(run.State.ShopTurn, Is.Zero);
            Assert.That(run.State.MapStep, Is.Zero);
            Assert.That(run.State.Health, Is.EqualTo(20));
            Assert.That(run.State.MapProgress.GetStatus("f1_shop_start"),
                Is.EqualTo(RunNodeStatus.Reachable));
            Assert.That(run.State.MapProgress.GetStatus("f1_opening_normal"),
                Is.EqualTo(RunNodeStatus.Locked));

            Assert.That(run.EnterNode("f1_opening_normal").Error,
                Is.EqualTo(RunOperationError.NodeNotReachable));
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Shop));
            Assert.That(run.State.ShopTurn, Is.EqualTo(1));
            Assert.That(run.State.MapStep, Is.EqualTo(1));
            Assert.That(run.Shop.Gold, Is.EqualTo(3));
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.MapSelection));
            Assert.That(run.PendingBattle, Is.Null);
            Assert.That(run.State.MapProgress.GetStatus("f1_opening_normal"),
                Is.EqualTo(RunNodeStatus.Reachable));
        }

        [Test]
        public void SafeRoute_UsesFiveExplicitShopsAndFiveCombatNodes()
        {
            var run = CreateRun(17);

            CompleteShop(run, "f1_shop_start", 3);
            CompleteCombat(run, "f1_opening_normal");
            CompleteShop(run, "f1_shop_2", 5);
            CompleteCombat(run, "f1_safe_normal");
            CompleteRest(run, "f1_rest");
            CompleteShop(run, "f1_shop_3", 5);
            CompleteCombat(run, "f1_mid_mechanic");
            CompleteShop(run, "f1_shop_4", 6);
            CompleteCombat(run, "f1_late_shield");
            CompleteShop(run, "f1_shop_boss", 7);

            Assert.That(run.State.ShopTurn, Is.EqualTo(5));
            Assert.That(run.Shop.Round, Is.EqualTo(5));
            Assert.That(run.State.MapStep, Is.EqualTo(10));
            Assert.That(run.EnterNode("f1_boss").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(run.State.CurrentMap.TryGetNode("f1_boss", out var boss), Is.True);
            Assert.That(boss.CombatIndex, Is.EqualTo(5));
            ResolvePlayerWin(run);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RelicChoice));
            Assert.That(run.SkipRelicChoice().Error, Is.EqualTo(RunOperationError.InvalidChoice));
            Assert.That(run.SelectRelicCandidate(
                run.State.PendingRelicChoice.Candidates[0].CandidateId).Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
            Assert.That(run.State.Statistics.BattlesWon, Is.EqualTo(5));
        }

        [Test]
        public void BossLoss_RetryKeepsShopTurnMapStepAndBattleSeed()
        {
            var run = ReachFirstBoss(23);
            var firstAttempt = run.State.CurrentAttempt.NodeAttemptId;
            var firstSeed = run.PendingBattle.BattleSeed;
            var shopTurn = run.State.ShopTurn;
            var mapStep = run.State.MapStep;
            var losingState = run.PendingBattle.BoardState.Clone();
            var result = new BattleSimulationResult(
                losingState,
                BattleSide.Enemy,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());

            Assert.That(run.TryCompleteBattle(result, out _), Is.True);
            var healthAfterFirstSettlement = run.State.Health;
            Assert.That(healthAfterFirstSettlement, Is.LessThan(20));
            Assert.That(run.State.CurrentAttempt.NodeResolved, Is.False);
            Assert.That(run.Shop.IsShopOpen, Is.False);

            Assert.That(run.RetryBoss().Success, Is.True);
            Assert.That(run.State.CurrentAttempt.NodeAttemptId, Is.Not.EqualTo(firstAttempt));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(run.State.ShopTurn, Is.EqualTo(shopTurn));
            Assert.That(run.State.MapStep, Is.EqualTo(mapStep));
            Assert.That(run.Shop.Gold, Is.Zero);
            Assert.That(run.Shop.IsShopOpen, Is.False);
            Assert.That(run.PendingBattle.BattleSeed, Is.EqualTo(firstSeed));
            Assert.That(run.State.MapProgress.GetStatus("f1_boss"),
                Is.EqualTo(RunNodeStatus.Current));
        }

        [Test]
        public void NormalLoss_AppliesDamageOnceAndStillResolvesCombatNode()
        {
            var run = CreateRun(31);
            CompleteShop(run, "f1_shop_start", 3);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            var finalState = run.PendingBattle.BoardState.Clone();
            var result = new BattleSimulationResult(
                finalState,
                BattleSide.Enemy,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());

            Assert.That(run.TryCompleteBattle(result, out _), Is.True);
            var health = run.State.Health;
            Assert.That(run.TryCompleteBattle(result, out _), Is.False);
            Assert.That(run.State.Health, Is.EqualTo(health));
            Assert.That(run.State.CurrentAttempt.HealthDamageApplied, Is.True);
            Assert.That(run.State.CurrentAttempt.NodeResolved, Is.True);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.State.MapProgress.GetStatus("f1_shop_2"),
                Is.EqualTo(RunNodeStatus.Reachable));
        }

        [Test]
        public void MinionPool_ExplicitReservationCanBeReturnedExactlyOnce()
        {
            var configs = CreateConfigs();
            var minion = configs.MinionsById["copper_ring_apprentice"];
            var pool = new MinionPool(new[] { minion });
            var initial = pool.GetRemainingCopies(minion.Id);

            Assert.That(pool.TryReserveCopies(minion.Id, 1), Is.True);
            Assert.That(pool.GetRemainingCopies(minion.Id), Is.EqualTo(initial - 1));
            Assert.That(pool.ReturnCopies(minion.Id, 1), Is.EqualTo(1));
            Assert.That(pool.ReturnCopies(minion.Id, 1), Is.EqualTo(0));
            Assert.That(pool.GetRemainingCopies(minion.Id), Is.EqualTo(initial));
        }

        private static RunSession ReachFirstBoss(int seed)
        {
            var run = CreateRun(seed);
            CompleteShop(run, "f1_shop_start", 3);
            CompleteCombat(run, "f1_opening_normal");
            CompleteShop(run, "f1_shop_2", 5);
            CompleteCombat(run, "f1_safe_normal");
            CompleteRest(run, "f1_rest");
            CompleteShop(run, "f1_shop_3", 5);
            CompleteCombat(run, "f1_mid_mechanic");
            CompleteShop(run, "f1_shop_4", 6);
            CompleteCombat(run, "f1_late_shield");
            CompleteShop(run, "f1_shop_boss", 7);
            Assert.That(run.EnterNode("f1_boss").Success, Is.True);
            return run;
        }

        private static void CompleteShop(RunSession run, string nodeId, int expectedGold)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Shop));
            Assert.That(run.Shop.Gold, Is.EqualTo(expectedGold));
            ClaimAllRewards(run);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.MapSelection));
        }

        private static void CompleteCombat(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle));
            ResolvePlayerWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
        }

        private static void CompleteRest(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
        }

        private static void ClaimAllRewards(RunSession run)
        {
            while (run.State.PendingCardRewards.Count > 0)
            {
                var result = run.ClaimNextCardReward();
                if (result.Success)
                    continue;
                Assert.That(result.Error, Is.EqualTo(RunOperationError.BenchFull));
                Assert.That(run.SkipNextCardReward().Success, Is.True);
            }
        }

        private static void ResolvePlayerWin(RunSession run)
        {
            var result = new BattleSimulationResult(
                new BattleBoardState(),
                BattleSide.Player,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());
            Assert.That(run.TryCompleteBattle(result, out var returnScene), Is.True);
            Assert.That(returnScene, Is.EqualTo("RunTest"));
        }

        private static RunSession CreateRun(int seed)
        {
            return new RunSession(CreateConfigs(), seed);
        }

        private static ConfigService CreateConfigs()
        {
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            return configs;
        }
    }
}
