using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;
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
            Assert.That(fixtures.FixtureVersion, Is.EqualTo("0.3.0"));
            Assert.That(fixtures.CoreClassifierVersion, Is.EqualTo("0.2.2"));
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
            Assert.That(summonNormal.PlayerFlourishStacks, Is.EqualTo(7));
            Assert.That(summonHigh.PlayerFlourishStacks, Is.EqualTo(10));
            Assert.That(summonNormal.Player[0].CurrentAttack, Is.EqualTo(12));
            Assert.That(summonHigh.Player[0].CurrentAttack, Is.EqualTo(24));
            Assert.That(summonNormal.Player[0].CurrentHealth, Is.EqualTo(5));
            Assert.That(summonHigh.Player[0].CurrentHealth, Is.EqualTo(13));
            Assert.That(summonNormal.Player[0].Id, Is.EqualTo("hundred_song_herd"));
            Assert.That(summonHigh.Player[0].Id, Is.EqualTo("hundred_song_herd"));
        }

        [Test]
        public void CurrentContentConfigAndR16SourceFixtureIdentityAreVersioned()
        {
            var configRoot = Path.Combine(
                Application.dataPath,
                "Resources",
                "Configs",
                "Json");
            var configHash = BalanceConfigHasher.Compute(new[]
            {
                "minions.v0.1.json",
                "spells.v0.1.json",
                "encounters.v0.1.json",
                "rewards.v0.1.json",
                "content-release.v0.1.json"
            }.Select(file => File.ReadAllText(Path.Combine(configRoot, file))).ToArray());
            Assert.That(configHash, Is.EqualTo(
                "df8034664c76abe03a9aa9ad024da7ebcb57f9addcf5948e53308e8c50b6622c"));
            Assert.That(configs.ContentRelease.ContentVersion, Is.EqualTo("5.3.1"));

            var serializer = new NewtonsoftJsonSerializer();
            var document = serializer.FromJson<BalanceFixtureFile>(
                File.ReadAllText(FixturePath()));
            Assert.That(document.FixtureVersion, Is.EqualTo("0.3.0"));
            Assert.That(document.CoreClassifierVersion, Is.EqualTo("0.2.2"));
            Assert.That(document.Calibration.CandidateId,
                Is.EqualTo("R16-tomb-astrolabe-tuning-dotnet"));
            Assert.That(document.Calibration.MinimumSamplesPerBuild, Is.EqualTo(1000));
            Assert.That(document.Builds,
                Has.All.Matches<BalanceBuildDefinition>(build =>
                    build.Calibration.ObservedSamples == 1000 &&
                    build.Calibration.Status == "Ready"));
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
        public void CoreClassifier_MoltenBreakLineupIsB02InsteadOfMixed()
        {
            var ids = new[]
            {
                "oathbroken_blade_soul",
                "molten_core_standard",
                "undying_furnace_king",
                "counterflow_smith",
                "cinder_armor_arbiter"
            };
            var cards = ids.Select((id, index) => ShopCardInstance.CreateMinion(
                $"classifier-{index}",
                configs.MinionsById[id])).ToList();
            var classified = CoreBuildClassifier.ClassifyFinalBuild(
                cards,
                new CoreActivationEvidence
                {
                    ShieldEvents = 2,
                    ShieldBenefitEvents = 1
                });

            Assert.That(CoreBuildClassifier.Version, Is.EqualTo("0.2.2"));
            Assert.That(classified, Is.EqualTo("B02_BREAK"));
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

            var result = new BattleSimulator(new System.Random(17), ResolveMinion).Simulate(state);

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
                telemetry.Record("Turn10Snapshot", new
                {
                    buildId = "B03_SUMMON",
                    battle = new[]
                    {
                        new
                        {
                            instanceId = "turn10-core",
                            cardId = "young_deer_spirit",
                            attack = 12,
                            health = 20
                        }
                    },
                    RefreshesPaid = 3,
                    RefreshesFree = 1,
                    MinionsBought = 6,
                    MinionsSold = 1,
                    SpellsUsed = 2,
                    TavernUpgrades = 3,
                    GoldWasted = 1,
                    TriplesFormed = 1
                });
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
                Assert.That(summary.TurnTenBuildId, Is.EqualTo("B03_SUMMON"));
                Assert.That(summary.TurnTenPermanentAttack, Is.EqualTo(12));
                Assert.That(summary.TurnTenPermanentHealth, Is.EqualTo(20));
                Assert.That(summary.TurnTenRefreshesPaid, Is.EqualTo(3));
                Assert.That(summary.TurnTenSpellsUsed, Is.EqualTo(2));
                Assert.That(summary.TurnTenTriplesFormed, Is.EqualTo(1));
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
        public void GrowthCalibration_UsesTurnTenFormationAndP50P90Panels()
        {
            var summaries = new[]
            {
                CalibrationSummary(2003, "B02_BREAK", "B02_BREAK", 40, 30),
                CalibrationSummary(2004, "B02_BREAK", "B02_BREAK", 50, 40),
                CalibrationSummary(2005, "B02_BREAK", "B02_BREAK", 60, 50),
                CalibrationSummary(2020, "B02_BREAK", "Unclassified", 0, 0)
            };

            var row = new RunGrowthCalibrationAggregator()
                .Aggregate(summaries, "R3", "R3")
                .Single(value => value.BuildId == "B02_BREAK");

            Assert.That(row.IntendedRuns, Is.EqualTo(4));
            Assert.That(row.IntendedRunsFormedByTurnTen, Is.EqualTo(3));
            Assert.That(row.FormationRate, Is.EqualTo(0.75d));
            Assert.That(row.NormalAttackP50, Is.EqualTo(50));
            Assert.That(row.NormalHealthP50, Is.EqualTo(40));
            Assert.That(row.HighAttackP90, Is.EqualTo(60));
            Assert.That(row.HighHealthP90, Is.EqualTo(50));
            Assert.That(row.NormalRepresentativeSeed, Is.EqualTo(2004));
            Assert.That(row.HighRepresentativeSeed, Is.EqualTo(2005));
            Assert.That(row.CalibrationStatus, Is.EqualTo("Provisional"));
            Assert.That(BalanceGrowthCalibrationCsv.Serialize(new[] { row }),
                Does.Contain("normalAttackP50"));
        }

        [Test]
        public void FixtureV03_AllowsIndependentNormalAndHighDevelopmentStats()
        {
            var serializer = new NewtonsoftJsonSerializer();
            var document = serializer.FromJson<BalanceFixtureFile>(
                File.ReadAllText(FixturePath()));
            document.FixtureVersion = "0.3.0";
            document.Calibration = new BalanceFixtureCalibrationDefinition
            {
                CandidateId = "test-calibration",
                SourceCsv = "balance_fixture_calibration.csv",
                NormalPercentile = "P50",
                HighPercentile = "P90",
                MinimumSamplesPerBuild = 1
            };
            foreach (var build in document.Builds)
            {
                build.Calibration = new BalanceBuildCalibrationDefinition
                {
                    ObservedSamples = 1,
                    FormationRate = 1d,
                    NormalRepresentativeSeed = 2000,
                    HighRepresentativeSeed = 2001,
                    Status = "Ready"
                };
                foreach (var slot in build.Slots)
                {
                    var config = ResolveMinion(slot.MinionId);
                    var highAttackBonus = slot.PermanentAttackBonus + 1;
                    var highHealthBonus = slot.PermanentHealthBonus + 1;
                    slot.NormalIsGolden = false;
                    slot.HighIsGolden = false;
                    slot.HighPermanentAttackBonus = highAttackBonus;
                    slot.HighPermanentHealthBonus = highHealthBonus;
                    slot.ExpectedHighAttack = config.Attack + highAttackBonus;
                    slot.ExpectedHighHealth = config.Health + highHealthBonus;
                }
            }

            var expectedHighAttack = document.Builds
                .Single(build => build.BuildId == "B02_BREAK")
                .Slots.Single(slot => slot.Slot == 0)
                .ExpectedHighAttack;
            var calibrated = BalanceFixtureCatalog.Load(
                serializer.ToJson(document),
                ResolveMinion);
            var high = calibrated.CreateFixture("B02_BREAK", "H");

            Assert.That(high.Player[0].IsGolden, Is.False);
            Assert.That(high.Player[0].CurrentAttack,
                Is.EqualTo(expectedHighAttack));
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
                "balance-fixtures.v0.3.json");
        }

        private static BalanceRunSummary CalibrationSummary(
            int seed,
            string intendedBuildId,
            string turnTenBuildId,
            int attack,
            int health)
        {
            return new BalanceRunSummary
            {
                Seed = seed,
                IntendedBuildId = intendedBuildId,
                TurnTenReached = true,
                TurnTenBuildId = turnTenBuildId,
                TurnTenPermanentAttack = attack,
                TurnTenPermanentHealth = health,
                TurnTenBoardJson = $"[{{\"seed\":{seed}}}]",
                FirstCoreTurn = 4,
                SecondCoreTurn = 8,
                TurnTenRefreshesPaid = 3,
                TurnTenRefreshesFree = 1,
                TurnTenMinionsBought = 6,
                TurnTenSpellsUsed = 2,
                TurnTenTavernUpgrades = 3,
                TurnTenTriplesFormed = 1
            };
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
