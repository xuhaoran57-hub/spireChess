using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Simulation;
using SpireChess.Utils;
using UnityEngine;

namespace SpireChess.Tests.EditMode
{
    public sealed class StageSixInfrastructureTests
    {
        private ConfigService configs;
        private BalanceFixtureCatalog fixtures;

        [SetUp]
        public void SetUp()
        {
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            fixtures = BalanceFixtureCatalog.Load(
                File.ReadAllText(FixturePath()),
                ResolveMinion);
        }

        [Test]
        public void BalanceFixtures_DefineAndValidateAllNormalAndHighSnapshots()
        {
            Assert.That(fixtures.FixtureVersion, Is.EqualTo("0.2.0"));
            Assert.That(fixtures.CoreClassifierVersion, Is.EqualTo("0.2.1"));
            Assert.That(fixtures.BuildIds, Has.Count.EqualTo(6));
            foreach (var buildId in fixtures.BuildIds)
            {
                foreach (var level in new[] { "N", "H" })
                {
                    var state = fixtures.CreateFixture(buildId, level);
                    Assert.That(state.Player, Has.All.Not.Null);
                    for (var slot = 0; slot < BattleBoardState.SlotCount; slot++)
                    {
                        Assert.That(state.Player[slot].SourceInstanceId,
                            Is.EqualTo($"{buildId}_{level}-S{slot}"));
                        Assert.That(state.Player[slot].Config.IsToken, Is.False);
                        Assert.That(state.Player[slot].IsGolden, Is.EqualTo(level == "H"));
                    }
                }
            }

            Assert.That(fixtures.CreateDummy("D00_OUTPUT_DUMMY").Player,
                Has.All.Matches<BattleMinionRuntime>(value =>
                    value.CurrentAttack == 0 && value.CurrentHealth == 500));
            Assert.That(fixtures.CreateDummy("D01_MECHANIC_PRESSURE_N").Player,
                Has.All.Matches<BattleMinionRuntime>(value =>
                    value.CurrentAttack == 8 && value.CurrentHealth == 500));
            Assert.That(fixtures.CreateDummy("D01_MECHANIC_PRESSURE_H").Player,
                Has.All.Matches<BattleMinionRuntime>(value =>
                    value.CurrentAttack == 16 && value.CurrentHealth == 1000));
            var matrix = BalanceMatrixBuilder.Build(fixtures);
            Assert.That(matrix, Has.Count.EqualTo(84));
            Assert.That(matrix.Select(value => value.ScenarioId).Distinct().Count(),
                Is.EqualTo(84));
            Assert.That(matrix.Where(value => value.Orientation == "OUTPUT_DUMMY"),
                Has.All.Matches<BalanceBattleScenario>(value =>
                    !value.CountsForCompetitiveSafety));
            Assert.That(matrix.Where(value => value.Orientation != "OUTPUT_DUMMY"),
                Has.All.Matches<BalanceBattleScenario>(value =>
                    value.CountsForCompetitiveSafety));

            var summonNormal = fixtures.CreateFixture("B03_SUMMON", "N");
            var summonHigh = fixtures.CreateFixture("B03_SUMMON", "H");
            Assert.That(summonNormal.Player[0].CurrentHealth, Is.EqualTo(10));
            Assert.That(summonHigh.Player[0].CurrentHealth, Is.EqualTo(20));
            Assert.That(summonNormal.Player[4].Id, Is.EqualTo("vinecrown_priest"));
            Assert.That(summonNormal.Player[4].FlourishStacks, Is.EqualTo(2));
            Assert.That(summonHigh.Player[4].Id, Is.EqualTo("vinecrown_priest"));
            Assert.That(summonHigh.Player[4].FlourishStacks, Is.EqualTo(4));
        }

