using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Simulation;
using SpireChess.Utils;
using UnityEngine;

namespace SpireChess.Tests.EditMode
{
    public sealed class ChapterEncounterSamplingTests
    {
        private ConfigService configs;
        private BalanceFixtureCatalog fixtures;
        private ChapterProgressFixtureCatalog progressFixtures;

        [SetUp]
        public void SetUp()
        {
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(
                validation.IsValid,
                Is.True,
                string.Join("\n", validation.Errors));
            fixtures = BalanceFixtureCatalog.Load(
                File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Tests",
                    "Fixtures",
                    "Balance",
                    "balance-fixtures.v0.3.json")),
                ResolveMinion);
            progressFixtures = ChapterProgressFixtureCatalog.Load(
                File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Tests",
                    "Fixtures",
                    "Balance",
                    "chapter-progress-fixtures.v0.4.json")),
                fixtures,
                ResolveMinion);
        }

        [Test]
        public void Catalog_IncludesAllReachableFormalMapAndEventEncounters()
        {
            var definitions = ChapterEncounterCatalog.Build(configs);

            Assert.That(definitions, Has.Count.EqualTo(33));
            Assert.That(
                definitions.Count(value =>
                    value.Source == ChapterEncounterSource.Map),
                Is.EqualTo(30));
            Assert.That(
                definitions.Count(value =>
                    value.Source == ChapterEncounterSource.Event),
                Is.EqualTo(3));
            Assert.That(
                definitions.GroupBy(value => value.DefinitionId),
                Has.All.Matches<IGrouping<string, ChapterEncounterDefinition>>(
                    value => value.Count() == 1));
            Assert.That(
                definitions.Where(value =>
                    value.Source == ChapterEncounterSource.Map)
                    .GroupBy(value => value.Floor)
                    .Select(value => value.Count()),
                Has.All.EqualTo(10));
            Assert.That(
                definitions.Select(value => value.EncounterId),
                Has.All.Matches<string>(value =>
                    configs.ContentRelease.EncounterIds.Contains(value)));
            Assert.That(
                definitions.Select(value => value.EncounterId),
                Has.None.StartsWith("stage5b_"));
            Assert.That(
                definitions.Select(value => value.EncounterId),
                Has.None.EqualTo("f1_event_ambush_encounter"));
            Assert.That(
                definitions.Where(value =>
                    value.Source == ChapterEncounterSource.Event)
                    .Select(value => value.EncounterId),
                Is.EquivalentTo(new[]
                {
                    "f1_c4_event_ambush_encounter",
                    "f2_c4_event_ambush_encounter",
                    "f3_c4_event_ambush_encounter"
                }));
        }

        [Test]
        public void Runner_CreatesTheSameEnemyRuntimeShapeAsRunSession()
        {
            var definition = ChapterEncounterCatalog.Build(configs)
                .Single(value => value.EncounterId == "f3_c6_boss_encounter");
            var buildId = fixtures.BuildIds.First();
            var board = new ChapterEncounterSamplingRunner(ResolveMinion)
                .CreateBoard(definition, fixtures, buildId, "N");

            Assert.That(board.Player, Has.All.Not.Null);
            Assert.That(
                board.Player.Select(value => value.SourceInstanceId),
                Is.EqualTo(Enumerable.Range(0, BattleBoardState.SlotCount)
                    .Select(slot => $"{buildId}_N-S{slot}")));
            foreach (var slot in definition.Encounter.EnemySlots)
            {
                var runtime = board.Enemy[slot.Slot];
                var config = configs.MinionsById[slot.MinionId];
                Assert.That(runtime, Is.Not.Null);
                Assert.That(runtime.Id, Is.EqualTo(slot.MinionId));
                Assert.That(runtime.IsGolden, Is.EqualTo(slot.Golden));
                Assert.That(
                    runtime.CurrentAttack,
                    Is.EqualTo(
                        (slot.Golden ? config.GoldenAttack : config.Attack) +
                        slot.AttackBonus));
                Assert.That(
                    runtime.CurrentHealth,
                    Is.EqualTo(
                        (slot.Golden ? config.GoldenHealth : config.Health) +
                        slot.HealthBonus));
                Assert.That(
                    runtime.PermanentAttackBonus,
                    Is.EqualTo(slot.AttackBonus));
                Assert.That(
                    runtime.PermanentHealthBonus,
                    Is.EqualTo(slot.HealthBonus));
            }
        }

        [Test]
        public void ProgressFixtures_MapNineActualCadenceCheckpointsAndRespectTierGates()
        {
            Assert.That(progressFixtures.Checkpoints, Has.Count.EqualTo(9));
            Assert.That(
                progressFixtures.Checkpoints
                    .OrderBy(value => value.RunTurn)
                    .Select(value => value.RunTurn),
                Is.EqualTo(new[] { 2, 4, 5, 8, 10, 11, 14, 16, 17 }));
            Assert.That(
                progressFixtures.Checkpoints
                    .Where(value => value.Floor == 3)
                    .OrderBy(value => value.CombatIndex)
                    .Select(value => value.HighGrowthBlend),
                Is.EqualTo(new[] { 0d, 0.25d, 0.5d }));

            foreach (var checkpoint in progressFixtures.Checkpoints)
            {
                foreach (var buildId in progressFixtures.BuildIds)
                {
                    var board = progressFixtures.CreateFixture(
                        checkpoint.Floor,
                        checkpoint.CombatIndex,
                        buildId);
                    var active = board.Player.Where(value => value != null).ToList();
                    Assert.That(
                        active,
                        Has.Count.EqualTo(checkpoint.ActiveSlots),
                        $"{buildId} {checkpoint.Id}");
                    Assert.That(
                        active.Select(value => value.Config.Tier),
                        Has.All.LessThanOrEqualTo(checkpoint.MaxTavernTier),
                        $"{buildId} {checkpoint.Id}");
                    Assert.That(
                        active.Select(value => value.SourceInstanceId),
                        Has.All.StartsWith($"{buildId}_{checkpoint.Id}-S"));
                }
            }
        }

        [Test]
        public void ProgressRunner_UsesC2ForOpeningC4ForRoutesAndC5ForBoss()
        {
            var definitions = ChapterEncounterCatalog.Build(configs);
            var runner = new ChapterEncounterSamplingRunner(ResolveMinion);
            var opening = definitions.Single(value =>
                value.NodeId == "f3_opening_normal");
            var route = definitions.Single(value =>
                value.NodeId == "f3_route_normal");
            var boss = definitions.Single(value =>
                value.NodeId == "f3_boss");

            Assert.That(
                runner.CreateBoard(opening, progressFixtures, "B03_SUMMON")
                    .Player[0].SourceInstanceId,
                Does.Contain("F3_C2"));
            Assert.That(
                runner.CreateBoard(route, progressFixtures, "B03_SUMMON")
                    .Player[0].SourceInstanceId,
                Does.Contain("F3_C4"));
            Assert.That(
                runner.CreateBoard(boss, progressFixtures, "B03_SUMMON")
                    .Player[0].SourceInstanceId,
                Does.Contain("F3_C5"));
        }

        [Test]
        public void ProgressFixtures_ApplyExistingFlourishToFieldedWildSpirits()
        {
            var summon = progressFixtures.CreateFixture(
                3,
                5,
                "B03_SUMMON");

            Assert.That(summon.PlayerFlourishStacks, Is.EqualTo(9));
            foreach (var minion in summon.Player.Where(value => value != null))
            {
                Assert.That(minion.Config.Race, Is.EqualTo("WildSpirit"));
                Assert.That(
                    minion.CurrentAttack,
                    Is.EqualTo(
                        minion.BaseAttack +
                        minion.PermanentAttackBonus +
                        summon.PlayerFlourishStacks));
            }
        }

        [Test]
        public void ProgressFixtures_ExposeF3DeathAndShieldBreakAnchors()
        {
            foreach (var combatIndex in new[] { 2, 4, 5 })
            {
                var death = progressFixtures.CreateFixture(
                    3,
                    combatIndex,
                    "B04_DEATH");
                Assert.That(death.Player[0].HasTaunt, Is.True);
                Assert.That(death.Player[4].HasShield, Is.True);
            }

            var shieldBreak = progressFixtures.CreateFixture(
                3,
                5,
                "B02_BREAK");
            Assert.That(shieldBreak.Player[3].HasShield, Is.True);
        }

        [Test]
        public void Analyzer_FlagsSafetyHardCountersRouteOrderBossAndLevelInversion()
        {
            var rows = new List<ChapterEncounterAggregate>
            {
                Row("route_agg", "Elite", 4, "Aggressive", "N", 80, 0.40d),
                Row("route_adv", "Normal", 4, "Adventure", "N", 60),
                Row("route_safe", "Normal", 4, "Conservative", "N", 40),
                Row("late_a", "Normal", 5, string.Empty, "N", 50),
                Row("late_b", "Normal", 5, string.Empty, "N", 50),
                Row("boss", "Boss", 6, string.Empty, "N", 80),
                Row("boss", "Boss", 6, string.Empty, "H", 50),
                Row(
                    "event_safety",
                    "EventCombat",
                    4,
                    "Event",
                    "H",
                    1,
                    effectLimitHits: 1,
                    source: ChapterEncounterSource.Event)
            };

            var anomalies = ChapterEncounterAnomalyAnalyzer.Analyze(rows);
            var codes = anomalies.Select(value => value.Code).ToArray();

            Assert.That(codes, Does.Contain("EFFECT_LIMIT"));
            Assert.That(codes, Does.Contain("BUILD_HARD_COUNTER"));
            Assert.That(codes, Does.Contain("ROUTE_ORDER_INVERSION"));
            Assert.That(codes, Does.Contain("BOSS_EASIER_THAN_C5"));
            Assert.That(codes, Does.Contain("DEVELOPMENT_INVERSION"));
            Assert.That(codes, Does.Contain("HIGH_BUILD_WALL"));
            Assert.That(
                anomalies.First().Severity,
                Is.EqualTo("P0"),
                "Safety anomalies should be sorted first.");
        }

        [Test]
        public void WilsonInterval_IsBoundedAndContainsObservedRate()
        {
            var interval = ChapterEncounterStatistics.Wilson95(60, 100);

            Assert.That(interval.Low, Is.InRange(0d, 0.60d));
            Assert.That(interval.High, Is.InRange(0.60d, 1d));
            Assert.That(
                ChapterEncounterStatistics.Wilson95(0, 0).Low,
                Is.Zero);
        }

        private ChapterEncounterAggregate Row(
            string encounterId,
            string nodeType,
            int combatIndex,
            string routeTag,
            string level,
            int wins,
            double buildSpread = 0d,
            int effectLimitHits = 0,
            string source = ChapterEncounterSource.Map)
        {
            return new ChapterEncounterAggregate
            {
                Source = source,
                MapId = "map_wilderness",
                MapName = "荒野",
                Floor = 1,
                NodeId = encounterId,
                NodeType = nodeType,
                CombatIndex = combatIndex,
                RouteTag = routeTag,
                EventId = source == ChapterEncounterSource.Event
                    ? "event"
                    : string.Empty,
                EventName = source == ChapterEncounterSource.Event
                    ? "事件"
                    : string.Empty,
                EncounterId = encounterId,
                EncounterName = encounterId,
                ThreatRating = 3,
                DevelopmentLevel = level,
                ScenarioCount = 6,
                Battles = 100,
                PlayerWins = wins,
                EnemyWins = 100 - wins,
                BuildScoreSpread = buildSpread,
                WeakestBuildId = "B01_SHIELD",
                StrongestBuildId = "B06_REFRESH",
                EffectLimitHitCount = effectLimitHits
            };
        }

        private MinionConfig ResolveMinion(string id)
        {
            return configs.MinionsById.TryGetValue(id, out var minion)
                ? minion
                : null;
        }
    }
}
