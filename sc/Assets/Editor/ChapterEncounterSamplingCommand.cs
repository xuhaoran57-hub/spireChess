using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Simulation;
using SpireChess.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SpireChess.Editor
{
    public static class ChapterEncounterSamplingCommand
    {
        private const int DefaultFirstSeed = 1000;
        private const int DefaultSeedCount = 100;

        [MenuItem("Spire Chess/Balance/Run v0.4 Chapter Encounter S0")]
        public static void RunS0FromMenu()
        {
            Run(ChapterEncounterSamplingOptions.CreateDefault());
        }

        [MenuItem("Spire Chess/Balance/Run v0.4 Chapter Progress Encounter S0")]
        public static void RunProgressS0FromMenu()
        {
            var options = ChapterEncounterSamplingOptions.CreateDefault();
            options.UseProgressFixtures();
            Run(options);
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Run(ChapterEncounterSamplingOptions.FromCommandLine(
                    Environment.GetCommandLineArgs()));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                throw;
            }
        }

        private static void Run(ChapterEncounterSamplingOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                              throw new InvalidOperationException(
                                  "Unity project root is unavailable.");
            var repositoryRoot = Directory.GetParent(projectRoot)?.FullName ??
                                 projectRoot;
            var outputDirectory = options.ResolveOutputDirectory(repositoryRoot);
            var gitState = ReadGitState(repositoryRoot);
            if (options.RequireCleanSource &&
                (!gitState.IsAvailable || gitState.SourceTreeDirty))
            {
                throw new InvalidOperationException(
                    "Chapter encounter acceptance requires a clean, " +
                    "identifiable Git source tree. " + gitState.Diagnostic);
            }

            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(string.Join("\n", validation.Errors));
            }

            MinionConfig ResolveMinion(string id)
            {
                return configs.MinionsById.TryGetValue(id, out var minion)
                    ? minion
                    : null;
            }

            var fixturePath = Path.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "Balance",
                "balance-fixtures.v0.3.json");
            var sourceFixtureSha256 =
                ChapterEncounterEvidenceHasher.ComputeFileSha256(fixturePath);
            var sourceFixtures = BalanceFixtureCatalog.Load(
                File.ReadAllText(fixturePath),
                ResolveMinion);
            ChapterProgressFixtureCatalog progressFixtures = null;
            string progressFixturePath = null;
            string progressFixtureSha256 = null;
            if (options.UsesProgressFixtures)
            {
                progressFixturePath = Path.Combine(
                    Application.dataPath,
                    "Tests",
                    "Fixtures",
                    "Balance",
                    "chapter-progress-fixtures.v0.4.json");
                progressFixtureSha256 =
                    ChapterEncounterEvidenceHasher.ComputeFileSha256(
                        progressFixturePath);
                progressFixtures = ChapterProgressFixtureCatalog.Load(
                    File.ReadAllText(progressFixturePath),
                    sourceFixtures,
                    ResolveMinion);
            }
            var definitions = ChapterEncounterCatalog.Build(configs);
            var seeds = Enumerable.Range(options.FirstSeed, options.SeedCount)
                .ToList()
                .AsReadOnly();
            var expectedScenarioCount = definitions.Count *
                                        sourceFixtures.BuildIds.Count *
                                        (options.UsesProgressFixtures ? 1 : 2);
            var expectedBattleCount = expectedScenarioCount * seeds.Count;
            var fixtureLevels = options.UsesProgressFixtures
                ? new[] { "C2", "C4", "C5" }
                : new[] { "N", "H" };

            Debug.Log(
                $"Chapter encounter sampling starting: encounters={definitions.Count}, " +
                $"builds={sourceFixtures.BuildIds.Count}, " +
                $"fixtureMode={options.FixtureMode}, seeds={seeds.Count}, " +
                $"expectedBattles={expectedBattleCount}.");
            var stopwatch = Stopwatch.StartNew();
            var completedEncounterCount = 0;
            Action<ChapterEncounterDefinition> onEncounterStarted = definition =>
            {
                completedEncounterCount++;
                Debug.Log(
                    $"Sampling encounter {completedEncounterCount}/{definitions.Count}: " +
                    $"F{definition.Floor} {definition.EncounterId} " +
                    $"({definition.Source}).");
            };
            var runner = new ChapterEncounterSamplingRunner(ResolveMinion);
            var scenarioResults = options.UsesProgressFixtures
                ? runner.Run(
                    definitions,
                    progressFixtures,
                    seeds,
                    onEncounterStarted)
                : runner.Run(
                    definitions,
                    sourceFixtures,
                    seeds,
                    onEncounterStarted);
            var aggregates = ChapterEncounterAggregate.Build(scenarioResults);
            var anomalies = ChapterEncounterAnomalyAnalyzer.Analyze(aggregates);
            var scopes = BuildScopeSummaries(aggregates);
            stopwatch.Stop();

            Directory.CreateDirectory(outputDirectory);
            var scenarioFile = Path.Combine(
                outputDirectory,
                "chapter_encounter_scenarios.csv");
            var aggregateFile = Path.Combine(
                outputDirectory,
                "chapter_encounter_aggregates.csv");
            var scopeFile = Path.Combine(
                outputDirectory,
                "chapter_encounter_scopes.csv");
            var anomalyFile = Path.Combine(
                outputDirectory,
                "chapter_encounter_anomalies.csv");
            var reportFile = Path.Combine(
                outputDirectory,
                "chapter_encounter_report.md");
            var progressFixtureFile = options.UsesProgressFixtures
                ? Path.Combine(outputDirectory, "chapter_progress_fixtures.csv")
                : null;
            var metadataFile = Path.Combine(outputDirectory, "metadata.json");
            var utf8 = new UTF8Encoding(false);

            File.WriteAllText(
                scenarioFile,
                SerializeScenarioCsv(scenarioResults),
                utf8);
            File.WriteAllText(
                aggregateFile,
                SerializeAggregateCsv(aggregates),
                utf8);
            File.WriteAllText(
                scopeFile,
                SerializeScopeCsv(scopes),
                utf8);
            File.WriteAllText(
                anomalyFile,
                SerializeAnomalyCsv(anomalies),
                utf8);
            if (options.UsesProgressFixtures)
            {
                File.WriteAllText(
                    progressFixtureFile,
                    SerializeProgressFixtureCsv(progressFixtures),
                    utf8);
            }
            File.WriteAllText(
                reportFile,
                BuildMarkdownReport(
                    configs,
                    sourceFixtures.BuildIds.Count,
                    options.FixtureMode,
                    definitions,
                    seeds,
                    scenarioResults,
                    aggregates,
                    scopes,
                    anomalies,
                    stopwatch.Elapsed),
                utf8);

            var safetyExceptions = scenarioResults.Sum(value =>
                value.Batch.Exceptions);
            var effectLimitHits = scenarioResults.Sum(value =>
                value.EffectLimitHitCount);
            var roundLimitCount = scenarioResults.Sum(value =>
                value.RoundLimitCount);
            var p0AnomalyCount = anomalies.Count(value =>
                value.Severity == "P0");
            var p1AnomalyCount = anomalies.Count(value =>
                value.Severity == "P1");
            var p2AnomalyCount = anomalies.Count(value =>
                value.Severity == "P2");
            var gateFailures = BuildAcceptanceGateFailures(
                expectedScenarioCount,
                expectedBattleCount,
                scenarioResults.Count,
                scenarioResults.Sum(value => value.Batch.Battles),
                safetyExceptions,
                effectLimitHits,
                roundLimitCount,
                p0AnomalyCount,
                p1AnomalyCount);
            var outputFiles = new List<string>
            {
                scenarioFile,
                aggregateFile,
                scopeFile,
                anomalyFile,
                reportFile
            };
            if (progressFixtureFile != null)
            {
                outputFiles.Add(progressFixtureFile);
            }
            var outputEvidence =
                ChapterEncounterEvidenceHasher.HashFiles(outputFiles);
            var metadata = new ChapterEncounterSamplingMetadata
            {
                GeneratedAtUtc = DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                Runtime = "Unity Editor",
                UnityVersion = Application.unityVersion,
                ContentVersion = configs.ContentRelease.ContentVersion,
                ConfigHash = configs.Identity.ConfigHash,
                GitCommit = gitState.Commit,
                SourceTreeDirty = gitState.SourceTreeDirty,
                RequireCleanSource = options.RequireCleanSource,
                StrictAcceptance = options.StrictAcceptance,
                FixtureMode = options.FixtureMode,
                FixtureVersion = options.UsesProgressFixtures
                    ? progressFixtures.FixtureVersion
                    : sourceFixtures.FixtureVersion,
                FixtureFile = options.UsesProgressFixtures
                    ? Path.GetFileName(progressFixturePath)
                    : Path.GetFileName(fixturePath),
                FixtureSha256 = options.UsesProgressFixtures
                    ? progressFixtureSha256
                    : sourceFixtureSha256,
                SourceFixtureVersion = options.UsesProgressFixtures
                    ? progressFixtures.SourceFixtureVersion
                    : sourceFixtures.FixtureVersion,
                SourceFixtureFile = Path.GetFileName(fixturePath),
                SourceFixtureSha256 = sourceFixtureSha256,
                CoreClassifierVersion = sourceFixtures.CoreClassifierVersion,
                SeedSet = options.SeedSetName,
                FirstSeed = options.FirstSeed,
                SeedCount = options.SeedCount,
                FixedBuildCount = sourceFixtures.BuildIds.Count,
                DevelopmentLevels = fixtureLevels,
                FormalEncounterCount = definitions.Count,
                MapEncounterCount = definitions.Count(value =>
                    value.Source == ChapterEncounterSource.Map),
                EventEncounterCount = definitions.Count(value =>
                    value.Source == ChapterEncounterSource.Event),
                ScenarioCount = scenarioResults.Count,
                BattleCount = scenarioResults.Sum(value => value.Batch.Battles),
                Exceptions = safetyExceptions,
                EffectLimitHits = effectLimitHits,
                RoundLimitCount = roundLimitCount,
                AnomalyCount = anomalies.Count,
                P0AnomalyCount = p0AnomalyCount,
                P1AnomalyCount = p1AnomalyCount,
                P2AnomalyCount = p2AnomalyCount,
                AcceptancePassed = gateFailures.Count == 0,
                GateFailures = gateFailures.ToArray(),
                ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                ScenarioFile = Path.GetFileName(scenarioFile),
                AggregateFile = Path.GetFileName(aggregateFile),
                ScopeFile = Path.GetFileName(scopeFile),
                AnomalyFile = Path.GetFileName(anomalyFile),
                ReportFile = Path.GetFileName(reportFile),
                ProgressFixtureFile = progressFixtureFile == null
                    ? null
                    : Path.GetFileName(progressFixtureFile),
                HashAlgorithm = "SHA-256",
                OutputFiles = outputEvidence.ToArray(),
                OutputSetSha256 =
                    ChapterEncounterEvidenceHasher.ComputeOutputSetSha256(
                        outputEvidence)
            };
            File.WriteAllText(
                metadataFile,
                JsonConvert.SerializeObject(metadata, Formatting.Indented),
                utf8);

            Debug.Log(
                $"Chapter encounter sampling complete: battles={metadata.BattleCount}, " +
                $"exceptions={metadata.Exceptions}, effectLimits={metadata.EffectLimitHits}, " +
                $"roundLimits={metadata.RoundLimitCount}, anomalies={metadata.AnomalyCount}, " +
                $"elapsed={metadata.ElapsedSeconds:0.0}s, output={outputDirectory}.");

            if (metadata.Exceptions > 0 || metadata.EffectLimitHits > 0 ||
                (options.StrictAcceptance && gateFailures.Count > 0))
            {
                throw new InvalidOperationException(
                    "Chapter encounter sampling failed its acceptance gate: " +
                    string.Join("; ", gateFailures) + ". " +
                    "See metadata.json and chapter_encounter_anomalies.csv.");
            }
        }

        private static IReadOnlyList<string> BuildAcceptanceGateFailures(
            int expectedScenarioCount,
            int expectedBattleCount,
            int actualScenarioCount,
            int actualBattleCount,
            int exceptions,
            int effectLimitHits,
            int roundLimitCount,
            int p0AnomalyCount,
            int p1AnomalyCount)
        {
            var failures = new List<string>();
            if (actualScenarioCount != expectedScenarioCount)
            {
                failures.Add(
                    $"scenarioCount={actualScenarioCount}, " +
                    $"expected={expectedScenarioCount}");
            }
            if (actualBattleCount != expectedBattleCount)
            {
                failures.Add(
                    $"battleCount={actualBattleCount}, " +
                    $"expected={expectedBattleCount}");
            }
            if (exceptions > 0) failures.Add($"exceptions={exceptions}");
            if (effectLimitHits > 0)
            {
                failures.Add($"effectLimitHits={effectLimitHits}");
            }
            if (roundLimitCount > 0)
            {
                failures.Add($"roundLimitCount={roundLimitCount}");
            }
            if (p0AnomalyCount > 0)
            {
                failures.Add($"p0AnomalyCount={p0AnomalyCount}");
            }
            if (p1AnomalyCount > 0)
            {
                failures.Add($"p1AnomalyCount={p1AnomalyCount}");
            }
            return failures.AsReadOnly();
        }

        private static IReadOnlyList<ChapterEncounterScopeSummary> BuildScopeSummaries(
            IReadOnlyList<ChapterEncounterAggregate> aggregates)
        {
            var summaries = new List<ChapterEncounterScopeSummary>();
            foreach (var group in aggregates.GroupBy(value => new
                     {
                         value.Floor,
                         value.DevelopmentLevel
                     }))
            {
                var all = group.ToList();
                var mapRows = all.Where(value =>
                    value.Source == ChapterEncounterSource.Map).ToList();
                summaries.Add(ChapterEncounterScopeSummary.Create(
                    "ChapterOfficial",
                    $"F{group.Key.Floor}",
                    group.Key.Floor,
                    group.Key.DevelopmentLevel,
                    all));
                summaries.Add(ChapterEncounterScopeSummary.Create(
                    "ChapterMap",
                    $"F{group.Key.Floor}",
                    group.Key.Floor,
                    group.Key.DevelopmentLevel,
                    mapRows));

                foreach (var row in mapRows.Where(value =>
                             value.CombatIndex == 4 &&
                             !string.IsNullOrWhiteSpace(value.RouteTag)))
                {
                    summaries.Add(ChapterEncounterScopeSummary.Create(
                        "C4Route",
                        row.RouteTag,
                        row.Floor,
                        row.DevelopmentLevel,
                        new[] { row }));
                }

                var boss = mapRows.Where(value =>
                    string.Equals(
                        value.NodeType,
                        "Boss",
                        StringComparison.OrdinalIgnoreCase)).ToList();
                if (boss.Count > 0)
                {
                    summaries.Add(ChapterEncounterScopeSummary.Create(
                        "Boss",
                        boss[0].EncounterId,
                        group.Key.Floor,
                        group.Key.DevelopmentLevel,
                        boss));
                }

                var events = all.Where(value =>
                    value.Source == ChapterEncounterSource.Event).ToList();
                if (events.Count > 0)
                {
                    summaries.Add(ChapterEncounterScopeSummary.Create(
                        "Event",
                        events[0].EncounterId,
                        group.Key.Floor,
                        group.Key.DevelopmentLevel,
                        events));
                }
            }

            return summaries
                .OrderBy(value => value.Floor)
                .ThenBy(value => value.DevelopmentLevel)
                .ThenBy(value => value.ScopeType)
                .ThenBy(value => value.ScopeId)
                .ToList()
                .AsReadOnly();
        }

        private static string SerializeScenarioCsv(
            IEnumerable<ChapterEncounterScenarioResult> scenarioResults)
        {
            var builder = new StringBuilder();
            AppendCsvRow(builder, new object[]
            {
                "scenario_id", "source", "map_id", "map_name", "floor",
                "node_id", "node_type", "combat_index", "route_tag",
                "event_id", "event_name", "encounter_id", "encounter_name",
                "threat_rating", "build_id", "development_level", "battles",
                "player_wins", "enemy_wins", "draws", "player_win_rate",
                "player_score_rate", "win_ci95_low", "win_ci95_high",
                "average_rounds", "p90_rounds", "round_limit_count",
                "effect_limit_hits", "exceptions", "average_player_survivors",
                "average_enemy_survivors", "enemy_cleave_hits_per_battle",
                "enemy_summon_attempts_per_battle",
                "enemy_summon_successes_per_battle",
                "enemy_shields_granted_per_battle",
                "enemy_shield_blocks_per_battle",
                "enemy_furnace_transfers_per_battle",
                "enemy_flourish_gained_per_battle"
            });
            foreach (var row in scenarioResults)
            {
                var definition = row.Definition;
                var batch = row.Batch;
                var interval = ChapterEncounterStatistics.Wilson95(
                    batch.PlayerWins,
                    batch.Battles);
                AppendCsvRow(builder, new object[]
                {
                    row.ScenarioId,
                    definition.Source,
                    definition.MapId,
                    definition.MapName,
                    definition.Floor,
                    definition.NodeId,
                    definition.NodeType,
                    definition.CombatIndex,
                    definition.RouteTag,
                    definition.EventId,
                    definition.EventName,
                    definition.EncounterId,
                    definition.EncounterName,
                    definition.ThreatRating,
                    row.BuildId,
                    row.DevelopmentLevel,
                    batch.Battles,
                    batch.PlayerWins,
                    batch.EnemyWins,
                    batch.Draws,
                    Format(batch.PlayerWinRate),
                    Format(batch.PlayerScoreRate),
                    Format(interval.Low),
                    Format(interval.High),
                    Format(row.AverageRounds),
                    Format(row.P90Rounds),
                    row.RoundLimitCount,
                    row.EffectLimitHitCount,
                    batch.Exceptions,
                    Format(row.AveragePlayerSurvivors),
                    Format(row.AverageEnemySurvivors),
                    FormatPerBattle(row.EnemyCleaveHits, batch.Battles),
                    FormatPerBattle(row.EnemySummonAttempts, batch.Battles),
                    FormatPerBattle(row.EnemySummonSuccesses, batch.Battles),
                    FormatPerBattle(row.EnemyShieldsGranted, batch.Battles),
                    FormatPerBattle(row.EnemyShieldDamageBlocks, batch.Battles),
                    FormatPerBattle(row.EnemyFurnaceTransfers, batch.Battles),
                    FormatPerBattle(row.EnemyFlourishGained, batch.Battles)
                });
            }
            return builder.ToString();
        }

        private static string SerializeAggregateCsv(
            IEnumerable<ChapterEncounterAggregate> aggregates)
        {
            var builder = new StringBuilder();
            AppendCsvRow(builder, new object[]
            {
                "aggregate_id", "source", "map_id", "map_name", "floor",
                "node_id", "node_type", "combat_index", "route_tag",
                "event_id", "event_name", "encounter_id", "encounter_name",
                "threat_rating", "development_level", "scenario_count",
                "battles", "player_wins", "enemy_wins", "draws",
                "player_win_rate", "player_score_rate", "win_ci95_low",
                "win_ci95_high", "build_score_spread", "weakest_build_id",
                "strongest_build_id", "average_rounds", "p90_rounds",
                "round_limit_count", "round_limit_rate", "effect_limit_hits",
                "exceptions", "average_player_survivors",
                "average_enemy_survivors", "enemy_cleave_hits_per_battle",
                "enemy_summon_attempts_per_battle",
                "enemy_summon_successes_per_battle",
                "enemy_shields_granted_per_battle",
                "enemy_shield_blocks_per_battle",
                "enemy_furnace_transfers_per_battle",
                "enemy_flourish_gained_per_battle"
            });
            foreach (var row in aggregates)
            {
                var interval = ChapterEncounterStatistics.Wilson95(
                    row.PlayerWins,
                    row.Battles);
                AppendCsvRow(builder, new object[]
                {
                    row.AggregateId,
                    row.Source,
                    row.MapId,
                    row.MapName,
                    row.Floor,
                    row.NodeId,
                    row.NodeType,
                    row.CombatIndex,
                    row.RouteTag,
                    row.EventId,
                    row.EventName,
                    row.EncounterId,
                    row.EncounterName,
                    row.ThreatRating,
                    row.DevelopmentLevel,
                    row.ScenarioCount,
                    row.Battles,
                    row.PlayerWins,
                    row.EnemyWins,
                    row.Draws,
                    Format(row.PlayerWinRate),
                    Format(row.PlayerScoreRate),
                    Format(interval.Low),
                    Format(interval.High),
                    Format(row.BuildScoreSpread),
                    row.WeakestBuildId,
                    row.StrongestBuildId,
                    Format(row.AverageRounds),
                    Format(row.P90Rounds),
                    row.RoundLimitCount,
                    Format(row.RoundLimitRate),
                    row.EffectLimitHitCount,
                    row.Exceptions,
                    Format(row.AveragePlayerSurvivors),
                    Format(row.AverageEnemySurvivors),
                    FormatPerBattle(row.EnemyCleaveHits, row.Battles),
                    FormatPerBattle(row.EnemySummonAttempts, row.Battles),
                    FormatPerBattle(row.EnemySummonSuccesses, row.Battles),
                    FormatPerBattle(row.EnemyShieldsGranted, row.Battles),
                    FormatPerBattle(row.EnemyShieldDamageBlocks, row.Battles),
                    FormatPerBattle(row.EnemyFurnaceTransfers, row.Battles),
                    FormatPerBattle(row.EnemyFlourishGained, row.Battles)
                });
            }
            return builder.ToString();
        }

        private static string SerializeScopeCsv(
            IEnumerable<ChapterEncounterScopeSummary> summaries)
        {
            var builder = new StringBuilder();
            AppendCsvRow(builder, new object[]
            {
                "scope_type", "scope_id", "floor", "development_level",
                "encounter_count", "battles", "player_wins", "enemy_wins",
                "draws", "player_win_rate", "player_score_rate",
                "win_ci95_low", "win_ci95_high", "average_rounds",
                "round_limit_count", "effect_limit_hits", "exceptions"
            });
            foreach (var row in summaries)
            {
                var interval = ChapterEncounterStatistics.Wilson95(
                    row.PlayerWins,
                    row.Battles);
                AppendCsvRow(builder, new object[]
                {
                    row.ScopeType,
                    row.ScopeId,
                    row.Floor,
                    row.DevelopmentLevel,
                    row.EncounterCount,
                    row.Battles,
                    row.PlayerWins,
                    row.EnemyWins,
                    row.Draws,
                    Format(row.PlayerWinRate),
                    Format(row.PlayerScoreRate),
                    Format(interval.Low),
                    Format(interval.High),
                    Format(row.AverageRounds),
                    row.RoundLimitCount,
                    row.EffectLimitHitCount,
                    row.Exceptions
                });
            }
            return builder.ToString();
        }

        private static string SerializeAnomalyCsv(
            IEnumerable<ChapterEncounterAnomaly> anomalies)
        {
            var builder = new StringBuilder();
            AppendCsvRow(builder, new object[]
            {
                "severity", "code", "scope", "floor", "development_level",
                "encounter_id", "message", "evidence", "recommendation"
            });
            foreach (var row in anomalies)
            {
                AppendCsvRow(builder, new object[]
                {
                    row.Severity,
                    row.Code,
                    row.Scope,
                    row.Floor,
                    row.DevelopmentLevel,
                    row.EncounterId,
                    row.Message,
                    row.Evidence,
                    row.Recommendation
                });
            }
            return builder.ToString();
        }

        private static string SerializeProgressFixtureCsv(
            ChapterProgressFixtureCatalog fixtures)
        {
            var builder = new StringBuilder();
            AppendCsvRow(builder, new object[]
            {
                "fixture_id", "checkpoint_id", "floor", "combat_index",
                "run_turn", "max_tavern_tier", "build_id", "active_slots",
                "golden_slots", "attack", "health", "flourish_stacks", "board"
            });
            foreach (var checkpoint in fixtures.Checkpoints
                         .OrderBy(value => value.RunTurn))
            {
                foreach (var buildId in fixtures.BuildIds)
                {
                    var board = fixtures.CreateFixture(
                        checkpoint.Floor,
                        checkpoint.CombatIndex,
                        buildId);
                    var active = board.Player
                        .Select((minion, slot) => new { minion, slot })
                        .Where(value => value.minion != null)
                        .ToList();
                    AppendCsvRow(builder, new object[]
                    {
                        $"{buildId}_{checkpoint.Id}",
                        checkpoint.Id,
                        checkpoint.Floor,
                        checkpoint.CombatIndex,
                        checkpoint.RunTurn,
                        checkpoint.MaxTavernTier,
                        buildId,
                        active.Count,
                        string.Join(
                            "/",
                            active.Where(value => value.minion.IsGolden)
                                .Select(value => value.slot)),
                        active.Sum(value => value.minion.CurrentAttack),
                        active.Sum(value => value.minion.CurrentHealth),
                        board.PlayerFlourishStacks,
                        string.Join(
                            ";",
                            active.Select(value =>
                                $"S{value.slot}:{value.minion.Id}=" +
                                $"{value.minion.CurrentAttack}/" +
                                $"{value.minion.CurrentHealth}" +
                                (value.minion.IsGolden ? "(G)" : string.Empty)))
                    });
                }
            }
            return builder.ToString();
        }

        private static string BuildMarkdownReport(
            ConfigService configs,
            int buildCount,
            string fixtureMode,
            IReadOnlyList<ChapterEncounterDefinition> definitions,
            IReadOnlyList<int> seeds,
            IReadOnlyList<ChapterEncounterScenarioResult> scenarioResults,
            IReadOnlyList<ChapterEncounterAggregate> aggregates,
            IReadOnlyList<ChapterEncounterScopeSummary> scopes,
            IReadOnlyList<ChapterEncounterAnomaly> anomalies,
            TimeSpan elapsed)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# v0.4.0 章节遭遇胜率采样");
            builder.AppendLine();
            builder.AppendLine("## 采样口径");
            builder.AppendLine();
            builder.AppendLine(
                $"- 正式可达遭遇：{definitions.Count} 个（地图战斗 " +
                $"{definitions.Count(value => value.Source == ChapterEncounterSource.Map)} " +
                $"个，事件后续战斗 " +
                $"{definitions.Count(value => value.Source == ChapterEncounterSource.Event)} 个）。");
            if (string.Equals(
                    fixtureMode,
                    ChapterEncounterSamplingOptions.ProgressFixtureMode,
                    StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendLine(
                    $"- 固定构筑：{buildCount} 套；按 F1/F2/F3 的 C2、C4、C5 " +
                    "累计商店回合快照匹配遭遇，Boss 复用本章 C5 终检夹具。");
            }
            else
            {
                builder.AppendLine(
                    $"- 固定构筑：{buildCount} 套；每套使用 N（普通）和 " +
                    "H（高成型）两档。");
            }
            builder.AppendLine(
                $"- 固定种子：{seeds.First()}–{seeds.Last()}，每场景 " +
                $"{seeds.Count} 场；共 {scenarioResults.Sum(value => value.Batch.Battles):N0} " +
                "场真实 PVE 先后手模拟。");
            builder.AppendLine(
                "- 胜率为玩家方直接获胜比例；计分胜率将平局按 0.5 计入。95% 区间为 " +
                "Wilson 区间，仅用于观察采样波动。");
            builder.AppendLine(
                "- “全部正式遭遇”按当前地图和已发布事件的可达引用计算；旧版遗留遭遇与 " +
                "stage5b 调试演练不在本批次。");
            if (string.Equals(
                    fixtureMode,
                    ChapterEncounterSamplingOptions.ProgressFixtureMode,
                    StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendLine(
                    "- 进度夹具约束累计商店回合、上阵数量、酒馆等级、卡牌 Tier、金色数量、" +
                    "永久成长与下一战覆盖层；C1/C2、C3/C4、C5/Boss 分别使用 C2、C4、C5 快照。");
            }
            else
            {
                builder.AppendLine(
                    "- N/H 是固定构筑强度端点，不是逐章实际商店进度快照；绝对全胜/全败只标为 " +
                    "P2，跨路线、Boss、构筑硬克制和安全异常优先级更高。");
            }
            builder.AppendLine();
            builder.AppendLine(
                $"内容版本 `{configs.ContentRelease.ContentVersion}`，配置哈希 " +
                $"`{configs.Identity.ConfigHash}`，耗时 {elapsed.TotalSeconds:0.0} 秒。");
            builder.AppendLine();

            builder.AppendLine("## 结论总览");
            builder.AppendLine();
            builder.AppendLine(
                $"异常共 {anomalies.Count} 项：P0 " +
                $"{anomalies.Count(value => value.Severity == "P0")}，P1 " +
                $"{anomalies.Count(value => value.Severity == "P1")}，P2 " +
                $"{anomalies.Count(value => value.Severity == "P2")}。");
            builder.AppendLine();
            builder.AppendLine(
                "| 章节 | 档位 | 地图遭遇数 | 场次 | 胜率 | 计分胜率 | 95% 区间 | " +
                "平均回合 | 回合上限 | 安全异常 |");
            builder.AppendLine(
                "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (var row in scopes.Where(value =>
                         value.ScopeType == "ChapterMap"))
            {
                var interval = ChapterEncounterStatistics.Wilson95(
                    row.PlayerWins,
                    row.Battles);
                builder.AppendLine(
                    $"| F{row.Floor} | {row.DevelopmentLevel} | " +
                    $"{row.EncounterCount} | {row.Battles:N0} | " +
                    $"{Percent(row.PlayerWinRate)} | {Percent(row.PlayerScoreRate)} | " +
                    $"{Percent(interval.Low)}–{Percent(interval.High)} | " +
                    $"{row.AverageRounds:0.00} | {row.RoundLimitCount} | " +
                    $"{row.Exceptions + row.EffectLimitHitCount} |");
            }
            builder.AppendLine();

            builder.AppendLine("## C4 路线胜率");
            builder.AppendLine();
            builder.AppendLine(
                "| 章节 | 档位 | 激进 胜/计分 | 冒险 胜/计分 | 保守 胜/计分 | " +
                "计分最大差 | 风险顺序 |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---|");
            foreach (var group in aggregates
                         .Where(value =>
                             value.Source == ChapterEncounterSource.Map &&
                             value.CombatIndex == 4 &&
                             !string.IsNullOrWhiteSpace(value.RouteTag))
                         .GroupBy(value => new
                         {
                             value.Floor,
                             value.DevelopmentLevel
                         })
                         .OrderBy(value => value.Key.Floor)
                         .ThenBy(value => value.Key.DevelopmentLevel))
            {
                var aggressive = FindRoute(group, "Aggressive");
                var adventure = FindRoute(group, "Adventure");
                var conservative = FindRoute(group, "Conservative");
                if (aggressive == null || adventure == null || conservative == null)
                {
                    continue;
                }
                var values = new[]
                {
                    aggressive.PlayerScoreRate,
                    adventure.PlayerScoreRate,
                    conservative.PlayerScoreRate
                };
                var ordered = conservative.PlayerScoreRate + 0.05d >=
                              adventure.PlayerScoreRate &&
                              adventure.PlayerScoreRate + 0.05d >=
                              aggressive.PlayerScoreRate;
                builder.AppendLine(
                    $"| F{group.Key.Floor} | {group.Key.DevelopmentLevel} | " +
                    $"{Percent(aggressive.PlayerWinRate)} / " +
                    $"{Percent(aggressive.PlayerScoreRate)} | " +
                    $"{Percent(adventure.PlayerWinRate)} / " +
                    $"{Percent(adventure.PlayerScoreRate)} | " +
                    $"{Percent(conservative.PlayerWinRate)} / " +
                    $"{Percent(conservative.PlayerScoreRate)} | " +
                    $"{Percent(values.Max() - values.Min())} | " +
                    $"{(ordered ? "符合" : "倒挂")} |");
            }
            builder.AppendLine();

            builder.AppendLine("## C2 / C5 分支差异");
            builder.AppendLine();
            builder.AppendLine(
                "| 章节 | 阶段 | 档位 | 遭遇 A | 胜/计分 | 遭遇 B | 胜/计分 | " +
                "计分差值 |");
            builder.AppendLine("|---|---:|---:|---|---:|---|---:|---:|");
            foreach (var group in aggregates
                         .Where(value =>
                             value.Source == ChapterEncounterSource.Map &&
                             (value.CombatIndex == 2 || value.CombatIndex == 5) &&
                             string.IsNullOrWhiteSpace(value.RouteTag))
                         .GroupBy(value => new
                         {
                             value.Floor,
                             value.CombatIndex,
                             value.DevelopmentLevel
                         })
                         .OrderBy(value => value.Key.Floor)
                         .ThenBy(value => value.Key.CombatIndex)
                         .ThenBy(value => value.Key.DevelopmentLevel))
            {
                var rows = group.OrderBy(value => value.NodeId).ToList();
                if (rows.Count != 2) continue;
                var branchScoreGap = Math.Abs(
                    rows[0].PlayerScoreRate -
                    rows[1].PlayerScoreRate);
                builder.AppendLine(
                    $"| F{group.Key.Floor} | C{group.Key.CombatIndex} | " +
                    $"{group.Key.DevelopmentLevel} | {Md(rows[0].EncounterName)} | " +
                    $"{Percent(rows[0].PlayerWinRate)} / " +
                    $"{Percent(rows[0].PlayerScoreRate)} | " +
                    $"{Md(rows[1].EncounterName)} | " +
                    $"{Percent(rows[1].PlayerWinRate)} / " +
                    $"{Percent(rows[1].PlayerScoreRate)} | " +
                    $"{Percent(branchScoreGap)} |");
            }
            builder.AppendLine();

            builder.AppendLine("## Boss 压力");
            builder.AppendLine();
            builder.AppendLine(
                "| 章节 | 档位 | Boss | Boss 胜/计分 | C5 胜/计分均值 | " +
                "计分 Boss-C5 | 计分构筑跨度 | 平均回合 |");
            builder.AppendLine("|---|---:|---|---:|---:|---:|---:|---:|");
            foreach (var group in aggregates
                         .Where(value =>
                             value.Source == ChapterEncounterSource.Map)
                         .GroupBy(value => new
                         {
                             value.Floor,
                             value.DevelopmentLevel
                         })
                         .Where(group =>
                             group.Any(value => string.Equals(
                                 value.NodeType,
                                 "Boss",
                                 StringComparison.OrdinalIgnoreCase)) &&
                             group.Any(value => value.CombatIndex == 5))
                         .OrderBy(value => value.Key.Floor)
                         .ThenBy(value => value.Key.DevelopmentLevel))
            {
                var boss = group.Single(value => string.Equals(
                    value.NodeType,
                    "Boss",
                    StringComparison.OrdinalIgnoreCase));
                var c5Win = group.Where(value => value.CombatIndex == 5)
                    .Average(value => value.PlayerWinRate);
                var c5Score = group.Where(value => value.CombatIndex == 5)
                    .Average(value => value.PlayerScoreRate);
                builder.AppendLine(
                    $"| F{group.Key.Floor} | {group.Key.DevelopmentLevel} | " +
                    $"{Md(boss.EncounterName)} | {Percent(boss.PlayerWinRate)} / " +
                    $"{Percent(boss.PlayerScoreRate)} | {Percent(c5Win)} / " +
                    $"{Percent(c5Score)} | " +
                    $"{SignedPercent(boss.PlayerScoreRate - c5Score)} | " +
                    $"{Percent(boss.BuildScoreSpread)} | {boss.AverageRounds:0.00} |");
            }
            builder.AppendLine();

            builder.AppendLine("## 事件后续战斗");
            builder.AppendLine();
            builder.AppendLine(
                "| 章节 | 档位 | 事件 | 遭遇 | 胜/计分 | 计分构筑跨度 | 平均回合 |");
            builder.AppendLine("|---|---:|---|---|---:|---:|---:|");
            foreach (var row in aggregates
                         .Where(value => value.Source == ChapterEncounterSource.Event)
                         .OrderBy(value => value.Floor)
                         .ThenBy(value => value.DevelopmentLevel))
            {
                builder.AppendLine(
                    $"| F{row.Floor} | {row.DevelopmentLevel} | {Md(row.EventName)} | " +
                    $"{Md(row.EncounterName)} | {Percent(row.PlayerWinRate)} / " +
                    $"{Percent(row.PlayerScoreRate)} | " +
                    $"{Percent(row.BuildScoreSpread)} | {row.AverageRounds:0.00} |");
            }
            builder.AppendLine();

            builder.AppendLine("## 构筑敏感度最高的遭遇");
            builder.AppendLine();
            builder.AppendLine(
                "| 排名 | 章节 | 档位 | 遭遇 | 胜/计分 | 计分构筑跨度 | " +
                "最弱构筑 | 最强构筑 |");
            builder.AppendLine("|---:|---|---:|---|---:|---:|---|---|");
            var sensitive = aggregates
                .OrderByDescending(value => value.BuildScoreSpread)
                .ThenBy(value => value.Floor)
                .Take(12)
                .ToList();
            for (var index = 0; index < sensitive.Count; index++)
            {
                var row = sensitive[index];
                builder.AppendLine(
                    $"| {index + 1} | F{row.Floor} | {row.DevelopmentLevel} | " +
                    $"{Md(row.EncounterName)} | {Percent(row.PlayerWinRate)} / " +
                    $"{Percent(row.PlayerScoreRate)} | " +
                    $"{Percent(row.BuildScoreSpread)} | {row.WeakestBuildId} | " +
                    $"{row.StrongestBuildId} |");
            }
            builder.AppendLine();

            builder.AppendLine("## 异常清单");
            builder.AppendLine();
            if (anomalies.Count == 0)
            {
                builder.AppendLine("未发现命中当前规则的异常。");
            }
            else
            {
                builder.AppendLine("| 优先级 | 规则 | 数量 |");
                builder.AppendLine("|---:|---|---:|");
                foreach (var group in anomalies
                             .GroupBy(value => new
                             {
                                 value.Severity,
                                 value.Code
                             })
                             .OrderBy(value => value.Key.Severity)
                             .ThenBy(value => value.Key.Code))
                {
                    builder.AppendLine(
                        $"| {group.Key.Severity} | `{group.Key.Code}` | " +
                        $"{group.Count()} |");
                }
                builder.AppendLine();
                builder.AppendLine(
                    "| 优先级 | 规则 | 章节/档位 | 遭遇 | 结论 | 证据 | 建议 |");
                builder.AppendLine("|---:|---|---|---|---|---|---|");
                foreach (var row in anomalies.Where(value =>
                             value.Code != "SATURATED_RESULT"))
                {
                    builder.AppendLine(
                        $"| {row.Severity} | `{row.Code}` | " +
                        $"F{row.Floor}/{row.DevelopmentLevel} | " +
                        $"{Md(row.EncounterId)} | {Md(row.Message)} | " +
                        $"{Md(row.Evidence)} | {Md(row.Recommendation)} |");
                }
                builder.AppendLine();
                builder.AppendLine("### 结果饱和汇总");
                builder.AppendLine();
                builder.AppendLine("| 章节 | 档位 | 饱和遭遇数 |");
                builder.AppendLine("|---|---:|---:|");
                foreach (var group in anomalies
                             .Where(value => value.Code == "SATURATED_RESULT")
                             .GroupBy(value => new
                             {
                                 value.Floor,
                                 value.DevelopmentLevel
                             })
                             .OrderBy(value => value.Key.Floor)
                             .ThenBy(value => value.Key.DevelopmentLevel))
                {
                    builder.AppendLine(
                        $"| F{group.Key.Floor} | {group.Key.DevelopmentLevel} | " +
                        $"{group.Count()} |");
                }
                builder.AppendLine();
                builder.AppendLine(
                    "逐遭遇饱和明细保留在 `chapter_encounter_anomalies.csv`。");
            }
            builder.AppendLine();

            builder.AppendLine("## 异常规则");
            builder.AppendLine();
            builder.AppendLine(
                "- P0：模拟异常或效果队列上限，必须先解决，结果不能用于调数。");
            builder.AppendLine(
                "- P1：构筑计分胜率跨度 ≥35%、路线风险倒挂、Boss 压力异常、" +
                "跨章反向、明显难度悬崖，或 H 档胜率 ≤2%。");
            builder.AppendLine(
                "- P2：结果饱和、C2/C5 分支差 ≥25%、路线差异过平/过陡，" +
                "以及章节内明显压力回落。");
            builder.AppendLine(
                "- 比较规则保留 5–10 个百分点容差，避免把固定种子的小波动误判为倒挂。");
            builder.AppendLine();
            builder.AppendLine(
                "逐构筑结果见 `chapter_encounter_scenarios.csv`，逐遭遇聚合见 " +
                "`chapter_encounter_aggregates.csv`，章节/路线/Boss 汇总见 " +
                "`chapter_encounter_scopes.csv`，完整异常见 " +
                "`chapter_encounter_anomalies.csv`。");
            return builder.ToString();
        }

        private static ChapterEncounterAggregate FindRoute(
            IEnumerable<ChapterEncounterAggregate> rows,
            string routeTag)
        {
            return rows.SingleOrDefault(value => string.Equals(
                value.RouteTag,
                routeTag,
                StringComparison.OrdinalIgnoreCase));
        }

        private static GitState ReadGitState(string repositoryRoot)
        {
            try
            {
                var commit = RunGit(repositoryRoot, "rev-parse HEAD").Trim();
                var status = RunGit(
                    repositoryRoot,
                    "status --porcelain");
                var sourceTreeDirty = !string.IsNullOrWhiteSpace(status);
                return new GitState
                {
                    IsAvailable = true,
                    Commit = commit,
                    SourceTreeDirty = sourceTreeDirty,
                    Diagnostic = sourceTreeDirty
                        ? "The source tree was dirty before sampling started."
                        : "The source tree was clean before sampling started."
                };
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Unable to read Git state: {exception.Message}");
                return new GitState
                {
                    IsAvailable = false,
                    Commit = "unknown",
                    SourceTreeDirty = true,
                    Diagnostic = "Git state was unavailable: " + exception.Message
                };
            }
        }

        private static string RunGit(string repositoryRoot, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"-C \"{repositoryRoot}\" {arguments}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Unable to start Git.");
                }
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(error);
                }
                return output;
            }
        }

        private static string FormatPerBattle(int value, int battles)
        {
            return Format(ChapterEncounterStatistics.Rate(value, battles));
        }

        private static string Format(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Percent(double value)
        {
            return value.ToString("P1", CultureInfo.InvariantCulture);
        }

        private static string SignedPercent(double value)
        {
            return value.ToString("+0.0%;-0.0%;0.0%", CultureInfo.InvariantCulture);
        }

        private static string Md(string value)
        {
            return (value ?? string.Empty)
                .Replace("|", "\\|")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static void AppendCsvRow(
            StringBuilder builder,
            IEnumerable<object> values)
        {
            builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        private static string EscapeCsv(object value)
        {
            if (value == null) return string.Empty;
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ??
                       string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? text
                : $"\"{text.Replace("\"", "\"\"")}\"";
        }

        private sealed class ChapterEncounterSamplingOptions
        {
            public const string EndpointFixtureMode = "endpoints";
            public const string ProgressFixtureMode = "progress";

            public string SeedSetName { get; private set; }
            public int FirstSeed { get; private set; }
            public int SeedCount { get; private set; }
            public string OutputDirectory { get; private set; }
            public string FixtureMode { get; private set; }
            public bool RequireCleanSource { get; private set; }
            public bool StrictAcceptance { get; private set; }
            public bool UsesProgressFixtures => string.Equals(
                FixtureMode,
                ProgressFixtureMode,
                StringComparison.OrdinalIgnoreCase);

            public static ChapterEncounterSamplingOptions CreateDefault()
            {
                return new ChapterEncounterSamplingOptions
                {
                    SeedSetName = "S0_CHAPTER_ENCOUNTERS",
                    FirstSeed = DefaultFirstSeed,
                    SeedCount = DefaultSeedCount,
                    FixtureMode = EndpointFixtureMode
                };
            }

            public void UseProgressFixtures()
            {
                FixtureMode = ProgressFixtureMode;
                SeedSetName = "S0_CHAPTER_PROGRESS";
            }

            public static ChapterEncounterSamplingOptions FromCommandLine(
                IReadOnlyList<string> arguments)
            {
                var options = CreateDefault();
                options.OutputDirectory = ReadArgument(
                    arguments,
                    "-chapterSampleOutput");
                options.SeedSetName = ReadArgument(
                    arguments,
                    "-chapterSampleSeedSet") ?? options.SeedSetName;
                options.FixtureMode = ReadArgument(
                    arguments,
                    "-chapterSampleFixtureMode") ?? options.FixtureMode;
                options.FirstSeed = ReadIntArgument(
                    arguments,
                    "-chapterSampleFirstSeed",
                    options.FirstSeed);
                options.SeedCount = ReadIntArgument(
                    arguments,
                    "-chapterSampleSeedCount",
                    options.SeedCount);
                options.RequireCleanSource = ReadBoolArgument(
                    arguments,
                    "-chapterSampleRequireCleanSource",
                    options.RequireCleanSource);
                options.StrictAcceptance = ReadBoolArgument(
                    arguments,
                    "-chapterSampleStrictAcceptance",
                    options.StrictAcceptance);
                if (options.SeedCount < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        "-chapterSampleSeedCount",
                        "Seed count must be at least one.");
                }
                if (!string.Equals(
                        options.FixtureMode,
                        EndpointFixtureMode,
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        options.FixtureMode,
                        ProgressFixtureMode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "-chapterSampleFixtureMode must be endpoints or progress.");
                }
                if (options.UsesProgressFixtures &&
                    options.SeedSetName == "S0_CHAPTER_ENCOUNTERS")
                {
                    options.SeedSetName = "S0_CHAPTER_PROGRESS";
                }
                return options;
            }

            private static bool ReadBoolArgument(
                IReadOnlyList<string> arguments,
                string name,
                bool fallback)
            {
                var text = ReadArgument(arguments, name);
                if (string.IsNullOrWhiteSpace(text)) return fallback;
                if (!bool.TryParse(text, out var value))
                {
                    throw new ArgumentException(
                        $"{name} must be true or false, got {text}.");
                }
                return value;
            }

            public string ResolveOutputDirectory(string repositoryRoot)
            {
                if (!string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    return Path.GetFullPath(Path.IsPathRooted(OutputDirectory)
                        ? OutputDirectory
                        : Path.Combine(repositoryRoot, OutputDirectory));
                }
                return Path.Combine(
                    repositoryRoot,
                    "balance-results",
                    "v0.4.0",
                    UsesProgressFixtures
                        ? "chapter-progress-s0"
                        : "chapter-encounter-s0");
            }

            private static int ReadIntArgument(
                IReadOnlyList<string> arguments,
                string name,
                int fallback)
            {
                var text = ReadArgument(arguments, name);
                if (string.IsNullOrWhiteSpace(text)) return fallback;
                if (!int.TryParse(
                        text,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var value))
                {
                    throw new ArgumentException(
                        $"{name} must be an integer, got {text}.");
                }
                return value;
            }

            private static string ReadArgument(
                IReadOnlyList<string> arguments,
                string name)
            {
                for (var index = 0; index < arguments.Count - 1; index++)
                {
                    if (string.Equals(
                            arguments[index],
                            name,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return arguments[index + 1];
                    }
                }
                return null;
            }
        }

        private sealed class ChapterEncounterSamplingMetadata
        {
            [JsonProperty("schemaVersion")]
            public string SchemaVersion { get; set; } = "0.2.0";

            [JsonProperty("generatedAtUtc")]
            public string GeneratedAtUtc { get; set; }

            [JsonProperty("runtime")]
            public string Runtime { get; set; }

            [JsonProperty("unityVersion")]
            public string UnityVersion { get; set; }

            [JsonProperty("contentVersion")]
            public string ContentVersion { get; set; }

            [JsonProperty("configHash")]
            public string ConfigHash { get; set; }

            [JsonProperty("gitCommit")]
            public string GitCommit { get; set; }

            [JsonProperty("sourceTreeDirty")]
            public bool SourceTreeDirty { get; set; }

            [JsonProperty("requireCleanSource")]
            public bool RequireCleanSource { get; set; }

            [JsonProperty("strictAcceptance")]
            public bool StrictAcceptance { get; set; }

            [JsonProperty("fixtureVersion")]
            public string FixtureVersion { get; set; }

            [JsonProperty("fixtureFile")]
            public string FixtureFile { get; set; }

            [JsonProperty("fixtureSha256")]
            public string FixtureSha256 { get; set; }

            [JsonProperty("fixtureMode")]
            public string FixtureMode { get; set; }

            [JsonProperty("sourceFixtureVersion")]
            public string SourceFixtureVersion { get; set; }

            [JsonProperty("sourceFixtureFile")]
            public string SourceFixtureFile { get; set; }

            [JsonProperty("sourceFixtureSha256")]
            public string SourceFixtureSha256 { get; set; }

            [JsonProperty("coreClassifierVersion")]
            public string CoreClassifierVersion { get; set; }

            [JsonProperty("seedSet")]
            public string SeedSet { get; set; }

            [JsonProperty("firstSeed")]
            public int FirstSeed { get; set; }

            [JsonProperty("seedCount")]
            public int SeedCount { get; set; }

            [JsonProperty("fixedBuildCount")]
            public int FixedBuildCount { get; set; }

            [JsonProperty("developmentLevels")]
            public string[] DevelopmentLevels { get; set; }

            [JsonProperty("formalEncounterCount")]
            public int FormalEncounterCount { get; set; }

            [JsonProperty("mapEncounterCount")]
            public int MapEncounterCount { get; set; }

            [JsonProperty("eventEncounterCount")]
            public int EventEncounterCount { get; set; }

            [JsonProperty("scenarioCount")]
            public int ScenarioCount { get; set; }

            [JsonProperty("battleCount")]
            public int BattleCount { get; set; }

            [JsonProperty("exceptions")]
            public int Exceptions { get; set; }

            [JsonProperty("effectLimitHits")]
            public int EffectLimitHits { get; set; }

            [JsonProperty("roundLimitCount")]
            public int RoundLimitCount { get; set; }

            [JsonProperty("anomalyCount")]
            public int AnomalyCount { get; set; }

            [JsonProperty("p0AnomalyCount")]
            public int P0AnomalyCount { get; set; }

            [JsonProperty("p1AnomalyCount")]
            public int P1AnomalyCount { get; set; }

            [JsonProperty("p2AnomalyCount")]
            public int P2AnomalyCount { get; set; }

            [JsonProperty("acceptancePassed")]
            public bool AcceptancePassed { get; set; }

            [JsonProperty("gateFailures")]
            public string[] GateFailures { get; set; }

            [JsonProperty("elapsedSeconds")]
            public double ElapsedSeconds { get; set; }

            [JsonProperty("scenarioFile")]
            public string ScenarioFile { get; set; }

            [JsonProperty("aggregateFile")]
            public string AggregateFile { get; set; }

            [JsonProperty("scopeFile")]
            public string ScopeFile { get; set; }

            [JsonProperty("anomalyFile")]
            public string AnomalyFile { get; set; }

            [JsonProperty("reportFile")]
            public string ReportFile { get; set; }

            [JsonProperty("progressFixtureFile")]
            public string ProgressFixtureFile { get; set; }

            [JsonProperty("hashAlgorithm")]
            public string HashAlgorithm { get; set; }

            [JsonProperty("outputFiles")]
            public ChapterEncounterEvidenceFile[] OutputFiles { get; set; }

            [JsonProperty("outputSetSha256")]
            public string OutputSetSha256 { get; set; }
        }

        private sealed class GitState
        {
            public bool IsAvailable { get; set; }
            public string Commit { get; set; }
            public bool SourceTreeDirty { get; set; }
            public string Diagnostic { get; set; }
        }

        private sealed class ChapterEncounterScopeSummary
        {
            public string ScopeType { get; private set; }
            public string ScopeId { get; private set; }
            public int Floor { get; private set; }
            public string DevelopmentLevel { get; private set; }
            public int EncounterCount { get; private set; }
            public int Battles { get; private set; }
            public int PlayerWins { get; private set; }
            public int EnemyWins { get; private set; }
            public int Draws { get; private set; }
            public int Exceptions { get; private set; }
            public int RoundLimitCount { get; private set; }
            public int EffectLimitHitCount { get; private set; }
            public double AverageRounds { get; private set; }
            public double PlayerWinRate => ChapterEncounterStatistics.Rate(
                PlayerWins,
                Battles);
            public double PlayerScoreRate => Battles <= 0
                ? 0d
                : (PlayerWins + Draws * 0.5d) / Battles;

            public static ChapterEncounterScopeSummary Create(
                string scopeType,
                string scopeId,
                int floor,
                string developmentLevel,
                IEnumerable<ChapterEncounterAggregate> values)
            {
                var rows = values.ToList();
                var successfulBattles = rows.Sum(value =>
                    value.Battles - value.Exceptions);
                return new ChapterEncounterScopeSummary
                {
                    ScopeType = scopeType,
                    ScopeId = scopeId,
                    Floor = floor,
                    DevelopmentLevel = developmentLevel,
                    EncounterCount = rows.Count,
                    Battles = rows.Sum(value => value.Battles),
                    PlayerWins = rows.Sum(value => value.PlayerWins),
                    EnemyWins = rows.Sum(value => value.EnemyWins),
                    Draws = rows.Sum(value => value.Draws),
                    Exceptions = rows.Sum(value => value.Exceptions),
                    RoundLimitCount = rows.Sum(value => value.RoundLimitCount),
                    EffectLimitHitCount = rows.Sum(value =>
                        value.EffectLimitHitCount),
                    AverageRounds = successfulBattles <= 0
                        ? 0d
                        : rows.Sum(value =>
                            value.AverageRounds *
                            (value.Battles - value.Exceptions)) /
                          successfulBattles
                };
            }
        }
    }

    public sealed class ChapterEncounterEvidenceFile
    {
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("bytes")]
        public long Bytes { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }
    }

    public static class ChapterEncounterEvidenceHasher
    {
        public static IReadOnlyList<ChapterEncounterEvidenceFile> HashFiles(
            IEnumerable<string> paths)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            var files = paths.Select(path =>
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        throw new ArgumentException(
                            "Evidence file paths cannot be empty.",
                            nameof(paths));
                    }
                    var file = new FileInfo(path);
                    if (!file.Exists)
                    {
                        throw new FileNotFoundException(
                            "Evidence file was not found.",
                            file.FullName);
                    }
                    return new ChapterEncounterEvidenceFile
                    {
                        FileName = file.Name,
                        Bytes = file.Length,
                        Sha256 = ComputeFileSha256(file.FullName)
                    };
                })
                .OrderBy(value => value.FileName, StringComparer.Ordinal)
                .ToList();
            if (files.Select(value => value.FileName)
                .Distinct(StringComparer.Ordinal).Count() != files.Count)
            {
                throw new InvalidOperationException(
                    "Evidence file names must be unique.");
            }
            return files.AsReadOnly();
        }

        public static string ComputeFileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha256 = SHA256.Create())
            {
                return ToLowerHex(sha256.ComputeHash(stream));
            }
        }

        public static string ComputeOutputSetSha256(
            IEnumerable<ChapterEncounterEvidenceFile> files)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));
            var canonical = string.Join(
                "\n",
                files.OrderBy(value => value.FileName, StringComparer.Ordinal)
                    .Select(value =>
                        value.FileName + "\t" +
                        value.Bytes.ToString(CultureInfo.InvariantCulture) + "\t" +
                        value.Sha256)) + "\n";
            using (var sha256 = SHA256.Create())
            {
                return ToLowerHex(sha256.ComputeHash(
                    new UTF8Encoding(false).GetBytes(canonical)));
            }
        }

        private static string ToLowerHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
    }
}
