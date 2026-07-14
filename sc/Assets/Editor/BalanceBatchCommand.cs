using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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
    public static class BalanceBatchCommand
    {
        private const string DefaultCandidate = "R1-flourish-unity";
        private const string DefaultTuningRound = "R1";

        [MenuItem("Spire Chess/Balance/Run R1 Flourish S0")]
        public static void RunS0FromMenu()
        {
            Run(BalanceBatchOptions.CreateDefault("S0"));
        }

        [MenuItem("Spire Chess/Balance/Run R1 Flourish S1")]
        public static void RunS1FromMenu()
        {
            Run(BalanceBatchOptions.CreateDefault("S1"));
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Run(BalanceBatchOptions.FromCommandLine(Environment.GetCommandLineArgs()));
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

        private static void Run(BalanceBatchOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                              throw new InvalidOperationException("Unity project root is unavailable.");
            var repositoryRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
            var gitCommit = ReadGitState(repositoryRoot);
            var outputDirectory = options.ResolveOutputDirectory(repositoryRoot);

            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(string.Join("\n", validation.Errors));
            }

            MinionConfig ResolveMinion(string id)
            {
                return configs.MinionsById.TryGetValue(id, out var minion) ? minion : null;
            }

            var fixturePath = Path.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "Balance",
                "balance-fixtures.v0.2.json");
            var fixtures = BalanceFixtureCatalog.Load(
                File.ReadAllText(fixturePath),
                ResolveMinion);
            var configHash = ComputeConfigHash(Application.dataPath);
            var seeds = ResolveSeeds(options.SeedSet);
            var shouldReplay = options.Replay ??
                               string.Equals(options.SeedSet, "S0", StringComparison.OrdinalIgnoreCase);

            Debug.Log(
                $"Balance batch starting: candidate={options.CandidateId}, " +
                $"seedSet={options.SeedSet}, seeds={seeds.Count}, replay={shouldReplay}.");
            var stopwatch = Stopwatch.StartNew();
            var first = new BalanceMatrixRunner(ResolveMinion).Run(fixtures, seeds);
            IReadOnlyList<BalanceScenarioResult> replay = null;
            if (shouldReplay)
            {
                replay = new BalanceMatrixRunner(ResolveMinion).Run(fixtures, seeds);
            }
            stopwatch.Stop();

            Directory.CreateDirectory(outputDirectory);
            var metadataTemplate = new BalanceBatchMetadata
            {
                CandidateId = options.CandidateId,
                ContentVersion = configs.ContentRelease.ContentVersion,
                ConfigHash = configHash,
                GitCommit = gitCommit,
                UnityVersion = Application.unityVersion,
                TuningRound = options.TuningRound,
                FixtureVersion = fixtures.FixtureVersion,
                CoreClassifierVersion = fixtures.CoreClassifierVersion,
                SeedSet = CanonicalSeedSet(options.SeedSet)
            };

            var samplesPath = Path.Combine(outputDirectory, "balance_battle_samples.csv.gz");
            WriteSamples(samplesPath, metadataTemplate, first);
            File.WriteAllText(
                Path.Combine(outputDirectory, "balance_scenario_summary.csv"),
                SerializeScenarioSummaries(first),
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(outputDirectory, "balance_battle_summary.csv"),
                BalanceBattleCsv.SerializeMirrorSummaries(CreateMirrorSummaries(fixtures, first)),
                new UTF8Encoding(false));

            var determinismFailuresPath = Path.Combine(
                outputDirectory,
                "determinism_failures.csv");
            var determinismFailures = WriteDeterminismFailures(
                determinismFailuresPath,
                first,
                replay);
            var counters = CountSafety(first);
            var metadata = new BalanceUnityBatchMetadata
            {
                CandidateId = options.CandidateId,
                TuningRound = options.TuningRound,
                Runtime = "Unity Editor",
                UnityVersion = Application.unityVersion,
                ContentVersion = configs.ContentRelease.ContentVersion,
                ConfigHash = configHash,
                GitCommit = gitCommit,
                FixtureVersion = fixtures.FixtureVersion,
                CoreClassifierVersion = fixtures.CoreClassifierVersion,
                SeedSet = CanonicalSeedSet(options.SeedSet),
                SeedCount = seeds.Count,
                ScenarioCount = first.Count,
                BattleCount = first.Sum(value => value.Batch.Battles),
                ReplayBattleCount = replay?.Sum(value => value.Batch.Battles) ?? 0,
                TotalSimulations = first.Sum(value => value.Batch.Battles) +
                                   (replay?.Sum(value => value.Batch.Battles) ?? 0),
                DeterminismFailureCount = determinismFailures,
                TotalExceptions = counters.Exceptions,
                EffectLimitHits = counters.EffectLimitHits,
                CompetitiveRoundLimits = counters.CompetitiveRoundLimits,
                OutputDummyRoundLimits = counters.OutputDummyRoundLimits,
                ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                SampleFile = Path.GetFileName(samplesPath),
                SampleFileSha256 = ComputeFileHash(samplesPath)
            };
            var metadataPath = Path.Combine(outputDirectory, "metadata.json");
            File.WriteAllText(
                metadataPath,
                JsonConvert.SerializeObject(metadata, Formatting.Indented),
                new UTF8Encoding(false));

            Debug.Log(
                $"Balance batch complete: battles={metadata.BattleCount}, " +
                $"replays={metadata.ReplayBattleCount}, exceptions={metadata.TotalExceptions}, " +
                $"determinismFailures={metadata.DeterminismFailureCount}, " +
                $"output={outputDirectory}.");

            if (metadata.TotalExceptions > 0 ||
                metadata.EffectLimitHits > 0 ||
                metadata.CompetitiveRoundLimits > 0 ||
                metadata.DeterminismFailureCount > 0)
            {
                throw new InvalidOperationException(
                    "Balance batch failed its safety or determinism gate. See metadata.json.");
            }
        }

        private static string ComputeConfigHash(string dataPath)
        {
            var configRoot = Path.Combine(dataPath, "Resources", "Configs", "Json");
            var files = new[]
            {
                "minions.v0.1.json",
                "spells.v0.1.json",
                "encounters.v0.1.json",
                "rewards.v0.1.json",
                "content-release.v0.1.json"
            };
            return BalanceConfigHasher.Compute(files
                .Select(file => File.ReadAllText(Path.Combine(configRoot, file)))
                .ToArray());
        }

        private static IReadOnlyList<int> ResolveSeeds(string seedSet)
        {
            switch ((seedSet ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "S0":
                case "S0_SMOKE":
                    return BalanceSeedSets.S0Smoke;
                case "S1":
                case "S1_CALIBRATION":
                    return BalanceSeedSets.S1Calibration;
                case "S3":
                case "S3_HOLDOUT_A":
                    return BalanceSeedSets.S3HoldoutA;
                case "S4":
                case "S4_HOLDOUT_B":
                    return BalanceSeedSets.S4HoldoutB;
                default:
                    throw new ArgumentException($"Unknown balance seed set: {seedSet}.");
            }
        }

        private static string CanonicalSeedSet(string seedSet)
        {
            switch ((seedSet ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "S0":
                case "S0_SMOKE":
                    return "S0_SMOKE";
                case "S1":
                case "S1_CALIBRATION":
                    return "S1_CALIBRATION";
                case "S3":
                case "S3_HOLDOUT_A":
                    return "S3_HOLDOUT_A";
                case "S4":
                case "S4_HOLDOUT_B":
                    return "S4_HOLDOUT_B";
                default:
                    return seedSet;
            }
        }

        private static void WriteSamples(
            string path,
            BalanceBatchMetadata template,
            IEnumerable<BalanceScenarioResult> results)
        {
            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(
                       stream,
                       System.IO.Compression.CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
            {
                var firstBlock = true;
                foreach (var result in results)
                {
                    var scenario = result.Scenario;
                    var metadata = CopyMetadata(template);
                    metadata.BatchId = $"{template.CandidateId}-{template.SeedSet}-{scenario.ScenarioId}";
                    metadata.PlayerFixtureId = scenario.PlayerFixtureId;
                    metadata.EnemyFixtureId = scenario.EnemyFixtureId;
                    metadata.DevelopmentLevel = scenario.DevelopmentLevel;
                    metadata.Orientation = scenario.Orientation;
                    var csv = BalanceBattleCsv.SerializeSamples(metadata, result.Batch);
                    if (!firstBlock)
                    {
                        var firstNewline = csv.IndexOf('\n');
                        csv = firstNewline < 0 ? string.Empty : csv.Substring(firstNewline + 1);
                    }
                    writer.Write(csv);
                    firstBlock = false;
                }
            }
        }

        private static BalanceBatchMetadata CopyMetadata(BalanceBatchMetadata source)
        {
            return new BalanceBatchMetadata
            {
                BalanceSchemaVersion = source.BalanceSchemaVersion,
                CandidateId = source.CandidateId,
                ContentVersion = source.ContentVersion,
                ConfigHash = source.ConfigHash,
                GitCommit = source.GitCommit,
                UnityVersion = source.UnityVersion,
                TuningRound = source.TuningRound,
                FixtureVersion = source.FixtureVersion,
                CoreClassifierVersion = source.CoreClassifierVersion,
                SeedSet = source.SeedSet
            };
        }

        private static IReadOnlyList<BalanceMirrorSummary> CreateMirrorSummaries(
            BalanceFixtureCatalog fixtures,
            IReadOnlyList<BalanceScenarioResult> results)
        {
            var summaries = new List<BalanceMirrorSummary>();
            foreach (var level in new[] { "N", "H" })
            {
                for (var first = 0; first < fixtures.BuildIds.Count; first++)
                {
                    for (var second = first + 1; second < fixtures.BuildIds.Count; second++)
                    {
                        var buildA = fixtures.BuildIds[first];
                        var buildB = fixtures.BuildIds[second];
                        var aAsPlayer = results.Single(value =>
                            value.Scenario.PlayerFixtureId == $"{buildA}_{level}" &&
                            value.Scenario.EnemyFixtureId == $"{buildB}_{level}");
                        var bAsPlayer = results.Single(value =>
                            value.Scenario.PlayerFixtureId == $"{buildB}_{level}" &&
                            value.Scenario.EnemyFixtureId == $"{buildA}_{level}");
                        summaries.Add(BalanceMirrorSummary.Create(
                            buildA,
                            buildB,
                            level,
                            aAsPlayer.Batch,
                            bAsPlayer.Batch));
                    }
                }
            }
            return summaries.AsReadOnly();
        }

        private static string SerializeScenarioSummaries(
            IEnumerable<BalanceScenarioResult> results)
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                "scenarioId,playerFixtureId,enemyFixtureId,developmentLevel,orientation," +
                "battles,playerWins,enemyWins,draws,playerScoreRate,roundLimitRate," +
                "meanRounds,p90Rounds,meanPlayerRound1RawDamage,meanEnemyRound1RawDamage," +
                "meanPlayerSurvivors,meanEnemySurvivors,meanPlayerSummonFailures," +
                "meanEnemySummonFailures,meanPlayerShieldBlocks,meanEnemyShieldBlocks," +
                "meanPlayerPermanentDelta,meanEnemyPermanentDelta,meanPlayerFlourishGained," +
                "meanEnemyFlourishGained,effectLimitHits,exceptions");
            foreach (var result in results)
            {
                var samples = result.Batch.Samples.Where(value => value.Succeeded).ToList();
                AppendCsvRow(builder, new object[]
                {
                    result.Scenario.ScenarioId,
                    result.Scenario.PlayerFixtureId,
                    result.Scenario.EnemyFixtureId,
                    result.Scenario.DevelopmentLevel,
                    result.Scenario.Orientation,
                    result.Batch.Battles,
                    result.Batch.PlayerWins,
                    result.Batch.EnemyWins,
                    result.Batch.Draws,
                    Format(result.Batch.PlayerScoreRate),
                    Format(Rate(samples.Count(value =>
                        value.OutcomeReason == BattleOutcomeReason.RoundLimit), samples.Count)),
                    Format(Average(samples.Select(value => value.Diagnostics.RoundCount))),
                    Format(Percentile(samples.Select(value => value.Diagnostics.RoundCount), 0.9d)),
                    Format(Average(samples.Select(value => value.Diagnostics.Player.RoundOneRawDamage))),
                    Format(Average(samples.Select(value => value.Diagnostics.Enemy.RoundOneRawDamage))),
                    Format(Average(samples.Select(value => value.PlayerSurvivors))),
                    Format(Average(samples.Select(value => value.EnemySurvivors))),
                    Format(Average(samples.Select(value => value.Diagnostics.Player.SummonFailures))),
                    Format(Average(samples.Select(value => value.Diagnostics.Enemy.SummonFailures))),
                    Format(Average(samples.Select(value => value.Diagnostics.Player.ShieldDamageBlocks))),
                    Format(Average(samples.Select(value => value.Diagnostics.Enemy.ShieldDamageBlocks))),
                    Format(Average(samples.Select(value =>
                        value.Diagnostics.Player.PermanentAttackDelta +
                        value.Diagnostics.Player.PermanentHealthDelta))),
                    Format(Average(samples.Select(value =>
                        value.Diagnostics.Enemy.PermanentAttackDelta +
                        value.Diagnostics.Enemy.PermanentHealthDelta))),
                    Format(Average(samples.Select(value => value.Diagnostics.Player.FlourishGained))),
                    Format(Average(samples.Select(value => value.Diagnostics.Enemy.FlourishGained))),
                    samples.Count(value => value.Diagnostics.HitEffectLimit),
                    result.Batch.Exceptions
                });
            }
            return builder.ToString();
        }

        private static int WriteDeterminismFailures(
            string path,
            IReadOnlyList<BalanceScenarioResult> first,
            IReadOnlyList<BalanceScenarioResult> replay)
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                "scenarioId,seed,expectedHash,actualHash,expectedException,actualException");
            var failures = 0;
            if (replay != null)
            {
                foreach (var expectedScenario in first)
                {
                    var actualScenario = replay.Single(value =>
                        value.Scenario.ScenarioId == expectedScenario.Scenario.ScenarioId);
                    var actualBySeed = actualScenario.Batch.Samples.ToDictionary(value => value.Seed);
                    foreach (var expected in expectedScenario.Batch.Samples)
                    {
                        actualBySeed.TryGetValue(expected.Seed, out var actual);
                        if (actual != null &&
                            expected.DeterminismHash == actual.DeterminismHash &&
                            expected.ExceptionType == actual.ExceptionType)
                        {
                            continue;
                        }

                        failures++;
                        AppendCsvRow(builder, new object[]
                        {
                            expectedScenario.Scenario.ScenarioId,
                            expected.Seed,
                            expected.DeterminismHash,
                            actual?.DeterminismHash,
                            expected.ExceptionType,
                            actual?.ExceptionType
                        });
                    }
                }
            }
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
            return failures;
        }

        private static BalanceSafetyCounters CountSafety(
            IEnumerable<BalanceScenarioResult> results)
        {
            var counters = new BalanceSafetyCounters();
            foreach (var result in results)
            {
                counters.Exceptions += result.Batch.Exceptions;
                foreach (var sample in result.Batch.Samples.Where(value => value.Succeeded))
                {
                    if (sample.Diagnostics.HitEffectLimit) counters.EffectLimitHits++;
                    if (sample.OutcomeReason != BattleOutcomeReason.RoundLimit) continue;
                    if (result.Scenario.CountsForCompetitiveSafety)
                    {
                        counters.CompetitiveRoundLimits++;
                    }
                    else
                    {
                        counters.OutputDummyRoundLimits++;
                    }
                }
            }
            return counters;
        }

        private static string ReadGitState(string repositoryRoot)
        {
            try
            {
                var commit = RunGit(repositoryRoot, "rev-parse --short=12 HEAD").Trim();
                var status = RunGit(repositoryRoot, "status --porcelain --untracked-files=no");
                return string.IsNullOrWhiteSpace(status) ? commit : commit + "+dirty";
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Unable to read Git state: {exception.Message}");
                return "unknown";
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
                if (process == null) throw new InvalidOperationException("Unable to start Git.");
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

        private static string ComputeFileHash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static double Average(IEnumerable<int> values)
        {
            var materialized = values.ToList();
            return materialized.Count == 0 ? 0d : materialized.Average();
        }

        private static double Percentile(IEnumerable<int> values, double percentile)
        {
            var sorted = values.OrderBy(value => value).ToList();
            if (sorted.Count == 0) return 0d;
            var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
            return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))];
        }

        private static double Rate(int numerator, int denominator)
        {
            return denominator <= 0 ? 0d : (double)numerator / denominator;
        }

        private static string Format(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static void AppendCsvRow(StringBuilder builder, IEnumerable<object> values)
        {
            builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        private static string EscapeCsv(object value)
        {
            if (value == null) return string.Empty;
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? text
                : $"\"{text.Replace("\"", "\"\"")}\"";
        }

        private sealed class BalanceSafetyCounters
        {
            public int Exceptions { get; set; }
            public int EffectLimitHits { get; set; }
            public int CompetitiveRoundLimits { get; set; }
            public int OutputDummyRoundLimits { get; set; }
        }

        private sealed class BalanceUnityBatchMetadata
        {
            [JsonProperty("balanceSchemaVersion")]
            public string BalanceSchemaVersion { get; set; } = "0.2.0";

            [JsonProperty("candidateId")]
            public string CandidateId { get; set; }

            [JsonProperty("tuningRound")]
            public string TuningRound { get; set; }

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

            [JsonProperty("fixtureVersion")]
            public string FixtureVersion { get; set; }

            [JsonProperty("coreClassifierVersion")]
            public string CoreClassifierVersion { get; set; }

            [JsonProperty("seedSet")]
            public string SeedSet { get; set; }

            [JsonProperty("seedCount")]
            public int SeedCount { get; set; }

            [JsonProperty("scenarioCount")]
            public int ScenarioCount { get; set; }

            [JsonProperty("battleCount")]
            public int BattleCount { get; set; }

            [JsonProperty("replayBattleCount")]
            public int ReplayBattleCount { get; set; }

            [JsonProperty("totalSimulations")]
            public int TotalSimulations { get; set; }

            [JsonProperty("determinismFailureCount")]
            public int DeterminismFailureCount { get; set; }

            [JsonProperty("totalExceptions")]
            public int TotalExceptions { get; set; }

            [JsonProperty("effectLimitHits")]
            public int EffectLimitHits { get; set; }

            [JsonProperty("competitiveRoundLimits")]
            public int CompetitiveRoundLimits { get; set; }

            [JsonProperty("outputDummyRoundLimits")]
            public int OutputDummyRoundLimits { get; set; }

            [JsonProperty("elapsedSeconds")]
            public double ElapsedSeconds { get; set; }

            [JsonProperty("sampleFile")]
            public string SampleFile { get; set; }

            [JsonProperty("sampleFileSha256")]
            public string SampleFileSha256 { get; set; }
        }

        private sealed class BalanceBatchOptions
        {
            public string CandidateId { get; private set; }
            public string TuningRound { get; private set; }
            public string SeedSet { get; private set; }
            public string OutputDirectory { get; private set; }
            public bool? Replay { get; private set; }

            public static BalanceBatchOptions CreateDefault(string seedSet)
            {
                return new BalanceBatchOptions
                {
                    CandidateId = DefaultCandidate,
                    TuningRound = DefaultTuningRound,
                    SeedSet = seedSet
                };
            }

            public static BalanceBatchOptions FromCommandLine(IReadOnlyList<string> arguments)
            {
                var seedSet = ReadArgument(arguments, "-balanceSeedSet");
                if (string.IsNullOrWhiteSpace(seedSet))
                {
                    throw new ArgumentException("-balanceSeedSet is required.");
                }

                var replayText = ReadArgument(arguments, "-balanceReplay");
                return new BalanceBatchOptions
                {
                    CandidateId = ReadArgument(arguments, "-balanceCandidate") ?? DefaultCandidate,
                    TuningRound = ReadArgument(arguments, "-balanceTuningRound") ?? DefaultTuningRound,
                    SeedSet = seedSet,
                    OutputDirectory = ReadArgument(arguments, "-balanceOutput"),
                    Replay = string.IsNullOrWhiteSpace(replayText)
                        ? (bool?)null
                        : bool.Parse(replayText)
                };
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
                    "phase-6-v0.2",
                    CandidateId,
                    CanonicalSeedSet(SeedSet));
            }

            private static string ReadArgument(IReadOnlyList<string> arguments, string name)
            {
                for (var index = 0; index < arguments.Count - 1; index++)
                {
                    if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                    {
                        return arguments[index + 1];
                    }
                }
                return null;
            }
        }
    }
}