        [Test]
        public void S0Smoke_RepeatedRunsHaveIdenticalPerBattleHashes()
        {
            var fixture = CreateMatch(
                fixtures.CreateFixture("B03_SUMMON", "N"),
                fixtures.CreateDummy("D01_MECHANIC_PRESSURE_N"));
            var runner = new BattleBatchRunner(ResolveMinion);

            var first = runner.Run(fixture, BalanceSeedSets.S0Smoke);
            var second = runner.Run(fixture, BalanceSeedSets.S0Smoke);

            Assert.That(first.Exceptions, Is.Zero);
            Assert.That(second.Exceptions, Is.Zero);
            Assert.That(first.Samples, Has.All.Matches<BattleSample>(value =>
                !string.IsNullOrWhiteSpace(value.DeterminismHash)));
            Assert.That(BattleBatchComparer.CountDeterminismFailures(first, second), Is.Zero);
        }

        [Test]
        public void BalanceMatrix_AllScenariosRunWithoutSafetyFailures()
        {
            var results = new BalanceMatrixRunner(ResolveMinion)
                .Run(fixtures, new[] { 1000 });

            Assert.That(results, Has.Count.EqualTo(84));
            Assert.That(results.Sum(value => value.Batch.Exceptions), Is.Zero);
            Assert.That(results.SelectMany(value => value.Batch.Samples),
                Has.All.Matches<BattleSample>(value =>
                    value.Succeeded && !value.Diagnostics.HitEffectLimit));
        }

