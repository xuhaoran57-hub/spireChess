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
        public void FixedMap_StartsAtMandatoryOpenerAndBranchesToBoss()
        {
            var run = CreateRun(11);

            Assert.That(run.State.CurrentMap.Id, Is.EqualTo("phase4c_floor1"));
            Assert.That(run.State.CurrentMap.Nodes.Count, Is.EqualTo(7));
            Assert.That(run.State.RunTurn, Is.EqualTo(0));
            Assert.That(run.State.Health, Is.EqualTo(20));
            Assert.That(run.State.MapProgress.GetStatus("f1_opening_normal"),
                Is.EqualTo(RunNodeStatus.Reachable));
            Assert.That(run.State.MapProgress.GetStatus("f1_elite_wall"),
                Is.EqualTo(RunNodeStatus.Locked));
            Assert.That(run.State.MapProgress.GetStatus("f1_safe_normal"),
                Is.EqualTo(RunNodeStatus.Locked));
            Assert.That(run.State.MapProgress.GetStatus("f1_boss"),
                Is.EqualTo(RunNodeStatus.Locked));

            Assert.That(run.EnterNode("f1_elite_wall").Error,
                Is.EqualTo(RunOperationError.NodeNotReachable));
            Assert.That(run.State.RunTurn, Is.EqualTo(0));
        }

        [Test]
        public void FullDomainWinPath_UsesRunTurnRewardsAndFifoClaimGate()
        {
            var run = CreateRun(17);

            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            Assert.That(run.State.RunTurn, Is.EqualTo(1));
            Assert.That(run.Shop.Gold, Is.EqualTo(3));
            var openerAttempt = run.State.CurrentAttempt.NodeAttemptId;
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.False);
            Assert.That(run.State.RunTurn, Is.EqualTo(1));
            ResolvePlayerWin(run);
            Assert.That(run.State.CurrentAttempt.NodeAttemptId, Is.EqualTo(openerAttempt));
            Assert.That(run.State.CurrentAttempt.BattleSettled, Is.True);
            Assert.That(run.State.CurrentAttempt.RewardGenerated, Is.True);
            Assert.That(run.State.CurrentAttempt.NodeResolved, Is.True);
            Assert.That(run.State.DelayedShopResources.GoldBonus, Is.EqualTo(1));
            Assert.That(run.ContinueAfterBattle().Success, Is.True);

            Assert.That(run.State.MapProgress.GetStatus("f1_elite_wall"),
                Is.EqualTo(RunNodeStatus.Reachable));
            Assert.That(run.State.MapProgress.GetStatus("f1_safe_normal"),
                Is.EqualTo(RunNodeStatus.Reachable));
            Assert.That(run.EnterNode("f1_safe_normal").Success, Is.True);
            Assert.That(run.State.RunTurn, Is.EqualTo(2));
            Assert.That(run.Shop.Round, Is.EqualTo(2));
            Assert.That(run.Shop.Gold, Is.EqualTo(5), "4 base gold plus opening reward");
            ResolvePlayerWin(run);
            Assert.That(run.State.PendingCardRewards.Count, Is.EqualTo(1));
            Assert.That(run.State.PendingCardRewards[0].CardType, Is.EqualTo(ShopCardType.Spell));
            Assert.That(run.ContinueAfterBattle().Success, Is.True);

            Assert.That(run.State.MapProgress.GetStatus("f1_elite_wall"),
                Is.EqualTo(RunNodeStatus.Locked));
            Assert.That(run.EnterNode("f1_rest").Success, Is.True);
            Assert.That(run.State.RunTurn, Is.EqualTo(3));
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
            Assert.That(run.EnterNode("f1_boss").Success, Is.True);
            Assert.That(run.State.RunTurn, Is.EqualTo(4));
            Assert.That(run.Shop.Gold, Is.EqualTo(6));
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Error,
                Is.EqualTo(ShopOperationError.InvalidTiming));
            Assert.That(run.ClaimNextCardReward().Success, Is.True);
            Assert.That(run.State.PendingCardRewards, Is.Empty);
            Assert.That(run.Shop.Collection.Bench[0]?.ConfigId, Is.EqualTo("minor_tempering"));
            ResolvePlayerWin(run);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));
            Assert.That(run.State.PendingRewardChoice.Candidates.Count, Is.EqualTo(3));
            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
            Assert.That(run.State.MapProgress.GetStatus("f1_boss"),
                Is.EqualTo(RunNodeStatus.Resolved));
        }

        [Test]
        public void BossLoss_RetryCreatesNewAttemptAndDoesNotDoubleSettle()
        {
            var run = CreateRun(23);
            CompleteNormalNode(run, "f1_opening_normal");
            CompleteNormalNode(run, "f1_safe_normal");
            Assert.That(run.EnterNode("f1_rest").Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);

            Assert.That(run.EnterNode("f1_boss").Success, Is.True);
            var firstAttempt = run.State.CurrentAttempt.NodeAttemptId;
            Assert.That(run.ClaimNextCardReward().Success, Is.True);
            var end = run.EndShopAndPrepareBattle("RunTest");
            Assert.That(end.Success, Is.True);
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
            Assert.That(run.TryCompleteBattle(result, out _), Is.False);
            Assert.That(run.State.Health, Is.EqualTo(healthAfterFirstSettlement));
            Assert.That(run.State.CurrentAttempt.NodeResolved, Is.False);

            Assert.That(run.RetryBoss().Success, Is.True);
            Assert.That(run.State.CurrentAttempt.NodeAttemptId, Is.Not.EqualTo(firstAttempt));
            Assert.That(run.State.RunTurn, Is.EqualTo(5));
            Assert.That(run.Shop.Gold, Is.EqualTo(7));
            Assert.That(run.State.MapProgress.GetStatus("f1_boss"),
                Is.EqualTo(RunNodeStatus.Current));
        }

        [Test]
        public void NormalLoss_AppliesDamageOnceAndStillResolvesNode()
        {
            var run = CreateRun(31);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            var finalState = run.PendingBattle.BoardState.Clone();
            var result = new BattleSimulationResult(
                finalState,
                BattleSide.Enemy,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());

            Assert.That(run.TryCompleteBattle(result, out _), Is.True);
            Assert.That(run.State.Health, Is.EqualTo(18));
            Assert.That(run.State.CurrentAttempt.HealthDamageApplied, Is.True);
            Assert.That(run.State.CurrentAttempt.NodeResolved, Is.True);
            Assert.That(run.State.LastRewardSummary, Is.Empty);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.State.MapProgress.GetStatus("f1_elite_wall"),
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

        private static void CompleteNormalNode(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True);
            ResolvePlayerWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
        }

        private static void ResolvePlayerWin(RunSession run)
        {
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
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
