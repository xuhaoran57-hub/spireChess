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
        public void FixedProvider_LoadsThreeShopFirstMapsWithFiveShopsPerPath()
        {
            var configs = CreateConfigs();
            var provider = new FixedMapProvider(configs.RunMaps);

            for (var floor = 1; floor <= 3; floor++)
            {
                var map = provider.CreateMap(new MapRequest(1, floor));
                Assert.That(map.Id, Is.EqualTo($"phase4d_floor{floor}"));
                Assert.That(map.Nodes.Count, Is.EqualTo(15));
                Assert.That(map.StartNodeIds, Is.EqualTo(new[] { $"f{floor}_shop_start" }));
                Assert.That(map.Nodes.Count(node => node.Type == RunNodeType.Shop), Is.EqualTo(5));
                Assert.That(map.Nodes.Count(node => node.Type == RunNodeType.Boss), Is.EqualTo(1));
                Assert.That(map.Nodes.Where(IsCombat),
                    Has.All.Matches<MapNodeDefinition>(node => node.CombatIndex >= 1 && node.CombatIndex <= 5));
            }
        }

        [Test]
        public void ReleasedEncounterCurve_MatchesProvisionalRawStatTargets()
        {
            var configs = CreateConfigs();
            var expected = new Dictionary<string, int[]>
            {
                ["f1_opening_encounter"] = new[] { 1, 2 },
                ["f1_safe_normal_encounter"] = new[] { 4, 5 },
                ["f1_elite_wall_encounter"] = new[] { 4, 7 },
                ["f1_mid_mechanic_encounter"] = new[] { 5, 9 },
                ["f1_late_shield_encounter"] = new[] { 8, 12 },
                ["f1_late_summon_encounter"] = new[] { 7, 11 },
                ["f1_boss_encounter"] = new[] { 11, 23 },
                ["f2_opening_encounter"] = new[] { 14, 18 },
                ["f2_normal_encounter"] = new[] { 18, 22 },
                ["f2_elite_encounter"] = new[] { 20, 26 },
                ["f2_mid_mechanic_encounter"] = new[] { 23, 27 },
                ["f2_late_break_encounter"] = new[] { 27, 29 },
                ["f2_late_spell_encounter"] = new[] { 25, 30 },
                ["f2_boss_encounter"] = new[] { 36, 40 },
                ["f3_opening_encounter"] = new[] { 62, 68 },
                ["f3_normal_encounter"] = new[] { 70, 76 },
                ["f3_elite_encounter"] = new[] { 76, 82 },
                ["f3_mid_mechanic_encounter"] = new[] { 80, 86 },
                ["f3_late_forge_encounter"] = new[] { 90, 94 },
                ["f3_late_wild_encounter"] = new[] { 90, 94 },
                ["f3_boss_encounter"] = new[] { 100, 100 }
            };

            foreach (var pair in expected)
            {
                var encounter = configs.EncountersById[pair.Key];
                var attack = 0;
                var health = 0;
                foreach (var slot in encounter.EnemySlots)
                {
                    var minion = configs.MinionsById[slot.MinionId];
                    attack += (slot.Golden ? minion.GoldenAttack : minion.Attack) + slot.AttackBonus;
                    health += (slot.Golden ? minion.GoldenHealth : minion.Health) + slot.HealthBonus;
                }

                Assert.That(attack, Is.EqualTo(pair.Value[0]), pair.Key);
                Assert.That(health, Is.EqualTo(pair.Value[1]), pair.Key);
            }
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
            var shopTurn = run.State.ShopTurn;
            var mapStep = run.State.MapStep;
            var shop = run.Shop;

            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            Assert.That(run.State.Floor, Is.EqualTo(2));
            Assert.That(run.State.CurrentMap.Id, Is.EqualTo("phase4d_floor2"));
            Assert.That(run.State.Health, Is.EqualTo(health));
            Assert.That(run.State.ShopTurn, Is.EqualTo(shopTurn));
            Assert.That(run.State.MapStep, Is.EqualTo(mapStep));
            Assert.That(run.Shop, Is.SameAs(shop));
            Assert.That(run.State.MapProgress.GetStatus("f2_shop_start"),
                Is.EqualTo(RunNodeStatus.Reachable));
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
            Assert.That(selected.Shop.MinionPool.GetRemainingCopies(candidate.CardId),
                Is.EqualTo(remaining + 1));
        }

        [Test]
        public void ThreeFloorSafeRoute_ReachesFinalVictoryWithFifteenBattlesAndShopTurns()
        {
            var run = new RunSession(CreateConfigs(), 901);
            CompleteFloor(run, 1, "f1_safe_normal", "f1_rest", "f1_late_shield");
            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            CompleteFloor(run, 2, "f2_normal", "f2_rest", "f2_late_break");
            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            CompleteFloor(run, 3, "f3_normal", "f3_rest", "f3_late_wild");

            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RunWon));
            Assert.That(run.State.Floor, Is.EqualTo(3));
            Assert.That(run.State.ShopTurn, Is.EqualTo(15));
            Assert.That(run.State.MapStep, Is.EqualTo(33));
            Assert.That(run.State.Statistics.BattlesWon, Is.EqualTo(15));
            Assert.That(run.State.Statistics.BattlesNotWon, Is.Zero);
            Assert.That(run.State.Statistics.BossesDefeated, Is.EqualTo(3));
            Assert.That(run.State.Statistics.CompletedAtUtc, Is.Not.Null);
        }

        private static RunSession ReachFirstBossVictory(int seed)
        {
            var run = new RunSession(CreateConfigs(), seed);
            CompleteFloor(run, 1, "f1_safe_normal", "f1_rest", "f1_late_shield");
            return run;
        }

        private static void CompleteFloor(
            RunSession run,
            int floor,
            string branchCombat,
            string utility,
            string lateCombat)
        {
            CompleteShop(run, $"f{floor}_shop_start");
            CompleteCombat(run, $"f{floor}_opening_normal");
            CompleteShop(run, $"f{floor}_shop_2");
            CompleteCombat(run, branchCombat);
            Assert.That(run.EnterNode(utility).Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
            CompleteShop(run, $"f{floor}_shop_3");
            CompleteCombat(run, $"f{floor}_mid_mechanic");
            CompleteShop(run, $"f{floor}_shop_4");
            CompleteCombat(run, lateCombat);
            CompleteShop(run, $"f{floor}_shop_boss");
            Assert.That(run.EnterNode($"f{floor}_boss").Success, Is.True);
            ResolveWin(run);
        }

        private static void CompleteShop(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            ClaimAllRewards(run);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
        }

        private static void CompleteCombat(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            ResolveWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
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

        private static void ResolveWin(RunSession run)
        {
            var result = new BattleSimulationResult(
                new BattleBoardState(), BattleSide.Player, BattleOutcomeReason.Victory,
                new List<string>(), new List<BattleStep>());
            Assert.That(run.TryCompleteBattle(result, out var scene), Is.True);
            Assert.That(scene, Is.EqualTo("RunTest"));
        }

        private static bool IsCombat(MapNodeDefinition node)
        {
            return node.Type == RunNodeType.Normal ||
                   node.Type == RunNodeType.Elite ||
                   node.Type == RunNodeType.Boss;
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