        [Test]
        public void BattleDiagnostics_RecordDamageShieldAttackAndSafetyCounters()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["forge_soul_shield_squire"],
                initialAttack: 1,
                initialHealth: 100,
                sourceInstanceId: "player");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["wandering_swordsman"],
                initialAttack: 5,
                initialHealth: 100,
                sourceInstanceId: "enemy");

            var result = new BattleSimulator(new Random(17), ResolveMinion).Simulate(state);

            Assert.That(result.Diagnostics.RoundCount, Is.GreaterThan(0));
            Assert.That(result.Diagnostics.Player.NormalAttacks, Is.GreaterThan(0));
            Assert.That(result.Diagnostics.Enemy.NormalAttacks, Is.GreaterThan(0));
            Assert.That(result.Diagnostics.Player.ShieldDamageBlocks, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Player.ShieldsLost, Is.EqualTo(1));
            Assert.That(result.Diagnostics.ProcessedEffectCount, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Diagnostics.HitEffectLimit, Is.False);
        }

        [Test]
        public void BattleReporting_ExportsPerSeedHashesAndCanonicalConfigHash()
        {
            var fixture = CreateMatch(
                fixtures.CreateFixture("B01_SHIELD", "N"),
                fixtures.CreateDummy("D00_OUTPUT_DUMMY"));
            var batch = new BattleBatchRunner(ResolveMinion).Run(fixture, 1000, 2);
            var csv = BalanceBattleCsv.SerializeSamples(
                new BalanceBatchMetadata
                {
                    CandidateId = "R0",
                    SeedSet = "S0_SMOKE",
                    PlayerFixtureId = "B01_SHIELD_N",
                    EnemyFixtureId = "D00_OUTPUT_DUMMY"
                },
                batch);

            Assert.That(csv, Does.Contain("determinismHash"));
            Assert.That(csv, Does.Contain(batch.Samples[0].DeterminismHash));
            Assert.That(
                BalanceConfigHasher.Compute("{\"b\":2,\"a\":1}"),
                Is.EqualTo(BalanceConfigHasher.Compute("{\"a\":1,\"b\":2}")));
        }

        [Test]
        public void RunTelemetryAggregator_BuildsRunSummaryAndCardFunnel()
        {
            var path = Path.Combine(Path.GetTempPath(), $"spire-chess-{Guid.NewGuid():N}.ndjson");
            try
            {
                var telemetry = new RunTelemetry(path, "5.1.0", 2000);
                telemetry.Record("RunStarted", new { coreClassifierVersion = "0.2.1" });
                telemetry.Record("NodeEntered", new { nodeId = "f1_start" });
                telemetry.Record("ShopSnapshot", new
                {
                    trigger = "OnShopPhaseStart",
                    minionOfferIds = new[] { "young_deer_spirit" },
                    spellOfferId = "minor_tempering"
                });
                telemetry.Record("ShopEvent", new
                {
                    type = "OnBuy",
                    cardId = "young_deer_spirit"
                });
                telemetry.Record("Turn10Snapshot", new { battle = new object[0] });
                telemetry.Record("RunEnded", new
                {
                    result = "Won",
                    floorReached = 3,
                    runTurn = 20,
                    elapsedMinutes = 12.5,
                    healthRemaining = 5,
                    BattlesWon = 10,
                    BattlesNotWon = 1,
                    ElitesAttempted = 2,
                    ElitesDefeated = 2,
                    BossAttempts = 3,
                    BossesDefeated = 3,
                    RefreshesPaid = 4,
                    RefreshesFree = 2,
                    MinionsBought = 8,
                    MinionsSold = 2,
                    SpellsUsed = 3,
                    TavernUpgrades = 4,
                    GoldWasted = 1,
                    FirstCoreTurn = 3,
                    SecondCoreTurn = 8,
                    TriplesFormed = 1,
                    TargetedDiscoversUsed = 1,
                    finalBuildId = "B03_SUMMON",
                    finalBoard = new[] { new { cardId = "young_deer_spirit" } }
                });

                var ndjson = File.ReadAllText(path);
                var aggregator = new RunTelemetryAggregator(id =>
                    configs.MinionsById.TryGetValue(id, out var minion)
                        ? minion.Tier
                        : configs.SpellsById.TryGetValue(id, out var spell) ? spell.Tier : 0);
                var summary = aggregator.AggregateRun(ndjson, "R0", "R0", path);
                var funnel = aggregator.AggregateCardFunnel(
                    new[] { ndjson }, "R0", "R0");

                Assert.That(summary.Result, Is.EqualTo("Won"));
                Assert.That(summary.FinalBuildId, Is.EqualTo("B03_SUMMON"));
                Assert.That(summary.RouteNodeIds, Is.EqualTo("f1_start"));
                var minionRow = funnel.Single(value => value.CardId == "young_deer_spirit");
                Assert.That(minionRow.Offered, Is.EqualTo(1));
                Assert.That(minionRow.BoughtOrPicked, Is.EqualTo(1));
                Assert.That(minionRow.SurvivedToRunEnd, Is.EqualTo(1));
                Assert.That(BalanceTelemetryCsv.SerializeRunSummaries(new[] { summary }),
                    Does.Contain("balanceSchemaVersion"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void RunSession_RecordsShopSnapshotsAndPaidRefreshes()
        {
            var path = Path.Combine(Path.GetTempPath(), $"spire-chess-{Guid.NewGuid():N}.ndjson");
            try
            {
                var session = new RunSession(configs, 1234);
                session.EnableTelemetry(new RunTelemetry(path, configs.ContentRelease.ContentVersion, 1234));
                Assert.That(session.Shop.StartRound(1).Success, Is.True);
                Assert.That(session.Shop.Refresh().Success, Is.True);
                Assert.That(session.Shop.EndRound().Success, Is.True);

                var events = File.ReadAllLines(path);
                Assert.That(events.Count(value =>
                    value.Contains("\"eventType\":\"ShopSnapshot\"")), Is.EqualTo(3));
                Assert.That(session.State.Statistics.RefreshesPaid, Is.EqualTo(1));
                Assert.That(session.State.Statistics.RefreshesFree, Is.Zero);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static string FixturePath()
        {
            return Path.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "Balance",
                "balance-fixtures.v0.2.json");
        }

        private MinionConfig ResolveMinion(string id)
        {
            return configs.MinionsById.TryGetValue(id, out var value) ? value : null;
        }

        private static BattleBoardState CreateMatch(
            BattleBoardState playerFixture,
            BattleBoardState enemyFixture)
        {
            var state = new BattleBoardState();
            for (var slot = 0; slot < BattleBoardState.SlotCount; slot++)
            {
                state.Player[slot] = playerFixture.Player[slot]?.Clone();
                state.Enemy[slot] = enemyFixture.Player[slot]?.Clone();
            }
            return state;
        }
    }
}
