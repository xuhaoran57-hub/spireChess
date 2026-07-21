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
        public void FixedProvider_LoadsThreeFullMapsWithSixShopsAndTwelvePaths()
        {
            var configs = CreateConfigs();
            var provider = new FixedMapProvider(
                configs.RunMaps,
                configs.MapRuleProfilesById);

            for (var floor = 1; floor <= 3; floor++)
            {
                var map = provider.CreateMap(new MapRequest(1, floor));
                Assert.That(map.Id, Is.EqualTo($"phase8b_floor{floor}"));
                Assert.That(map.Nodes.Count, Is.EqualTo(19));
                Assert.That(map.StartNodeIds, Is.EqualTo(new[] { $"f{floor}_shop_start" }));
                Assert.That(map.Nodes.Count(node => node.Type == RunNodeType.Shop), Is.EqualTo(6));
                Assert.That(map.Nodes.Count(node => node.Type == RunNodeType.Boss), Is.EqualTo(1));
                Assert.That(map.Nodes.Where(IsCombat),
                    Has.All.Matches<MapNodeDefinition>(
                        node => node.CombatIndex >= 1 && node.CombatIndex <= 6));
                var paths = MapValidator.EnumerateBossPaths(map);
                Assert.That(paths.Count, Is.EqualTo(12));
                Assert.That(paths, Has.All.Matches<IReadOnlyList<MapNodeDefinition>>(path =>
                    path.Count == 13 &&
                    path.Count(node => node.Type == RunNodeType.Shop) == 6 &&
                    path.Count(IsCombat) == 6 &&
                    path.Count(IsUtility) == 1 &&
                    path.Count(node => !string.IsNullOrWhiteSpace(node.RouteTag)) == 1));
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
                ["f1_event_ambush_encounter"] = new[] { 5, 7 },
                ["f1_elite_wall_encounter"] = new[] { 4, 7 },
                ["f1_mid_mechanic_encounter"] = new[] { 5, 9 },
                ["f1_late_shield_encounter"] = new[] { 8, 12 },
                ["f1_late_summon_encounter"] = new[] { 7, 11 },
                ["f1_boss_encounter"] = new[] { 11, 23 },
                ["f2_opening_encounter"] = new[] { 14, 18 },
                ["f2_normal_encounter"] = new[] { 18, 22 },
                ["f2_event_ambush_encounter"] = new[] { 20, 24 },
                ["f2_elite_encounter"] = new[] { 20, 26 },
                ["f2_mid_mechanic_encounter"] = new[] { 23, 27 },
                ["f2_late_break_encounter"] = new[] { 27, 29 },
                ["f2_late_spell_encounter"] = new[] { 25, 30 },
                ["f2_boss_encounter"] = new[] { 36, 40 },
                ["f3_opening_encounter"] = new[] { 62, 68 },
                ["f3_normal_encounter"] = new[] { 70, 76 },
                ["f3_event_ambush_encounter"] = new[] { 76, 82 },
                ["f3_elite_encounter"] = new[] { 76, 82 },
                ["f3_mid_mechanic_encounter"] = new[] { 80, 86 },
                ["f3_late_forge_encounter"] = new[] { 90, 94 },
                ["f3_late_wild_encounter"] = new[] { 90, 94 },
                ["f3_boss_encounter"] = new[] { 100, 100 },
                ["f1_early_summon_encounter"] = new[] { 5, 6 },
                ["f1_route_normal_encounter"] = new[] { 9, 12 },
                ["f1_route_safe_encounter"] = new[] { 7, 12 },
                ["f1_c4_elite_encounter"] = new[] { 10, 15 },
                ["f1_c5_shield_encounter"] = new[] { 12, 16 },
                ["f1_c5_summon_encounter"] = new[] { 11, 16 },
                ["f1_c6_boss_encounter"] = new[] { 15, 29 },
                ["f1_c4_event_ambush_encounter"] = new[] { 9, 13 },
                ["f2_early_spell_encounter"] = new[] { 19, 23 },
                ["f2_route_normal_encounter"] = new[] { 28, 32 },
                ["f2_route_safe_encounter"] = new[] { 25, 29 },
                ["f2_c4_elite_encounter"] = new[] { 31, 36 },
                ["f2_c5_break_encounter"] = new[] { 34, 38 },
                ["f2_c5_spell_encounter"] = new[] { 32, 40 },
                ["f2_c6_boss_encounter"] = new[] { 44, 50 },
                ["f2_c4_event_ambush_encounter"] = new[] { 28, 33 },
                ["f3_early_summon_encounter"] = new[] { 72, 80 },
                ["f3_route_normal_encounter"] = new[] { 90, 98 },
                ["f3_route_safe_encounter"] = new[] { 86, 94 },
                ["f3_c4_elite_encounter"] = new[] { 98, 104 },
                ["f3_c5_forge_encounter"] = new[] { 105, 110 },
                ["f3_c5_wild_encounter"] = new[] { 105, 110 },
                ["f3_c6_boss_encounter"] = new[] { 120, 125 },
                ["f3_c4_event_ambush_encounter"] = new[] { 92, 100 }
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
        public void FixedProvider_RejectsEliteBeforeConfiguredMinimum()
        {
            var configs = CreateConfigs();
            var floorOne = configs.RunMaps.Single(map => map.Floor == 1);
            floorOne.Nodes.Single(node => node.Id == "f1_elite_wall").CombatIndex = 2;
            var provider = new FixedMapProvider(
                configs.RunMaps,
                configs.MapRuleProfilesById);

            Assert.Throws<System.InvalidOperationException>(() =>
                provider.CreateMap(new MapRequest(1, 1)));
        }

        [Test]
        public void FirstBossRelic_BlocksThenAdvancesFloorWithoutResettingRunState()
        {
            var run = ReachFirstBossVictory(801);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RelicChoice));
            Assert.That(run.State.PendingRelicChoice.CompletionMode,
                Is.EqualTo(RelicCompletionMode.FloorComplete));
            Assert.That(run.State.PendingRelicChoice.Candidates.Count, Is.EqualTo(3));
            var health = run.State.Health;
            var shopTurn = run.State.ShopTurn;
            var mapStep = run.State.MapStep;
            var shop = run.Shop;

            Assert.That(run.SkipRelicChoice().Error, Is.EqualTo(RunOperationError.InvalidChoice));
            Assert.That(run.SelectRelicCandidate(
                run.State.PendingRelicChoice.Candidates[0].CandidateId).Success, Is.True);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            Assert.That(run.State.Floor, Is.EqualTo(2));
            Assert.That(run.State.CurrentMap.Id, Is.EqualTo("phase8b_floor2"));
            Assert.That(run.State.Health, Is.EqualTo(health));
            Assert.That(run.State.ShopTurn, Is.EqualTo(shopTurn));
            Assert.That(run.State.MapStep, Is.EqualTo(mapStep));
            Assert.That(run.Shop, Is.SameAs(shop));
            Assert.That(run.State.MapProgress.GetStatus("f2_shop_start"),
                Is.EqualTo(RunNodeStatus.Reachable));
        }

        [Test]
        public void BossRelicCandidates_AreDistinctCrownRelics()
        {
            var run = ReachFirstBossVictory(802);
            var candidates = run.State.PendingRelicChoice.Candidates;

            Assert.That(candidates.Select(value => value.RelicId).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(candidates.Select(value => value.Category).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(candidates, Has.All.Matches<RelicCandidate>(
                value => value.Grade == "Crown"));
        }

        [Test]
        public void ThreeFloorSafeRoute_ReachesFinalVictoryWithEighteenBattlesAndShopTurns()
        {
            var run = new RunSession(CreateConfigs(), 901);
            CompleteFloor(
                run, 1, "f1_safe_normal", "f1_route_safe", "f1_rest", "f1_late_shield");
            SelectFirstRelic(run);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            CompleteFloor(
                run, 2, "f2_normal", "f2_route_safe", "f2_rest", "f2_late_break");
            SelectFirstRelic(run);
            Assert.That(run.ContinueToNextFloor().Success, Is.True);
            CompleteFloor(
                run, 3, "f3_normal", "f3_route_safe", "f3_rest", "f3_late_wild");

            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RunWon));
            Assert.That(run.State.Floor, Is.EqualTo(3));
            Assert.That(run.State.ShopTurn, Is.EqualTo(18));
            Assert.That(run.State.MapStep, Is.EqualTo(39));
            Assert.That(run.State.Statistics.BattlesWon, Is.EqualTo(18));
            Assert.That(run.State.Statistics.BattlesNotWon, Is.Zero);
            Assert.That(run.State.Statistics.BossesDefeated, Is.EqualTo(3));
            Assert.That(run.State.Statistics.CompletedAtUtc, Is.Not.Null);
        }

        private static RunSession ReachFirstBossVictory(int seed)
        {
            var run = new RunSession(CreateConfigs(), seed);
            CompleteFloor(
                run, 1, "f1_safe_normal", "f1_route_safe", "f1_rest", "f1_late_shield");
            return run;
        }

        private static void SelectFirstRelic(RunSession run)
        {
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RelicChoice));
            Assert.That(run.SelectRelicCandidate(
                run.State.PendingRelicChoice.Candidates[0].CandidateId).Success, Is.True);
        }

        private static void CompleteFloor(
            RunSession run,
            int floor,
            string earlyCombat,
            string routeCombat,
            string utility,
            string lateCombat)
        {
            CompleteShop(run, $"f{floor}_shop_start");
            CompleteCombat(run, $"f{floor}_opening_normal");
            CompleteShop(run, $"f{floor}_shop_2");
            CompleteCombat(run, earlyCombat);
            CompleteShop(run, $"f{floor}_shop_3");
            CompleteCombat(run, $"f{floor}_mid_mechanic");
            CompleteShop(run, $"f{floor}_shop_4");
            CompleteCombat(run, routeCombat);
            Assert.That(run.EnterNode(utility).Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
            CompleteShop(run, $"f{floor}_shop_5");
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

        private static bool IsUtility(MapNodeDefinition node)
        {
            return node.Type == RunNodeType.Enhance ||
                   node.Type == RunNodeType.Event ||
                   node.Type == RunNodeType.Rest;
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
