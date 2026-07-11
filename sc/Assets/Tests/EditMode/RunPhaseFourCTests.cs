using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Utils;

namespace SpireChess.Tests
{
    public sealed class RunPhaseFourCTests
    {
        [Test]
        public void FixedProvider_LoadsThreeValidMapsWithExpectedStarts()
        {
            var configs = CreateConfigs();
            var provider = new FixedMapProvider(configs.RunMaps);
            var first = provider.CreateMap(new MapRequest(1, 1));
            var second = provider.CreateMap(new MapRequest(1, 2));
            var third = provider.CreateMap(new MapRequest(1, 3));

            Assert.That(configs.RunMaps.Count, Is.EqualTo(3));
            Assert.That(first.Nodes.Count, Is.EqualTo(7));
            Assert.That(second.Nodes.Count, Is.EqualTo(6));
            Assert.That(third.Nodes.Count, Is.EqualTo(6));
            Assert.That(first.StartNodeIds, Is.EqualTo(new[] { "f1_opening_normal" }));
            Assert.That(second.StartNodeIds, Is.EquivalentTo(new[] { "f2_elite", "f2_normal" }));
            Assert.That(third.StartNodeIds, Is.EquivalentTo(new[] { "f3_elite", "f3_normal" }));
        }

        [Test]
        public void FirstBossReward_BlocksThenAdvancesFloorWithoutResettingRunState()
        {
            var run = ReachFirstBossVictory(801);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));
            Assert.That(run.State.PendingRewardChoice.CompletionMode,
                Is.EqualTo(RewardCompletionMode.FloorComplete));
            Assert.That(run.State.PendingRewardChoice.Candidates.Count, Is.EqualTo(3));
            var health = run.State.Health;
            var turn = run.State.RunTurn;
            var shop = run.Shop;

            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            Assert.That(run.State.Floor, Is.EqualTo(2));
            Assert.That(run.State.CurrentMap.Id, Is.EqualTo("phase4c_floor2"));
            Assert.That(run.State.Health, Is.EqualTo(health));
            Assert.That(run.State.RunTurn, Is.EqualTo(turn));
            Assert.That(run.Shop, Is.SameAs(shop));
            Assert.That(run.State.MapProgress.GetStatus("f2_normal"), Is.EqualTo(RunNodeStatus.Reachable));
            Assert.That(run.State.MapProgress.GetStatus("f2_elite"), Is.EqualTo(RunNodeStatus.Reachable));
        }

        [Test]
        public void BossMinionCandidate_DoesNotExceedCurrentTavernTierPlusOne()
        {
            RunSession selected = null;
            RewardCandidate candidate = null;
            for (var seed = 1; seed <= 300 && selected == null; seed++)
            {
                var run = ReachFirstBossVictory(seed);
                candidate = run.State.PendingRewardChoice.Candidates
                    .FirstOrDefault(value => value.Type == "Minion");
                if (candidate != null) selected = run;
                else Assert.That(run.SkipRewardChoice().Success, Is.True);
            }

            Assert.That(selected, Is.Not.Null);
            var config = CreateConfigs().MinionsById[candidate.CardId];
            Assert.That(config.Tier, Is.LessThanOrEqualTo(selected.Shop.TavernTier + 1));
            var remaining = selected.Shop.MinionPool.GetRemainingCopies(candidate.CardId);
            Assert.That(selected.SkipRewardChoice().Success, Is.True);
            Assert.That(selected.Shop.MinionPool.GetRemainingCopies(candidate.CardId), Is.EqualTo(remaining + 1));
        }

        [Test]
        public void ThreeFloorSafeRoute_ReachesFinalVictoryAndRecordsStatistics()
        {
            var run = new RunSession(CreateConfigs(), 901);
            CompleteCombat(run, "f1_opening_normal");
            CompleteCombat(run, "f1_safe_normal");
            CompleteRest(run, "f1_rest");
            CompleteBossAndAdvance(run, "f1_boss");

            CompleteCombat(run, "f2_normal");
            CompleteRest(run, "f2_rest");
            CompleteBossAndAdvance(run, "f2_boss");

            CompleteCombat(run, "f3_normal");
            CompleteRest(run, "f3_rest");
            EnterCombatAndClaimRewards(run, "f3_boss");
            ResolveWin(run);

            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RunWon));
            Assert.That(run.State.Floor, Is.EqualTo(3));
            Assert.That(run.State.RunTurn, Is.EqualTo(10));
            Assert.That(run.State.Statistics.BattlesWon, Is.EqualTo(7));
            Assert.That(run.State.Statistics.BattlesNotWon, Is.Zero);
            Assert.That(run.State.Statistics.BossesDefeated, Is.EqualTo(3));
            Assert.That(run.State.Statistics.CompletedAtUtc, Is.Not.Null);
        }

        private static RunSession ReachFirstBossVictory(int seed)
        {
            var run = new RunSession(CreateConfigs(), seed);
            CompleteCombat(run, "f1_opening_normal");
            CompleteCombat(run, "f1_safe_normal");
            CompleteRest(run, "f1_rest");
            EnterCombatAndClaimRewards(run, "f1_boss");
            ResolveWin(run);
            return run;
        }

        private static void CompleteBossAndAdvance(RunSession run, string nodeId)
        {
            EnterCombatAndClaimRewards(run, nodeId);
            ResolveWin(run);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));
            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
        }

        private static void CompleteCombat(RunSession run, string nodeId)
        {
            EnterCombatAndClaimRewards(run, nodeId);
            ResolveWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
        }

        private static void EnterCombatAndClaimRewards(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True);
            while (run.State.PendingCardRewards.Count > 0)
            {
                Assert.That(run.ClaimNextCardReward().Success, Is.True);
            }
        }

        private static void CompleteRest(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
        }

        private static void ResolveWin(RunSession run)
        {
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            var result = new BattleSimulationResult(
                new BattleBoardState(), BattleSide.Player, BattleOutcomeReason.Victory,
                new List<string>(), new List<BattleStep>());
            Assert.That(run.TryCompleteBattle(result, out var scene), Is.True);
            Assert.That(scene, Is.EqualTo("RunTest"));
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
